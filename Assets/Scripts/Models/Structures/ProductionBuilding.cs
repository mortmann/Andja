using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class ProductionBuilding : UserStructure {


	public Item[] intake;
	public int[] needIntake;
	public int[] maxIntake;
	public bool outputClaimed;
	public float produceTime;
	public float produceCountdown;
	public Item[] output;
	public string growable;
	public int[] inputStorage;
	public int[] outputStorage;
	public int maxOutputStorage;
	Action<Structure> cbOutputChange;
	public int growableReadyCount;
	Queue<Structure> workingGrowables;

	public float Efficiency{
		get {
			return Mathf.Round(((float)OnRegisterCallbacks / (float)myRangeTiles.Count)*1000)/10f;
		}
	}
	public int OnRegisterCallbacks;

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
	public ProductionBuilding(string name,string growable, float time, Item[] output , int tileWidth, int tileHeight,int buildcost,Item[] buildItems,int maintenancecost,bool hasHitbox=true, bool mustBeBuildOnShore=false) {
		this.name = name;
		this.produceTime = time;
		this.output = output;
		this.maxOutputStorage = 5;
		this.tileWidth = tileWidth;
		this.tileHeight = tileHeight;
		this.mustBeBuildOnShore = mustBeBuildOnShore;
		this.maintenancecost = maintenancecost;
		this.hasHitbox = hasHitbox;
		this.myBuildingTyp = BuildingTyp.Production;
		this.BuildTyp = BuildTypes.Single;
		this.growable = growable;
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
		this.growable = str.growable;
		this.myBuildingTyp = BuildingTyp.Production;
	}


	public override Structure Clone (){
		return new ProductionBuilding(this);
	}


	public override void update (float deltaTime){
		if(needIntake == null && output == null){
			return;
		}
		if(needIntake == null && growableReadyCount==0){
			return;
		}
		if (needIntake != null) {
			for (int i = 0; i < intake.Length; i++) {
				if (needIntake[i] > intake[i].count) {
					return;
				}
			}
		}

		if (output != null) {
			for (int i = 0; i < output.Length; i++) {
				if (output[i].count == maxOutputStorage) {
					return;
				}
			}
		}
		produceCountdown -= deltaTime;
		if(produceCountdown <= 0) {
			produceCountdown = produceTime;
			if (needIntake != null) {
				for (int i = 0; i < intake.Length; i++) {
					intake[i].count--;
				}
			}
			if (output != null) {
				for (int i = 0; i < output.Length; i++) {
					output[i].count++;
					if(growable != null){
						Growable g = (Growable)workingGrowables.Dequeue ();
						growableReadyCount--;
						((Growable)g).Reset ();
					}

					if (cbOutputChange != null) {
						cbOutputChange (this);
					}
				}
			}
		}
	}

	public bool addToIntake (Item toAdd){
		if(intake == null){
			return false;
		}
		for(int i = 0; i < intake.Length; i++) {
			if(intake[i].ID == toAdd.ID) {
				if((intake[i].count+ toAdd.count) >= maxIntake[i]) {
					return false;
				}
				callbackIfnotNull ();
				intake[i].count += toAdd.count;
			}
		}
		return true;
	}
	public Item[] getOutput(){
		Item[] temp = new Item[output.Length];
		for (int i = 0; i < output.Length; i++) {
			temp [i] = output [i].CloneWithCount ();
			output[i].count= 0;
		}
		return temp;
	}
	public Item getOneOutput() {
		if(output == null){
			return null;
		}
		for (int i = 0; i < output.Length; i++) {
			if (output[i].count > 0) {
				callbackIfnotNull ();
				Item temp = output [i].CloneWithCount();
				output [i].count = 0;
				return temp;
			}
		}
		return null;
	}
	public void RegisterOutputChanged(Action<Structure> callbackfunc) {
		cbOutputChange += callbackfunc;
	}

	public void UnregisterOutputChanged(Action<Structure> callbackfunc) {
		cbOutputChange -= callbackfunc;
	}
	public override void OnBuild(){
		growable = "tree";
		workingGrowables = new Queue<Structure> ();
		if(intake != null){
			return;		
		}
		if(growable == null | growable == ""){
			return;
		}
		GameObject.FindObjectOfType<BuildController> ().BuildOnTile (3, myRangeTiles);
		// if we are here this produces only 
		// and it has a growable to "plant"
		foreach (Tile rangeTile in myRangeTiles) {
			if(rangeTile.structures != null){
				if(rangeTile.structures.name.Contains (growable)){
					rangeTile.structures.RegisterOnChangedCallback (OnGrowableChanged);	
					OnRegisterCallbacks++;
					if(((Growable)rangeTile.structures).hasProduced == true){
						growableReadyCount ++;
						workingGrowables.Enqueue (rangeTile.structures);
					}
				}
			}
		}
		foreach(Tile rangeTile in myRangeTiles){
			rangeTile.RegisterTileStructureChangedCallback (OnTileStructureChange);
		}
	}

	public void OnGrowableChanged(Structure str){
		if(str is Growable == false){
			return;
		}
		if(str.name != growable){
			return;
		}
		if(((Growable)str).hasProduced == false){
			return;
		}
		workingGrowables.Enqueue (str);
		growableReadyCount ++;
		// send worker todo this job
		// not important right now
	}
	public void OnTileStructureChange(Tile t, Structure old){
		if(old != null && old.name == growable){
			OnRegisterCallbacks--;
		}
		if(t.structures == null){
			return;
		}
		if(t.structures.name == growable){
			OnRegisterCallbacks++;
			t.structures.RegisterOnChangedCallback (OnGrowableChanged);	
		}
	}

	public override void WriteXml (XmlWriter writer){
		BaseWriteXml (writer);
		writer.WriteAttributeString("OutputClaimed", outputClaimed.ToString () );
		writer.WriteAttributeString("ProduceCountdown", produceCountdown.ToString () );
		if (inputStorage != null) {
			writer.WriteStartElement ("Inputs");
			foreach (int i in inputStorage) {
				writer.WriteStartElement ("InputStorage");
				writer.WriteAttributeString ("amount", i.ToString ());
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}
		if (outputStorage != null) {
			writer.WriteStartElement ("Outputs");
			foreach (int i in outputStorage) {
				writer.WriteStartElement ("OutputStorage");
				writer.WriteAttributeString ("amount", i.ToString ());
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}
	}
	public override void ReadXml(XmlReader reader) {
		BaseReadXml (reader);
		outputClaimed = bool.Parse (reader.GetAttribute("OutputClaimed"));
		produceCountdown = float.Parse( reader.GetAttribute("ProduceCountdown") );
		int input= 0;
		if(reader.ReadToDescendant("Inputs") ) {
			do {
				inputStorage[input] = int.Parse( reader.GetAttribute("amount") );
				input++;
			} while( reader.ReadToNextSibling("InputStorage") );
		}
		int output= 0;
		if(reader.ReadToDescendant("Outputs") ) {
			do {
				outputStorage[output] = int.Parse( reader.GetAttribute("amount") );
				output++;
			} while( reader.ReadToNextSibling("OutputStorage") );
		}
	}
}
