using UnityEngine;
using System.Collections.Generic;
using System;

using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class ProductionBuilding : OutputStructure {

	#region Serialize


	#endregion
	#region RuntimeOrOther

	public Dictionary<OutputStructure,Item[]> RegisteredStructures;
	public Item[] intake;
	public int[] needIntake;
	public int[] maxIntake;
	MarketBuilding nearestMarketBuilding;

	#endregion



	public override float Efficiency{
		get {
			float inputs=0;
			for (int i = 0; i < intake.Length; i++) {
				inputs += intake[0].count/needIntake[0];
			}
			if(inputs==0){
				return 0;
			}
			return Mathf.Clamp(Mathf.Round(inputs*1000)/10f,0,100);
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
		maxIntake= new int[needIntake.Length];
		if (intake != null && needIntake!=null) {
			int i=0;
			foreach(int needed in needIntake){
				this.maxIntake[i] = 5*needed; // make it 5 times the needed
				i++;
			}
		}
		maxNumberOfWorker = 1;
		this.mustBeBuildOnShore = mustBeBuildOnShore;
		this.maintenancecost = maintenancecost;
		this.hasHitbox = hasHitbox;
		this.myBuildingTyp = BuildingTyp.Blocking;
		BuildTyp = BuildTypes.Single;
		this.canTakeDamage = true;

	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	protected ProductionBuilding(){
	}

	protected ProductionBuilding(ProductionBuilding str){
		this.canTakeDamage = str.canTakeDamage;
		this.ID = str.ID;
		this.name = str.name;
		this.intake = str.intake;
		this.needIntake = str.needIntake;
		this.produceTime = str.produceTime;
		this.produceCountdown =  str.produceTime;
		this.output = str.output;
		this.maxOutputStorage = str.maxOutputStorage;
		this.maxNumberOfWorker = str.maxNumberOfWorker;
		this.tileWidth = str.tileWidth;
		this.tileHeight = str.tileHeight;
		this.maxIntake = str.maxIntake;
		this.mustBeBuildOnShore = str.mustBeBuildOnShore;
		this.maintenancecost = str.maintenancecost;
		this.buildcost = str.buildcost;
		this.BuildTyp = str.BuildTyp;
		this.rotated = str.rotated;
		this.hasHitbox = str.hasHitbox;
		this.myBuildingTyp = BuildingTyp.Blocking;
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

		base.update_Worker (deltaTime);

		for (int i = 0; i < intake.Length; i++) {
			if (needIntake [i] > intake [i].count) {
				return;
			}
		}

		produceCountdown -= deltaTime;
		Debug.Log ("prod" + produceCountdown); 
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

	public override void SendOutWorkerIfCan (){
		if(myWorker.Count >= maxNumberOfWorker || jobsToDo.Count == 0 && nearestMarketBuilding == null){
			return;
		}
		Dictionary<Item,int> needItems = new Dictionary<Item, int> ();
		for (int i = 0; i < intake.Length; i++) {
			if (maxIntake[i] > intake[i].count) {
				needItems.Add ( intake [i].Clone (),maxIntake[i]-intake[i].count );
			}
		}
		if(needItems.Count == 0){
			return;
		}
		List<Item> getItems = new List<Item> ();
		List<int> ints = new List<int> ();
		OutputStructure goal = null;
		if (jobsToDo.Count == 0 && nearestMarketBuilding != null) {
			getItems = new List<Item>(needItems.Keys);
			for (int i = 0; i < getItems.Count; i++) {
				if(City.hasItem (getItems[i]) == false){
					needItems.Remove (getItems[i]);

				}
			}
			goal = nearestMarketBuilding;

			ints = new List<int>(needItems.Values);
		} else {
			foreach (OutputStructure ustr in jobsToDo.Keys) {
				goal = ustr;
				for (int i = 0; i < jobsToDo[ustr].Length; i++) {
					getItems.Add (jobsToDo[ustr][i]);
					ints.Add (needItems[jobsToDo[ustr][i]]);
				}
				break;
			}
		}
		if(goal == null || getItems == null){
			Debug.Log ("no goal or items");
			return;
		}
		myWorker.Add (new Worker(this,goal,getItems.ToArray (),ints.ToArray (),false));
		WorldController.Instance.world.CreateWorkerGameObject (myWorker[0]);

	}
	public void OnOutputChangedStructure(Structure str){
		if(str is OutputStructure == false){
			return;
		}
		if(jobsToDo.ContainsKey((OutputStructure)str)){
			jobsToDo.Remove ((OutputStructure)str);
		}
		OutputStructure ustr = ((OutputStructure)str);
		List<Item> getItems = new List<Item> ();
		List<Item> items = new List<Item> (ustr.output);
		foreach (Item item in RegisteredStructures[(OutputStructure)str]) {
			Item i = items.Find (x => x.ID == item.ID);
			if(i.count > 0){
				getItems.Add (i);
			}
		}
		if (((OutputStructure)str).outputClaimed == false) {
			jobsToDo.Add (ustr,getItems.ToArray());
		}

	}
	public bool addToIntake (Inventory toAdd){
		if(intake == null){
			return false;
		}
		for(int i = 0; i < intake.Length; i++) {
			if((intake[i].count+ toAdd.GetAmountForItem(intake[i])) > maxIntake[i]) {
				return false;
			}
			Debug.Log (toAdd.GetAmountForItem(intake[i]));
			intake[i].count += toAdd.GetAmountForItem(intake[i]);
			toAdd.setItemCountNull (intake[i]);
			callbackIfnotNull ();
		}

		return true;
	}

	public override void OnBuild(){
		myWorker = new List<Worker> ();
		jobsToDo = new Dictionary<OutputStructure, Item[]> ();
//		for (int i = 0; i < intake.Length; i++) {
//			intake [i].count = maxIntake [i];
//		}
		foreach(Tile rangeTile in myRangeTiles){
			if(rangeTile.Structure == null){
				continue;
			}
			if(rangeTile.Structure is OutputStructure){
				if (rangeTile.Structure is MarketBuilding) {
					findNearestMarketBuilding (rangeTile);
					continue;
				}
				if (RegisteredStructures.ContainsKey ((OutputStructure)rangeTile.Structure) == false) {
					Item[] items = hasNeedItem (((OutputStructure)rangeTile.Structure).output);
					if(items.Length == 0){
						continue;
					}
					((OutputStructure)rangeTile.Structure).RegisterOutputChanged (OnOutputChangedStructure);
					RegisteredStructures.Add ((OutputStructure)rangeTile.Structure,items);
				}
			}

		}
		City.RegisterStructureAdded (OnStructureBuild);
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
		if (str is OutputStructure == false) {
			return;
		}
		bool inRange = false;
		for (int i = 0; i < str.myBuildingTiles.Count; i++) {
			if (myRangeTiles.Contains (str.myBuildingTiles [i]) == true) {
				inRange = true;
				break;
			}
		}
		if(inRange == false){
			return;
		}
		if (str is MarketBuilding) {
			findNearestMarketBuilding(str.BuildTile);
			return;
		}
		Item[] items = hasNeedItem(((OutputStructure)str).output);
		if(items.Length > 0 ){
			((OutputStructure)str).RegisterOutputChanged (OnOutputChangedStructure);
		}
	}
	public void findNearestMarketBuilding(Tile tile){
		if (tile.Structure is MarketBuilding) {
			if (nearestMarketBuilding == null) {
				nearestMarketBuilding =(MarketBuilding) tile.Structure;
			} else {
				float firstDistance = nearestMarketBuilding.middleVector.magnitude - middleVector.magnitude;
				float secondDistance = tile.Structure.middleVector.magnitude - middleVector.magnitude;
				if (Mathf.Abs (secondDistance) < Mathf.Abs (firstDistance)) {
					nearestMarketBuilding =(MarketBuilding) tile.Structure;
				}
			}
		}
	}


}
