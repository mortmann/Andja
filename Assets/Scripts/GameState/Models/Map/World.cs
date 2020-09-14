using EpPathFinding.cs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

[JsonObject(MemberSerialization.OptIn)]
public class World : IGEventable {
    static World _Current { get; set; }
    public static World Current {
        get { return _Current; }
        set {
            if (value != null && _Current != null)
                Debug.LogWarning("WARNING WORLD OVERWRITTEN!");
            _Current = value;
        }
    }

    #region Serialize

    [JsonPropertyAttribute] public List<Island> Islands { get; protected set; }
    [JsonPropertyAttribute] public List<Unit> Units { get; protected set; }
    [JsonPropertyAttribute] public List<Crate> Crates { get; protected set; }
    [JsonPropertyAttribute] public List<Projectile> Projectiles { get; protected set; }

    #endregion
    #region RuntimeOrOther
    public int Width => GameData.Width;
    public int Height => GameData.Height;

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

    public Vector2 Center => new Vector2(Width / 2, Height / 2);

    Action<Unit> cbUnitCreated;
    Action<Worker> cbWorkerCreated;
    Action<Tile> cbTileChanged;
    Action<Crate> cbCrateSpawn;
    Action<Crate> cbCrateDespawned;
    Action<Unit, IWarfare> cbAnyUnitDestroyed;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class.
    /// Used in the GameState!
    /// </summary>
    /// <param name="tiles">Tiles.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    public World(Tile[] addTiles, int width = 1000, int height = 1000, bool isIslandEditor = true) {
        //this.Width = width;
        //this.Height = height;
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
        //whole world IS 1 Island -- so add all tiles to single island        
        if(isIslandEditor) {
            Island isl = new Island(addTiles);
            Islands.Add(isl);
        }
    }


    internal void LoadData(Tile[] tiles, int width, int height) {
        Tiles = tiles;
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (GetTileAt(x, y) == null)
                    SetTileAt(x, y, new Tile(x, y));
            }
        }
        //Width = width;
        //Height = height;
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
        //this.Width = width;
        //this.Height = height;
        
        Current = this;

        if (Crates == null)
            Crates = new List<Crate>();
        if (Projectiles == null)
            Projectiles = new List<Projectile>();
    }

    public World(List<Tile> tileList, int Width, int Height) {
        //this.Width = Width;
        //this.Height = Height;
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
        Islands = new List<Island>();
        Units = new List<Unit>();
        Crates = new List<Crate>();
        Projectiles = new List<Projectile>();
    }
    internal void Update(float deltaTime) {
        foreach (Island i in Islands) {
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

    public void CreateIsland(MapGenerator.IslandData islandStruct) {
        Island island = new Island(islandStruct.Tiles, islandStruct.climate) {
            Fertilities = islandStruct.GetFertilities(),
            Placement = new Vector2(islandStruct.x, islandStruct.y),
            Ressources = islandStruct.Resources
        };
        Islands.Add(island);
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
    public Unit CreateUnit(Unit prefabUnit, Player player, Tile startTile, int nonPlayerNumber = 0) {
        int playerNumber = nonPlayerNumber;
        if (player != null)
            playerNumber = player.Number;
        Unit unit = prefabUnit.Clone(playerNumber, startTile);
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
    public void OnUnitDestroy(Unit u, IWarfare warfare) {
        //Spawn items from Inventory on the map
        if (u.IsShip) {
            Ship ship = u as Ship;
            if (ship.isOffWorld)
                return;
        }
        if (u.inventory != null) {
            foreach (Item i in u.inventory.GetAllItemsAndRemoveThem()) {
                SpawnItemOnMap(i, u.PositionVector);
            }
        }
        cbAnyUnitDestroyed?.Invoke(u, warfare);
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
    internal void RegisterAnyUnitDestroyed(Action<Unit,IWarfare> onAnyUnitDestroyed) {
        cbAnyUnitDestroyed += onAnyUnitDestroyed;
    }
    internal void UnregisterUnitDestroyed(Action<Unit, IWarfare> onAnyUnitDestroyed) {
        cbAnyUnitDestroyed -= onAnyUnitDestroyed;
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

    public void LoadWaterTiles() {
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (GetTileAt(x, y) == null) {
                    SetTileAt(x, y, new Tile(x, y));
                }
            }
        }
    }

    public void Destroy() {
        Current = null;
    }
}
