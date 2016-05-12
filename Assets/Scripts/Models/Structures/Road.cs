using UnityEngine;
using System.Collections.Generic;
using System;
public class Road : Structure {
	private Route _route;
	public Route Route {
		get {return _route;}
		set { _route = value;
		}
	}
	Action<Road> cbRoadChanged;
	public Road(string name, int buildcost = 50){
		this.name = name;
		this.tileWidth = 1;
		this.tileHeight = 1;
		maintenancecost = 0;
		buildcost = 25;
		BuildTyp = BuildTypes.Path;
		myBuildingTyp = BuildingTyp.Pathfinding;
		buildingRange = 0;
	}
	protected Road(Road str){
		this.name = str.name;
		this.tileWidth = str.tileWidth;
		this.tileHeight = str.tileHeight;
		this.mustBeBuildOnShore = str.mustBeBuildOnShore;
		this.maintenancecost = str.maintenancecost;
		this.buildcost = str.buildcost;
		this.BuildTyp = str.BuildTyp;
		this.rotated = str.rotated;
		this.hasHitbox = str.hasHitbox;
		this.buildingRange = str.buildingRange;
		this.myBuildingTyp = str.myBuildingTyp;
	}
	public override Structure Clone(){
		return new Road (this);
	}

	public override void OnBuild (){
		List<Route> routes = new List<Route> ();
		int routeCount=0;
		foreach(Tile t in myBuildingTiles[0].GetNeighbours ()){
			if (t.structures == null) {
				continue;
			}
			if (t.structures.BuildTyp != BuildTypes.Path) {
				continue;
			}
			if (t.structures is Road) {
				if (((Road)t.structures).Route != null) {
					if (routes.Contains (((Road)t.structures).Route) == false) {
						routes.Add( ((Road)t.structures).Route );
						routeCount++;
					}
					((Road)t.structures).updateOrientation ();
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
		if(neig[0].structures != null){
			if (neig [0].structures is Road) {
				connectOrientation += "N";
			}
		}
		if(neig[1].structures!= null){
			if(neig[1].structures is Road){
				connectOrientation += "E";
			}
		}
		if(neig[2].structures!= null){
			if(neig[2].structures is Road){
				connectOrientation += "S";			
			}
		}
		if(neig[3].structures!= null){
			if(neig[3].structures is Road){
				connectOrientation += "W";
			}
		}
		if (cbRoadChanged != null)
			cbRoadChanged (this);
	}
	public void RegisterOnRoadCallback(Action<Road> cb) {
		cbRoadChanged += cb;
	}

	public void UnregisterOnRoadCallback(Action<Road> cb) {
		cbRoadChanged -= cb;
	}

}
