using UnityEngine;
using System.Collections;
using System;

public class Worker {

	Structure myHome;
	Pathfinding path;
	public bool isAtHome;
	float workTime = 1f;
	float doTimer;
	Inventory inventory;
	ProductionBuilding workStructure;
	Action<Worker> cbWorkerChanged;
	Action<Worker> cbWorkerDestroy;
	bool goingToWork;

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
	public Worker(Structure myHome, ProductionBuilding structure){
		this.myHome = myHome;
		workStructure = structure;

		structure.outputClaimed = true;
		isAtHome = false;
		goingToWork = true;
		inventory = new Inventory (4,false);
		doTimer = workTime;
		AddJobStructure(structure);
	}
	public void Update(float deltaTime){
		if(path == null){
			return;
		}
		float moving = path.Update_DoMovement (deltaTime).magnitude;
		if(cbWorkerChanged != null)
			cbWorkerChanged(this);
		if ( moving > 0) {
//				transform.Translate (move, Space.World);
//			_x += move.x;
//			_y += move.y;
			return;
		}
		// coming home from doing the work
		// drop off the items its carrying
		if (goingToWork == false) {
			if (myHome is MarketBuilding) {
				((MarketBuilding)myHome).city.myInv.addIventory (inventory);
			}
			doTimer -= deltaTime;
			if (doTimer > 0) {
				return;
			}
			isAtHome = true;
			path = null;
			return;
		}
		//we are here at the job tile
		//do its job -- get the items in tile
		doTimer -= deltaTime;
		if (doTimer > 0) {
			return;
		}
		if (workStructure != null) {
			foreach (Item item in workStructure.getOutput ()) {
				inventory.addItem (item);
			}
			workStructure.outputClaimed = false;
			doTimer = workTime/2;
			goingToWork = false;
			path.Reverse ();
			path.IsAtDest = false;
		}

		
	}
	public void Destroy() {
		if(cbWorkerDestroy != null)
			cbWorkerDestroy(this);
	}
	public void AddJobStructure(ProductionBuilding structure){
		if(structure == null){
			return;
		}
		goingToWork = true;
		//job_dest_tile = tile;
		path = new Pathfinding (myHome.roadsAroundStructure (),structure.roadsAroundStructure (),1.5f);
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

}
