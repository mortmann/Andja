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
	public int[] inputStorage;

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

	public override void OnBuild(){
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
