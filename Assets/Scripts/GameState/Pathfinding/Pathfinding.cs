using Andja.Controller;
using Andja.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Andja.Pathfinding {

    public enum PathingMode { World, IslandMultipleStartpoints, IslandSingleStartpoint, Route };

    public enum PathDestination { Tile, Exact };

    public enum TurningType { OnPoint, TurnRadius };

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BasePathfinding {
        #region Serialize

        [JsonPropertyAttribute] public float dest_X;
        [JsonPropertyAttribute] public float dest_Y;
        [JsonPropertyAttribute] public float start_X; //not that important -- only for when we need to go back
        [JsonPropertyAttribute] public float start_Y; //not that important -- only for when we need to go back
        [JsonPropertyAttribute] protected bool _IsAtDest = true; //at creation it should be at destination

        // If we aren't moving, then destTile = currTile
        [JsonPropertyAttribute] protected Tile _destTile;

        [JsonPropertyAttribute] public float rotation;
        //Somehow this needs to be at the start to be -1 -- if not pathfinding will be screwed up for workers (atleast found)
        [JsonPropertyAttribute] protected float _y = -1;
        [JsonPropertyAttribute] protected float _x = -1;
        [JsonPropertyAttribute] public Tile startTile;

        #endregion Serialize

        #region RuntimeOrPrototyp
        private float rotateTime;
        private float rotateAngle;
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
        public Vector3 Position => new Vector3(X, Y);
        public Vector2 Position2 => new Vector2(X, Y);
        public Vector3 LastMove { get; protected set; }

        private Vector3 rotationDirection;
        public Vector2? NextDestination { get; protected set; }  // The next tile in the pathfinding sequence

        public Queue<Vector2> worldPath;
        public Queue<Vector2> backPath;

        public bool _isDoneCalculating;
        public bool IsDoneCalculating {
            get { return _isDoneCalculating; }
            set {
                _isDoneCalculating = value;
            }
        }
        protected IPathfindAgent agent;
        protected float Speed => agent.Speed;
        protected bool CanEndInUnwakable => agent.CanEndInUnwakable;
        protected float RotationSpeed => agent.RotationSpeed;
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
                if (_x > 0 && Mathf.Abs(value - _x) > 0.2f * Speed * WorldController.Instance.timeMultiplier)
                    Debug.LogWarning("UNIT JUMPED -- FIX ME -- " + Mathf.Abs(value - _x));
                _x = value;
            }
        }

        public float Y {
            get {
                return _y;
            }
            protected set {
                if (_y > 0 && Mathf.Abs(value - _y) > 0.2f * Speed * WorldController.Instance.timeMultiplier)
                    Debug.LogWarning("UNIT JUMPED -- FIX ME -- " + Mathf.Abs(value - _y));
                _y = value;
            }
        }

        private Tile _currTile;
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

        public Vector2 PeekNextDestination {
            get {
                if (worldPath != null) {
                    return worldPath.Peek();
                }
                return new Vector2(dest_X, dest_Y);
            }
        }

        public TurningType TurnType => agent.TurnType;
        public PathDestination PathDestinationType => agent.PathDestination;
        public PathingMode PathingMode => agent.PathingMode;

        #endregion RuntimeOrPrototyp

        public BasePathfinding() {
        }

        public virtual void Update_DoMovement(float deltaTime) {
            //for loading purpose or any other strange reason
            //we have a destination & are not there atm && we have no path then calculate it!
            if (DestTile != null && DestTile != CurrTile && IsAtDestination == false
                && NextDestination != Destination && worldPath == null && IsDoneCalculating != false) {
                SetDestination(dest_X, dest_Y);
            }
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
            if (TurnType == TurningType.OnPoint && LastMove.sqrMagnitude <= 0.1 &&
                IsAtDestination == false && CurrTile != DestTile) {
                if(UpdateRotationOnPoint(deltaTime) == false) {
                    return;
                }
            }
            else {
                UpdateRotationOnMove(deltaTime);
            }
            LastMove = DoMovement(deltaTime);
        }

        public void UpdateDoRotate(float deltaTime) {
            //if ((rotateTo + 0.1f <= rotation && rotateTo + 0.1f >= rotation)==false) {
            //    rotateTimer += deltaTime * rotationSpeed;
            //    rotation = Mathf.LerpAngle(rotation, rotateTo, rotateTimer/rotateTime);
            //    Debug.Log(rotateTimer / rotateTime);
            //}
            //if (Mathf.Abs(rotateAngle) > 0) {
            //    float deltaRotate = rotateAngle > 0.5f || rotateAngle < -0.5f ? deltaTime * rotationSpeed : rotateAngle;
            //    deltaRotate *= Mathf.Sign(rotateAngle);
            //    rotateAngle -= deltaRotate;
            //    rotation += deltaRotate;
            //    rotation %= 360;
            //}

            if (Mathf.Approximately(rotation, rotateAngle)) {
                rotation = rotateAngle;
            }
            rotation = Mathf.MoveTowardsAngle(rotation, rotateAngle, deltaTime * RotationSpeed);

        }

        private void UpdateRotationOnMove(float deltaTime) {
            Vector2 PointA = new Vector2(rotationDirection.x, rotationDirection.y);
            Vector2 PointB = new Vector2(X, Y);
            Vector2 moveDirection = PointA - PointB;
            float distanceToTurn = (Position2 - PointA).magnitude;
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            float timeForTurning = Mathf.Abs(angle - rotation) / RotationSpeed;
            float timeToTurn = distanceToTurn / Speed;
            //if (timeToTurn < timeForTurning / 2) {
            //    rotation = Mathf.LerpAngle(rotation, angle, deltaTime * rotationSpeed);//Mathf.LerpAngle ( rotation , angle , t);
            //}
            //rotation = Mathf.LerpAngle(rotation, angle, deltaTime * rotationSpeed);//Mathf.LerpAngle ( rotation , angle , t);
            //Debug.Log(angle);
            rotation = Mathf.MoveTowardsAngle(rotation, angle, deltaTime * RotationSpeed);
        }

        /// <summary>
        /// Rotates to the given - seperate call needed
        /// NOT IN THIS FUNCTION - ONLY SETS HOW TO ROTATE
        /// DOES NOT SET IT TO THE ANGLE
        /// </summary>
        /// <param name="angle"></param>
        internal void Rotate(float angle) {
            rotateAngle = angle;
            rotateTime = Mathf.Abs(angle / RotationSpeed);
        }

        public abstract void SetDestination(Tile end);

        public abstract void SetDestination(float x, float y);

        private Vector3 DoMovement(float deltaTime) {
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
            if (NextDestination == Position2)
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
            if (Mathf.Approximately(rotation, angle)) {
                rotation = angle;
                return true;
            }
            rotation = Mathf.MoveTowardsAngle(rotation, angle, delta * RotationSpeed);
            return false;
        }

        protected virtual void CalculatePath() {
        }
        /// <summary>
        /// Currently no thread cause it needs a complete rewrite to make it smooooth
        /// </summary>
        protected void StartCalculatingThread() {
            //Debug.Log(agent + " StartCalculatingThread - Currently running " + (AllThreads.Count));
            IsDoneCalculating = false;
            IsAtDestination = false;
            start_X = X;
            start_Y = Y;
            //if (calculatePathThread != null) {
            //    if (calculatePathThread.IsAlive)
            //        calculatePathThread.Abort();
            //    AllThreads.Remove(calculatePathThread);
            //}
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            //CalculatePath();
            if(agent is Worker) {
                CalculatePath();
            }
            else {
                Thread calculatePathThread = new Thread(CalculatePath);
                calculatePathThread.Start();
            }
            Debug.Log(stopwatch.Elapsed.TotalSeconds + " " + this.GetType().FullName + " " + CanEndInUnwakable);
            //Task.Run(CalculatePath, CancellationToken.None);
            //CalculatePath();
            //ThreadPool.QueueUserWorkItem(new WaitCallback(CalculatePathObject));
            //AllThreads.Add(calculatePathThread);
        }

        public void Load(IPathfindAgent agent) {
            Debug.Log(agent);
            this.agent = agent;
            _IsAtDest = Mathf.Approximately(_x, dest_X) && Mathf.Approximately(_y, dest_Y);
            IsDoneCalculating = true;
            rotateAngle = rotation;
            //if(this is RoutePathfinding == false && IsAtDestination == false) {
            //    SetDestination(dest_X, dest_Y);
            //}
        }

    }
}