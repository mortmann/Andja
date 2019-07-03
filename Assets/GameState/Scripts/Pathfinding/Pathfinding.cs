using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System;

public enum Path_mode { world, islandMultipleStartpoints, islandSingleStartpoint, route };
public enum Path_dest { tile, exact };
public enum Turn_type { OnPoint, TurnRadius };

[JsonObject(MemberSerialization.OptIn)]
public abstract class Pathfinding {
    #region Serialize

    [JsonPropertyAttribute] public float dest_X;
    [JsonPropertyAttribute] public float dest_Y;
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

    public Vector3 Position {
        get { return new Vector3(X, Y); }
    }
    public Vector2 Position2 {
        get { return new Vector2(X, Y); }
    }
    public Queue<Tile> worldPath;
    public Queue<Tile> backPath;

    protected Path_dest pathDest;

    public Vector3 LastMove { get; protected set; }

    protected float rotationSpeed = 5;
    protected Thread calculatingPathThread;
    public bool IsDoneCalculating = false;
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

    protected Path_mode pathmode;
    public Turn_type myTurnType = Turn_type.OnPoint;
    public float turnSpeed = 10;

    #endregion

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
            if (Mathf.Abs(value - _x) > 0.05f)
                Debug.LogWarning("UNIT JUMPED -- FIX ME");
            _x = value;
        }
    }
    public float Y {
        get {
            return _y;
        }
        protected set {
            //TODO: REMOVE FOR RELEASE
            if (Mathf.Abs(value - _y) > 0.05f)
                Debug.LogWarning("UNIT JUMPED -- FIX ME");
            _y = value;
        }
    }
    public Tile NextTile { get; protected set; }  // The next tile in the pathfinding sequence
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
            if (_currTile == null) {
                X = value.X;
                Y = value.Y;
            }
            _currTile = value;
        }
    }

    public Vector2 NextDestination {
        get {
            if (NextTile == null)
                return new Vector2(dest_X, dest_Y);
            return NextTile.Vector2;
        }
    }
    public Vector2 PeekNextDestination {
        get {
            if(worldPath!=null) {
                return worldPath.Peek().Vector2;
            }
            return new Vector2(dest_X, dest_Y);
        }
    }
    public Pathfinding() {
    }

    private Vector3 rotationDirection;

    public virtual void Update_DoMovement(float deltaTime) {
        

        //for loading purpose or any other strange reason
        //we have a destination & are not there atm && we have no path then calculate it!
        if (DestTile != null && DestTile != CurrTile && IsAtDestination == false && NextTile != DestTile && worldPath == null)
            SetDestination(dest_X, dest_Y);
        if (IsDoneCalculating == false)
            return;
        
        if (IsAtDestination) {
            LastMove = Vector3.zero;
            return;
        }

        if (CurrTile == DestTile && pathDest != Path_dest.exact) {
            IsAtDestination = true;
            LastMove = Vector3.zero;
            return;
        }

        //if were standing or if we can turn OnPoint(OnSpot) turn to face the rightway
        if (myTurnType == Turn_type.OnPoint && LastMove.sqrMagnitude <= 0.1) {
            //so we can turn on point but not move
            //so rotate around with the turnspeed
            //we can only rotate if we know the next tile we are going to visit
            if (IsAtDestination == false && CurrTile != DestTile && UpdateRotationOnPoint(deltaTime) == false) {
                return;
            }
        }
        else {
            UpdateRotationOnMove(deltaTime);
        }
        if (pathDest == Path_dest.exact && NextTile == DestTile) {
            worldPath.Clear();
            LastMove = AccurateMove(deltaTime);
            return;
        }
        LastMove = DoWorldPath(deltaTime);

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
        if (timeToTurn < timeForTurning / 2) {
            rotation = Mathf.LerpAngle(rotation, angle, deltaTime * rotationSpeed);//Mathf.LerpAngle ( rotation , angle , t);
        }
        rotation = Mathf.LerpAngle(rotation, angle, deltaTime * rotationSpeed);//Mathf.LerpAngle ( rotation , angle , t);

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

    private Vector3 DoWorldPath(float deltaTime) {
        //no move command so return!
        if (DestTile == CurrTile) {
            IsAtDestination = true;
            return Vector3.zero;
        }

        if (NextTile == null || NextTile == CurrTile) {
            // Get the next tile from the pathfinder.
            if (worldPath == null || worldPath.Count == 0) {
                return Vector3.zero;
            }
            NextTile = worldPath.Dequeue();
        }

        rotationDirection = new Vector3(NextTile.X, NextTile.Y);
        Vector3 dir = new Vector3(NextTile.X - X, NextTile.Y - Y);
        dir = dir.normalized;

        Vector3 temp = deltaTime * dir * Speed;

        X += temp.x;
        Y += temp.y;
        if (World.Current.IsInTileAt(NextTile, X, Y)) {
            CurrTile = NextTile;
        }
        return temp;
    }
    private Vector3 AccurateMove(float deltaTime) {
        // we are one tile from destination tile away...
        // now we have to go to the correct x/y coordinations
        if (X >= dest_X - 0.1f && X <= dest_X + 0.1f && Y >= dest_Y - 0.1f && Y <= dest_Y + 0.1f) {
            //we are near enough
            CurrTile = World.Current.GetTileAt(X, Y);
            NextTile = null;
            DestTile = CurrTile;
            IsAtDestination = true;
            return Vector3.zero;
        }
        Vector3 dir = new Vector3(dest_X - X, dest_Y - Y);
        rotationDirection = new Vector3(dest_X, dest_Y);
        dir = dir.normalized;
        Vector3 temp = dir * Speed;
        // DUNNO why this works and not vector * deltaTime
        temp.x *= deltaTime;
        temp.y *= deltaTime;
        X += temp.x;
        Y += temp.y;
        return temp;
    }

    public void Reverse() {
        IsAtDestination = false;
        DestTile = startTile;
        startTile = backPath.Peek();
        worldPath = backPath;
    }

    public void CreateReversePath() {
        backPath = new Queue<Tile>(worldPath.Reverse());
    }

    float t = 0;
    public bool UpdateRotationOnPoint(float delta) {
        if (rotationDirection.magnitude == 0 || CurrTile == NextTile) {
            return true;
        }
        Vector2 PointA = new Vector2(rotationDirection.x, rotationDirection.y);
        Vector2 PointB = new Vector2(X, Y);
        Vector2 moveDirection = PointA - PointB;
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        //		if(angle<0){
        //			angle =360+ angle;
        //		}
        //rotation = angle;

        if (rotation < angle + 0.1f
            && rotation > angle - 0.1f) {
            //no need to rotate so set the rotation to the correct one
            rotation = angle;
            t = 0;
            return true;
        }
        t += delta;

        rotation = Mathf.LerpAngle(rotation, angle, delta * rotationSpeed);//Mathf.LerpAngle ( rotation , angle , t);
        return false;
    }

    protected virtual void CalculatePath() {

    }

    protected void StartCalculatingThread() {
        IsDoneCalculating = false;
        IsAtDestination = false;

        Thread calcPath = new Thread(CalculatePath);
        calcPath.Start();
    }

}
