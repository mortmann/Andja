using UnityEngine;
using System.Collections;

public class Warehouse : MarketBuilding {

	public float buildRange = 18;

	public Warehouse(){
		tileWidth = 3;
		tileHeight = 3;
		name = "warehouse";
		buildcost = 500;
		maintenancecost = 10;
		mustBeBuildOnShore = true;
		BuildTyp = BuildTypes.Single;
	}

	protected Warehouse(Structure str){
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
	}


	public override void OnBuild(){
		Tile t = myBuildingTiles [0];
		this.city = BuildController.Instance.CreateCity(t);
		if(city == null) {
			return;
		}
		//dostuff thats happen when build
		for (int w = 0; w < buildRange; w++) {
			city.addTile (t);
			Tile tn = t.North ();
			for (int h = 1; h < buildRange; h++) {
				city.addTile (tn);
				tn.North ();
			}
		}
	}

	public override Structure Clone (){
		return new Warehouse (this);
	}
}
