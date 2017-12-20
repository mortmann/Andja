using UnityEngine;
using System.Collections.Generic;
using System;

using Newtonsoft.Json;

public enum InputTyp { AND, OR }

public class ProductionPrototypeData : OutputPrototypData {
	public Item[] intake;
	public InputTyp myInputTyp;
}
	
[JsonObject(MemberSerialization.OptIn)]
public class ProductionBuilding : OutputStructure {

	#region Serialize
	private Item[] _intake;
	[JsonPropertyAttribute] public Item[] MyIntake {
		get {
			if(_intake == null){
				if(ProductionData.intake==null){
					return null;
				}
				if(ProductionData.myInputTyp == InputTyp.AND){
					_intake = new Item[ProductionData.intake.Length];
					for(int i = 0; i<ProductionData.intake.Length;i++){
						_intake [i] = ProductionData.intake [i].Clone();
					}
				} 
				if(ProductionData.myInputTyp == InputTyp.OR){
					_intake = new Item[1];
					_intake [0] = ProductionData.intake [0].Clone();
				} 
			}
			return _intake;
		}
		set {
			_intake = value;
		}
	}

	#endregion
	#region RuntimeOrOther
	private int _orItemIndex=int.MinValue; //TODO think about to switch to short if it needs to save space 
	public int orItemIndex {
		get {
			if(_orItemIndex==int.MinValue){
				for (int i = 0; i < ProductionData.intake.Length; i++) {
					Item item = ProductionData.intake [i];
					if (item.ID == MyIntake [0].ID) {
						_orItemIndex = i;
					}
				}
			}
			return _orItemIndex;
		}
		set {
			_orItemIndex = value;
		}
	}


	public Dictionary<OutputStructure,Item[]> RegisteredStructures;
	MarketBuilding nearestMarketBuilding;
//	public int[] needIntake { get  { return ProductionData.needIntake; }}
	public InputTyp myInputTyp { get  { return ProductionData.myInputTyp; }}

	#endregion

	protected ProductionPrototypeData _productionData;
	public ProductionPrototypeData  ProductionData {
		get { if(_productionData==null){
				_productionData = (ProductionPrototypeData)PrototypController.Instance.GetStructurePrototypDataForID (ID);
			}
			return _productionData;
		}
	}

//	public override float Efficiency{
//		get {
//			float inputs=0;
//			for (int i = 0; i < MyIntake.Length; i++) {
//				if(ProductionData.intake[i].count==0){
//					Debug.LogWarning(ProductionData.intake[i].ToString() + " INTAKE REQUEST IS 0!!");
//					continue;
//				}
//				inputs += MyIntake[i].count/ProductionData.intake[i].count;
//			}
//			if(inputs==0){
//				return 0;
//			}
//			return Mathf.Clamp(Mathf.Round(inputs*1000)/10f,0,100);
//		}
//	}

	public ProductionBuilding(int id,ProductionPrototypeData  ProductionData) {
		this.ID = id;
		this._productionData = ProductionData;
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	protected ProductionBuilding(){
		RegisteredStructures = new Dictionary<OutputStructure, Item[]> ();
	}

	protected ProductionBuilding(ProductionBuilding str){
		OutputCopyData (str);
	}


	public override Structure Clone (){
		return new ProductionBuilding(this);
	}
		
	public override void update (float deltaTime){
		if(output == null){
			return;
		}
		for (int i = 0; i < output.Length; i++) {
			if (output[i].count == maxOutputStorage) {
				return;
			}
		}

		base.update_Worker (deltaTime);

		if(hasRequiredInput() == false){
			return;
		}
		produceCountdown += deltaTime;
		if(produceCountdown >= produceTime) {
			produceCountdown = 0;
			if(MyIntake!=null){
				for (int i = 0; i < MyIntake.Length; i++) {
					MyIntake[i].count--;
				}
			}
			for (int i = 0; i < output.Length; i++) {
				output[i].count += OutputData.output[i].count;
				if (cbOutputChange != null) {
					cbOutputChange (this);
				}
			}
		}
	}
	public bool hasRequiredInput(){
		if (ProductionData.intake == null) {
			return true;
		}
		if(myInputTyp == InputTyp.AND){
			for (int i = 0; i < MyIntake.Length; i++) {
				if (ProductionData.intake [i].count > MyIntake [i].count) {
					return false;
				}
			}
		}
		else if(myInputTyp == InputTyp.OR){
			if (ProductionData.intake [orItemIndex].count > MyIntake [0].count) {
				return false;
			}
		}
		return true;
	}

	public override void SendOutWorkerIfCan (){
		if(myWorker.Count >= maxNumberOfWorker || jobsToDo.Count == 0 && nearestMarketBuilding == null){
			return;
		}
		Dictionary<Item,int> needItems = new Dictionary<Item, int> ();
		for (int i = 0; i < MyIntake.Length; i++) {
			if (GetMaxIntakeForIntakeIndex(i) > MyIntake[i].count) {
				needItems.Add ( MyIntake [i].Clone (),GetMaxIntakeForIntakeIndex(i)-MyIntake[i].count );
			}
		}
		if(needItems.Count == 0){
			return;
		}
		if (jobsToDo.Count == 0 && nearestMarketBuilding != null) {
			List<Item> getItems = new List<Item> ();
			for (int i = MyIntake.Length - 1; i >= 0; i--) {
				if(City.hasItem (MyIntake[i])){
					Item item = MyIntake [i].Clone ();
					item.count = GetMaxIntakeForIntakeIndex(i) - MyIntake [i].count;
					getItems.Add (item);
				}
			}
			if(getItems.Count<=0){
				return;
			}
			myWorker.Add (new Worker(this,nearestMarketBuilding, getItems.ToArray() ,false));
			WorldController.Instance.world.CreateWorkerGameObject (myWorker[0]);
		} else {
			base.SendOutWorkerIfCan ();
		}
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
			if((MyIntake[i].count+ toAdd.GetAmountForItem(MyIntake[i])) > GetMaxIntakeForIntakeIndex(i)) {
				return false;
			}
			MyIntake[i].count += toAdd.GetAmountForItem(MyIntake[i]);
			toAdd.setItemCountNull (MyIntake[i]);
			callbackIfnotNull ();
		}

		return true;
	}

	public override Item[] GetRequieredItems(OutputStructure str,Item[] items){
		List<Item> all = new List<Item> ();
		for (int i = MyIntake.Length - 1; i >= 0; i--) {
			int id = MyIntake [i].ID;
			for (int s = 0; s < items.Length; s++) {
				if(items[i].ID==id){
					Item item = items [i].Clone ();
					item.count = GetMaxIntakeForIntakeIndex(i) - MyIntake [i].count;
					if(item.count>0)
						all.Add (item);
				}
			}
		}
		return all.ToArray();
	}
	public override void OnBuild(){
		jobsToDo = new Dictionary<OutputStructure, Item[]> ();
		RegisteredStructures = new Dictionary<OutputStructure, Item[]> ();
//		for (int i = 0; i < intake.Length; i++) {
//			intake [i].count = maxIntake [i];
//		}
		if(myRangeTiles!=null){
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
		//FIXME this is a temporary fix to a stupid bug, which cause
		//i cant find because it works otherwise
		// bug is that myHome doesnt get set by json for this kind of structures
		// but it works for warehouse for example
		// to save save space we could always set it here but that would mean for every kind extra or in place structure???
		if(myWorker!=null){
			foreach(Worker w in myWorker){
				w.myHome = this;		
			}	
		}

	}

	public void ChangeInput(Item change){
		if(change == null){
			return;
		}
		if(myInputTyp == InputTyp.AND){
			return;
		}
		for (int i = 0; i < ProductionData.intake.Length; i++) {
			Item item = ProductionData.intake [i];
			if (item.ID == change.ID) {
				MyIntake [0] = item.Clone ();
				break;
			}
		}
	}
	/// <summary>
	/// Give an index for the needed Item, so only use in for loops
	/// OR
	/// for OR Inake use with orItemIndex
	/// </summary>
	/// <param name="i">The index.</param>
	public int GetMaxIntakeForIntakeIndex(int itemIndex){
		if(itemIndex<0||itemIndex>ProductionData.intake.Length){
			Debug.LogError ("GetMaxIntakeMultiplier received an invalid number " + itemIndex);
			return -1;
		}
		return ProductionData.intake[itemIndex].count * 5; //TODO THINK ABOUT THIS
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
			RegisteredStructures.Add ((OutputStructure)str, items);
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
