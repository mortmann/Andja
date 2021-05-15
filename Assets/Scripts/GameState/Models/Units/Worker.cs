using Andja.Controller;
using Andja.Pathfinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {
    public class WorkerPrototypeData {
        public string workerID;
        public string workSound;
        public string toWorkSprites;
        public string fromWorkSprites;
        public int pixelsPerSprite = 64;
        public float speed = 1;
        public float rotationSpeed = 720;
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class Worker : IPathfindAgent {
        public const float WorldSize = 0.25f;

        #region Serialize
        [JsonPropertyAttribute] public string ID;
        [JsonPropertyAttribute] public Structure Home;
        [JsonPropertyAttribute] private BasePathfinding path;
        [JsonPropertyAttribute] private float workTimer;
        [JsonPropertyAttribute] private Item[] toGetItems;
        [JsonPropertyAttribute] private Inventory inventory;
        [JsonPropertyAttribute] private bool goingToWork;
        [JsonPropertyAttribute] public bool isAtHome;
        [JsonPropertyAttribute] private Structure _workStructure;
        [JsonPropertyAttribute] private bool isDone;
        [JsonPropertyAttribute] private bool hasToFollowRoads;
        [JsonPropertyAttribute] private bool hasToEnterWorkStructure;
        [JsonPropertyAttribute] private bool walkTimeIsWorkTime;
        [JsonPropertyAttribute] private float workAtHomeTime;

        #endregion Serialize

        #region runtimeVariables

        public float WorkTimer => workTimer;
        public WorkerPrototypeData Data {
            get {
                if (_prototypData == null) {
                    _prototypData = PrototypController.Instance.GetWorkerPrototypDataForID(ID);
                }
                return _prototypData;
            }
        }

        public OutputStructure WorkOutputStructure {
            set {
                _workStructure = value;
            }
            get {
                if (_workStructure is OutputStructure)
                    return (OutputStructure)_workStructure;
                return null;
            }
        }

        public Structure WorkStructure {
            set {
                _workStructure = value;
            }
            get {
                return _workStructure;
            }
        }
        public string WorkSound => Data.workSound;
        public string toWorkSprites => Data.toWorkSprites;
        public string fromWorkSprites => Data.fromWorkSprites;
        public float Speed => Data.speed;
        public int PixelsPerSprite => Data.pixelsPerSprite;

        public bool IsFull => inventory.HasAnything() || Home is ServiceStructure && goingToWork == false;
        private Func<Structure, float, bool> WorkOnStructure {
            get {
                if (Home is ServiceStructure)
                    return ((ServiceStructure)Home).WorkOnTarget;
                return null;
            }
        }

        private Action<Worker> cbWorkerChanged;
        private Action<Worker> cbWorkerDestroy;
        private Action<Worker, string, bool> cbSoundCallback;
        private bool hasRegistered;
        private float walkTime;
        private WorkerPrototypeData _prototypData;
        #endregion runtimeVariables

        public float X {
            get {
                return path.X;
            }
        }

        public float Y {
            get {
                return path.Y;
            }
        }

        public float Rotation {
            get {
                return path.rotation;
            }
        }

        public float RotationSpeed => Data.rotationSpeed;

        public TurningType TurnType => hasToFollowRoads? TurningType.OnPoint : TurningType.TurnRadius;
        public PathDestination PathDestination => PathDestination.Tile;
        public PathingMode PathingMode => path.CurrTile!=null? PathingMode.IslandSingleStartpoint : PathingMode.IslandMultipleStartpoints;
        public bool CanEndInUnwakable => hasToEnterWorkStructure || goingToWork == false;

        public PathHeuristics Heuristic => hasToFollowRoads ? PathHeuristics.Manhattan : PathHeuristics.Euclidean;

        public bool CanMoveDiagonal => hasToFollowRoads == false;

        public IReadOnlyList<int> CanEnterCities => null; // For now worker always can enter all tiles regardless who owns it

        public Worker(Structure Home, OutputStructure structure, float workTime, string workerID, Item[] toGetItems = null,
                        bool hasToFollowRoads = true,
                        bool walkTimeIsWorkTime = false, bool hasToEnterWorkStructure = true, float workAtHomeTime = 0f) {
            this.Home = Home;
            WorkOutputStructure = structure;
            this.hasToFollowRoads = hasToFollowRoads;
            this.walkTimeIsWorkTime = walkTimeIsWorkTime;
            this.workAtHomeTime = workAtHomeTime;
            if (structure is MarketStructure == false) {
                structure.outputClaimed = true;
            }
            this.hasToEnterWorkStructure = hasToEnterWorkStructure;
            isAtHome = false;
            goingToWork = true;
            inventory = new Inventory(4);
            workTimer = workTime;
            this.ID = workerID ?? "placeholder";
            this.toGetItems = toGetItems;
            SetGoalStructure(structure);
            Setup();
        }

        public Worker(ServiceStructure Home, Structure structure, float workTime, string workerID, bool hasToFollowRoads = true, bool hasToEnterWorkStructure = true) {
            this.Home = Home;
            this.ID = workerID ?? "placeholder";
            WorkStructure = structure;
            this.hasToFollowRoads = hasToFollowRoads;
            this.hasToEnterWorkStructure = hasToEnterWorkStructure;
            isAtHome = false;
            goingToWork = true;
            workTimer = workTime;
            SetGoalStructure(structure);
            Setup();
        }

        public Worker() {
            SaveController.AddWorkerForLoad(this);
        }

        private void Setup() {
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
            if (Home.IsActiveAndWorking == false && goingToWork) {
                GoHome();
            }
            if (hasRegistered == false) {
                WorkStructure.RegisterOnDestroyCallback(OnWorkStructureDestroy);
                walkTime = Vector3.Distance(Home.Center, WorkStructure.Center);
                hasRegistered = true;
            }
            //worker can only work if
            // -homeStructure is active
            // -goalStructure can be reached -> search new goal
            // -goalStructure has smth to be worked eg grown/has output
            // -Efficiency of home > 0
            // -home is not full (?) maybe second worker?
            //If any of these are false the worker should return to home
            //except there is no way to home then remove
            if (path == null) {
                //theres no goal so delete it after some time?
                Debug.Log("worker has no goal");
                GoHome();
                return;
            }

            //do the movement
            path.Update_DoMovement(deltaTime);

            cbWorkerChanged?.Invoke(this);

            if (path.IsAtDestination == false) {
                if (walkTimeIsWorkTime) {
                    workTimer -= deltaTime;
                    if (workTimer <= 0) {
                        DropOffItems(0);
                        //we have an issue -- done before it is home
                        Debug.LogWarning("Worker done before it is at Home. Fix this with either smaller Range," +
                            " longer Worktime or remove Worker. " + Home.ToString() + ". Destination " + path.Destination);
                    }
                }
                return;
            }
            if (goingToWork) {
                //if we are here this means we're
                //AT the destination and can start working
                DoWork(deltaTime);
            }
            else {
                // coming home from doing the work
                // drop off the items its carrying
                if (toGetItems != null && inventory.HasAnything()) {
                    DropOffItems(deltaTime);
                }
                else {
                    isAtHome = true;
                }
            }
        }

        internal bool IsWorking() {
            //has it anything? && is not going to get anything it is not Working -> so opposite should be working 
            return (inventory.HasAnything() == false && goingToWork == false) == false;
        }

        public void DropOffItems(float deltaTime) {
            workTimer -= deltaTime;
            if (workTimer > 0) {
                return;
            }
            if (Home is MarketStructure) {
                ((MarketStructure)Home).City.Inventory.AddIventory(inventory);
            }
            else
            if (Home is ProductionStructure) {
                ((ProductionStructure)Home).AddToIntake(inventory);
            }
            else
            if (Home is FarmStructure) {
                ((FarmStructure)Home).AddHarvastable();
            }
            else if (Home is OutputStructure) {
                //this home is a OutputStructures or smth that takes it to output
                ((OutputStructure)Home).AddToOutput(inventory);
            }
            isAtHome = true;
        }

        public void GoHome() {
            isDone = false;
            goingToWork = false;
            WorkStructure?.UnregisterOnDestroyCallback(OnWorkStructureDestroy);
            //WorkStructure = null;
            SetGoalStructure(Home, true); //todo: think about some optimisation for just "reverse path"
                                          //doTimer = workTime / 2;
        }

        public void DoWork(float deltaTime) {
            if (WorkStructure == null && path.DestTile != null) {
                WorkStructure = path.DestTile.Structure;
            }
            //we are here at the job tile
            //do its job -- get the items in tile
            if (WorkOutputStructure is GrowableStructure) {
                DoFarmWork(deltaTime);
            }
            else
            if (WorkOutputStructure is OutputStructure) {
                DoOutPutStructureWork(deltaTime);
            }
            else
            if (WorkOnStructure != null) {
                DoWorkOnStructure(deltaTime);
            }
            else {
                Debug.LogError("Worker has nothing todo -- why does he exist? He is from " + Home.ToString() + "! Killing him now.");
                Destroy();
            }
            if (isDone) {
                GoHome();
            }
        }

        private void DoWorkOnStructure(float deltaTime) {
            isDone = WorkOnStructure(WorkStructure, deltaTime);
        }

        public void DoFarmWork(float deltaTime) {
            workTimer -= deltaTime;
            if (workTimer > walkTime + workAtHomeTime) {
                PlaySound(WorkSound, true);
                return;
            }
            PlaySound(WorkSound, false);            
            inventory.AddItems(((GrowableStructure)WorkStructure).GetOutput());
            ((GrowableStructure)WorkStructure).Harvest();
            isDone = true;
        }

        private void PlaySound(string soundWorkName, bool play) {
            if (string.IsNullOrWhiteSpace(soundWorkName)) {
                return;
            }
            cbSoundCallback?.Invoke(this, soundWorkName, play);
        }

        public void DoOutPutStructureWork(float deltaTime) {
            workTimer -= deltaTime;
            if (workTimer > walkTime + workAtHomeTime) {
                PlaySound(WorkSound, true);
                return;
            }
            PlaySound(WorkSound, false);
            if (toGetItems == null) {
                foreach (Item item in WorkOutputStructure.GetOutput()) {
                    inventory.AddItem(item);
                }
            }
            if (toGetItems != null) {
                foreach (Item item in WorkOutputStructure.GetOutputWithItemCountAsMax(toGetItems)) {
                    inventory.AddItem(item);
                }
            }
            if (WorkOutputStructure is MarketStructure) {
                foreach (Item item in WorkOutputStructure.GetOutputWithItemCountAsMax(toGetItems)) {
                    if (item == null) {
                        Debug.LogError("item is null for to get item! Worker is from " + WorkOutputStructure);
                    }
                    inventory.AddItem(item);
                }
            }
            WorkOutputStructure.outputClaimed = false;
            isDone = true;
        }

        internal void Load() {
            path.Load(this);
            if (path is RoutePathfinding rp) {
                if (rp.StartStructure == null) {
                    if (WorkStructure.IsDestroyed) {
                        Destroy();
                    }
                    if (goingToWork) {
                        rp.GoalStructure = WorkStructure;
                    }
                    else {
                        rp.GoalStructure = Home;
                    }
                }
            }
        }

        public void Destroy() {
            if (goingToWork)
                WorkOutputStructure?.ResetOutputClaimed();
            cbWorkerDestroy?.Invoke(this);
        }

        public void SetGoalStructure(Structure structure, bool goHome = false) {
            if (structure == null) {
                return;
            }
            if (hasToFollowRoads == false) {
                if (path == null)
                    path = new TilesPathfinding(this);
                if (goHome == false) {
                    ((TilesPathfinding)path).SetDestination(new List<Tile>(Home.Tiles), new List<Tile>(structure.Tiles));
                }
                else {
                    ((TilesPathfinding)path).SetDestination(new List<Tile>() { path.CurrTile }, new List<Tile>(Home.Tiles));
                }
            }
            else {
                if (path == null)
                    path = new RoutePathfinding(this);
                if (goHome == false) {
                    ((RoutePathfinding)path).SetDestination(Home, structure);
                }
                else {
                    ((RoutePathfinding)path).SetDestination(WorkStructure, Home);
                }
            }
            WorkStructure = structure;
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

    }
}