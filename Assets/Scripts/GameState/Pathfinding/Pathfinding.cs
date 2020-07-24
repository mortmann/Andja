using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System;

public enum Pathing_Mode { World, IslandMultipleStartpoints, IslandSingleStartpoint, Route };
public enum Path_Destination { Tile, Exact };
public enum Turning_Type { OnPoint, TurnRadius };

[JsonObject(MemberSerialization.OptIn)]
public abstract class Pathfinding {
    #region Serialize

    [JsonPropertyAttribute] public float dest_X;
    [JsonPropertyAttribute] public float dest_Y;
    [JsonPropertyAttribute] public float start_X; //not that important -- only for when we need to go back
    [JsonPropertyAttribute] public float start_Y; //not that important -- only for when we need to go back
    [JsonPropertyAttribute] protected bool _IsAtDest;
    // If we aren't moving, then destTile = currTile
    [JsonPropertyAttribute] protected Tile _destTile;
    [JsonPropertyAttribute] public float rotation;
    [JsonPropertyAttribute] public float rotateTo;

    [JsonPropertyAttribute] protected float _y = -1;
    [JsonPropertyAttribute] protected float _x = -1;
    [JsonPropertyAttribute] public Tile startTile;

    #endregion
    #region RuntimeOrPrototyp
    public bool IsAtDestination {
        get { return _IsAtDest; }
        set {
            //TODO: remove these cases were it sets it multiple times to the same value
            bool old = _IsAtDest;
            _IsAtDest = value;
            if (old != _IsAtDest)
                cbIsAtDestination?.Invoke(value);
        }
    }
    public Action<bool> cbIsAtDestination;
    public Vector2 Destination => new Vector2(dest_X, dest_Y);
    public Vector2 Start => new Vector2(start_X, start_Y);

    public Vector3 Position {
        get { return new Vector3(X, Y); }
    }
    public Vector2 Position2 {
        get { return new Vector2(X, Y); }
    }
    public Queue<Vector2> worldPath;
    public Queue<Vector2> backPath;

    protected Path_Destination pathDestination;

    public Vector3 LastMove { get; protected set; }

    protected float rotationSpeed = 90;
    protected Thread calculatingPathThread;
    public bool IsDoneCalculating = true;

    protected float _speed = 1;
    protected virtual float Speed {
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

    protected Pathing_Mode pathmode;
    public Turning_Type TurnType = Turning_Type.OnPoint;
    public float turnSpeed = 10;

    public Tile DestTile {
        get {
            return _destTile;
        }
        set {
            if (_destTile != value) {
                IsAtDestination = false;
            }
            _destTile = value;
        }
    }
    public float X {
        get {
            return _x;
        }
        protected set {
            //TODO: REMOVE FOR RELEASE
            if (_x>0&&Mathf.Abs(value - _x) > 0.2f * Speed * WorldController.Instance.timeMultiplier)
                Debug.LogWarning("UNIT JUMPED -- FIX ME -- " + Mathf.Abs(value - _x));
            _x = value;
        }
    }
    public float Y {
        get {
            return _y;
        }
        protected set {
            //TODO: REMOVE FOR RELEASE
            if (_y > 0 &&Mathf.Abs( value - _y) > 0.2f * Speed * WorldController.Instance.timeMultiplier)
                Debug.LogWarning("UNIT JUMPED -- FIX ME -- " + Mathf.Abs(value - _y));
            _y = value;
        }
    }
    private Vector3 rotationDirection;
    public Vector2? NextDestination { get; protected set; }  // The next tile in the pathfinding sequence
    Tile _currTile;
    public Tile CurrTile {
        get {
            if (_currTile == null) {
                _currTile = World.Current.GetTileAt(X, Y);
            }
            return _currTile;
        }
        set {
            if (value == null) {
                return;
            }
            _currTile = value;
        }
    }

    //public Vector2 NextDestination {
    //    get {
    //        if (NextDestination == null)
    //            return new Vector2(dest_X, dest_Y);
    //        return NextDestination.Vector2;
    //    }
    //}
    public Vector2 PeekNextDestination {
        get {
            if(worldPath!=null) {
                return worldPath.Peek();
            }
            return new Vector2(dest_X, dest_Y);
        }
    }
    #endregion

    public Pathfinding() { }


    public virtual void Update_DoMovement(float deltaTime) {
        //for loading purpose or any other strange reason
        //we have a destination & are not there atm && we have no path then calculate it!
        if (DestTile != null && DestTile != CurrTile && IsAtDestination == false 
            && NextDestination != Destination && worldPath == null && IsDoneCalculating != false)
            SetDestination(dest_X, dest_Y);
        if (IsDoneCalculating == false)
            return;
        
        if (IsAtDestination) {
            LastMove = Vector3.zero;
            return;
        }

        //if were standing or if we can turn OnPoint(OnSpot) turn to face the rightway
        //so we can turn on point but not move
        //so rotate around with the turnspeed
        //we can only rotate if we know the next tile we are going to visit
        if (TurnType == Turning_Type.OnPoint && LastMove.sqrMagnitude <= 0.1 &&
            IsAtDestination == false && CurrTile != DestTile && UpdateRotationOnPoint(deltaTime) == false) {
            return;
        }
        else {
            UpdateRotationOnMove(deltaTime);
        }
        LastMove = Do_Movement(deltaTime);
    }
    float rotateTime;
    float rotateAngle;
    public void Update_DoRotate(float deltaTime) {
        //if ((rotateTo + 0.1f <= rotation && rotateTo + 0.1f >= rotation)==false) {
        //    rotateTimer += deltaTime * rotationSpeed;
        //    rotation = Mathf.LerpAngle(rotation, rotateTo, rotateTimer/rotateTime);
        //    Debug.Log(rotateTimer / rotateTime);
        //}
        if(Mathf.Abs(rotateAngle) > 0) {
            float deltaRotate = rotateAngle > 0.5f || rotateAngle < -0.5f ? deltaTime * rotationSpeed : rotateAngle;
            deltaRotate *= Mathf.Sign(rotateAngle);
            rotateAngle -= deltaRotate;
            rotation += deltaRotate;
            rotation %= 360; 
        }
    }

    private void UpdateRotationOnMove(float deltaTime) {
        Vector2 PointA = new Vector2(rotationDirection.x, rotationDirection.y);
        Vector2 PointB = new Vector2(X, Y);
        Vector2 moveDirection = PointA - PointB;
        float distanceToTurn = (Position2 - PointA).magnitude;
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        float timeForTurning = Mathf.Abs(angle - rotation) / turnSpeed;
        float timeToTurn = distanceToTurn / Speed;
        //if (timeToTurn < timeForTurning / 2) {
        //    rotation = Mathf.LerpAngle(rotation, angle, deltaTime * rotationSpeed);//Mathf.LerpAngle ( rotation , angle , t);
        //}
        //rotation = Mathf.LerpAngle(rotation, angle, deltaTime * rotationSpeed);//Mathf.LerpAngle ( rotation , angle , t);
        //Debug.Log(angle);
        rotation = Mathf.MoveTowardsAngle(rotation, angle, deltaTime * rotationSpeed);

    }
    /// <summary>
    /// Rotates for the given amount over time.
    /// NOT IN THIS FUNCTION - ONLY SETS HOW TO ROTATE
    /// DOES NOT SET IT TO THE ANGLE
    /// </summary>
    /// <param name="angle"></param>
    internal void Rotate(float angle) {
        rotateAngle = angle;
        rotateTo = rotation + angle;
        rotateTime = Mathf.Abs(angle / rotationSpeed);
        //rotation %= 360;
    } 

    public abstract void SetDestination(Tile end);
    public abstract void SetDestination(float x, float y);

    private Vector3 Do_Movement(float deltaTime) {
        //no move command so return!
        if (Position2 == Destination) {
            IsAtDestination = true;
            return Vector3.zero;
        }

        if (NextDestination == null || NextDestination == Position2) {
            // Get the next tile from the pathfinder.
            if (worldPath == null || worldPath.Count == 0) {
                return Vector3.zero;
            }
            NextDestination = worldPath.Dequeue();
        }

        rotationDirection = new Vector3(NextDestination.Value.x, NextDestination.Value.y);

        Vector3 dest = NextDestination.Value;
        Vector3 newPos = Vector3.MoveTowards(Position, dest, Speed * deltaTime);
        Vector3 move = newPos - Position;
        X = newPos.x;
        Y = newPos.y;
        if(NextDestination==Position2)
            CurrTile = World.Current.GetTileAt(Position2);
        return move;
    }

    public void Reverse() {
        IsAtDestination = false;
        DestTile = startTile;
        startTile = World.Current.GetTileAt(X, Y);
        worldPath = backPath;
    }

    public void CreateReversePath() {
        backPath = new Queue<Vector2>(worldPath.Reverse());
    }

    public bool UpdateRotationOnPoint(float delta) {
        if (rotationDirection.magnitude == 0 || Position2 == NextDestination) {
            return true;
        }
        Vector2 PointA = new Vector2(rotationDirection.x, rotationDirection.y);
        Vector2 PointB = new Vector2(X, Y);
        Vector2 moveDirection = PointA - PointB;
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        if (Mathf.Approximately(rotation,angle)) {
            rotation = angle;
            return true;
        }
        
        rotation = Mathf.MoveTowardsAngle(rotation, angle, delta * rotationSpeed);
        return false;
    }

    protected virtual void CalculatePath() {

    }

    protected void StartCalculatingThread() {
        IsDoneCalculating = false;
        IsAtDestination = false;
        start_X = X;
        start_Y = Y;
        Thread calcPath = new Thread(CalculatePath);
        calcPath.Start();
    }

}
