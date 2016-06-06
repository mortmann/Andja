using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System;

public abstract class UserStructure : Structure {
	public float health;
	protected int maxNumberOfWorker = 1;
	public List<Worker> myWorker;
	public Dictionary<UserStructure,Item[]> jobsToDo;
	public bool outputClaimed;
	public float produceTime;
	public float produceCountdown;
	public Item[] output;
	public int[] outputStorage;
	public int maxOutputStorage;
	protected Action<Structure> cbOutputChange;

	protected Tile _jobTile;

	public virtual float Efficiency{
		get {
			return 100;
		}
	}

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
		if (jobsToDo.Count == 0) {
			return;
		}
		UserStructure giveJob = null;
		foreach (UserStructure item in jobsToDo.Keys) {
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
	public Item[] getOutput(){
		Item[] temp = new Item[output.Length];
		for (int i = 0; i < output.Length; i++) {
			temp [i] = output [i].CloneWithCount ();
			output[i].count= 0;
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
	public void RegisterOutputChanged(Action<Structure> callbackfunc) {
		cbOutputChange += callbackfunc;
	}

	public void UnregisterOutputChanged(Action<Structure> callbackfunc) {
		cbOutputChange -= callbackfunc;
	}
	public List<Route> GetMyRoutes(){
		List<Route> myRoutes = new List<Route>();
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

	public void WriteUserXml(XmlWriter writer){
		writer.WriteAttributeString("OutputClaimed", outputClaimed.ToString () );
		writer.WriteAttributeString("ProduceCountdown", produceCountdown.ToString () );
		if (outputStorage != null) {
			writer.WriteStartElement ("Outputs");
			foreach (int i in outputStorage) {
				writer.WriteStartElement ("OutputStorage");
				writer.WriteAttributeString ("amount", i.ToString ());
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}
		if (myWorker != null) {
			writer.WriteStartElement ("Workers");
			foreach (Worker w in myWorker) {
				writer.WriteStartElement ("Worker");
				w.WriteXml (writer);
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}
	}

	public void ReadUserXml(XmlReader reader){
		outputClaimed = bool.Parse (reader.GetAttribute("OutputClaimed"));
		produceCountdown = float.Parse( reader.GetAttribute("ProduceCountdown") );
		int output= 0;
		if(reader.ReadToDescendant("Outputs") ) {
			do {
				outputStorage[output] = int.Parse( reader.GetAttribute("amount") );
				output++;
			} while( reader.ReadToNextSibling("OutputStorage") );
		}
		if(reader.ReadToDescendant("Workers") ) {
			do {
				Worker w = new Worker(this);
				w.ReadXml (reader);
				myWorker.Add (w);
			} while( reader.ReadToNextSibling("Worker") );
		}
	}
}
