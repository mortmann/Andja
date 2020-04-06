using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Worker {
    #region Serialize
    [JsonPropertyAttribute] public Structure Home;
    [JsonPropertyAttribute] Pathfinding path;
    [JsonPropertyAttribute] float doTimer;
    [JsonPropertyAttribute] Item[] toGetItems;
    //[JsonPropertyAttribute] int[] toGetAmount;
    [JsonPropertyAttribute] Inventory inventory;
    [JsonPropertyAttribute] bool goingToWork;
    [JsonPropertyAttribute] public bool isAtHome;
    [JsonPropertyAttribute] private Structure _workStructure;

    #endregion
    #region runtimeVariables
    public OutputStructure WorkOutputStructure {
        set {
            _workStructure = value;
        }
        get {
            if(_workStructure is OutputStructure)
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

    Func<Structure, float, bool> WorkOnStructure {
        get {
            if (Home is ServiceStructure)
                return ((ServiceStructure)Home).WorkOnTarget;
            return null;
        }
    }

    Action<Worker> cbWorkerChanged;
    Action<Worker> cbWorkerDestroy;
    Action<Worker, string> cbSoundCallback;
    bool hasRegistered;
    //TODO sound
    string soundWorkName = "";//idk how to load/read this in? has this the workstructure not worker???
    #endregion
    #region readInVariables
    bool hasToFollowRoads;
    private bool isDone;
    #endregion
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
    public Worker(Structure Home, OutputStructure structure, float workTime = 1f, Item[] toGetItems = null, bool hasToFollowRoads = true) {
        this.Home = Home;
        WorkOutputStructure = structure;
        this.hasToFollowRoads = hasToFollowRoads;
        if (structure is MarketStructure == false) {
            structure.outputClaimed = true;
        }
        isAtHome = false;
        goingToWork = true;
        inventory = new Inventory(4);
        doTimer = workTime;
        SetGoalStructure(structure);
        this.toGetItems = toGetItems;
    }
    public Worker(ServiceStructure Home, Structure structure, float workTime, bool hasToFollowRoads = true) {
        this.Home = Home;
        WorkStructure = structure;
        this.hasToFollowRoads = hasToFollowRoads;
        isAtHome = false;
        goingToWork = true;
        doTimer = workTime;
        SetGoalStructure(structure);
    }

    public Worker() {
        SaveController.AddWorkerForLoad(this);
    }
    public void OnWorkStructureDestroy(Structure str) {
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
        if (Home.IsActiveAndWorking == false) {
            GoHome();
        }
        if (hasRegistered == false) {
            WorkStructure.RegisterOnDestroyCallback(OnWorkStructureDestroy);
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
            //if (destTile != null) {
            //	if(destTile.Structure is OutputStructure)
            //		SetGoalStructure ((OutputStructure)destTile.Structure);
            //}
            //theres no goal so delete it after some time?
            Debug.Log("worker has no goal");
            GoHome();
            return;
        }

        //do the movement 
        
        path.Update_DoMovement(deltaTime);

        cbWorkerChanged?.Invoke(this);

        if (path.IsAtDestination == false) {
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
            if(toGetItems != null) {
                DropOffItems(deltaTime);
            } else {
                isAtHome = true;
            }
        }
    }
    public void DropOffItems(float deltaTime) {
        doTimer -= deltaTime;
        if (doTimer > 0) {
            return;
        }
        if (Home is MarketStructure) {
            ((MarketStructure)Home).City.Inventory.AddIventory(inventory);
        }
        else
        if (Home is ProductionStructure) {
            ((ProductionStructure)Home).AddToIntake(inventory);
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
        WorkStructure = null;
        SetGoalStructure(Home,true); //todo: think about some optimisation for just "reverse path"
        //doTimer = workTime / 2;
    }
    public void DoWork(float deltaTime) {
        if (WorkStructure == null && path.DestTile != null) {
            WorkStructure = path.DestTile.Structure;
        }
        //we are here at the job tile
        //do its job -- get the items in tile
        if (WorkOutputStructure is OutputStructure) {
            DoOutPutStructureWork(deltaTime);
        } else 
        if(WorkOnStructure != null) {
            DoWorkOnStructure(deltaTime);
        } else {
            Debug.LogError("Worker has nothing todo -- why does he exist? He is from " + Home.ToString() + "! Killing him now.");
            Destroy();
        }
        if(isDone) {
            GoHome();
        }
    }

    private void DoWorkOnStructure(float deltaTime) {
        isDone = WorkOnStructure(WorkStructure,deltaTime);
    }

    public void DoOutPutStructureWork(float deltaTime) {
        doTimer -= deltaTime;
        if (doTimer > 0) {
            cbSoundCallback?.Invoke(this, soundWorkName);
            return;
        }
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
            if(path == null)
                path = new TilesPathfinding();
            if (goHome == false) {
                ((TilesPathfinding)path).SetDestination(new List<Tile>(Home.NeighbourTiles), new List<Tile>(structure.NeighbourTiles));
            } else {
                ((TilesPathfinding)path).SetDestination(new List<Tile>() { path.CurrTile }, new List<Tile>(Home.NeighbourTiles));
            }
        }
        else {
            if (path == null)
                path = new RoutePathfinding();
            if(goHome == false) {
                ((RoutePathfinding)path).SetDestination(Home.RoadsAroundStructure(), structure.RoadsAroundStructure());
            } else {
                ((RoutePathfinding)path).SetDestination(new List<Tile>() { path.CurrTile }, new List<Tile>(Home.RoadsAroundStructure()));
            }
        }
        _workStructure = structure;
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

    public void RegisterOnSoundCallback(Action<Worker, string> cb) {
        cbSoundCallback += cb;
    }

    public void UnregisterOnSoundCallback(Action<Worker, string> cb) {
        cbSoundCallback -= cb;
    }

}
