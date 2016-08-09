using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Warehouse : MarketBuilding {
	public List<Unit> inRangeUnits;

	public Tile tradeTile;

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
		this.canTakeDamage = true;

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
		this.inRangeUnits = new List<Unit> ();
		this.canTakeDamage = str.canTakeDamage;

	}
	public override void update (float deltaTime) {



	}
	public override bool SpecialCheckForBuild (List<Tile> tiles){
		foreach (Tile item in tiles) {
			if(item.myCity==null || item.myCity.IsWilderness ()){
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
		Tile temp=null;
		foreach (Tile item in myBuildingTiles) {
			if(item.myIsland!=null){
				t = item;
			}
			if (item.Type == TileType.Water){
				if(temp==null||temp.X>item.X||temp.Y>item.Y){
					temp = item;	
				}
			}
		}
		//now we have the tile thats has the smallest x/y 
		//to get the tile we now have to rotate a vector thats
		//1 up and 1 left from the temptile
		Vector3 rot = new Vector3 (-1, 1, 0);
		rot = Quaternion.AngleAxis (rotated, new Vector3(0,1,0)) * rot;
		if (rotated == 180)//cheap fix --update this
			rot = new Vector3 (1, 1, 0);
		tradeTile = World.current.GetTileAt (temp.X+rot.x,temp.Y+rot.y);


		if(t.myIsland.myCities.Exists (x=>x.playerNumber==PlayerController.Instance.number)){
			this.City = t.myIsland.myCities.Find (x => x.playerNumber == PlayerController.Instance.number);
		} else {
			this.City = BuildController.Instance.CreateCity(t,this);
		}

		if (City == null) {
			return;
		}
		//dostuff thats happen when build
		City.addTiles (myRangeTiles);
		City.addTiles (new HashSet<Tile>(myBuildingTiles));
	}
	public Tile getTradeTile(){
		return tradeTile; //maybe this changes or not s
	}
	protected override void OnDestroy (){
		List<Tile> h = new List<Tile> (myBuildingTiles);
		h.AddRange (myRangeTiles); 
		City.removeTiles (h);
		//you lose any res that the worker carrying
		foreach (Worker item in myWorker) {
			item.Destroy ();
		}
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
