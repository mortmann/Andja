using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum path_mode {world,island,route};

public class Pathfinding  {
	private Path_AStar pathAStar;
	private float movementPercentage; // Goes from 0 to 1 as we move from currTile to destTile
	public float dest_X;
	public float dest_Y;
	public bool IsAtDest;
	// If we aren't moving, then destTile = currTile
	protected Tile _destTile;
	//for building 
	protected List<Tile> roadTilesAroundStartStructure;
	protected List<Tile> roadTilesAroundEndStructure;
	private Vector3 pos;
	public float rotation;
	private Transform transform;
	public Queue<Tile> backPath;

	private float _speed;
	private float speed {
		get{ 
			return _speed;
		}
		set {
			if(value == 0){
				_speed = 1;
			} else {
				_speed = value;
			}
		}
	}
	private path_mode pathmode;

	public Pathfinding( Transform transform, float speed, Tile startTile , path_mode pm = path_mode.world){
		currTile = startTile;
		destTile = startTile;
		X = currTile.X;
		Y = currTile.Y;
		dest_X = currTile.X;
		dest_Y = currTile.Y;
		this.speed = speed;
		this.transform = transform;
		pathmode = pm;
	}

	public Pathfinding( List<Tile> startTiles,List<Tile> endTiles , float speed , path_mode pm = path_mode.route){
		roadTilesAroundStartStructure = startTiles;
		roadTilesAroundEndStructure = endTiles;
		this.speed = speed;
		pathmode = pm;
		pathAStar = GetPathStar ();
		if(pathAStar.Length () == 0){
			Debug.LogError ("pathfinding path has 0 tiles");
		} else {
			backPath = new Queue<Tile> (pathAStar.path.Reverse ());
		}
		currTile = pathAStar.Dequeue ();
		X = currTile.X;
		Y = currTile.Y;
	}
	public Tile startTile;
	public Tile destTile {
		get { return _destTile; }
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
			X = value.X;
			Y = value.Y;
			_currTile = value; }
	}

	private Vector3 rotationDirection;
	public Vector3 Update_DoMovement(float deltaTime) {
		if (currTile == null || destTile == null) {
			return Vector3.zero;
		}
		if (transform != null) {
			X = transform.position.x;
			Y = transform.position.y;
		} 
		if (nextTile == destTile && pathmode != path_mode.route ) {
			
			pathAStar = null;
			// we are one tile from destination tile away...
			// now we have to go to the correct x/y coordinations
			if(X>=dest_X-0.1f && X <= dest_X + 0.1f && Y >= dest_Y - 0.1f && Y <= dest_Y + 0.1f) {
				//we are near enough
				currTile = WorldController.Instance.world.GetTileAt(X, Y);
				return Vector3.zero;
			}
			Vector3 dir = new Vector3(dest_X - X, dest_Y - Y);
			rotationDirection = new Vector3 (dest_X, dest_Y);
			dir = dir.normalized;
			Vector3 temp = dir * speed;
			// DUNNO why this works and not vector * deltaTime
			temp.x *= deltaTime;
			temp.y *= deltaTime;
			if (transform == null) {
				X += temp.x;
				Y += temp.y;
			}
			return temp;

		} else {
			//no move command so return!
			if (destTile == currTile) {
//				IsAtDest = true;
				return Vector3.zero;
			}
			if (nextTile == null || nextTile == currTile) {
				// Get the next tile from the pathfinder.
				if (pathAStar == null || pathAStar.Length() == 0) {
					pathAStar = GetPathStar ();
					if (pathAStar == null) {
						return Vector3.zero;
					}
				}
				nextTile = pathAStar.Dequeue();
			}

			rotationDirection = new Vector3 (nextTile.X, nextTile.Y);
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
			if (movementPercentage > 1) {
				// We have reached our destination
				// TODO: Get the next tile from the pathfinding system.
				//       If there are no more tiles, then we have TRULY
				//       reached our destination.
				currTile = nextTile;
				movementPercentage = 0;
				// FIXME?  Do we actually want to retain any overshot movement?
			}
			Vector3 temp = deltaTime * dir * speed;
			if (transform == null) {
				X += temp.x;
				Y += temp.y;
			}
			return temp;
		}


	}

	public Path_AStar GetPathStar(){
		// Generate a path to our destination
		if (pathmode == path_mode.world) {
			return new Path_AStar (World.current, currTile, destTile); // This will calculate a path from curr to dest.
		} else if (pathmode == path_mode.island) {
			Path_AStar p = null;
			foreach (Tile start in roadTilesAroundStartStructure) {
				if(start.Structure != null && start.Structure.myBuildingTyp != BuildingTyp.Pathfinding){
					continue;
				}
				foreach (Tile end in roadTilesAroundEndStructure) {
					if(end.Structure != null && end.Structure.myBuildingTyp != BuildingTyp.Pathfinding){
						continue;
					}
					Path_AStar temp = new Path_AStar (start.myIsland, start, end);
					if (p == null || temp.path.Count < p.path.Count ) {
						p = temp;
						destTile = end;
						startTile = start;
					} 
				}
			}
			return p;
		} else if (pathmode == path_mode.route) {
				Path_AStar p = null;
				foreach (Tile start in roadTilesAroundStartStructure) {
					foreach (Tile end in roadTilesAroundEndStructure) {
						if (((Road)end.Structure).Route == ((Road)start.Structure).Route) {
							Path_AStar temp = new Path_AStar (((Road)start.Structure).Route, start, end);
							if (p == null || temp.path.Count < p.path.Count ) {
								p = temp;
								destTile = end;
								startTile = start;
							} 
						}
					}
				return p;
			} 
		}
		Debug.LogError ("pathmode not valid");
		return null;
	}
	public void Reverse (){
		IsAtDest = false;
		destTile = startTile;
		pathAStar=new Path_AStar(backPath);
	}

	public float UpdateRotation() {
		Vector2 PointA = new Vector2(rotationDirection.x , rotationDirection.y);    
		Vector2 PointB = new Vector2(X, Y);    
		Vector2 moveDirection = PointA-PointB;
		float angle = Mathf.Atan2(moveDirection.y, moveDirection.x);
		if (transform != null) {
			rotation = transform.rotation.z;
			if ((rotation > angle + 0.1f && rotation > angle - 0.1f) == false) {
				return angle * Mathf.Rad2Deg;
			} 
		} else {
			if ((rotation > angle + 0.1f && rotation > angle - 0.1f) == false) {
				rotation += angle * Mathf.Rad2Deg;
			} 
		}
		//no need to rotate
		return 0;
	}

	public void AddMovementCommand(float x, float y) {
		IsAtDest = false;
		destTile = World.current.GetTileAt (x, y);
		dest_X = x;
		dest_Y = y;
	}
}
