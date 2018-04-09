using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using Newtonsoft.Json;

public enum path_mode { world, islandMultipleStartpoints, islandSingleStartpoint, route };
public enum path_dest { tile, exact };
public enum turn_type { OnPoint, TurnRadius };

[JsonObject(MemberSerialization.OptIn)]
public abstract class Pathfinding {
	#region Serialize

	[JsonPropertyAttribute] public float dest_X;
	[JsonPropertyAttribute] public float dest_Y;
	[JsonPropertyAttribute] public bool IsAtDest=true;
	// If we aren't moving, then destTile = currTile
	[JsonPropertyAttribute] protected Tile _destTile;
	[JsonPropertyAttribute] public float rotation;

	[JsonPropertyAttribute] protected float _y;
	[JsonPropertyAttribute] protected float _x;
	[JsonPropertyAttribute] public Tile startTile;


	#endregion
	#region RuntimeOrPrototyp
	public Vector3 Position {
		get { return new Vector3 (X,Y);}
	}
	public Queue<Tile> worldPath;
	public Queue<Tile> backPath;

	protected path_dest pathDest;

	public Vector3 LastMove { get; protected set;}

	protected float rotationSpeed=90;


	protected float _speed = 1;
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

	protected path_mode pathmode;
	protected turn_type myTurnType=turn_type.OnPoint;
	public float turnSpeed;

	#endregion

    public Tile destTile {
        get { 
			if (_destTile == null){
				return currTile;
			}
			return _destTile; }
        set {
            if (_destTile != value) {
                _destTile = value;
                IsAtDest = false;
            }
        }
    }
    public float X {
        get {
            return _x;
        }
        protected set {
            _x = value;
        }
    }
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
        get { 
			if(_currTile == null){
				return World.Current.GetTileAt (X, Y);
			}
			return _currTile; }
        set {
			if(value==null){
				return;
			}
            if (_currTile == null) {
                X = value.X;
                Y = value.Y;
            }
            _currTile = value;
        }
    }

    public void WorldTGraphUpdate(World world) {
//        GetPathStar();
    }

    private Vector3 rotationDirection;

    public virtual void Update_DoMovement(float deltaTime) {
        if (currTile == destTile) {
			IsAtDest = true;				
			LastMove = Vector3.zero;
			return;
        }

		//if were standing or if we can turn OnPoint(OnSpot) turn to face the rightway
		if(myTurnType==turn_type.OnPoint||LastMove.sqrMagnitude<=0.1){
			//so we can turn on point but not move
			//so rotate around with the turnspeed
			//we can only rotate if we know the next tile we are going to visit
			if(IsAtDest==false&&currTile!=destTile && UpdateRotationOnPoint(deltaTime)==false){		
				Debug.Log ("UpdateRotationOnPoint(deltaTime)");
				return;
			}
		}
        if (pathDest == path_dest.exact && nextTile == destTile) {
			LastMove = accurateMove(deltaTime);
			return;
        }
		LastMove = DoWorldPath(deltaTime);

    }



    private Vector3 DoWorldPath(float deltaTime) {

        //no move command so return!
        if (destTile == currTile) {
			IsAtDest = true;				
            return Vector3.zero;
        }
        if (nextTile == null || nextTile == currTile) {
            // Get the next tile from the pathfinder.
			if (worldPath == null || worldPath.Count == 0) {
				if(destTile!=null){
					CalculatePath ();
				}
                return Vector3.zero;
            }
            nextTile = worldPath.Dequeue();
        }

        rotationDirection = new Vector3(nextTile.X, nextTile.Y);
        Vector3 dir = new Vector3(nextTile.X - X, nextTile.Y - Y);
        dir = dir.normalized;

        Vector3 temp = deltaTime * dir * speed;

        X += temp.x;
        Y += temp.y;
		if(World.Current.IsInTileAt(nextTile,X,Y)){
			currTile = nextTile;
		}

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

    public void Reverse() {
        IsAtDest = false;
        destTile = startTile;
		worldPath = backPath;
	}

	public void CreateReversePath(){
		backPath =  new Queue<Tile>(worldPath.Reverse ());
	}

	float t = 0;
	public bool UpdateRotationOnPoint(float delta) {
		if(rotationDirection.magnitude==0||currTile==nextTile){
			return true;
		}
        Vector2 PointA = new Vector2(rotationDirection.x, rotationDirection.y);
        Vector2 PointB = new Vector2(X, Y);
        Vector2 moveDirection = PointA - PointB;
		float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
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
		
	protected virtual void CalculatePath (){
		
	}

}
