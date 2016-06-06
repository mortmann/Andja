using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class ProductionBuilding : UserStructure {
	public Dictionary<Structure,Item[]> RegisteredStructures;
	public Item[] intake;
	public int[] needIntake;
	public int[] maxIntake;
	public int[] inputStorage;
	MarketBuilding nearestMarketBuilding;

	public override float Efficiency{
		get {
			float inputs=0;
			for (int i = 0; i < inputStorage.Length; i++) {
				inputs += inputStorage[0]/needIntake[0];
			}
			if(inputs==0){
				return 0;
			}
			return Mathf.Round(inputs*1000)/10f;
		}
	}

	public ProductionBuilding(int id,string name,Item[] intake, int[] needIntake, float produceTime, Item[] output, int tileWidth, int tileHeight,int buildcost,Item[] buildItems,int maintenancecost,bool hasHitbox=true, bool mustBeBuildOnShore=false) {
		this.ID = id;
		this.name = name;
		this.intake = intake;
		this.needIntake = needIntake;
		this.produceTime = produceTime;
		this.output = output;
		this.maxOutputStorage = 5; // hardcoded 5 ? need this to change?
		this.tileWidth = tileWidth;
		this.tileHeight = tileHeight;
		if (intake != null && needIntake!=null) {
			int i=0;
			foreach(int needed in needIntake){
				this.maxIntake[i] = 5*needed; // make it 5 times the needed
				i++;
			}
		}
		this.mustBeBuildOnShore = mustBeBuildOnShore;
		this.maintenancecost = maintenancecost;
		this.hasHitbox = hasHitbox;
		this.myBuildingTyp = BuildingTyp.Production;
		BuildTyp = BuildTypes.Single;
	}
	protected ProductionBuilding(){
	}

	protected ProductionBuilding(ProductionBuilding str){
		this.ID = str.ID;
		this.name = str.name;
		this.intake = str.intake;
		this.needIntake = str.needIntake;
		this.produceTime = str.produceTime;
		this.produceCountdown =  str.produceTime;
		this.output = str.output;
		this.maxOutputStorage = str.maxOutputStorage;
		this.tileWidth = str.tileWidth;
		this.tileHeight = str.tileHeight;
		this.maxIntake = str.maxIntake;
		this.mustBeBuildOnShore = str.mustBeBuildOnShore;
		this.maintenancecost = str.maintenancecost;
		this.buildcost = str.buildcost;
		this.BuildTyp = str.BuildTyp;
		this.rotated = str.rotated;
		this.hasHitbox = str.hasHitbox;
		this.myBuildingTyp = BuildingTyp.Production;
	}


	public override Structure Clone (){
		return new ProductionBuilding(this);
	}


	public override void update (float deltaTime){
		if(needIntake == null && output == null){
			return;
		}
		if(needIntake == null){
			return;
		}

		for (int i = 0; i < output.Length; i++) {
			if (output[i].count == maxOutputStorage) {
				return;
			}
		}

		for (int i = 0; i < intake.Length; i++) {
			if (needIntake[i] > intake[i].count) {
				UserStructure jobStructure = GetJobForWorker (intake[i],needIntake[i]-intake[i].count);
				if (jobStructure == null) {
					continue;
				}
				myWorker.Add (new Worker(this));
				return;
			}
		}



		produceCountdown -= deltaTime;
		if(produceCountdown <= 0) {
			produceCountdown = produceTime;
			for (int i = 0; i < intake.Length; i++) {
				intake[i].count--;
			}
			for (int i = 0; i < output.Length; i++) {
				output[i].count++;

				if (cbOutputChange != null) {
					cbOutputChange (this);
				}
			}
		}
	}

	public UserStructure GetJobForWorker (Item neededIntake,int amount){
		if(jobsToDo.Count == 0 && nearestMarketBuilding == null){
			return null;
		}
		if (jobsToDo.Count == 0) {
			return nearestMarketBuilding;
		}
		foreach (UserStructure item in jobsToDo.Keys) {
			return item;
		}
		Debug.LogError ("GetJobForWorker - This should never occur!");
		return null;
	}
	public void OnOutputChangedStructure(Structure str){
		if(str is UserStructure == false){
			return;
		}
		UserStructure ustr = ((UserStructure)str);
		List<Item> getItems = new List<Item> ();
		List<Item> items = new List<Item> (ustr.output);
		foreach (Item item in RegisteredStructures[str]) {
			Item i = items.Find (x => x.ID == item.ID);
			if(i.count > 0){
				getItems.Add (i);
			}
		}
		if (((UserStructure)str).outputClaimed == false) {
			jobsToDo.Add (ustr,getItems.ToArray());
		}

	}
	public bool addToIntake (Inventory toAdd){
		if(intake == null){
			return false;
		}
		foreach (Item item in toAdd.items.Values) {
			for(int i = 0; i < intake.Length; i++) {
				if(intake[i].ID == item.ID) {
					if((intake[i].count+ item.count) >= maxIntake[i]) {
						return false;
					}
					intake[i].count += item.count;
					callbackIfnotNull ();
				}
			}
		}
		return true;
	}

	public override void OnBuild(){
		jobsToDo = new Dictionary<UserStructure, Item[]> ();
		foreach(Tile rangeTile in myRangeTiles){
			if(rangeTile.Structure == null){
				continue;
			}
			if(rangeTile.Structure is UserStructure){
				if (rangeTile.Structure is MarketBuilding) {
					findNearestMarketBuilding (rangeTile);
					continue;
				}
				if (RegisteredStructures.ContainsKey (rangeTile.Structure) == false) {
					Item[] items = hasNeedItem (((UserStructure)rangeTile.Structure).output);
					if(items.Length == 0){
						continue;
					}
					((UserStructure)rangeTile.Structure).RegisterOutputChanged (OnOutputChangedStructure);
					RegisteredStructures.Add (rangeTile.Structure,items);
				}
			}

		}
		BuildController.Instance.RegisterStructureCreated (OnStructureBuild);
	}
	public Item[] hasNeedItem(Item[] output){
		List<Item> items = new List<Item> ();
		for (int i = 0; i < output.Length; i++) {
			for (int s = 0; s < intake.Length; s++) {
				if (output [i].ID == intake [s].ID) {
					items.Add (output [i]);
				}
			}
		}
		return items.ToArray();
	}
	public void OnStructureBuild(Structure str){
		if (str is UserStructure == false) {
			return;
		}
		for (int i = 0; i < str.myBuildingTiles.Count; i++) {
			if (myRangeTiles.Contains (str.myBuildingTiles [i]) == false) {
				return;
			}
		}
		if (str is MarketBuilding) {
			findNearestMarketBuilding(str.BuildTile);
			return;
		}
		Item[] items = hasNeedItem(((UserStructure)str).output);
		if(items.Length > 0 ){
			((UserStructure)str).RegisterOutputChanged (OnOutputChangedStructure);
		}
	}
	public void findNearestMarketBuilding(Tile tile){
		if (tile.Structure is MarketBuilding) {
			if (nearestMarketBuilding == null) {
				nearestMarketBuilding =(MarketBuilding) tile.Structure;
			} else {
				float firstDistance = nearestMarketBuilding.middleVector.magnitude - middleVector.magnitude;
				float secondDistance = tile.Structure.middleVector.magnitude - middleVector.magnitude;
				if (secondDistance < firstDistance) {
					nearestMarketBuilding =(MarketBuilding) tile.Structure;
				}
			}
		}
	}

	public override void WriteXml (XmlWriter writer){
		BaseWriteXml (writer);
		WriteUserXml (writer);
		if (inputStorage != null) {
			writer.WriteStartElement ("Inputs");
			foreach (int i in inputStorage) {
				writer.WriteStartElement ("InputStorage");
				writer.WriteAttributeString ("amount", i.ToString ());
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}

	}
	public override void ReadXml(XmlReader reader) {
		BaseReadXml (reader);
		ReadUserXml (reader);
		int input= 0;
		if(reader.ReadToDescendant("Inputs") ) {
			do {
				inputStorage[input] = int.Parse( reader.GetAttribute("amount") );
				input++;
			} while( reader.ReadToNextSibling("InputStorage") );
		}

	}
}
