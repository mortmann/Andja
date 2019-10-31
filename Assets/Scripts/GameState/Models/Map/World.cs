using EpPathFinding.cs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

[JsonObject(MemberSerialization.OptIn)]
public class World : IGEventable {
    public static World Current { get; protected set; }

    #region Serialize

    [JsonPropertyAttribute] public List<Island> IslandList { get; protected set; }
    [JsonPropertyAttribute] public List<Unit> Units { get; protected set; }
    [JsonPropertyAttribute] public List<Crate> Crates { get; protected set; }
    [JsonPropertyAttribute] public List<Projectile> Projectiles { get; protected set; }

    #endregion
    #region RuntimeOrOther
    public int Width;
    public int Height;

    public Tile[] Tiles { get; protected set; }
    public static List<Need> GetCopieOfAllNeeds() {
        return PrototypController.Instance.GetCopieOfAllNeeds();
    }
    public IReadOnlyDictionary<Climate, List<Fertility>> allFertilities;
    public IReadOnlyDictionary<string, Fertility> idToFertilities;


    protected bool[][] _tilesmap;
    public bool[][] Tilesmap {
        get {
            if (_tilesmap == null) {
                _tilesmap = new bool[Width][];
                for (int x = 0; x < Width; x++) {
                    _tilesmap[Width] = new bool[Height];
                    for (int y = 0; y < Height; y++) {
                        _tilesmap[x][y] = (World.Current.GetTileAt(x, y).Type == TileType.Ocean);
                    }
                }
            }
            return _tilesmap;
        }
    }
    protected StaticGrid _tilesgrid;
    public StaticGrid TilesGrid {
        get {
            if (_tilesgrid == null) {
                bool[][] boolgrid = new bool[Width][];
                for (int x = 0; x < Width; x++) {
                    boolgrid[x] = new bool[Height];
                    for (int y = 0; y < Height; y++) {
                        boolgrid[x][y] = (World.Current.GetTileAt(x, y).Type == TileType.Ocean);
                    }
                }
                _tilesgrid = new StaticGrid(Width, Height, boolgrid);
            }
            return (StaticGrid)_tilesgrid.Clone();
        }
    }

    Action<Unit> cbUnitCreated;
    Action<Worker> cbWorkerCreated;
    Action<Tile> cbTileChanged;
    Action<World> cbTileGraphChanged;
    Action<Crate> cbCrateSpawn;
    Action<Crate> cbCrateDespawned;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class.
    /// Used in the GameState!
    /// </summary>
    /// <param name="tiles">Tiles.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    public World(Tile[] addTiles, int width = 1000, int height = 1000) {
        this.Width = width;
        this.Height = height;
        this.Tiles = new Tile[Width * Height];
        foreach (Tile t in addTiles) {
            if (t != null)
                SetTileAt(t.X, t.Y, t);
        }
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (GetTileAt(x, y) == null)
                    SetTileAt(x, y, new Tile(x, y));
            }
        }

        SetupWorld();
    }


    internal void LoadData(Tile[] tiles, int width, int height) {
        Tiles = tiles;
        Width = width;
        Height = height;
        foreach (Unit u in Units) {
            u.Load();
            u.RegisterOnDestroyCallback(OnUnitDestroy);
            u.RegisterOnCreateProjectileCallback(OnCreateProjectile);
            cbUnitCreated?.Invoke(u);
        }
        foreach (Crate c in Crates) {
            cbCrateSpawn?.Invoke(c);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class. Used in the Editor!
    /// </summary>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    [JsonConstructor]
    public World(int width, int height) {
        this.Width = width;
        this.Height = height;
        
        Current = this;

        if (Crates == null)
            Crates = new List<Crate>();
        if (Projectiles == null)
            Projectiles = new List<Projectile>();
    }

    public World(List<Tile> tileList, int Width, int Height) {
        this.Width = Width;
        this.Height = Height;
        Tiles = new Tile[Width * Height];
        foreach (Tile item in tileList) {
            SetTileAt(item.X, item.Y, item);
        }
        LoadWaterTiles();
        Current = this;
        allFertilities = PrototypController.Instance.AllFertilities;
        idToFertilities = PrototypController.Instance.IdToFertilities;
    }

    public void SetupWorld() {
        Current = this;
        allFertilities = PrototypController.Instance.AllFertilities;
        idToFertilities = PrototypController.Instance.IdToFertilities;
        //		EventController.Instance.RegisterOnEvent (OnEventCreate,OnEventEnded);
        IslandList = new List<Island>();
        Units = new List<Unit>();
        Crates = new List<Crate>();
        Projectiles = new List<Projectile>();
    }
    internal void Update(float deltaTime) {
        foreach (Island i in IslandList) {
            i.Update(deltaTime);
        }
        for (int pos = Crates.Count - 1; pos >= 0; pos--) {
            Crates[pos].Update(deltaTime);
        }
    }

    internal IEnumerable<IGEventable> GetShipUnits() {
        List<IGEventable> list = new List<IGEventable>(Units);
        list.RemoveAll(x => ((Unit)x).IsShip);
        return list;
    }
    internal IEnumerable<IGEventable> GetLandUnits() {
        List<IGEventable> list = new List<IGEventable>(Units);
        list.RemoveAll(x => ((Unit)x).IsShip == false);
        return list;
    }


    internal void FixedUpdate(float deltaTime) {
        for (int i = Units.Count - 1; i >= 0; i--) {
            Units[i].Update(deltaTime);
            if (Units[i].IsDead == true) {
                Units.RemoveAt(i);
            }
        }
        for (int i = Projectiles.Count - 1; i >= 0; i--) {
            Projectiles[i].Update(deltaTime);
        }
    }

    public void CreateIsland(MapGenerator.IslandStruct islandStruct) {
        Island island = new Island(islandStruct.Tiles, islandStruct.climate) {
            myFertilities = new List<Fertility>(islandStruct.fertilities),
            Placement = new Vector2(islandStruct.x, islandStruct.y),
            Ressources = new Dictionary<string, int>(islandStruct.Ressources)
        };
        IslandList.Add(island);
    }
    public void SetTileAt(int x, int y, Tile t) {
        if (t == null)
            Debug.LogWarning("Set a Tile to null! Is this wanted?");
        if (x >= Width || y >= Height) {
            return;
        }
        if (x < 0 || y < 0) {
            return;
        }
        Tiles[x * Height + y] = t;
    }
    public Tile GetTileAt(int x, int y) {
        if (x >= Width || y >= Height) {
            return null;
        }
        if (x < 0 || y < 0) {
            return null;
        }
        return Tiles[x * Height + y];
    }
    public bool IsInTileAt(Tile t, float x, float y) {
        if (x >= Width || y >= Height) {
            return false;
        }
        if (x < 0 || y < 0) {
            return false;
        }
        if (x <= (float)t.X + 0.1f && x >= (float)t.X - 0.1f) {
            if (y <= (float)t.Y + 0.1f && y >= (float)t.Y - 0.1f) {
                return true;
            }
        }
        return false;
    }
    public Tile GetTileAt(Vector2 vec) {
        return GetTileAt(vec.x, vec.y);
    }

    public Tile GetTileAt(float fx, float fy) {
        int x = Mathf.FloorToInt(fx);
        int y = Mathf.FloorToInt(fy);
        return GetTileAt(x, y);
    }
    public Unit CreateUnit(Unit unit) {
        Units.Add(unit);
        unit.RegisterOnDestroyCallback(OnUnitDestroy);
        unit.RegisterOnCreateProjectileCallback(OnCreateProjectile);
        cbUnitCreated?.Invoke(unit);
        return unit;
    }

    private void OnCreateProjectile(Projectile pro) {
        pro.RegisterOnDestroyCallback(ProjectileDestroyed);
        Projectiles.Add(pro);
    }
    private void ProjectileDestroyed(Projectile pro) {
        Projectiles.Remove(pro);
    }
    public void OnUnitDestroy(Unit u) {
        //Spawn items from Inventory on the map
        if (u.inventory != null) {
            foreach (Item i in u.inventory.GetAllItemsAndRemoveThem()) {
                SpawnItemOnMap(i, u.VectorPosition);
            }
        }
    }
    public void SpawnItemOnMap(Item i, Vector2 toSpawnPosition) {
        Vector2 randomFactor = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
        if (GetTileAt((toSpawnPosition + randomFactor)).Type != TileType.Ocean) {
            toSpawnPosition += randomFactor;
        }
        Crate c = new Crate(toSpawnPosition, i);
        c.onDespawn += DespawnItem;
        Crates.Add(c);
        cbCrateSpawn?.Invoke(c);

    }
    public void DespawnItem(Crate c) {
        Crates.Remove(c);
        cbCrateDespawned?.Invoke(c);
    }
    internal void RegisterCrateSpawned(Action<Crate> onSpawned) {
        cbCrateSpawn += onSpawned;
    }
    internal void UnregisterCrateSpawned(Action<Crate> onSpawned) {
        cbCrateSpawn -= onSpawned;
    }
    internal void RegisterCrateDespawned(Action<Crate> onDespawned) {
        cbCrateDespawned += onDespawned;
    }
    internal void UnregisterCrateDespawned(Action<Crate> onDespawned) {
        cbCrateDespawned -= onDespawned;
    }
    // we dont need this right now because str cant be build on Ocean tiles only
    // on shore tiles 
    public void ChangeWorldGraph(Tile t, bool b) {
        Tilesmap[t.X][t.Y] = b;
    }

    public Fertility GetFertility(string ID) {
        return idToFertilities[ID];
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
        cbTileChanged?.Invoke(t);
    }

    #endregion
    #region igeventable
    public override void OnEventCreate(GameEvent ge) {
        if (ge.HasWorldEffect()) {
             
        }
        cbEventCreated?.Invoke(ge);
    }
    public override void OnEventEnded(GameEvent ge) {
        if (ge.HasWorldEffect()) {
            
        }
        cbEventEnded?.Invoke(ge);
    }

    public override int GetPlayerNumber() {
        return -2;
    }
    #endregion
    public string GetJsonSave() {
        WorldSave ws = new WorldSave(Units, IslandList);
        return JsonUtility.ToJson(ws);
    }
    public void LoadWaterTiles() {
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (GetTileAt(x, y) == null) {
                    SetTileAt(x, y, new Tile(x, y));
                }
            }
        }
    }

    [Serializable]
    public class WorldSave {

        public List<Unit> units;
        public List<Island> islands;



        public WorldSave() {
        }
        public WorldSave(List<Unit> units, List<Island> islands) {
            this.units = units;
            this.islands = islands;
        }

    }


}