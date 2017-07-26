using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Warehouse : MarketBuilding {

	#region Serialize


	#endregion
	#region RuntimeOrOther

	public Tile tradeTile;
	public List<Unit> inRangeUnits;

	#endregion
	public Warehouse(int id,MarketPrototypData mpd){
		this.ID = id;
		inRangeUnits = new List<Unit> ();
		this._marketData = mpd;


	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public Warehouse(){
	}
	protected Warehouse(Warehouse str){
		this.ID = str.ID;

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
		Tile[,] sortedTiles = new Tile[tileWidth,tileHeight];
		List<Tile> ts = new List<Tile>(myBuildingTiles);
		ts.Sort ((x, y) => x.X.CompareTo (y.X)+x.Y.CompareTo (y.Y));
		foreach(Tile ti in ts){
			int x = ti.X - ts [0].X;
			int y = ti.Y - ts [0].Y;
			sortedTiles [x, y] = ti; // so we have the tile at the correct spot
		}
		Tile t = sortedTiles [Mathf.RoundToInt (tileWidth / 2), Mathf.RoundToInt (tileHeight / 2)];
		//now we have the tile thats has the smallest x/y 
		//to get the tile we now have to rotate a vector thats
		//1 up and 1 left from the temptile

		Vector3 rot = new Vector3 (-tileWidth/2 - 1, 0, 0);
		rot = Quaternion.AngleAxis (rotated, Vector3.up) * rot;
		if (rotated == 180) //cheap fix --update this
			rot = new Vector3 (tileWidth/2 + 1, 0, 0);
		tradeTile = World.current.GetTileAt (t.X+rot.x,t.Y+rot.y);

		if(t.myIsland.myCities.Exists (x=>x.playerNumber==playerNumber)){
			this.City = t.myIsland.myCities.Find (x => x.playerNumber == playerNumber);
		} else {
			this.City = BuildController.Instance.CreateCity(t,this);
		}
		this.City.myWarehouse = this;
		if (City == null) {
			return;
		}
		if(myRangeTiles==null||myRangeTiles.Count==0){
			myRangeTiles = GetInRangeTiles (BuildTile);
			Debug.Log (myRangeTiles.Count); 
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


}
