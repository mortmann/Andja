﻿using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Worker : IXmlSerializable{

	Structure myHome;
	Pathfinding path;
	public bool isAtHome;
	float workTime = 1f;
	float doTimer;
	Inventory inventory;
	ProductionBuilding workStructure;
	Tile destTile;
	Tile currTile;
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
	public Worker(Structure myHome){
		this.myHome = myHome;
		isAtHome = false;
		goingToWork = true;
		inventory = new Inventory (4,false);
		doTimer = workTime;
	}
	public void Update(float deltaTime){
		if(path == null){
			if (destTile != null) {
				if(destTile.structures is ProductionBuilding)
					AddJobStructure ((ProductionBuilding)destTile.structures);
			}
			//theres no goal so delete it after some time?
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
	//////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 						SAVING & LOADING
	/// 
	//////////////////////////////////////////////////////////////////////////////////////
	public XmlSchema GetSchema() {
		return null;
	}
	public void WriteXml(XmlWriter writer){

		writer.WriteStartElement("Inventory");
		inventory.WriteXml(writer);
		writer.WriteEndElement();
		writer.WriteAttributeString("currTile_X", path.currTile.X.ToString () );
		writer.WriteAttributeString("currTile_Y", path.currTile.Y.ToString () );
		writer.WriteAttributeString("destTile_X", workStructure.JobTile.X.ToString () );
		writer.WriteAttributeString("destTile_Y", workStructure.JobTile.Y.ToString () );
		writer.WriteAttributeString("goingToWork", goingToWork.ToString () );

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
	}
}
