using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

public class OutputPrototypData : StructurePrototypeData {
	public float contactRange=0;
	public bool forMarketplace=true;
	public int maxNumberOfWorker = 1;
	public float produceTime;
	public int maxOutputStorage;
	public Item[] output;
}


[JsonObject(MemberSerialization.OptIn)]
public abstract class OutputStructure : Structure {
	#region Serialize
	[JsonPropertyAttribute] public List<Worker> myWorker;
	[JsonPropertyAttribute] public float produceCountdown;
	protected Item[] _output; // FIXME DOESNT GET LOADED IN!??!? why? fixed?
	#endregion
	#region RuntimeOrOther
	[JsonPropertyAttribute] public virtual Item[] output {
		get {
			if(_output == null){
				if(OutputData.output==null){
					return null;
				}
				_output = new Item[OutputData.output.Length];
				for(int i = 0; i<OutputData.output.Length;i++){
					_output [i] = OutputData.output [i].Clone();
				}
			}
			return _output;
		}
		set {
			_output = value;
		}
	}
	public Dictionary<OutputStructure,Item[]> jobsToDo;
	public bool outputClaimed;
	protected Action<Structure> cbOutputChange;
	bool canWork { get { return Efficiency > 0; }}
	public float efficiencyModifier = 1;
	public bool workersHasToFollowRoads = false;
	public float contactRange {get{ return OutputData.contactRange;}}
	public bool forMarketplace {get{ return OutputData.forMarketplace;}}
	protected int maxNumberOfWorker {get{ return OutputData.maxNumberOfWorker;}}
	public float produceTime {get{ return OutputData.produceTime;}}
	public int maxOutputStorage {get{ return OutputData.maxOutputStorage;}}

	protected OutputPrototypData _outputData;
	public OutputPrototypData OutputData {
		get { if(_outputData==null){
				_outputData = (OutputPrototypData)PrototypController.Instance.GetStructurePrototypDataForID (ID);
			}
			return _outputData;
		}
	}
	#endregion

	public OutputStructure(){
		jobsToDo = new Dictionary<OutputStructure, Item[]> ();

	}

	protected void OutputCopyData(OutputStructure o){
		BaseCopyData (o);
	}

	public virtual float Efficiency{
		get {
			return 100 * efficiencyModifier;
		}
	}
	protected Tile _jobTile;
	public Tile JobTile {
		get {
			if (_jobTile == null) {
				return myBuildingTiles [0];
			} else {
				return _jobTile;
			}
		}
		set {
			_jobTile = value;
		}
	}
	public void update_Worker(float deltaTime){
		if(maxNumberOfWorker <= 0){
			return;
		}
		if(myWorker==null){
			myWorker = new List<Worker> ();
		}
		for (int i = myWorker.Count-1; i >= 0; i--) {
			Worker w = myWorker[i];
			w.Update (deltaTime);
			if (w.isAtHome) {
				WorkerComeBack (w);
			}
		}		

		SendOutWorkerIfCan ();
	}
	public virtual void SendOutWorkerIfCan (){
		if (jobsToDo.Count == 0) {
			return;
		}
		List<OutputStructure> givenJobs = new List<OutputStructure> ();
		foreach (OutputStructure jobStr in jobsToDo.Keys) {
			if (myWorker.Count == maxNumberOfWorker) {
				break;
			}
			Item[] items = GetRequieredItems(jobStr,jobsToDo [jobStr]);
			if(items==null||items.Length<=0){
				continue;
			}
			Worker ws = new Worker (this, jobStr,items,workersHasToFollowRoads);

			givenJobs.Add(jobStr);
			WorldController.Instance.world.CreateWorkerGameObject (ws);
			myWorker.Add (ws);
		}
		foreach (OutputStructure giveJob in givenJobs) {
			if (giveJob != null) {
				if(giveJob is ProductionBuilding){
					return;
				}
				jobsToDo.Remove (giveJob);
			}
		}
	}
	public virtual Item[] GetRequieredItems(OutputStructure str,Item[] items){
		if(items==null){
			items = str.output;
		}
		List<Item> all = new List<Item> ();
		for (int i = output.Length - 1; i >= 0; i--) {
			int id = output [i].ID;
			for (int s = 0; s < items.Length; s++) {
				if(items[i].ID==id){
					Item item = items [i].Clone ();
					item.count = maxOutputStorage - output [i].count;
					if(item.count>0)
						all.Add (item);
				}
			}
		}
		return all.ToArray();
	}

	public void WorkerComeBack(Worker w){
		if (myWorker.Contains (w) == false) {
			Debug.LogError ("WorkerComeBack - Worker comesback, but doesnt live here!");
			return;
		}
		w.Destroy ();
		myWorker.Remove (w);
	}

	public void addToOutput(Inventory inv){
		for(int i=0; i<output.Length; i++){
			//maybe switch to manually foreach because it may be faster
			//because worker that use this function usually only carry 
			//what the home eg this needs
			if(inv.ContainsItemWithID (output[i].ID)){
				Item item = inv.getAllOfItem (output[i]);
				output[i].count = Mathf.Clamp ( output[i].count + item.count,0,maxOutputStorage);
			}
		}
	}

	public Item[] getOutput(){
		Item[] temp = new Item[output.Length];
		for (int i = 0; i < output.Length; i++) {
			temp [i] = output [i].CloneWithCount ();
			output[i].count= 0;
			CallOutputChangedCB ();
		}
		return temp;
	}
	public virtual Item[] getOutput(Item[] getItems,int[] maxAmounts){
		Item[] temp = new Item[output.Length];
		for (int g = 0; g < getItems.Length; g++) {
			for (int i = 0; i < output.Length; i++) {
				if(output[i].count ==  0 || output[i].ID != getItems[g].ID){
					continue;
				}	
				temp [i] = output [i].CloneWithCount ();
				temp [i].count = Mathf.Clamp (temp [i].count, 0, maxAmounts [i]);
				output[i].count -= temp[i].count;
				CallOutputChangedCB ();
			}
		}
		return temp;
	}
	public virtual Item[] getOutputWithItemCountAsMax(Item[] getItems){
		Item[] temp = new Item[output.Length];
		for (int g = 0; g < getItems.Length; g++) {
			for (int i = 0; i < output.Length; i++) {
				if(output[i].ID != getItems[g].ID){
					continue;
				}	
				if(output[i].count ==  0){
					Debug.LogWarning ("output[i].count ==  0");
				}
				temp [i] = output [i].CloneWithCount ();
				temp [i].count = Mathf.Clamp (temp [i].count, 0, getItems [i].count);
				output[i].count -= temp[i].count;
				CallOutputChangedCB ();
			}
		}
		return temp;
	}
	public Item getOneOutput(Item item) {
		if(output == null){
			return null;
		}
		for (int i = 0; i < output.Length; i++) {
			if(item.ID != output[i].ID){
				continue;
			}
			if (output[i].count > 0) {
				Item temp = output [i].CloneWithCount();
				output [i].count = 0;
				callbackIfnotNull ();
				return temp;
			}
		}
		return null;
	}
	public override Item[] BuildingItems (){
		return buildingItems;
	}
	public void CallOutputChangedCB(){
        cbOutputChange?.Invoke(this);
    }
	public void RegisterOutputChanged(Action<Structure> callbackfunc) {
		cbOutputChange += callbackfunc;
	}

	public void UnregisterOutputChanged(Action<Structure> callbackfunc) {
		cbOutputChange -= callbackfunc;
	}
	public List<Route> GetMyRoutes(){
		List<Route> myRoutes = new List<Route>();
		HashSet<Tile> neighbourTiles = new HashSet<Tile> ();
		foreach (Tile item in myBuildingTiles) {
			foreach(Tile nbt in item.GetNeighbours()){
				if (myBuildingTiles.Contains (nbt) == false) {
					neighbourTiles.Add (nbt);
				}
			}
		}
		foreach (Tile t in neighbourTiles) {
			if (t.Structure == null) {
				continue;
			}
			if(t.Structure is Road){
				myRoutes.Add (((Road)t.Structure).Route);
			}
		}
		return myRoutes;
	}
	public void resetOutputClaimed(){
		this.outputClaimed = false;
		foreach (Item item in output) {
			if(item.count>0){
                cbOutputChange?.Invoke(this);
                return;
			}
		}
	}
	protected override void OnDestroy () {
		if(myWorker!=null){
			foreach (Worker item in myWorker) {
				item.Destroy ();
			}
		}

	}


}
