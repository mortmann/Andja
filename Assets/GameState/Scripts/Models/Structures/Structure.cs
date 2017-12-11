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
		
	public List<Tile> myPrototypeTiles {
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
		Tile firstTile = World.current.GetTileAt (0 + buildingRange+tileWidth/2,0 + buildingRange+tileHeight/2);
		Vector2 center = new Vector2 (firstTile.X, firstTile.Y);
		if (tileWidth > 1) {
			center.x += 0.5f + ((float)tileWidth) / 2f - 1;
		}
		if (tileHeight > 1) {
			center.y += 0.5f + ((float)tileHeight) / 2f - 1;
		}
		World w = WorldController.Instance.world;
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
			myPrototypeTiles.Remove (World.current.GetTileAt (firstTile.X + width, firstTile.Y));
			for (int height = 1; height < tileHeight; height++) {
				myPrototypeTiles.Remove (World.current.GetTileAt (firstTile.X + width, firstTile.Y+height));
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
	public int playerNumber {
		get {
			if(City==null){
				return -1;
			}
			return City.GetPlayerNumber ();}	
	}
	protected StructurePrototypeData _prototypData;
	public StructurePrototypeData data {
		get { if(_prototypData==null){
				_prototypData = PrototypController.Instance.GetStructurePrototypDataForID (ID);
			}
			return _prototypData;
		}
	}

	public bool isWalkable { get {return this.myBuildingTyp != BuildingTyp.Blocking;} }
	public bool hasHitbox { get {return data.hasHitbox;} }
	public float MaxHealth { get {return data.MaxHealth;} }

	public bool canBeBuild { get {return data.canBeBuild;} }

	public int buildingRange { get {return data.buildingRange;} }// = 0;
	public int PopulationLevel { get {return data.PopulationLevel;} }// = 0;
	public int PopulationCount { get {return data.PopulationCount;} }// = 0;

	private int _tileWidth { get {return data.tileWidth;} }
	private int _tileHeight { get {return data.tileHeight;} }

	public bool canRotate { get {return data.canRotate;} }// = true;
	public bool canBeBuildOver { get {return data.canBeBuildOver;} }// = false;
	public bool canBeUpgraded { get {return data.canBeUpgraded;} }// = false;
	public bool showExtraUI { get {return data.showExtraUI;} }
	public bool canTakeDamage { get {return data.canTakeDamage;} }// = false;


	public Direction mustFrontBuildDir { get {return data.mustFrontBuildDir;} }// = Direction.None; 

	public List<Tile> myPrototypeTiles { get {return data.myPrototypeTiles;} }

	public bool canStartBurning { get {return data.canStartBurning;} }
	public bool mustBeBuildOnShore { get {return data.mustBeBuildOnShore;} }//= false;
	public bool mustBeBuildOnMountain { get {return data.mustBeBuildOnMountain;} }//= false;

	public int maintenancecost{ get {return data.maintenancecost;} }
	public int buildcost{ get {return data.buildcost;} }

	public BuildTypes BuildTyp{ get {return data.BuildTyp;} }
	public BuildingTyp myBuildingTyp{ get {return data.myBuildingTyp;} }// = BuildingTyp.Blocking;
	public Item[] buildingItems{ get {return data.buildingItems;} }

	public string spriteName{ get { return data.spriteBaseName/*TODO: make multiple saved sprites possible*/; } }

	protected Action<Structure> cbStructureChanged;
	protected Action<Structure> cbStructureDestroy;
	protected Action<Structure,string> cbStructureSound;
	public bool extraUIOn = false;


	protected void BaseCopyData(Structure str){
		ID = str.ID;
		_prototypData = str.data;
	}

	#endregion
	#endregion
	#region Properties 
	public Vector2 middleVector {get {return new Vector2 (BuildTile.X + (float)tileWidth/2f,BuildTile.Y + (float)tileHeight/2f);}}
	public string SmallName { get { return spriteName.ToLower ();} }
	public City City {
		get { return _city;}
		set {
			if(_city!=null&&_city!=value){
				OnCityChange (_city,value);
				_city.removeStructure (this);
			}
			_city = value;
		}
	}
	public float Health {
		get {
			return _health;
		}
		set {
			if(canTakeDamage==false){
				return;
			}
			if(_health<=0){
				Destroy ();
			}
			_health = value;
		}
	}
	public int tileWidth {
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
	public int tileHeight {
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
	public virtual void update (float deltaTime){
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
		return spriteName;
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
	public void callbackIfnotNull(){
		if(cbStructureChanged != null)
			cbStructureChanged (this);
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
		if(canBuildOnSpot (tiles)==false){
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
			if(mt.myCity!=null && hasCity == false && buildInWilderniss == mt.myCity.IsWilderness ()){
				this.City = mt.myCity;
				hasCity = true;
				mt.myIsland.AddStructure (this);
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
		if (t.myCity!=null && t.myCity.playerNumber != player) {
			//here it cant build cause someoneelse owns it
			if (t.myCity.IsWilderness () == false ) {
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
			for (int w = 0; w < tileWidth; w++) {
//				tiles.Add (World.current.GetTileAt (x + w, y));
				for (int h = 0; h < tileHeight; h++) {
					tiles.Add (World.current.GetTileAt (x + w, y + h));
				}
			}
		} else {
			for (int w = 0; w < _tileWidth; w++) {
				tiles.Add (World.current.GetTileAt (x + w, y));
				for (int h = 1; h < _tileHeight; h++) {
					tiles.Add (World.current.GetTileAt (x + w, y + h));
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
		if (buildingRange == 0) {
			return null;
		}
		if (firstTile==null) {
			Debug.LogError ("Range Tiles Tile is null -> cant calculated of that");
			return null;
		}
		World w = WorldController.Instance.world;
		myRangeTiles = new HashSet<Tile> ();
		float width = firstTile.X-buildingRange - tileWidth / 2;
		float height = firstTile.Y-buildingRange - tileHeight / 2;
		foreach(Tile t in myPrototypeTiles){
			myRangeTiles.Add (w.GetTileAt (t.X +width,t.Y+height));			
		}
		return myRangeTiles;
	}
	public List<Tile> roadsAroundStructure(){
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
		return playerNumber;
	}
	public int GetTargetType(){
		return TargetType + ID;
	}
	public void RegisterOnEvent(Action<GameEvent> create,Action<GameEvent> ending){
		Debug.LogError ("Not implemented! Because nothing yet needs it and would take to much RAM!" );
	}
	public void TakeDamage(float damage){
		if(canTakeDamage==false){
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
		if(cbStructureDestroy!=null)
			cbStructureDestroy(this);
	}
	#endregion
	#region correctspot
	public virtual Item[] BuildingItems(){
		return buildingItems;
	}
	public bool canBuildOnSpot(List<Tile> tiles){
		List<bool> bools = new List<bool> (correctSpot (tiles).Values);
		return bools.Contains (false)==false;
	}

	public Dictionary<Tile,bool> correctSpot(List<Tile> tiles){
		Dictionary<Tile,bool> tileToCanBuild = new Dictionary<Tile, bool> ();
		//to make it faster
		if(mustFrontBuildDir==Direction.None&&mustBeBuildOnShore==false&&mustBeBuildOnMountain==false){
			foreach (Tile item in tiles) {
				tileToCanBuild.Add (item,item.checkTile ());
			}
			return tileToCanBuild;
		}

		//TO simplify this we are gonna sort the array so it is in order
		//from the coordinationsystem that means 0,0->width,height
		Tile[,] sortedTiles = new Tile[tileWidth,tileHeight];

		List<Tile> ts = new List<Tile>(tiles);
		ts.RemoveAll (x=>x==null);
		ts.Sort ((x, y) => x.X.CompareTo (y.X)+x.Y.CompareTo (y.Y));
		foreach(Tile t in ts){
			int x = t.X - ts [0].X;
			int y = t.Y - ts [0].Y;
			if(tileWidth<=x||tileHeight<=y){
				Debug.Log (ts[0] + " -> " + t + " | " + tileWidth + " - " + tileHeight); 

			}
			sortedTiles [x, y] = t; // so we have the tile at the correct spot
		}
		Direction row = RowToTest ();
		switch (row) {
		case Direction.None:
			Debug.LogWarning ("Not implementet! How are we gonna do this?");
			return tileToCanBuild;
		case Direction.N:
			return CheckTilesWithRowFix (tileToCanBuild,sortedTiles, tileWidth,tileHeight,false);
		case Direction.E:
			return CheckTilesWithRowFix (tileToCanBuild,sortedTiles, tileWidth,tileHeight,true);
		case Direction.S:
			return CheckTilesWithRowFix (tileToCanBuild,sortedTiles, tileWidth,0,false);
		case Direction.W:
			return CheckTilesWithRowFix (tileToCanBuild,sortedTiles, 0,tileHeight,true);
		default:
			return null;
		}
	}
	private Dictionary<Tile,bool> CheckTilesWithRowFix(Dictionary<Tile,bool> tileToCanBuild,Tile[,] tiles, int x,int y, bool fixX){
		if (fixX) {
			x = Mathf.Max (x-1, 0);
			for(int i=0;i<y;i++){
				if(tiles[x,i]==null){
					continue;
				}
				tileToCanBuild.Add (tiles [x, i], tiles [x, i].checkTile (mustBeBuildOnShore, mustBeBuildOnMountain));
				tiles [x, i] = null;
			}
		} else {
			y = Mathf.Max (y-1, 0);
			for(int i=0;i<x;i++){
				if(tiles[i,y]==null){
					continue;
				}
				tileToCanBuild.Add (tiles [i, y], tiles [i, y].checkTile (mustBeBuildOnShore, mustBeBuildOnMountain));
				tiles [i, y] = null;
			}
		}
		foreach(Tile t in tiles){
			if(t==null){
				continue;
			}
			tileToCanBuild.Add (t,t.checkTile ());
		}
		return tileToCanBuild;
	}
	private Direction RowToTest(){
		if(mustFrontBuildDir==Direction.None){
			return Direction.None;
		}
		int must = (int)mustFrontBuildDir;
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
		if(canRotate == false) {
			return;
		}
		rotated += 90;
		if (rotated == 360) {
			rotated = 0;
		}
	}
	public void AddTimes90ToRotate(int times){
		if(canRotate == false) {
			return;
		}
		rotated += 90 * times;
		rotated %= 360;
	}
	#endregion
	#region override
	public override string ToString (){
		if(BuildTile==null){
			return spriteName +"@error";
		}
		return spriteName + "@" + BuildTile.toString ();
	}
	#endregion

}

