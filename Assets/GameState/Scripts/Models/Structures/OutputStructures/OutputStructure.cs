using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public abstract class OutputStructure : Structure {
	#region Serialize

	[JsonPropertyAttribute] public List<Worker> myWorker;
	[JsonPropertyAttribute] public float produceCountdown;
	[JsonPropertyAttribute] public Item[] output;

	#endregion
	#region RuntimeOrOther

	public float contactRange=0;
	public bool forMarketplace=true;
	protected int maxNumberOfWorker = 1;
	public Dictionary<OutputStructure,Item[]> jobsToDo;
	public bool outputClaimed;
	public float produceTime;
	public int maxOutputStorage;
	protected Action<Structure> cbOutputChange;
	bool canWork { get { return Efficiency == 0; }}
	public float efficiencyModifier;

	#endregion



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
		if(myWorker == null){
			return;
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
		OutputStructure giveJob = null;
		foreach (OutputStructure item in jobsToDo.Keys) {
			if (myWorker.Count == maxNumberOfWorker) {
				break;
			}
			Worker ws;
			if (jobsToDo [item] != null) {
				ws= new Worker (this, item,jobsToDo [item]);
			} else {
				ws= new Worker (this, item);
			}
			giveJob = item;
			WorldController.Instance.world.CreateWorkerGameObject (ws);
			myWorker.Add (ws);
		}
		if (giveJob != null) {
			jobsToDo.Remove (giveJob);
		}
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
				if(output[i].count ==  0 || output[i].ID == getItems[g].ID){
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
		if (cbOutputChange!=null)
			cbOutputChange (this);
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
				if (cbOutputChange != null)
					cbOutputChange (this);
				return;
			}
		}
	}
	protected override void OnDestroy () {
		foreach (Worker item in myWorker) {
			item.Destroy ();
		}
	}


}
