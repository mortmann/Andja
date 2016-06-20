using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Warehouse : MarketBuilding {
	List<Unit> inRangeUnits;
	public Warehouse(int id){
		inRangeUnits = new List<Unit> ();
		contactRange = 6.3f;
		buildingRange = 18;
		this.ID = id;
		tileWidth = 3;
		tileHeight = 3;
		name = "warehouse";
		buildcost = 500;
		maintenancecost = 10;
		mustBeBuildOnShore = true;
		BuildTyp = BuildTypes.Single;
		showExtraUI = true;
		hasHitbox = true;
		mustFrontBuildDir = Direction.W;
	}
	public Warehouse(){
	}
	protected Warehouse(Warehouse str){
		this.ID = str.ID;
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
		this.showExtraUI = str.showExtraUI;
		this.hasHitbox = str.hasHitbox;
		this.mustFrontBuildDir = str.mustFrontBuildDir;
		this.contactRange = str.contactRange;
	}
	public override void update (float deltaTime) {



	}
	public override bool SpecialCheckForBuild (List<Tile> tiles){
		foreach (Tile item in tiles) {
			if(item.myCity==null && item.myCity.IsWilderness ()){
				continue;
			} 
			if(item.myCity.myWarehouse!=null){
				return false;
			}
		}
		return true;
	}
	public void addUnitToTrade(Unit u){
		inRangeUnits.Add (u);
	}
	public void removeUnitFromTrade(Unit u){
		if(inRangeUnits.Contains (u))
			inRangeUnits.Remove (u);
	}
	public override void OnBuild(){
		//changethis code?
		Tile t = myBuildingTiles [0];
		int i = 0;
		while(t.myIsland == null){
			t = myBuildingTiles [i];
			i++;
			if(myBuildingTiles.Count < i){
				break;
			}
		}

		this.city = BuildController.Instance.CreateCity(t);
		if (city == null) {
			return;
		}
		//dostuff thats happen when build
		city.addTiles (myRangeTiles);
		city.addTiles (new HashSet<Tile>(myBuildingTiles));
	}

	public override void OnClick (){
		extraUIOn = true;
		callbackIfnotNull ();
	}
	public override void OnClickClose (){
		extraUIOn = false;
		callbackIfnotNull ();
	}
	public override Structure Clone (){
		return new Warehouse (this);
	}

	public override void WriteXml (XmlWriter writer){
		base.WriteXml (writer);
	}
}
