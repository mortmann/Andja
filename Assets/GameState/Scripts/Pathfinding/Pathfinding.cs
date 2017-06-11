using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using Newtonsoft.Json;

public enum path_mode { world, islandMultipleStartpoints, islandSingleStartpoint, route };
public enum path_dest { tile, exact };
public enum turn_type { OnPoint, TurnRadius };

[JsonObject(MemberSerialization.OptIn)]
public class Pathfinding {
	#region Serialize
	[JsonPropertyAttribute] private float movementPercentage; // Goes from 0 to 1 as we move from currTile to destTile
	[JsonPropertyAttribute] public float dest_X;
	[JsonPropertyAttribute] public float dest_Y;
	[JsonPropertyAttribute] public bool IsAtDest=true;
	// If we aren't moving, then destTile = currTile
	[JsonPropertyAttribute] protected Tile _destTile;
	[JsonPropertyAttribute] private Vector3 pos;
	[JsonPropertyAttribute] public float rotation;
	[JsonPropertyAttribute] public Queue<Tile> worldPath;
	[JsonPropertyAttribute] public Queue<Tile> backPath;


	#endregion
	#region RuntimeOrPrototyp
	private Path_AStar pathAStar;
	//for building 
	protected List<Tile> roadTilesAroundStartStructure;
	protected List<Tile> roadTilesAroundEndStructure;
	private path_dest pathDest;
	public Vector3 LastMove { get; protected set;}
	private float rotationSpeed=90;
	private float _speed;
	private float speed {
		get {
			return _speed;
		}
		set {
			if (value == 0) {
				_speed = 1;
			}
			else {
				_speed = value;
			}
		}
	}
	private path_mode pathmode;
	private turn_type myTurnType=turn_type.OnPoint;
	public float turnSpeed;
	#endregion


    public Pathfinding(float speed, Tile startTile, path_mode pm = path_mode.world) {
        currTile = startTile;
        X = currTile.X;
        Y = currTile.Y;
        dest_X = currTile.X;
        dest_Y = currTile.Y;
        this.speed = speed;
        pathmode = pm;
        pathDest = path_dest.exact;
        if (pm == path_mode.world)
            World.current.RegisterTileGraphChanged(WorldTGraphUpdate);
    }

    public Pathfinding(List<Tile> startTiles, List<Tile> endTiles, float speed, path_mode pm = path_mode.route) {
        roadTilesAroundStartStructure = startTiles;
        roadTilesAroundEndStructure = endTiles;
        this.speed = speed;
        pathmode = pm;
        pathAStar = GetPathStar();
        if (pathAStar.Length() == 0) {
            Debug.LogError("pathfinding path has 0 tiles");
        }
        else {
            backPath = new Queue<Tile>(pathAStar.path.Reverse());
        }
        currTile = pathAStar.Dequeue();
        X = currTile.X;
        Y = currTile.Y;
        pathDest = path_dest.tile;
    }
    public Tile startTile;
    public Tile destTile {
        get { 
			if (_destTile == null)
				return currTile;
			return _destTile; }
        set {
            if (_destTile != value) {
                _destTile = value;
                IsAtDest = false;
                pathAStar = null;   // If this is a new destination, then we need to invalidate pathfinding.
            }
        }
    }
    private float _x;
    public float X {
        get {
            return _x;
        }
        protected set {
            _x = value;
        }
    }
    private float _y;
    public float Y {
        get {
            return _y;
        }
        protected set {
            _y = value;
        }
    }
    Tile nextTile;  // The next tile in the pathfinding sequence
    Tile _currTile;
    public Tile currTile {
        get { return _currTile; }
        set {
            if (_currTile == null) {
                X = value.X;
                Y = value.Y;
            }
            _currTile = value;
        }
    }

    public void WorldTGraphUpdate(World world) {
        if (pathmode == path_mode.route || pathmode == path_mode.islandMultipleStartpoints) {
            roadTilesAroundStartStructure = new List<Tile>();
            roadTilesAroundStartStructure.Add(currTile);
        }
        GetPathStar();
    }

    private Vector3 rotationDirection;
    public void Update_DoMovement(float deltaTime) {
        if (currTile == null || destTile == null) {
            Debug.LogError("currtile/desttile");
            LastMove = Vector3.zero;
			return;
        }
		//if were standing or if we can turn OnPoint(OnSpot) turn to face the rightway
		if(myTurnType==turn_type.OnPoint||LastMove.sqrMagnitude<=0.1){
			//so we can turn on point but not move
			//so rotate around with the turnspeed
			//we can only rotate if we know the next tile we are going to visit
			if(IsAtDest==false&&currTile!=destTile && UpdateRotationOnPoint(deltaTime)==false){
				return;
			}
		}

        //so for everything we can use astar
        //but the world(ships in water)
        //the tilegraph gets too big -> going to use bool-array instead
        if (pathmode != path_mode.world) {
            //if it has to be exact and if the goal is the nexttile
            if (pathDest == path_dest.exact && nextTile == destTile) {
				LastMove = accurateMove(deltaTime);
            }
			LastMove = DoAStar(deltaTime);
			return;
        }
		LastMove = DoWorldMove(deltaTime);
    }
    private Vector3 DoWorldMove(float deltaTime) {
		if(IsAtDest==false){
			if(destTile==currTile){
				return Vector3.zero;
			}
			if(nextTile==destTile){
				return accurateMove (deltaTime);
			}
			if (nextTile == null || nextTile == currTile) {
				nextTile = worldPath.Dequeue ();
			}
			rotationDirection = new Vector3(nextTile.X, nextTile.Y);
			Vector3 dir = new Vector3(nextTile.X - X, nextTile.Y - Y);
			Vector3 temp = deltaTime * dir.normalized * speed;
			X += temp.x;
			Y += temp.y;
			if(nextTile == World.current.GetTileAt(X+0.5f,Y+0.5f)){
				currTile = nextTile;
			}
			return temp;
		}
		return Vector3.zero;
    }
	private void CalculateWorldPath(){
		// create a grid
		PathFind.Grid grid = new PathFind.Grid(World.current.Width-1,World.current.Height-1, World.current.Tilesmap);
		// create source and target points
		PathFind.Point _from = new PathFind.Point(currTile.X, currTile.Y);
		PathFind.Point _to = new PathFind.Point(destTile.X, destTile.Y);
		// get path
		// path will either be a list of Points (x, y), or an empty list if no path is found.
		List<PathFind.Point> points = PathFind.Pathfinding.FindPath (grid, _from, _to);
		worldPath = new Queue<Tile> ();
		for (int i = 0; i < points.Count; i++) {
			worldPath.Enqueue (World.current.GetTileAt (points [i].x, points [i].y));
		}
	}
    private Vector3 DoAStar(float deltaTime) {
        //no move command so return!
        if (destTile == currTile) {
            IsAtDest = true;
            return Vector3.zero;
        }
        if (nextTile == null || nextTile == currTile) {
            // Get the next tile from the pathfinder.
            if (pathAStar == null || pathAStar.Length() == 0) {
                pathAStar = GetPathStar();
                if (pathAStar == null) {
                    return Vector3.zero;
                }
            }
            nextTile = pathAStar.Dequeue();
        }
//		Debug.Log ("ASTAR"); 
        rotationDirection = new Vector3(nextTile.X, nextTile.Y);
        Vector3 dir = new Vector3(nextTile.X - X, nextTile.Y - Y);
        dir = dir.normalized;

        float distThisFrame = speed * deltaTime;
        float distToTravel = Mathf.Sqrt(
            Mathf.Pow(currTile.X - nextTile.X, 2) +
            Mathf.Pow(currTile.Y - nextTile.Y, 2)
        );

        float percThisFrame = distThisFrame / distToTravel;
        // Add that to overall percentage travelled.
        movementPercentage += percThisFrame;
//		if(World.current.IsInTileAt (nextTile,X,Y)){
//			currTile = nextTile;
//		}

        if (movementPercentage > 1) {
            //			Debug.Log (movementPercentage+" "+currTile.toString() + " nexttile "+nextTile.toString ()); 
            // We have reached our destination
            // TODO: Get the next tile from the pathfinding system.
            //       If there are no more tiles, then we have TRULY
            //       reached our destination.
            currTile = nextTile;
            movementPercentage = 0;
            // FIXME?  Do we actually want to retain any overshot movement?
        }
        Vector3 temp = deltaTime * dir * speed;
        X += temp.x;
        Y += temp.y;
        return temp;

    }
    private Vector3 accurateMove(float deltaTime) {
        // we are one tile from destination tile away...
        // now we have to go to the correct x/y coordinations
        if (X >= dest_X - 0.1f && X <= dest_X + 0.1f && Y >= dest_Y - 0.1f && Y <= dest_Y + 0.1f) {
            //we are near enough
            currTile = WorldController.Instance.world.GetTileAt(X, Y);
            nextTile = null;
            destTile = currTile;
            IsAtDest = true;
            return Vector3.zero;
        }
        Vector3 dir = new Vector3(dest_X - X, dest_Y - Y);
        rotationDirection = new Vector3(dest_X, dest_Y);
        dir = dir.normalized;
        Vector3 temp = dir * speed;
        // DUNNO why this works and not vector * deltaTime
        temp.x *= deltaTime;
        temp.y *= deltaTime;
        X += temp.x;
        Y += temp.y;
        return temp;
    }


    public Path_AStar GetPathStar() {
        // Generate a path to our destination
        switch (pathmode) {
            case path_mode.world:
                Debug.Log("dis is not possible for WORLD");
                return null;

            case path_mode.route:
                
				Path_AStar p = null;
                foreach (Tile start in roadTilesAroundStartStructure) {
                    foreach (Tile end in roadTilesAroundEndStructure) {
                        if (((Road)end.Structure).Route == ((Road)start.Structure).Route) {
                            Path_AStar temp = new Path_AStar(((Road)start.Structure).Route, start, end);
                            if (p == null || temp.path.Count < p.path.Count) {
                                p = temp;
                                destTile = end;
                                startTile = start;
                            }
                        }
                    }
                    return p;
                }
                break;

            case path_mode.islandMultipleStartpoints:
			
                Path_AStar pa = null;
                foreach (Tile start in roadTilesAroundStartStructure) {
                    if (start.Structure != null && start.Structure.myBuildingTyp != BuildingTyp.Pathfinding) {
                        continue;
                    }
                    foreach (Tile end in roadTilesAroundEndStructure) {
                        if (end.Structure != null && end.Structure.myBuildingTyp != BuildingTyp.Pathfinding) {
                            continue;
                        }
                        Path_AStar temp = new Path_AStar(start.myIsland, start, end);
                        if (temp == null) {
                            continue;
                        }
                        if (pa == null || temp.path.Count < pa.path.Count) {
                            pa = temp;
                            destTile = end;
                            startTile = start;
                        }
                    }

                    return pa;
                }
                break;

            case path_mode.islandSingleStartpoint:
                return new Path_AStar(currTile.myIsland, currTile, destTile); // This will calculate a path from curr to dest.
            default:
                break;
        }
        Debug.LogError("pathmode not valid");
        return null;
    }
    public void Reverse() {
        IsAtDest = false;
        destTile = startTile;
        pathAStar = new Path_AStar(backPath);
    }
	float t = 0;
	public bool UpdateRotationOnPoint(float delta) {
		if(rotationDirection.magnitude==0||currTile==nextTile){
			return true;
		}
        Vector2 PointA = new Vector2(rotationDirection.x, rotationDirection.y);
        Vector2 PointB = new Vector2(X, Y);
        Vector2 moveDirection = PointA - PointB;
		float angle =Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
//		if(angle<0){
//			angle =360+ angle;
//		}
		rotation = angle;

		if (rotation < angle + 0.1f 
			&& rotation > angle - 0.1f) {
			//no need to rotate so set the rotation to the correct one
			rotation = angle;
			t = 0;
			return true;
		}
		t += delta;

		rotation = Mathf.LerpAngle (rotation , angle,t*rotationSpeed);//Mathf.LerpAngle ( rotation , angle , t);
		return false;
    }

    public void AddMovementCommand(float x, float y) {
        IsAtDest = false;
        destTile = World.current.GetTileAt(x, y);
        dest_X = x;
        dest_Y = y;
        pathDest = path_dest.exact;
		if(pathmode == path_mode.world){
			CalculateWorldPath ();
		}
        //		Debug.Log ("curr: " + currTile.toString ());
        //		Debug.Log ("dest: " + destTile.toString ()); 
        //		foreach (Tile item in GetPathStar ().path) {
        //			Debug.Log (item.Type); 
        //		}
    }

	//------------------------------
	//TEST
//	private void GetPathRoute(Tile goal){
//		foreach (Tile start in roadTilesAroundStartStructure) {
//			foreach (Tile end in roadTilesAroundEndStructure) {
//				if (((Road)end.Structure).Route == ((Road)start.Structure).Route) {
//					CalculatePathInRoute (start,end);
//				}
//			}
//		}
//	}
//
//	private void CalculatePathInRoute(Tile start, Tile goal){
//		SimplePriorityQueue<Tile> pq = new SimplePriorityQueue<Tile>();
//		List<Tile> currentWay = new List<Tile>();
//		HashSet<Tile> closed = new HashSet<Tile> ();
//		Tile curr = start;
//		while(curr!=goal){
//			foreach (Tile t in curr.GetNeighbours ()) {
//				bool next = false;
//				if(t.Structure is Road){
//					if(currentWay.Contains (t)||pq.Contains (t)){
//						continue;
//					}
//					float dist = (goal.vector - t.vector).sqrMagnitude;
//					pq.Enqueue (t,dist);
//					next = true;
//				}
//
//				if (next) {
//					currentWay.Add (curr);
//				} else {
//					Tile nearest = pq.Dequeue ();
//					HashSet<Tile> hs = new HashSet<Tile> (nearest.GetNeighbours ());
//					for (int i = currentWay.Count-1; i <=0; i--) {
//						if(hs.Contains (currentWay[i])){
//							currentWay.Add (nearest);
//							break;
//						}
//						currentWay.RemoveAt (i);
//					}
//				}
//				curr = pq.Dequeue ();
//			}
//		}
//	}
}
