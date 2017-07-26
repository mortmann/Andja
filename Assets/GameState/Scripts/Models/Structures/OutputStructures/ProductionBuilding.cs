using UnityEngine;
using System.Collections.Generic;
using System;

using Newtonsoft.Json;

public class ProductionPrototypeData : OutputPrototypData {
	public int[] maxIntake;
	public Item[] intake;
}


[JsonObject(MemberSerialization.OptIn)]
public class ProductionBuilding : OutputStructure {

	#region Serialize
	private Item[] _intake;
	[JsonPropertyAttribute] public Item[] MyIntake {
		get {
			if(_intake == null){
				_intake = ProductionData.intake;
			}
			return _intake;
		}
		set {
			_intake = value;
		}
	}

	#endregion
	#region RuntimeOrOther

	public Dictionary<OutputStructure,Item[]> RegisteredStructures;
	MarketBuilding nearestMarketBuilding;
//	public int[] needIntake { get  { return ProductionData.needIntake; }}
	public int[] maxIntake { get  { return ProductionData.maxIntake; }}
	#endregion

	protected ProductionPrototypeData _productionData;
	public ProductionPrototypeData  ProductionData {
		get { if(_productionData==null){
				_productionData = (ProductionPrototypeData)PrototypController.Instance.GetStructurePrototypDataForID (ID);
			}
			return _productionData;
		}
	}

	public override float Efficiency{
		get {
			float inputs=0;
			for (int i = 0; i < MyIntake.Length; i++) {
				inputs += MyIntake[0].count/ProductionData.intake[i].count;
			}
			if(inputs==0){
				return 0;
			}
			return Mathf.Clamp(Mathf.Round(inputs*1000)/10f,0,100);
		}
	}

	public ProductionBuilding(int id,ProductionPrototypeData  ProductionData) {
		this.ID = id;
		this._productionData = ProductionData;
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	protected ProductionBuilding(){
	}

	protected ProductionBuilding(ProductionBuilding str){
		OutputCopyData (str);
	}


	public override Structure Clone (){
		return new ProductionBuilding(this);
	}
		
	public override void update (float deltaTime){
		if(ProductionData.intake == null && output == null){
			return;
		}
		if(ProductionData.intake == null){
			return;
		}

		for (int i = 0; i < output.Length; i++) {
			if (output[i].count == maxOutputStorage) {
				return;
			}
		}

		base.update_Worker (deltaTime);

		for (int i = 0; i < MyIntake.Length; i++) {
			if (ProductionData.intake[i].count > MyIntake [i].count) {
				return;
			}
		}

		produceCountdown -= deltaTime;
		Debug.Log ("prod" + produceCountdown); 
		if(produceCountdown <= 0) {
			produceCountdown = produceTime;
			for (int i = 0; i < MyIntake.Length; i++) {
				MyIntake[i].count--;
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
		for (int i = 0; i < MyIntake.Length; i++) {
			if (maxIntake[i] > MyIntake[i].count) {
				needItems.Add ( MyIntake [i].Clone (),maxIntake[i]-MyIntake[i].count );
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
		if(MyIntake == null){
			return false;
		}
		for(int i = 0; i < MyIntake.Length; i++) {
			if((MyIntake[i].count+ toAdd.GetAmountForItem(MyIntake[i])) > maxIntake[i]) {
				return false;
			}
			Debug.Log (toAdd.GetAmountForItem(MyIntake[i]));
			MyIntake[i].count += toAdd.GetAmountForItem(MyIntake[i]);
			toAdd.setItemCountNull (MyIntake[i]);
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
			for (int s = 0; s < MyIntake.Length; s++) {
				if (output [i].ID == MyIntake [s].ID) {
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
