using UnityEngine;
using System.Collections;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Warehouse : MarketBuilding {
	public Warehouse(int id){
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

	public override void OnBuild(){
		//changethis code?
		Tile t = myBuildingTiles [0];
		int i = 0;
		while(t.myIsland == null){
			t = myBuildingTiles [i];
			i++;
			if(myBuildingTiles.Count > i){
				break;
			}
		}

		this.city = BuildController.Instance.CreateCity(t);
		if (city == null) {
			return;
		}
		//dostuff thats happen when build
		city.addTiles (myRangeTiles);
	}

	public override void OnClick (){
		callbackIfnotNull ();
	}
	public override void OnClickClose (){
	
	}
	public override Structure Clone (){
		return new Warehouse (this);
	}

	public override void WriteXml (XmlWriter writer){
		base.WriteXml (writer);
	}
}
