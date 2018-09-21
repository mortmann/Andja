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
		inRangeUnits = new List<Unit> ();	
	}
	protected Warehouse(Warehouse str){
		this.ID = str.ID;
		inRangeUnits = new List<Unit> ();
	}
	
	public override bool SpecialCheckForBuild (List<Tile> tiles){
		foreach (Tile item in tiles) {
			if(item.MyCity==null || item.MyCity.IsWilderness ()){
				continue;
			} 
			if(item.MyCity.myWarehouse!=null){
				return false;
			}
		}
		return true;
	}
	public void AddUnitToTrade(Unit u){
		inRangeUnits.Add (u);
	}
	public void RemoveUnitFromTrade(Unit u){
		if(inRangeUnits.Contains (u))
			inRangeUnits.Remove (u);
	}
	public override void OnBuild(){
		workersHasToFollowRoads = true; // DUNNO HOW where to set it without the need to copy it extra

		Tile[,] sortedTiles = new Tile[TileWidth,TileHeight];
		List<Tile> ts = new List<Tile>(myBuildingTiles);
		ts.Sort ((x, y) => x.X.CompareTo (y.X)+x.Y.CompareTo (y.Y));
		foreach(Tile ti in ts){
			int x = ti.X - ts [0].X;
			int y = ti.Y - ts [0].Y;
			sortedTiles [x, y] = ti; // so we have the tile at the correct spot
		}
        //now we have the tile thats has the smallest x/y 
        //to get the tile we now have to rotate a vector thats
        //1 up and 1 left from the temptile

        //Vector3 rot = new Vector3 (-_tileWidth/2 - 1, _tileHeight / 2 - 1, 0);
        //rot = Quaternion.AngleAxis (rotated, Vector3.forward) * rot;
        Vector2 rot = new Vector2( (float)TileWidth / 2f + 0.5f, 0);
        rot = Rotate(rot, rotated);
		tradeTile = World.Current.GetTileAt ( Mathf.FloorToInt(MiddlePoint.x - rot.x), Mathf.FloorToInt(MiddlePoint.y + rot.y) );

        this.City.myWarehouse = this;

		if (City == null) {
			return;
		}
		if(myRangeTiles==null||myRangeTiles.Count==0){
			myRangeTiles = GetInRangeTiles (BuildTile);
		}
		//dostuff thats happen when build
		City.AddTiles (myRangeTiles);
		City.AddTiles (new HashSet<Tile>(myBuildingTiles));
		RegisteredSturctures = new List<Structure> ();
		OutputMarkedSturctures = new List<Structure> ();
		jobsToDo = new Dictionary<OutputStructure, Item[]> ();

		// add all the tiles to the city it was build in
		//dostuff thats happen when build
		foreach(Tile rangeTile in myRangeTiles){
			if(rangeTile.MyCity!=City){
				continue;
			}
			OnStructureAdded (rangeTile.Structure);
		}
		City.RegisterStructureAdded (OnStructureAdded);
	}
    public Vector2 Rotate(Vector2 v, float degrees) {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
        float tx = v.x;
        float ty = v.y;
        v.x = (cos * tx) - (sin * ty);
        v.y = (sin * tx) + (cos * ty);
        return v;
    }
    public Tile GetTradeTile(){
		return tradeTile; //maybe this changes or not s
	}
	protected override void OnDestroy (){
		List<Tile> h = new List<Tile> (myBuildingTiles);
		h.AddRange (myRangeTiles); 
		City.RemoveTiles (h);
		//you lose any res that the worker is carrying
		foreach (Worker item in myWorker) {
			item.Destroy ();
		}
	}
	public override void OnClick (){
		extraUIOn = true;
		CallbackIfnotNull ();
	}
	public override void OnClickClose (){
		extraUIOn = false;
		CallbackIfnotNull ();
	}
	public override Structure Clone (){
		return new Warehouse (this);
	}


}
