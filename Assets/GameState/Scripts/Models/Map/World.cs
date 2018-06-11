using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using EpPathFinding.cs;

[JsonObject(MemberSerialization.OptIn)]
public class World : IGEventable{
	public const int TargetType = 10;
	public static World Current { get; protected set; }

	#region Serialize

	[JsonPropertyAttribute] public List<Island> IslandList { get; protected set; }
	[JsonPropertyAttribute] public List<Unit> Units { get; protected set; }

    #endregion
    #region RuntimeOrOther
    public int Width;
    public int Height;

    public Tile[] Tiles { get; protected set; }
	public static List<Need> GetCopieOfAllNeeds() {
		return PrototypController.Instance.GetCopieOfAllNeeds();
	}
	public Dictionary<Climate,List<Fertility>> allFertilities;
	public Dictionary<int,Fertility> idToFertilities;

	protected bool[][] _tilesmap;
	public bool[][] Tilesmap { get {
			if(_tilesmap == null){
				_tilesmap = new bool[Width][];
				for (int x = 0; x < Width; x++) {
					_tilesmap[Width] = new bool[Height];
					for (int y = 0; y < Height; y++) {
						_tilesmap [x][y] = (World.Current.GetTileAt (x, y).Type == TileType.Ocean);
					}	
				}
			}
			return _tilesmap;
		}
	}
	protected StaticGrid _tilesgrid;
	public StaticGrid TilesGrid { get {
			if(_tilesgrid == null){
				bool[][] boolgrid = new bool[Width][];
				for (int x = 0; x < Width; x++) {
					boolgrid[x] = new bool[Height];
					for (int y = 0; y < Height; y++) {
						boolgrid [x][y] = (World.Current.GetTileAt (x, y).Type == TileType.Ocean);
					}	
				}
				_tilesgrid = new StaticGrid (Width, Height, boolgrid);
			}
			return _tilesgrid;
		}
	}

	Action<Unit> cbUnitCreated;
	Action<Worker> cbWorkerCreated;
	Action<Tile> cbTileChanged;
	Action<World> cbTileGraphChanged;
	Action<GameEvent> cbEventCreated;
	Action<GameEvent> cbEventEnded;

	#endregion

	/// <summary>
	/// Initializes a new instance of the <see cref="World"/> class.
	/// Used in the GameState!
	/// </summary>
	/// <param name="tiles">Tiles.</param>
	/// <param name="width">Width.</param>
	/// <param name="height">Height.</param>
    public World(Tile[] addTiles, int width = 1000, int height = 1000){
		this.Width = width;
		this.Height = height;
		this.Tiles = new Tile[Width*Height];
        foreach (Tile t in addTiles){
			if(t!=null)
				SetTileAt (t.X, t.Y, t);
		}
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if (GetTileAt (x, y) == null)
					SetTileAt (x, y, new Tile (x,y));
			}
        }

        SetupWorld();

//		for (int x = 29; x < 41; x++) {
//			for (int y = 39; y < 61; y++) {
//				SetTileAt (x,y,new LandTile (x,y));
//				GetTileAt(x,y).Type = TileType.Shore;
//			}
//		}
//		for (int x = 30; x < 40; x++) {
//			for (int y = 40; y < 60; y++) {
//				SetTileAt (x,y,new LandTile (x,y));
//				GetTileAt(x,y).Type = TileType.Dirt;
//			}
//		}
//		for (int x = 60; x < 70; x++) {
//			for (int y = 40; y < 60; y++) {
//				SetTileAt (x,y,new LandTile (x,y));
//				GetTileAt(x,y).Type = TileType.Dirt;
//			}
//		}

//		CreateUnit(GetTileAt(34, 41),PlayerController.currentPlayerNumber,false);
//		CreateUnit(GetTileAt(34, 47),2,false); 
//		CreateUnit(GetTileAt(42, 38),PlayerController.currentPlayerNumber,true);    
//		CreateUnit(GetTileAt(34, 38),2,true);    

//		CreateIsland (31, 41);
//		CreateIsland (61, 41);
	}


    internal void SetTiles(Tile[] tiles, int width, int height) {
        Tiles = tiles;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class. Used in the Editor!
    /// </summary>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    [JsonConstructor]
    public World(int width, int height){
		this.Width = width;
		this.Height = height;
		Tiles = new Tile[Width*Height];
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				SetTileAt (x, y, new Tile (x,y));
			}
		}
		Current = this;
	}
	
	public World(List<Tile> tileList, int Width, int Height){
		this.Width = Width;
		this.Height = Height;
		Tiles = new Tile[Width*Height];
		foreach (Tile item in tileList) {
			SetTileAt (item.X, item.Y, item);
		}
		LoadWaterTiles ();
		Current = this;
		allFertilities = PrototypController.Instance.allFertilities;
		idToFertilities= PrototypController.Instance.idToFertilities;
	}
    //[JsonConstructor]
    //public World(int Width, int Height) {
    //    this.Width = Width;
    //    this.Height = Height;
    //    Tiles = new Tile[Width * Height];
    //    Current = this;
    //    allFertilities = PrototypController.Instance.allFertilities;
    //    idToFertilities = PrototypController.Instance.idToFertilities;
    //}
    public void SetupWorld(){
		Current = this;
		allFertilities = PrototypController.Instance.allFertilities;
		idToFertilities= PrototypController.Instance.idToFertilities;
//		EventController.Instance.RegisterOnEvent (OnEventCreate,OnEventEnded);
		IslandList = new List<Island>();
		Units = new List<Unit>();

	}
    internal void Update(float deltaTime) {
        foreach(Island i in IslandList) {
            i.Update(deltaTime);
        }

    }
	internal void Fixedupdate(float deltaTime){
		for (int i = Units.Count-1; i >=0; i--) {
			Units[i].Update (deltaTime);
			if(Units[i].IsDead ==true){
				Units.RemoveAt (i);
			}
		}

	}

	public void CreateIsland(MapGenerator.IslandStruct islandStruct){
		Fertility[] fers = new Fertility[3];
		if(PrototypController.Instance.GetFertilitiesForClimate(islandStruct.climate) ==null){
            Debug.LogError("NO fertility found for this climate " + islandStruct.climate);
			return;
		}
		List<Fertility> climFer = new List<Fertility>(PrototypController.Instance.GetFertilitiesForClimate(islandStruct.climate));

		for (int i = 0; i < fers.Length; i++) {
			if(climFer.Count==0){
				Debug.LogWarning("NOT ENOUGH FERTILITIES FOR CLIMATE " + islandStruct.climate);
				break;
			}
			Fertility f = climFer[UnityEngine.Random.Range (0,climFer.Count)];
			climFer.Remove (f);
			fers [i] = f;
		}
        foreach(Fertility f in fers) {
        }
        Island island = new Island(islandStruct.Tiles, islandStruct.climate) {
            myFertilities = new List<Fertility>(fers)
        };
        IslandList.Add (island);

	}
	public void SetTileAt(int x,int y,Tile t){
        if (t == null)
            Debug.LogWarning("Set a Tile to null! Is this wanted?");
		if (x >= Width ||y >= Height ) {
			return;
		}
		if (x < 0 || y < 0) {
			return;
		}
		Tiles[x * Height + y] = t;
	}
    public Tile GetTileAt(int x,int y){
        if (x >= Width ||y >= Height ) {
            return null;
        }
        if (x < 0 || y < 0) {
            return null;
        }
		return Tiles[x * Height + y];
    }
	public bool IsInTileAt(Tile t,float x,float y){
		if (x >= Width ||y >= Height ) {
			return false;
		}
		if (x < 0 || y < 0) {
			return false;
		}
		if (x  <= t.X + 0.1f && x  >= t.X - 0.1f) {
			if (y  <= t.Y + 0.1f && y  >= t.Y - 0.1f) {
				return true;
			}
		}
		return false;
	}
    public Tile GetTileAt(float fx, float fy) {
        int x = Mathf.FloorToInt(fx);
        int y = Mathf.FloorToInt(fy);
		return GetTileAt(x,y);
    }
	public Unit CreateUnit(Tile t,int playernumber,bool isShip) {
		Unit c = null;
		if(isShip){
			c = new Ship (t,playernumber);
		} else {
			c = new Unit (t,playernumber);			
		}
        Units.Add(c);
		c.RegisterOnDestroyCallback (OnUnitDestroy);
        cbUnitCreated?.Invoke(c);
        return c;
    }
	public void OnUnitDestroy(Unit u){
	}

	// we dont need this right now because str cant be build on Ocean tiles only
	// on shore tiles 
	public void ChangeWorldGraph(Tile t, bool b){
		Tilesmap [t.X][t.Y] = b;
	}

	public Fertility GetFertility(int ID){
		return idToFertilities [ID];
	}

	public void CreateWorkerGameObject(Worker worker) {
        cbWorkerCreated?.Invoke(worker);
    }
	#region callbacks
	public void RegisterTileGraphChanged(Action<World> callbackfunc) {
		cbTileGraphChanged += callbackfunc;
	}

	public void UnregisterTileGraphChanged(Action<World> callbackfunc) {
		cbTileGraphChanged -= callbackfunc;
	}
    public void RegisterTileChanged(Action<Tile> callbackfunc) {
        cbTileChanged += callbackfunc;
    }

    public void UnregisterTileChanged(Action<Tile> callbackfunc) {
        cbTileChanged -= callbackfunc;
    }

    public void RegisterUnitCreated(Action<Unit> callbackfunc) {
        cbUnitCreated += callbackfunc;
    }

    public void UnregisterUnitCreated(Action<Unit> callbackfunc) {
        cbUnitCreated -= callbackfunc;
    }

	public void RegisterWorkerCreated(Action<Worker> callbackfunc) {
		cbWorkerCreated += callbackfunc;
	}

	public void UnregisterWorkerCreated(Action<Worker> callbackfunc) {
		cbWorkerCreated -= callbackfunc;
	}
    // Gets called whenever ANY tile changes
    public void OnTileChanged(Tile t) {
        if (cbTileChanged == null)
            return;

        cbTileChanged(t);
    }
	public void RegisterOnEvent(Action<GameEvent> create,Action<GameEvent> ending){
		cbEventCreated += create;
		cbEventEnded += ending;
	}
	public void OnEventCreate(GameEvent ge){
		if(ge.HasWorldEffect ()==false){
			return;
		}
        cbEventCreated?.Invoke(ge);
    }
	public void OnEventEnded(GameEvent ge){
		if(ge.HasWorldEffect ()==false){
			return;
		}
        cbEventEnded?.Invoke(ge);
    }
	public int GetPlayerNumber(){
		return -2;
	}
	public int GetTargetType(){
		return TargetType;
	}
	#endregion

	public string GetJsonSave(){
		WorldSave ws = new WorldSave (Units, IslandList);
		return JsonUtility.ToJson (ws);
	}
	public void LoadWaterTiles (){
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				if(GetTileAt(x,y) == null){
					SetTileAt (x, y, new Tile (x,y));
				}
			}
		}
	}

	[Serializable]
	public class WorldSave{

		public List<Unit> units;
		public List<Island> islands;



		public WorldSave(){
		}
		public WorldSave(List<Unit> units, List<Island> islands){
			this.units = units;
			this.islands = islands;
		}

	}


}
