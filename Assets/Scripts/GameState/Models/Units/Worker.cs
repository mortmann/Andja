using System;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Pathfinding;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Andja.Model {
    public class WorkerPrototypeData {
        public string workerID;
        public string workSound;
        public string toWorkSprites;
        public string fromWorkSprites;
        public int pixelsPerSprite = 64;
        public float speed = 1;
        public float rotationSpeed = 720;
        public bool hasToFollowRoads = false;
        public bool hasToEnterWork = false;
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class Worker : IPathfindAgent {
        public const float WorldSize = 0.25f;
        public enum WorkStates { GoingToWork, Working, GoingHome, AtHome }

        #region Serialize
        [JsonPropertyAttribute] public string ID;
        [JsonPropertyAttribute] private BasePathfinding _path;
        [JsonPropertyAttribute] protected float workTimer;
        [JsonPropertyAttribute] public Item[] ToGetItems;
        [JsonPropertyAttribute] public UnitInventory Inventory { get; protected set; }
        [JsonPropertyAttribute] public WorkStates WorkState;
        [JsonPropertyAttribute] public bool isAtHome;
        [JsonPropertyAttribute] private Structure _workStructure;
        [JsonPropertyAttribute] private bool _isDone;
        [JsonPropertyAttribute] private readonly bool _walkTimeIsWorkTime;
        [JsonPropertyAttribute] private readonly float _workAtHomeTime;
        #endregion Serialize

        #region runtimeVariables
        public Structure Home;
        public float WorkTimer => workTimer;
        public WorkerPrototypeData Data => prototypeData ??= PrototypController.Instance.GetWorkerPrototypDataForID(ID);

        public OutputStructure WorkOutputStructure {
            set => _workStructure = value;
            get => _workStructure is OutputStructure os? os : null;
        }

        public Structure WorkStructure {
            set => _workStructure = value;
            get => _workStructure;
        }
        public string WorkSound => Data.workSound;
        public string ToWorkSprites => Data.toWorkSprites;
        public string FromWorkSprites => Data.fromWorkSprites;
        public float Speed => Data.speed;
        public int PixelsPerSprite => Data.pixelsPerSprite;
        private bool HasToFollowRoads => Data.hasToFollowRoads;
        private bool HasToEnterWorkStructure => Data.hasToEnterWork;


        public bool IsFull => Inventory?.HasAnything() == true || WorkStates.GoingHome == WorkState && Home is ServiceStructure;
        private Func<Structure, float, bool> WorkOnStructure {
            get {
                if (Home is ServiceStructure h)
                    return h.WorkOnTarget;
                return null;
            }
        }

        protected Action<Worker> cbWorkerChanged;
        protected Action<Worker> cbWorkerDestroy;
        protected Action<Worker, string, bool> cbSoundCallback;
        protected float walkTime;
        protected WorkerPrototypeData prototypeData;
        #endregion runtimeVariables

        public float X => _path.X;

        public float Y => _path.Y;

        public float Rotation => _path.rotation;

        public float RotationSpeed => Data.rotationSpeed;

        public TurningType TurnType => HasToFollowRoads ? TurningType.OnPoint : TurningType.TurnRadius;
        public PathDestination PathDestination => PathDestination.Tile;
        public PathingMode PathingMode => HasToFollowRoads ? PathingMode.Route : PathingMode.IslandMultiplePoints;
        public bool CanEndInUnwalkable => HasToEnterWorkStructure || WorkStates.Working == WorkState;

        public PathHeuristics Heuristic => HasToFollowRoads ? PathHeuristics.Manhattan : PathHeuristics.Euclidean;

        public PathDiagonal DiagonalType => HasToFollowRoads ? PathDiagonal.None : PathDiagonal.Always;

        public IReadOnlyList<int> CanEnterCities => null; // For now worker always can enter all tiles regardless who owns it

        public bool IsAlive => isAtHome == false;

        public Worker(Structure home, OutputStructure workStructure, float workTime, string workerID, Item[] toGetItems = null,
                        bool walkTimeIsWorkTime = false, float workAtHomeTime = 0f) {
            Home = home;
            WorkOutputStructure = workStructure;
            _walkTimeIsWorkTime = walkTimeIsWorkTime;
            _workAtHomeTime = workAtHomeTime;
            if (workStructure is MarketStructure == false) {
                workStructure.ClaimOutput();
            }
            isAtHome = false;
            WorkState = WorkStates.GoingToWork;
            Inventory = new UnitInventory(4);
            workTimer = workTime;
            ID = workerID ?? "placeholder";
            ToGetItems = toGetItems;
            StartPathfinding();
            Setup();
        }

        public Worker(ServiceStructure home, Structure structure, float workTime, string workerID) {
            Home = home;
            ID = workerID ?? "placeholder";
            WorkStructure = structure;
            isAtHome = false;
            WorkState = WorkStates.GoingToWork;
            workTimer = workTime;
            StartPathfinding();
            Setup();
        }
        /// <summary>
        /// This is for a workaround production structure nearest market searching
        /// </summary>
        /// <param name="workerID"></param>
        public Worker(string workerID) {
            ID = workerID ?? "placeholder";
        }
        public Worker() {
            SaveController.AddWorkerForLoad(this);
        }

        private void Setup() {
            WorkStructure.RegisterOnDestroyCallback(OnWorkStructureDestroy);
        }

        public void OnWorkStructureDestroy(Structure str, IWarfare destroyer) {
            if (str != WorkStructure) {
                Debug.LogError("OnWorkStructureDestroy called on not workstructure destroy!");
                return;
            }
            GoHome();
        }

        public void Update(float deltaTime) {
            if (Home == null) {
                Debug.LogError("worker has no Home -> for now set it manually");
                return;
            }
            if (Home.IsActiveAndWorking == false && WorkState != WorkStates.GoingHome) {
                GoHome();
            }
            //worker can only work if
            // -homeStructure is active
            // -goalStructure can be reached -> search new goal
            // -goalStructure has smth to be worked eg grown/has output
            // -Efficiency of home > 0
            // -home is not full (?) maybe second worker?
            //If any of these are false the worker should return to home
            //except there is no way to home then remove
            if (_path.Status == JobStatus.NoPath) {
                Destroy();
                return;
            }
            cbWorkerChanged?.Invoke(this);
            switch (WorkState) {
                case WorkStates.GoingToWork:
                    _path.Update_DoMovement(deltaTime);
                    if (_path.IsAtDestination) {
                        WorkState = WorkStates.Working;
                        //Precalculate so it has walkTime
                        StartPathfinding(); //todo: think about some optimisation for just "reverse path"
                    }
                    if (_walkTimeIsWorkTime == false) return;
                    workTimer -= deltaTime;
                    break;
                case WorkStates.Working:
                    DoWork(deltaTime);
                    break;
                case WorkStates.GoingHome:
                    _path.Update_DoMovement(deltaTime);
                    if (_path.IsAtDestination) {
                        WorkState = WorkStates.AtHome;
                    }
                    if (_walkTimeIsWorkTime == false) return;
                    workTimer -= deltaTime;
                    if (workTimer <= 0 == false) return;
                    DropOffItems(0);
                    //we have an issue -- done before it is home
                    if (World.Current.GetTileAt(X, Y).Structure != Home) {
                        Vector2 dist = new Vector2(X, Y) - _path.Destination;
                        Debug.LogWarning("Worker done before it is at Home. Fix this with either smaller Range," +
                                         " longer Worktime or remove Worker. " + Home + ". Destination " + _path.Destination
                                         + " Distance: " + dist.magnitude + " | " + walkTime);
                    }
                    break;
                case WorkStates.AtHome:
                    if (ToGetItems != null && Inventory.HasAnything()) {
                        DropOffItems(deltaTime);
                    }
                    else {
                        isAtHome = true;
                    }
                    break;
            }
        }

        internal bool IsWorking() {
            //has it anything? && is not going to get anything it is not Working -> so opposite should be working 
            return (Inventory.HasAnything() == false && WorkState == WorkStates.GoingHome) == false;
        }

        public void DropOffItems(float deltaTime) {
            workTimer -= deltaTime;
            if (workTimer > 0) {
                return;
            }
            switch (Home) {
                case MarketStructure marketStructure:
                    marketStructure.City.Inventory.AddInventory(Inventory);
                    break;
                case ProductionStructure productionStructure:
                    productionStructure.AddToIntake(Inventory);
                    break;
                case FarmStructure farmStructure:
                    farmStructure.AddHarvastable();
                    break;
                case OutputStructure outputStructure:
                    outputStructure.AddToOutput(Inventory);
                    break;
            }
            isAtHome = true;
        }

        public void GoHome() {
            if (WorkState == WorkStates.GoingToWork || WorkState == WorkStates.Working) {
                WorkOutputStructure?.ResetOutputClaimed();
            }
            _isDone = false;
            WorkState = WorkStates.GoingHome;
            WorkStructure?.UnregisterOnDestroyCallback(OnWorkStructureDestroy);
        }

        public void DoWork(float deltaTime) {
            if (WorkStructure == null && _path.DestTile != null) {
                WorkStructure = _path.DestTile.Structure;
            }
            //we are here at the job tile
            if (WorkOnStructure != null) {
                DoWorkOnStructure(deltaTime);
            }
            else
            if (WorkOutputStructure != null) {
                if (WorkOutputStructure is GrowableStructure) {
                    DoFarmWork(deltaTime);
                }
                else {
                    DoOutPutStructureWork(deltaTime);
                }
            }
            else {
                Debug.LogError("Worker has nothing todo -- why does he exist? He is from " + Home + "! Killing him now.");
                Destroy();
            }
            if (_isDone) {
                GoHome();
            }
        }

        private void DoWorkOnStructure(float deltaTime) {
            _isDone = WorkOnStructure(WorkStructure, deltaTime);
        }

        public void DoFarmWork(float deltaTime) {
            workTimer -= deltaTime;
            if (workTimer > walkTime + _workAtHomeTime) {
                PlaySound(WorkSound, true);
                return;
            }
            PlaySound(WorkSound, false);
            Inventory.AddItems(((GrowableStructure)WorkStructure).GetOutput());
            ((GrowableStructure)WorkStructure).Harvest();
            _isDone = true;
        }

        private void PlaySound(string soundWorkName, bool play) {
            if (string.IsNullOrWhiteSpace(soundWorkName)) {
                return;
            }
            cbSoundCallback?.Invoke(this, soundWorkName, play);
        }

        public void DoOutPutStructureWork(float deltaTime) {
            workTimer -= deltaTime;
            if (workTimer > walkTime + _workAtHomeTime) {
                PlaySound(WorkSound, true);
                return;
            }
            PlaySound(WorkSound, false);
            if (WorkOutputStructure is MarketStructure) {
                foreach (Item item in WorkOutputStructure.GetOutputWithItemCountAsMax(ToGetItems)) {
                    Inventory.AddItem(item);
                }
            } else {
                if (ToGetItems == null) {
                    foreach (Item item in WorkOutputStructure.GetOutput()) {
                        Inventory.AddItem(item);
                    }
                }
                if (ToGetItems != null) {
                    foreach (Item item in WorkOutputStructure.GetOutputWithItemCountAsMax(ToGetItems)) {
                        Inventory.AddItem(item);
                    }
                }
            }
            _isDone = true;
        }

        internal void Load(Structure parent) {
            Home = parent;
            if (WorkState == WorkStates.GoingToWork) {
                WorkOutputStructure?.ClaimOutput();
            } else {
                WorkStructure = Home;
            }
            if (WorkStructure == null || WorkStructure.IsDestroyed) {
                Destroy();
            } else {
                WorkStructure.RegisterOnDestroyCallback(OnWorkStructureDestroy);
            }
            if (_path == null) return;
            _path.Load(this);
            if (!(_path is RoutePathfinding { StartStructure: null } rp)) return;
            rp.GoalStructure = WorkState == WorkStates.GoingToWork ? WorkStructure : Home;
        }

        public void Destroy() {
            isAtHome = true; //for Pathfinding purpose -> if it is at home stop the Pathfinding
            if (WorkState == WorkStates.GoingToWork || WorkState == WorkStates.Working)
                WorkOutputStructure?.ResetOutputClaimed();
            cbWorkerDestroy?.Invoke(this);
            _path?.CancelJob();
        }

        public void StartPathfinding() {
            if (HasToFollowRoads == false) {
                _path ??= new TilesPathfinding(this);
                if (WorkState == WorkStates.GoingToWork) {
                    ((TilesPathfinding)_path).SetDestination(new List<Tile>(Home.Tiles), new List<Tile>(WorkStructure.Tiles));
                }
                else {
                    if(_path.CurrTile == null) {
                        Destroy();
                        return;
                    }
                    ((TilesPathfinding)_path).SetDestination(new List<Tile> { _path.CurrTile }, new List<Tile>(Home.Tiles));
                }
            }
            else {
                _path ??= new RoutePathfinding(this);
                if (WorkState == WorkStates.GoingToWork) {
                    ((RoutePathfinding)_path).SetDestination(Home, WorkStructure);
                }
                else {
                    ((RoutePathfinding)_path).SetDestination(HasToEnterWorkStructure ? WorkStructure : null, Home);
                }
            }
            walkTime = Vector2.Distance(WorkStructure.Center, Home.Center) * Speed;
            _path.cbPathCalcDone += DonePathCalc;
        }

        private void DonePathCalc() {
            walkTime = _path.WalkTime * Speed;
        }

        public void RegisterOnChangedCallback(Action<Worker> cb) {
            cbWorkerChanged += cb;
        }

        public void UnregisterOnChangedCallback(Action<Worker> cb) {
            cbWorkerChanged -= cb;
        }

        public void RegisterOnDestroyCallback(Action<Worker> cb) {
            cbWorkerDestroy += cb;
        }

        public void UnregisterOnDestroyCallback(Action<Worker> cb) {
            cbWorkerDestroy -= cb;
        }

        public void RegisterOnSoundCallback(Action<Worker, string, bool> cb) {
            cbSoundCallback += cb;
        }

        public void UnregisterOnSoundCallback(Action<Worker, string, bool> cb) {
            cbSoundCallback -= cb;
        }

        public void PathInvalidated() {
            
        }
    }
}