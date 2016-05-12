using UnityEngine;
using System.Collections.Generic;

public class MarketBuilding : UserStructure {
	public List<Route> myRoutes;
	public MarketBuilding(){
		tileWidth = 4;
		tileHeight = 4;
		name = "market";
		buildcost = 500;
		maintenancecost = 10;
		BuildTyp = BuildTypes.Single;
		myBuildingTyp = BuildingTyp.Blocking;
		buildingRange = 18;
	}
	protected MarketBuilding(MarketBuilding str){
		this.name = str.name;
		this.tileWidth = str.tileWidth;
		this.tileHeight = str.tileHeight;
		this.mustBeBuildOnShore = str.mustBeBuildOnShore;
		this.maintenancecost = str.maintenancecost;
		this.maxNumberOfWorker = str.maxNumberOfWorker;
		this.buildcost = str.buildcost;
		this.BuildTyp = str.BuildTyp;
		this.rotated = str.rotated;
		this.hasHitbox = str.hasHitbox;
		this.buildingRange = str.buildingRange;
	}
	public override Structure Clone (){
		return new MarketBuilding(this);
	}
	public override void update (float deltaTime){
		base.update_Worker (deltaTime);
	}
	public override void OnBuild(){
		myWorker = new List<Worker> ();
		myRoutes = GetMyRoutes ();
		jobsToDo = new List<ProductionBuilding> ();
		// add all the tiles to the city it was build in
		Tile t = myBuildingTiles [0];
		this.city = t.myCity;
		//dostuff thats happen when build
		for (int w = 0; w < buildingRange; w++) {
			city.addTile (t);
			Tile tn = t.North ();
			for (int h = 1; h < buildingRange; h++) {
				city.addTile (tn);
				tn.North ();
			}
		}
		BuildController.Instance.RegisterStructureCreated (OnStructureBuild);
	}
	public void OnOutputChangedStructure(Structure str){
		if(str is ProductionBuilding == false){
			return;
		}
		foreach (Route item in ((ProductionBuilding)str).GetMyRoutes()) {
			if (myRoutes.Contains (item)) {
				foreach (Tile tile in str.neighbourTiles) {
					if(tile.structures is Road == false){
						continue;
					}
					if(myRoutes.Contains(((Road)tile.structures).Route) == false){
						continue;
					}
					if (((ProductionBuilding)str).outputClaimed == false) {
						jobsToDo.Add ((ProductionBuilding)str);
					}
					return;
				}
			}
		}
	}

	public void OnStructureBuild(Structure structure){
		if(this == structure){
			return;
		}
		if(structure.myBuildingTyp == BuildingTyp.Production){
			foreach (Tile item in structure.myBuildingTiles) {
				if(myRangeTiles.Contains (item)){
					break;
				}
			}
			((ProductionBuilding)structure).RegisterOutputChanged (OnOutputChangedStructure);
		}
		if (structure.myBuildingTyp == BuildingTyp.Pathfinding) {
			if(neighbourTiles.Contains (structure.myBuildingTiles[0])){
				Route r = ((Road)structure).Route;
				if (myRoutes.Contains (r) == false) {
					myRoutes.Add (r);
				}
			}
		}
	}
}
