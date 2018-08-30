using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Worker {
	#region Serialize
	[JsonPropertyAttribute] public OutputStructure myHome;
	[JsonPropertyAttribute] Pathfinding path;
	[JsonPropertyAttribute] float doTimer;
	[JsonPropertyAttribute] Item[] toGetItems;
	//[JsonPropertyAttribute] int[] toGetAmount;
	[JsonPropertyAttribute] Inventory inventory;
	[JsonPropertyAttribute] bool goingToWork;
	[JsonPropertyAttribute] public bool isAtHome;
    [JsonPropertyAttribute] private OutputStructure _workStructure;

    #endregion
    #region runtimeVariables
    public OutputStructure WorkStructure {
        set {
            _workStructure = value;
        }
        get {
            return _workStructure;
        }
    }
	Action<Worker> cbWorkerChanged;
	Action<Worker> cbWorkerDestroy;
	Action<Worker, string> cbSoundCallback;
    bool hasRegistered;
	//TODO sound
	string soundWorkName="";//idk how to load/read this in? has this the workstructure not worker???
	#endregion
	#region readInVariables
	bool hasToFollowRoads;
	float workTime = 1f;
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
	public float Z {
		get {
			return path.Y;
		}
	}
	public Worker(OutputStructure myHome, OutputStructure structure,Item[] toGetItems = null, bool hasToFollowRoads = true){
		if(myHome is OutputStructure ==false){
			Debug.LogError ("Home is not OutputStructure--if this should be possible redesign");
			return;
		}
		this.myHome = myHome;
		WorkStructure = structure;
		this.hasToFollowRoads = hasToFollowRoads;
		if (structure is MarketBuilding == false) {
			structure.outputClaimed = true;
		}
		isAtHome = false;
		goingToWork = true;
		inventory = new Inventory (4);
		doTimer = workTime;
		SetGoalStructure(structure);
		this.toGetItems = toGetItems;
	}
	//public Worker(OutputStructure myHome, OutputStructure structure,Item[] toGetItems,int[] toGetAmount, bool hasToFollowRoads = false){
	//	if(myHome is OutputStructure ==false){
	//		Debug.LogError ("Home is not OutputStructure--if this should be possible redesign");
	//		return;
	//	}
	//	this.myHome = myHome;
 //       WorkStructure = structure;
	//	this.toGetAmount = toGetAmount;
	//	this.hasToFollowRoads = hasToFollowRoads;
	//	if (structure is MarketBuilding == false) {
	//		structure.outputClaimed = true;
	//	}
	//	isAtHome = false;
	//	goingToWork = true;
	//	inventory = new Inventory (4);
	//	doTimer = workTime;
	//	SetGoalStructure(structure);
	//	this.toGetItems = toGetItems;
	//}
	//public Worker(OutputStructure myHome, bool hasToFollowRoads = true){
	//	if(myHome is OutputStructure ==false){
	//		Debug.LogError ("Home is not OutputStructure--if this should be possible redesign");
	//		return;
	//	}
	//	this.myHome = myHome;		
	//	isAtHome = false;
	//	goingToWork = true;
	//	inventory = new Inventory (4);
	//	doTimer = workTime;
	//}
	public Worker(){
		SaveController.AddWorkerForLoad (this);
    }
    public void OnWorkStructureDestroy(Structure str) {
        if(str != WorkStructure) {
            Debug.LogError("OnWorkStructureDestroy called on not workstructure destroy!");
            return;
        }
        WorkStructure = null;
        GoHome();
    }
	public void Update(float deltaTime){
		if(myHome==null){
			Debug.LogError ("worker has no myHome -> for now set it manually");
			return;
		}
		if(myHome.isActive==false){
			GoHome ();
		} 
		if(myHome.Efficiency <= 0){
			GoHome ();
		}
        if (hasRegistered == false) {
            _workStructure.RegisterOnDestroyCallback(OnWorkStructureDestroy);
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
        if (path == null){
			//if (destTile != null) {
			//	if(destTile.Structure is OutputStructure)
			//		SetGoalStructure ((OutputStructure)destTile.Structure);
			//}
			//theres no goal so delete it after some time?
			Debug.Log ("worker has no goal");
            GoHome();
			return;
		}		

		//do the movement 
		path.Update_DoMovement (deltaTime);

        cbWorkerChanged?.Invoke(this);

        if (path.IsAtDestination==false) {
			return;
		}
		if (goingToWork) {
            //if we are here this means we're
            //AT the destination and can start working
            DoWork(deltaTime);
		} else {
            // coming home from doing the work
            // drop off the items its carrying
            DropOffItems(deltaTime);
        }
	}
	public void DropOffItems(float deltaTime){
		doTimer -= deltaTime;
		if (doTimer > 0) {
			return;
		}
		if (myHome is MarketBuilding) {
			((MarketBuilding)myHome).City.inventory.AddIventory (inventory);
		} else
		if (myHome is ProductionBuilding) {
			((ProductionBuilding)myHome).AddToIntake (inventory); 
		} else {
			//this home is a OutputBuilding or smth that takes it to output
			myHome.AddToOutput (inventory);
		}
		isAtHome = true;
		path = null;
	}
	public void GoHome() {
		SetGoalStructure (myHome);
	}
	public void DoWork(float deltaTime){
		//we are here at the job tile
		//do its job -- get the items in tile
		doTimer -= deltaTime;
		if (doTimer > 0) {
            cbSoundCallback?.Invoke(this, soundWorkName);
            return;
		}
		if(WorkStructure == null&& path.DestTile != null&& path.DestTile.Structure is OutputStructure){
            WorkStructure = (OutputStructure)path.DestTile.Structure;
		}
		if (WorkStructure != null) {
			if (toGetItems == null) {
				foreach (Item item in WorkStructure.GetOutput ()) {
					inventory.AddItem (item);
				}
			} 
			if (toGetItems != null) {
				foreach (Item item in WorkStructure.GetOutputWithItemCountAsMax (toGetItems)) {
					inventory.AddItem (item);
				}
			}
			if(WorkStructure is MarketBuilding){
				foreach (Item item in WorkStructure.GetOutputWithItemCountAsMax (toGetItems)) {
                    if (item == null) {
                        Debug.LogError("item is null for to get item! Worker is from " +WorkStructure);
                    }
					inventory.AddItem (item);
				}
			}
            WorkStructure.UnregisterOnDestroyCallback(OnWorkStructureDestroy);
            WorkStructure.outputClaimed = false;
			doTimer = workTime/2;
			goingToWork = false;
			path.Reverse ();
		}
//		Debug.Log ("WORK completed!");
	}


	public void Destroy() {
		if (goingToWork)
            WorkStructure.ResetOutputClaimed ();
        cbWorkerDestroy?.Invoke(this);
    }
	public void SetGoalStructure(OutputStructure structure){
		if(structure == null){
			return;
		}
        if (hasToFollowRoads == false) {
			path = new TilesPathfinding ();
			((TilesPathfinding)path).SetDestination (new List<Tile>(myHome.neighbourTiles),new List<Tile>(structure.neighbourTiles));
		} else {
			path = new RoutePathfinding();
			((RoutePathfinding)path).SetDestination (myHome.RoadsAroundStructure (),structure.RoadsAroundStructure ());
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

	public void RegisterOnSoundCallback (Action<Worker, string> cb) {
		cbSoundCallback += cb;
	}

	public void UnregisterOnSoundCallback (Action<Worker, string> cb) {
		cbSoundCallback -= cb;
	}
		
}
