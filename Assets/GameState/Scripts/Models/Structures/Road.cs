using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Road : Structure {
	#region Serialize
	#endregion
	#region RuntimeOrOther

	private Route _route;
	public Route Route {
		get {return _route;}
		set { _route = value;
		}
	}

	#endregion


	Action<Road> cbRoadChanged;

	public Road(int ID, StructurePrototypeData spd){
		this.ID = ID;
		this._prototypData = spd;
//		this.name = name;
//		this.tileWidth = 1;
//		this.tileHeight = 1;
//		maintenancecost = 0;
//		buildcost = 25;
//		BuildTyp = BuildTypes.Path;
//		myBuildingTyp = BuildingTyp.Pathfinding;
//		buildingRange = 0;
//		canBeUpgraded = true;

	}
	protected Road(Road str){
		BaseCopyData (str);
//		this.canBeUpgraded = str.canBeUpgraded;
//		this.ID = str.ID;
//		this.name = str.name;
//		this.tileWidth = str.tileWidth;
//		this.tileHeight = str.tileHeight;
//		this.mustBeBuildOnShore = str.mustBeBuildOnShore;
//		this.maintenancecost = str.maintenancecost;
//		this.buildcost = str.buildcost;
//		this.BuildTyp = str.BuildTyp;
//		this.rotated = str.rotated;
//		this.hasHitbox = str.hasHitbox;
//		this.buildingRange = str.buildingRange;
//		this.myBuildingTyp = str.myBuildingTyp;
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public Road(){}
	public override Structure Clone(){
		return new Road (this);
	}

	public override void OnBuild (){
		List<Route> routes = new List<Route> ();
		int routeCount=0;
		foreach(Tile t in myBuildingTiles[0].GetNeighbours ()){
			if (t.Structure == null) {
				continue;
			}
			if (t.Structure.BuildTyp != BuildTypes.Path) {
				continue;
			}
			if (t.Structure is Road) {
				if (((Road)t.Structure).Route != null) {
					if (routes.Contains (((Road)t.Structure).Route) == false) {
						routes.Add( ((Road)t.Structure).Route );
						routeCount++;
					}
					((Road)t.Structure).updateOrientation ();
				} 
			}
		}
		updateOrientation ();
		if(routeCount == 0) {
			//If there is no route next to it 
			//so create a new route 
			Route = new Route(myBuildingTiles [0]);
			myBuildingTiles [0].myCity.AddRoute (Route);	
			return;
		}
		if(routeCount == 1){
			// there is already a route 
			// so add it and return
			routes[0].addRoadTile(myBuildingTiles[0]);
			Route = routes[0];
			return;
		}
		//add all Roads from the others to road 1!
		for (int i = 1; i < routes.Count; i++) {
			routes [0].addRoute (routes [i]);
			Route = routes [0];
		}

	}
	public void updateOrientation (){
		Tile[] neig = myBuildingTiles [0].GetNeighbours ();
		connectOrientation = "_";
		if(neig[0].Structure != null){
			if (neig [0].Structure is Road) {
				connectOrientation += "N";
			}
		}
		if(neig[1].Structure!= null){
			if(neig[1].Structure is Road){
				connectOrientation += "E";
			}
		}
		if(neig[2].Structure!= null){
			if(neig[2].Structure is Road){
				connectOrientation += "S";			
			}
		}
		if(neig[3].Structure!= null){
			if(neig[3].Structure is Road){
				connectOrientation += "W";
			}
		}
		if (cbRoadChanged != null)
			cbRoadChanged (this);
	}
	protected override void OnDestroy () {
		if(Route!=null){
			Route.removeRoadTile (BuildTile);
		}
	}


	public void RegisterOnRoadCallback(Action<Road> cb) {
		cbRoadChanged += cb;
	}

	public void UnregisterOnRoadCallback(Action<Road> cb) {
		cbRoadChanged -= cb;
	}

}
