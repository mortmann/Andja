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
	[JsonPropertyAttribute] int[] toGetAmount;
	[JsonPropertyAttribute] Inventory inventory;
	[JsonPropertyAttribute] bool goingToWork;
	[JsonPropertyAttribute] Tile destTile;
	[JsonPropertyAttribute] public bool isAtHome;
	#endregion
	#region runtimeVariables
	public OutputStructure workStructure;
	Tile currTile;
	Action<Worker> cbWorkerChanged;
	Action<Worker> cbWorkerDestroy;
	Action<Worker, string> cbSoundCallback;
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
	public Worker(Structure myHome, OutputStructure structure,Item[] toGetItems = null, bool hasToFollowRoads = true){
		if(myHome is OutputStructure ==false){
			Debug.LogError ("Home is not OutputStructure--if this should be possible redesign");
			return;
		}
		this.myHome = (OutputStructure)myHome;
		workStructure = structure;
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
	public Worker(Structure myHome, OutputStructure structure,Item[] toGetItems,int[] toGetAmount, bool hasToFollowRoads = false){
		if(myHome is OutputStructure ==false){
			Debug.LogError ("Home is not OutputStructure--if this should be possible redesign");
			return;
		}
		this.myHome = (OutputStructure)myHome;
		workStructure = structure;
		this.toGetAmount = toGetAmount;
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
	public Worker(Structure myHome, bool hasToFollowRoads = true){
		if(myHome is OutputStructure ==false){
			Debug.LogError ("Home is not OutputStructure--if this should be possible redesign");
			return;
		}
		this.myHome = (OutputStructure)myHome;		
		isAtHome = false;
		goingToWork = true;
		inventory = new Inventory (4);
		doTimer = workTime;
	}
	public void Update(float deltaTime){
		if(myHome.isActive==false){
			GoHome ();
		} 
		if(myHome.Efficiency <= 0){
			GoHome ();
		}
		//worker can only work if
		// -homeStructure is active
		// -goalStructure can be reached -> search new goal
		// -goalStructure has smth to be worked eg grown/has output
		// -Efficiency of home > 0
		// -home is not full (?) maybe second worker?
		//If any of these are false the worker should return to home
		//except there is no way to home then remove

		if(path == null){
			if (destTile != null) {
				if(destTile.Structure is OutputStructure)
					SetGoalStructure ((OutputStructure)destTile.Structure);
			}
			//theres no goal so delete it after some time?
			Debug.Log ("worker has no goal");
			return;
		}
		//do the movement 
		path.Update_DoMovement (deltaTime);
		if(cbWorkerChanged != null)
			cbWorkerChanged(this);
		if (path.IsAtDest==false) {
			Debug.LogWarning ("if this is not working this is the error ^ was magnitude of move");
			return;
		}
		if (goingToWork == false) {
			// coming home from doing the work
			// drop off the items its carrying
			DropOffItems (deltaTime);
		} else {
			//if we are here this means we're
			//AT the destination and can start working
			DoWork (deltaTime);
		}
	}
	public void DropOffItems(float deltaTime){
		doTimer -= deltaTime;
		if (doTimer > 0) {
			return;
		}
		if (myHome is MarketBuilding) {
			((MarketBuilding)myHome).City.myInv.addIventory (inventory);
		} else
		if (myHome is ProductionBuilding) {
			((ProductionBuilding)myHome).addToIntake (inventory); 
		} else {
			//this home is a OutputBuilding or smth that takes it to output
			myHome.addToOutput (inventory);
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
			if(cbSoundCallback!=null){
				cbSoundCallback (this, soundWorkName);
			}
			return;
		}
		if (workStructure != null) {
			if (toGetItems == null) {
				foreach (Item item in workStructure.getOutput ()) {
					inventory.addItem (item);
				}
			} 
			if(workStructure is MarketBuilding){
				foreach (Item item in workStructure.getOutput (toGetItems,toGetAmount)) {
					inventory.addItem (item);
				}
			}

			workStructure.outputClaimed = false;
			doTimer = workTime/2;
			goingToWork = false;
			path.Reverse ();
			path.IsAtDest = false;
		}
	}


	public void Destroy() {
		if (goingToWork)
			workStructure.resetOutputClaimed ();
		if(cbWorkerDestroy != null)
			cbWorkerDestroy(this);
	}
	public void SetGoalStructure(OutputStructure structure){
		if(structure == null){
			return;
		}
		if(structure!=null){
			goingToWork = true;
		} else {
			goingToWork = false;
		}
		//job_dest_tile = tile;
		if (hasToFollowRoads == false) {
			path_mode pm = path_mode.islandMultipleStartpoints;
			path = new Pathfinding (new List<Tile>(myHome.neighbourTiles),new List<Tile>(structure.neighbourTiles),1.5f,pm);
		} else {
			path = new Pathfinding (myHome.roadsAroundStructure (),structure.roadsAroundStructure (),1.5f);
		}
		if (currTile != null) {
			path.currTile = currTile;
			currTile = null;
			destTile = null;
		}
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
