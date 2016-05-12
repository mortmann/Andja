using UnityEngine;
using System.Collections;
using System;

public class Unit : MonoBehaviour {
   

    void Start() {
		speed = 2f;
		pathfinding = new Pathfinding (transform, speed, WorldController.Instance.world.GetTileAt(35, 35));


        transform.Translate(new Vector3(35,35, 0));
		inventory = new Inventory (6);
        isShip = true;
		r2d = GetComponent<Rigidbody2D>();

    }

	private Pathfinding pathfinding;

	public Rigidbody2D r2d;

    public float X {
        get {
			return pathfinding.X;
        }
    }
    public float Y {
        get {
			return pathfinding.Y;
        }
    }
//    Tile _currTile;
//    public Tile currTile {
//        get { return _currTile; }
//        protected set { _currTile = value; }
//    }
//	Tile _destTile;
//	public Tile destTile {
//		get { return _destTile; }
//		set {
//			if (_destTile != value) {
//				_destTile = value;
//				pathAStar = null;   // If this is a new destination, then we need to invalidate pathfinding.
//			}
//		}
//	}
//	Tile nextTile;  // The next tile in the pathfinding sequence
//
//	Path_AStar pathAStar;
//	private float dest_X;
//	private float dest_Y;
//	float movementPercentage; // Goes from 0 to 1 as we move from currTile to destTile

    public bool isShip;
	public bool hasChanged = false;
    // If we aren't moving, then destTile = currTile


    public GameObject GetGameObject() {
        return gameObject;
    }


    float speed;   // Tiles per second



    Action<Unit> cbUnitChanged;

    public Inventory inventory;

    internal float width;
    internal float height;
    

    public void FixedUpdate() {
		r2d.MovePosition (transform.position + pathfinding.Update_DoMovement(Time.deltaTime));
		r2d.MoveRotation (transform.rotation.z + pathfinding.UpdateRotation ());
		if(hasChanged){
	        if (cbUnitChanged != null)
	            cbUnitChanged(this);
		}
    }

//    internal void setStartTile(Tile tile) {
//        currTile = tile;
//        if (gameObject.name.Contains("ship")) {
//            this.isShip = true;
//        }
//    }

//    private void Update_DoMovement(float deltaTime) {
//        if (currTile == null || destTile == null) {
//            return;
//        }
//        if (nextTile == destTile) {
//            pathAStar = null;
//            // we are one tile from destination tile away...
//            // now we have to go to the correct x/y coordinations
//            if(X>=dest_X-0.1f && X <= dest_X + 0.1f && Y >= dest_Y - 0.1f && Y <= dest_Y + 0.1f) {
//                //we are near enough
//                currTile = WorldController.Instance.world.GetTileAt(X, Y);
//                return;
//            }
//            Vector3 dir = new Vector3(dest_X - X, dest_Y - Y);
//			if (UpdateRotation (new Vector3(dest_X, dest_Y)) == false) {
//				return;
//			}
//            dir = dir.normalized;
////			transform.Translate(Time.deltaTime * dir * (speed), Space.World);
//			r2d.MovePosition (transform.position + Time.deltaTime * dir * (speed));
//            X = transform.position.x;
//            Y = transform.position.y;
//            return; 
//        } else {
//			//no move command so return!
//			if (destTile == currTile) {
//				return;
//			}
//            if (nextTile == null || nextTile == currTile) {
//                // Get the next tile from the pathfinder.
//                if (pathAStar == null || pathAStar.Length() == 0) {
//                    // Generate a path to our destination
//                    pathAStar = new Path_AStar(currTile.world, currTile, destTile); // This will calculate a path from curr to dest.
//                    // Let's ignore the first tile, because that's the tile we're currently in.
//					nextTile = pathAStar.Dequeue();
//                }
//                // Grab the next waypoint from the pathing system!
//                nextTile = pathAStar.Dequeue();
//                if (nextTile == currTile) {
//                    Debug.LogError("Update_DoMovement - nextTile is currTile?");
//                }
//                Debug.Log("Tile pathfinding to " + nextTile.toString());
//            }
//
//			Vector3 dir = new Vector3(nextTile.X - X, nextTile.Y - Y);
//            dir = dir.normalized;
//			if (UpdateRotation (new Vector3(nextTile.X, nextTile.Y)) == false) {
//				return;
//			}
////			transform.Translate(Time.deltaTime * dir * (speed), Space.World);
//			r2d.MovePosition (transform.position + Time.deltaTime * dir * (speed));
//			X = transform.position.x;
//            Y = transform.position.y;
//
//            float distThisFrame = speed * deltaTime;
//            float distToTravel = Mathf.Sqrt(
//            Mathf.Pow(currTile.X - nextTile.X, 2) +
//            Mathf.Pow(currTile.Y - nextTile.Y, 2)
//             );
//
//            float percThisFrame = distThisFrame / distToTravel;
//            // Add that to overall percentage travelled.
//            movementPercentage += percThisFrame;
//            if (movementPercentage > 1) {
//                // We have reached our destination
//
//                // TODO: Get the next tile from the pathfinding system.
//                //       If there are no more tiles, then we have TRULY
//                //       reached our destination.
//                Debug.Log(nextTile.toString() + " " + WorldController.Instance.world.GetTileAt(X+0.5f,Y + 0.5f).toString());
//                currTile = nextTile;
//                movementPercentage = 0;
//                // FIXME?  Do we actually want to retain any overshot movement?
//            }
//        }
//    }
//
//	private bool UpdateRotation(Vector3 dir) {
//		Vector2 PointA = new Vector2(dir.x , dir.y);    
//		Vector2 PointB = new Vector2(X, Y);    
//		Vector2 moveDirection = PointA-PointB;
//		float angle = Mathf.Atan2(moveDirection.y, moveDirection.x);
////		if (moveDirection != Vector2.zero) {
////			transform.rotation = Quaternion.RotateTowards (transform.rotation,new Quaternion(0,0,angle,transform.rotation.w),Time.deltaTime);
//		Debug.Log (angle*Mathf.Rad2Deg + " rotation " + transform.rotation.z + " _ " + Mathf.LerpAngle (transform.rotation.z,angle*Mathf.Rad2Deg, Time.deltaTime*100));
//		r2d.MoveRotation (Mathf.LerpAngle (transform.rotation.z,angle*Mathf.Rad2Deg, Time.deltaTime*100));
////		}
////		not rotated enough just return
////		if((transform.rotation.z>angle+0.1f &&transform.rotation.z>angle-0.1f ) == false){
////			return false;
////		} 
//		return true;
//    }

    public void RegisterOnChangedCallback(Action<Unit> cb) {
        cbUnitChanged += cb;
    }

    public void UnregisterOnChangedCallback(Action<Unit> cb) {
        cbUnitChanged -= cb;
    }

    public void AddMovementCommand(float x, float y) {
        Tile tile = WorldController.Instance.world.GetTileAt(x, y);
        if(tile == null){
            return;
        }
        if (isShip) {
            if (tile.Type != TileType.Water) {
                return;
            }
        } else {
            if (tile.Type == TileType.Water) {
                return;
            }
            if (tile.Type == TileType.Mountain) {
                return;
            }
        }
        Debug.Log("AddMovementCommand " + tile.toString());
		pathfinding.AddMovementCommand( x, y);
    }

}
