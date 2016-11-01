using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Worker : IXmlSerializable {

	public Structure myHome;
	Pathfinding path;
	public bool isAtHome;
	float workTime = 1f;
	float doTimer;
	Inventory inventory;
	public OutputStructure workStructure;
	Tile destTile;
	Tile currTile;
	Action<Worker> cbWorkerChanged;
	Action<Worker> cbWorkerDestroy;
	Action<Worker, string> cbSoundCallback;
	string soundWorkName="";//idk how to load/read this in? has this the workstructure not worker???
	bool hasToFollowRoads;
	bool goingToWork;
	Item[] toGetItems;
	int[] toGetAmount;

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
		this.myHome = myHome;
		workStructure = structure;
		this.hasToFollowRoads = hasToFollowRoads;
		if (structure is MarketBuilding == false) {
			structure.outputClaimed = true;
		}
		isAtHome = false;
		goingToWork = true;
		inventory = new Inventory (4);
		doTimer = workTime;
		AddJobStructure(structure);
		this.toGetItems = toGetItems;
	}
	public Worker(Structure myHome, OutputStructure structure,Item[] toGetItems,int[] toGetAmount, bool hasToFollowRoads = false){
		this.myHome = myHome;
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
		AddJobStructure(structure);
		this.toGetItems = toGetItems;
	}
	public Worker(Structure myHome, bool hasToFollowRoads = true){
		this.myHome = myHome;
		isAtHome = false;
		goingToWork = true;
		inventory = new Inventory (4);
		doTimer = workTime;
	}
	public void Update(float deltaTime){
		if(path == null){
			if (destTile != null) {
				if(destTile.Structure is OutputStructure)
					AddJobStructure ((OutputStructure)destTile.Structure);
			}
			//theres no goal so delete it after some time?
			Debug.Log ("worker has no goal");
			return;
		}
		float moving = path.Update_DoMovement (deltaTime).magnitude;
		if(cbWorkerChanged != null)
			cbWorkerChanged(this);
		if ( moving > 0) {
			return;
		}
		// coming home from doing the work
		// drop off the items its carrying
		if (goingToWork == false ) {
			doTimer -= deltaTime;
			if (doTimer > 0) {
				return;
			}
			if (myHome is MarketBuilding) {
				((MarketBuilding)myHome).City.myInv.addIventory (inventory);
			}
			if (myHome is ProductionBuilding) {
				((ProductionBuilding)myHome).addToIntake (inventory); 
			}
			isAtHome = true;
			path = null;
			return;
		}
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
	public void AddJobStructure(OutputStructure structure){
		if(structure == null){
			return;
		}
		goingToWork = true;
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

	//////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 						SAVING & LOADING
	/// 
	//////////////////////////////////////////////////////////////////////////////////////
	public XmlSchema GetSchema() {
		return null;
	}
	public void WriteXml(XmlWriter writer){
		writer.WriteAttributeString("currTile_X", path.currTile.X.ToString () );
		writer.WriteAttributeString("currTile_Y", path.currTile.Y.ToString () );
		writer.WriteAttributeString("destTile_X", workStructure.JobTile.X.ToString () );
		writer.WriteAttributeString("destTile_Y", workStructure.JobTile.Y.ToString () );
		writer.WriteAttributeString("goingToWork", goingToWork.ToString () );
		writer.WriteStartElement("Inventory");
		inventory.WriteXml(writer);
		writer.WriteEndElement();
	}
	public void ReadXml (XmlReader reader){
		isAtHome = false;
		int dx = int.Parse( reader.GetAttribute("destTile_X") );
		int dy = int.Parse( reader.GetAttribute("destTile_Y") );
		destTile = WorldController.Instance.world.GetTileAt (dx,dy);
		int cx = int.Parse( reader.GetAttribute("currTile_X") );
		int cy = int.Parse( reader.GetAttribute("currTile_Y") );
		currTile = WorldController.Instance.world.GetTileAt (cx,cy);
		goingToWork = bool.Parse (reader.GetAttribute ("goingToWork"));

		reader.ReadToDescendant ("Inventory");
		inventory = new Inventory ();
		inventory.ReadXml (reader);

	}
}
