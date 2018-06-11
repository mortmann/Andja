using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public enum BuildTypes {Drag, Path, Single};
public enum BuildingTyp {Pathfinding, Blocking,Free};
public enum Direction {None, N, E, S, W};

public class StructurePrototypeData : LanguageVariables {
	public bool hasHitbox;// { get; protected set; }
	public float MaxHealth;

	public int buildingRange = 0;
	public int PopulationLevel = 0;
	public int PopulationCount = 0;

	public int StructureLevel = 0;

	public int tileWidth;
	public int tileHeight;

	public bool canRotate = true;
	public bool canBeBuildOver = false;
	public bool canBeUpgraded = false;
	public bool canTakeDamage = false;
	public bool showExtraUI = false;
	public bool canBeBuild = true;

	public Direction mustFrontBuildDir= Direction.None; 

	//doenst get loaded in anyway
	private List<Tile> _myPrototypeTiles;
		
	public List<Tile> MyPrototypeTiles {
		get {
			if (_myPrototypeTiles == null) {
				CalculatePrototypTiles ();
			}
			return _myPrototypeTiles;
		}
	}

	public bool canStartBurning;
	public bool mustBeBuildOnShore = false;
	public bool mustBeBuildOnMountain = false;

	public int maintenancecost;
	public int buildcost;

	public BuildTypes BuildTyp;
	public BuildingTyp myBuildingTyp = BuildingTyp.Blocking;
	public Item[] buildingItems;

	public string spriteBaseName;


	private void CalculatePrototypTiles(){
		_myPrototypeTiles = new List<Tile> ();
		if(buildingRange == 0){
			return;
		}
		float x;
		float y;
		//get the tile at bottom left to create a "prototype circle"
		Tile firstTile = World.Current.GetTileAt (0 + buildingRange+tileWidth/2,0 + buildingRange+tileHeight/2);
		Vector2 center = new Vector2 (firstTile.X, firstTile.Y);
		if (tileWidth > 1) {
			center.x += 0.5f + ((float)tileWidth) / 2f - 1;
		}
		if (tileHeight > 1) {
			center.y += 0.5f + ((float)tileHeight) / 2f - 1;
		}
		World w = WorldController.Instance.World;
		List<Tile> temp = new List<Tile> ();
		float radius = this.buildingRange + 1.5f;
		for (float a = 0; a < 360; a += 0.5f) {
			x = center.x + radius * Mathf.Cos (a);
			y = center.y + radius * Mathf.Sin (a);
			//			GameObject go = new GameObject ();
			//			go.transform.position = new Vector3 (x, y);
			//			go.AddComponent<SpriteRenderer> ().sprite = Resources.Load<Sprite> ("Debug");
			x = Mathf.RoundToInt (x);
			y = Mathf.RoundToInt (y);
			for (int i = 0; i < buildingRange; i++) {
				Tile circleTile = w.GetTileAt (x, y);
				if (temp.Contains (circleTile) == false) {
					temp.Add (circleTile);
				}
			}
		}
		//like flood fill the inner circle
		Queue<Tile> tilesToCheck = new Queue<Tile> ();
		tilesToCheck.Enqueue (firstTile.South ());
		while (tilesToCheck.Count > 0) {
			Tile t = tilesToCheck.Dequeue ();
			if (temp.Contains (t) == false && _myPrototypeTiles.Contains (t) == false) {
				_myPrototypeTiles.Add (t);
				Tile[] ns = t.GetNeighbours (false);
				foreach (Tile t2 in ns) {
					tilesToCheck.Enqueue (t2);
				}
			}
		}
		for (int width = 0; width < tileWidth; width++) {
			MyPrototypeTiles.Remove (World.Current.GetTileAt (firstTile.X + width, firstTile.Y));
			for (int height = 1; height < tileHeight; height++) {
				MyPrototypeTiles.Remove (World.Current.GetTileAt (firstTile.X + width, firstTile.Y+height));
			}
		}
	}

}

[JsonObject(MemberSerialization.OptIn)]
public abstract class Structure : IGEventable {
	#region variables
	public const int TargetType = 100;
	#region Serialize
	//prototype id
	[JsonPropertyAttribute] public int ID;

	//build id -- when it was build
	[JsonPropertyAttribute] public uint buildID;

	[JsonPropertyAttribute] private City _city;

	[JsonPropertyAttribute] protected float _health;

	[JsonPropertyAttribute] public Tile BuildTile { get { 
			if (myBuildingTiles == null)
				return null;
			return myBuildingTiles [0]; 
		} 
		set {
			if (myBuildingTiles == null)
				myBuildingTiles = new List<Tile> ();
			myBuildingTiles.Add (value);
		}
	}
	[JsonPropertyAttribute] public int  rotated = 0; 
	[JsonPropertyAttribute] public bool buildInWilderniss = false;
	[JsonPropertyAttribute] public bool isActive = true;
	#endregion
	#region RuntimeOrOther
	public List<Tile> myBuildingTiles;
	public HashSet<Tile> neighbourTiles;
	public HashSet<Tile> myRangeTiles;
	public string connectOrientation;
	//player id
	public int PlayerNumber {
		get {
			if(City==null){
				return -1;
			}
			return City.GetPlayerNumber ();}	
	}
	protected StructurePrototypeData _prototypData;
	public StructurePrototypeData Data {
		get { if(_prototypData==null){
				_prototypData = PrototypController.Instance.GetStructurePrototypDataForID (ID);
			}
			return _prototypData;
		}
	}

	public bool IsWalkable { get {return this.MyBuildingTyp != BuildingTyp.Blocking;} }
	public bool HasHitbox { get {return Data.hasHitbox;} }
	public float MaxHealth { get {return Data.MaxHealth;} }

	public bool CanBeBuild { get {return Data.canBeBuild;} }

	public int BuildingRange { get {return Data.buildingRange;} }// = 0;
	public int PopulationLevel { get {return Data.PopulationLevel;} }// = 0;
	public int PopulationCount { get {return Data.PopulationCount;} }// = 0;

	private int _tileWidth { get {return Data.tileWidth;} }
	private int _tileHeight { get {return Data.tileHeight;} }

	public bool CanRotate { get {return Data.canRotate;} }// = true;
	public bool CanBeBuildOver { get {return Data.canBeBuildOver;} }// = false;
	public bool CanBeUpgraded { get {return Data.canBeUpgraded;} }// = false;
	public bool ShowExtraUI { get {return Data.showExtraUI;} }
	public bool CanTakeDamage { get {return Data.canTakeDamage;} }// = false;


	public Direction MustFrontBuildDir { get {return Data.mustFrontBuildDir;} }// = Direction.None; 

	public List<Tile> MyPrototypeTiles { get {return Data.MyPrototypeTiles;} }

	public bool CanStartBurning { get {return Data.canStartBurning;} }
	public bool MustBeBuildOnShore { get {return Data.mustBeBuildOnShore;} }//= false;
	public bool MustBeBuildOnMountain { get {return Data.mustBeBuildOnMountain;} }//= false;

	public int Maintenancecost{ get {return Data.maintenancecost;} }
	public int Buildcost{ get {return Data.buildcost;} }

	public BuildTypes BuildTyp{ get {return Data.BuildTyp;} }
	public BuildingTyp MyBuildingTyp{ get {return Data.myBuildingTyp;} }// = BuildingTyp.Blocking;
	public Item[] BuildingItems{ get {return Data.buildingItems;} }

	public string SpriteName{ get { return Data.spriteBaseName/*TODO: make multiple saved sprites possible*/; } }

	protected Action<Structure> cbStructureChanged;
	protected Action<Structure> cbStructureDestroy;
	protected Action<Structure,string> cbStructureSound;
	public bool extraUIOn = false;


	protected void BaseCopyData(Structure str){
		ID = str.ID;
		_prototypData = str.Data;
	}

	#endregion
	#endregion
	#region Properties 
	public Vector2 MiddleVector {get {return new Vector2 (BuildTile.X + (float)TileWidth/2f,BuildTile.Y + (float)TileHeight/2f);}}
	public string SmallName { get { return SpriteName.ToLower ();} }
	public City City {
		get { return _city;}
		set {
			if(_city!=null&&_city!=value){
				OnCityChange (_city,value);
				_city.RemoveStructure (this);
			}
			_city = value;
		}
	}
	public float Health {
		get {
			return _health;
		}
		set {
			if(CanTakeDamage==false){
				return;
			}
			if(_health<=0){
				Destroy ();
			}
			_health = value;
		}
	}
	public int TileWidth {
		get { 
			if (rotated == 0 || rotated == 180) {
				return _tileWidth;
			}  
			if (rotated == 90 || rotated == 270) {
				return _tileHeight;
			} 
			// should never come to this if its an error
			Debug.LogError ("Structure was rotated out of angle bounds: " + rotated);
			return 0;
		}
	}
	public int TileHeight {
		get { 
			if (rotated == 0 || rotated == 180) {
				return _tileHeight;
			} 
			if (rotated == 90 || rotated == 270) {
				return _tileWidth;
			} 
			// should never come to this if its an error
			Debug.LogError ("Structure was rotated out of angle bounds: " + rotated);
			return 0;
		}
	}
	#endregion
	#region Virtual/Abstract
	public abstract Structure Clone ();
	public virtual void Update (float deltaTime){
	}
	public abstract void OnBuild();
	public virtual void OnClick (){
	}
	public virtual void OnClickClose (){
	}
	protected virtual void OnDestroy(){}
	protected virtual void OnCityChange (City old,City newOne){}
	/// <summary>
	/// Extra Build UI for showing stuff when building
	/// structures. Or so.
	/// </summary>
	/// <param name="parent">Its the parent for the extra UI.</param>		
	public virtual void ExtraBuildUI(GameObject parent){
		//does nothing normally
		//stuff here to show for when building this
		//using this for e.g. farm efficiency bar!
	}
	public virtual void UpdateExtraBuildUI(GameObject parent,Tile t){
		//does nothing normally
		//stuff here to show for when building this
		//using this for e.g. farm efficiency bar!
	}
	public virtual void OnEventCreateVirtual(GameEvent ge){
		ge.InfluenceTarget (this, true);
	}
	public virtual void OnEventEndedVirtual(GameEvent ge){
		ge.InfluenceTarget (this, false);
	}
	public virtual string GetSpriteName(){
		return SpriteName;
	}
	#endregion 

	#region callbacks
	/// <summary>
	/// Do not override this function!
	/// USE virtual to override the reaction to an event that
	/// influences this Structure.
	/// </summary>
	/// <param name="ge">Ge.</param>
	public void OnEventCreate(GameEvent ge){
		//every subtype has do decide what todo
		//maybe some above reactions here 
		if(ge.target is Structure){
			if(ge.target==this){
				OnEventCreateVirtual (ge);
			}
			return;
		}
		if(ge.IsTarget (this)){
			OnEventCreateVirtual (ge);
		}
	}
	/// <summary>
	/// Do not override this function!
	/// USE virtual to override the reaction to an event that
	/// influences this Structure.
	/// </summary>
	/// <param name="ge">Ge.</param>
	public void OnEventEnded(GameEvent ge){
		//every subtype has do decide what todo
		//maybe some above reactions here 
		if(ge.target is Structure){
			if(ge.target==this){
				OnEventEndedVirtual (ge);
			}
			return;
		}
		if(ge.IsTarget (this)){
			OnEventEndedVirtual (ge);
		}
	}
	public void CallbackIfnotNull(){
        cbStructureChanged?.Invoke(this);
    }
    public void RegisterOnChangedCallback(Action<Structure> cb) {
        cbStructureChanged += cb;
    }
    public void UnregisterOnChangedCallback(Action<Structure> cb) {
        cbStructureChanged -= cb;
    }
	public void RegisterOnDestroyCallback(Action<Structure> cb) {
		cbStructureDestroy += cb;
	}
	public void UnregisterOnDestroyCallback(Action<Structure> cb) {
		cbStructureDestroy -= cb;
	}
	public void RegisterOnSoundCallback(Action<Structure,string> cb){
		cbStructureSound += cb;
	}
	public void UnregisterOnSoundCallback(Action<Structure,string> cb) {
		cbStructureSound -= cb;
	}
	#endregion
	#region placestructure
	public bool PlaceStructure(List<Tile> tiles){
		myBuildingTiles = new List<Tile> ();

		//test if the place is buildable
		// if it has to be on land
		if(CanBuildOnSpot (tiles)==false){
			Debug.Log ("canBuildOnSpot FAILED -- Give UI feedback"); 
			return false;
		}

		//special check for some structures 
		if (SpecialCheckForBuild (tiles) == false) {
			Debug.Log ("specialcheck failed -- Give UI feedback"); 
			return false;
		}

		myBuildingTiles.AddRange (tiles);
		//if we are here we can build this and
		//set the tiles to the this structure -> claim the tiles!
		bool hasCity = false;
		neighbourTiles = new HashSet<Tile>();
		foreach (Tile mt in myBuildingTiles) {
			mt.Structure = this;
			if(mt.MyCity!=null && hasCity == false && buildInWilderniss == mt.MyCity.IsWilderness ()){
				this.City = mt.MyCity;
				hasCity = true;
				mt.MyIsland.AddStructure (this);
			}
			foreach(Tile nbt in mt.GetNeighbours()){
				if (myBuildingTiles.Contains (nbt) == false) {
					neighbourTiles.Add (nbt);
				}
			}
		}

		//it searches all the tiles it has in its reach!
		GetInRangeTiles (myBuildingTiles[0]);

		// do on place structure stuff here!
		OnBuild ();
		City.RegisterOnEvent (OnEventCreate, OnEventEnded);

		return true;
	}

	public bool IsTileCityViable(Tile t, int player){
		if (t.MyCity!=null && t.MyCity.playerNumber != player) {
			//here it cant build cause someoneelse owns it
			if (t.MyCity.IsWilderness () == false ) {
				return false;
			} else {
				//HERE it can be build if 
				//EXCEPTION warehouses can be build on new islands
				if(this is Warehouse == false){
					return false;
				}
			}
		} 
		return true;
	}

	public virtual bool SpecialCheckForBuild(List<Tile> tiles){
		return true;
	}
	#endregion
	#region List<Tile>
	public List<Tile> GetBuildingTiles(float x , float y, bool ignoreRotation = false){
		x = Mathf.FloorToInt (x);
		y = Mathf.FloorToInt (y);
		List<Tile> tiles = new List<Tile> ();
		if (ignoreRotation == false) {
			for (int w = 0; w < TileWidth; w++) {
//				tiles.Add (World.current.GetTileAt (x + w, y));
				for (int h = 0; h < TileHeight; h++) {
					tiles.Add (World.Current.GetTileAt (x + w, y + h));
				}
			}
		} else {
			for (int w = 0; w < _tileWidth; w++) {
				tiles.Add (World.Current.GetTileAt (x + w, y));
				for (int h = 1; h < _tileHeight; h++) {
					tiles.Add (World.Current.GetTileAt (x + w, y + h));
				}
			}
		}

		return tiles;
	}
	/// <summary>
	/// Gets the in range tiles.
	/// </summary>
	/// <returns>The in range tiles.</returns>
	/// <param name="firstTile">The most left one, first row.</param>
	public HashSet<Tile> GetInRangeTiles (Tile firstTile) {
		if (BuildingRange == 0) {
			return null;
		}
		if (firstTile==null) {
			Debug.LogError ("Range Tiles Tile is null -> cant calculated of that");
			return null;
		}
		World w = WorldController.Instance.World;
		myRangeTiles = new HashSet<Tile> ();
		float width = firstTile.X-BuildingRange - TileWidth / 2;
		float height = firstTile.Y-BuildingRange - TileHeight / 2;
		foreach(Tile t in MyPrototypeTiles){
			myRangeTiles.Add (w.GetTileAt (t.X +width,t.Y+height));			
		}
		return myRangeTiles;
	}
	public List<Tile> RoadsAroundStructure(){
		List<Tile> roads = new List<Tile>();
		foreach (Tile item in myBuildingTiles) {
			foreach (Tile n in item.GetNeighbours ()) {
				if(n.Structure != null ){
					if (n.Structure is Road) {
						roads.Add (n);
					}
				}
			}
		}
		return roads;
	}
	#endregion
	#region other
	public int GetPlayerNumber(){
		return PlayerNumber;
	}
	public int GetTargetType(){
		return TargetType + ID;
	}
	public void RegisterOnEvent(Action<GameEvent> create,Action<GameEvent> ending){
		Debug.LogError ("Not implemented! Because nothing yet needs it and would take to much RAM!" );
	}
	public void TakeDamage(float damage){
		if(CanTakeDamage==false){
			return;
		}
		if(damage<0){
			damage = -damage;
			Debug.LogWarning ("Damage should be never smaller than 0 - Fixed it!");
		}
		Health -= damage;
		Health = Mathf.Clamp (Health, 0, MaxHealth);
	}
	public void HealHealth(float heal){
		if(heal<0){
			heal = -heal;
			Debug.LogWarning ("Healing should be never smaller than 0 - Fixed it!");
		}
		Health += heal;
		Health = Mathf.Clamp (Health, 0, MaxHealth);
	}
	public void Destroy(){
		OnDestroy ();
		foreach(Tile t in myBuildingTiles){
			t.Structure = null;
		}
        cbStructureDestroy?.Invoke(this);
    }
	#endregion
	#region correctspot
	public virtual Item[] GetBuildingItems(){
		return BuildingItems;
	}
	public bool CanBuildOnSpot(List<Tile> tiles){
		List<bool> bools = new List<bool> (CorrectSpot (tiles).Values);
		return bools.Contains (false)==false;
	}

	public Dictionary<Tile,bool> CorrectSpot(List<Tile> tiles){
		Dictionary<Tile,bool> tileToCanBuild = new Dictionary<Tile, bool> ();
		//to make it faster
		if(MustFrontBuildDir==Direction.None && MustBeBuildOnShore==false && MustBeBuildOnMountain==false){
			foreach (Tile item in tiles) {
				tileToCanBuild.Add (item,item.CheckTile ());
			}
			return tileToCanBuild;
		}

        //TO simplify this we are gonna sort the array so it is in order
        //from the coordinationsystem that means 0,0->width,height
        int max = Mathf.Max(TileWidth, TileHeight);
		Tile[,] sortedTiles = new Tile[max, max];

		List<Tile> ts = new List<Tile>(tiles);

        if (ts.Count == 0)
            return null;

		//ts.RemoveAll (x=>x==null);
		ts.Sort ((x, y) => x.X.CompareTo (y.X)*x.Y.CompareTo (y.Y));
		foreach(Tile t in ts){
			int x = t.X - ts [0].X;
			int y = t.Y - ts [0].Y;
			if( TileWidth<=x || TileHeight<=y || x<0 ||y<0 ){
                Debug.Log(ts.Count);
            }
            sortedTiles [x, y] = t; // so we have the tile at the correct spot
		}

		Direction row = RowToTest ();
		switch (row) {
		case Direction.None:
			Debug.LogWarning ("Not implementet! How are we gonna do this?");
			return tileToCanBuild;
		case Direction.N:
			return CheckTilesWithRowFix (tileToCanBuild,sortedTiles, TileWidth,TileHeight,false);
		case Direction.E:
			return CheckTilesWithRowFix (tileToCanBuild,sortedTiles, TileWidth,TileHeight,true);
		case Direction.S:
			return CheckTilesWithRowFix (tileToCanBuild,sortedTiles, TileWidth,0,false);
		case Direction.W:
			return CheckTilesWithRowFix (tileToCanBuild,sortedTiles, 0,TileHeight,true);
		default:
			return null;
		}
	}
	private Dictionary<Tile,bool> CheckTilesWithRowFix(Dictionary<Tile,bool> tileToCanBuild, Tile[,] tiles, int x,int y, bool fixX){
		if (fixX) {
			x = Mathf.Max (x-1, 0);
			for(int i=0;i<y;i++){
				if(tiles[x,i]==null){
					continue;
				}
				tileToCanBuild.Add (tiles [x, i], tiles [x, i].CheckTile (MustBeBuildOnShore, MustBeBuildOnMountain));
				tiles [x, i] = null;
			}
		} else {
			y = Mathf.Max (y-1, 0);
			for(int i=0;i<x;i++){
				if(tiles[i,y]==null){
					continue;
				}
				tileToCanBuild.Add (tiles [i, y], tiles [i, y].CheckTile (MustBeBuildOnShore, MustBeBuildOnMountain));
				tiles [i, y] = null;
			}
		}
		foreach(Tile t in tiles){
			if(t==null){
				continue;
			}
			tileToCanBuild.Add (t,t.CheckTile ());
		}
		return tileToCanBuild;
	}
	private Direction RowToTest(){
		if(MustFrontBuildDir==Direction.None){
			return Direction.None;
		}
		int must = (int)MustFrontBuildDir;
		//so we have either 1,2,3 or 4
		//so just loop through those and add per 90: 1
		int rotNum = rotated / 90; // so we have now 1,2,3
		//we add this to the must be correct one
		must += rotNum;
		if(must>4){
			must -= 4;
		}
		return (Direction)must;
	}

	#endregion
	#region rotation
	public int ChangeRotation(int x , int y, int rotate = 0){
		if(rotate == 360){
			return 0;
		}
		this.rotated = rotate;
		return rotate;
	}
	public void RotateStructure(){
		if(CanRotate == false) {
			return;
		}
		rotated += 90;
		if (rotated == 360) {
			rotated = 0;
		}
	}
	public void AddTimes90ToRotate(int times){
		if(CanRotate == false) {
			return;
		}
		rotated += 90 * times;
		rotated %= 360;
	}
	#endregion
	#region override
	public override string ToString (){
		if(BuildTile==null){
			return SpriteName +"@error";
		}
		return SpriteName + "@ X=" + BuildTile.X +" Y=" + BuildTile.Y;
	}
	#endregion

}

