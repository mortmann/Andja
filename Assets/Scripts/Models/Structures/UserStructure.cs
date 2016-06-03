using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public abstract class UserStructure : Structure {
	public float health;
	protected int maxNumberOfWorker = 1;
	public List<Worker> myWorker;
	public List<UserStructure> jobsToDo;
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
		if (jobsToDo.Count == 0) {
			return;
		}
		UserStructure giveJob = null;
		foreach (UserStructure item in jobsToDo) {
			if (myWorker.Count == maxNumberOfWorker) {
				break;
			}			
			Worker ws = new Worker (this, item);
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

	public List<Route> GetMyRoutes(){
		List<Route> myRoutes = new List<Route>();
		foreach (Tile t in neighbourTiles) {
			if (t.structures == null) {
				continue;
			}
			if(t.structures is Road){
				myRoutes.Add (((Road)t.structures).Route);
			}
		}
		return myRoutes;
	}

}
