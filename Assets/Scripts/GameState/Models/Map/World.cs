using Andja.Controller;
using Andja.Model.Generator;
using Andja.Pathfinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class World : GEventable, IWorld {
        private static IWorld _current { get; set; }

        public static IWorld Current {
            get { return _current; }
            set {
#if !UNITY_INCLUDE_TESTS
                if (value != null && _current != null)
                    Debug.LogWarning("WARNING WORLD OVERWRITTEN!");
#endif
                _current = value;
            }
        }

#region Serialize

        [JsonPropertyAttribute] public List<Island> Islands { get; protected set; }
        [JsonPropertyAttribute] public List<Unit> Units { get; protected set; }
        [JsonPropertyAttribute] public List<Crate> Crates { get; protected set; }
        [JsonPropertyAttribute] public List<Projectile> Projectiles { get; protected set; }

        #endregion Serialize

        #region RuntimeOrOther
        /// <summary>
        /// Unique ID that gets assigned to a created unit.
        /// Should start with one -- so 0 is unset
        /// </summary>
        private uint _unitBuildId = 1;
        public int Width => GameData.Width;
        public int Height => GameData.Height;

        public Tile[] Tiles { get; protected set; }

        public static List<Need> GetCopieOfAllNeeds() {
            return PrototypController.Instance.GetCopieOfAllNeeds();
        }

        public IReadOnlyDictionary<Climate, List<Fertility>> allFertilities;
        public IReadOnlyDictionary<string, Fertility> idToFertilities;
        protected Action<Projectile> cbCreateProjectile;

        private bool[][] _tilesMap;

        public bool[][] TilesMap {
            get {
                if (_tilesMap == null) {
                    _tilesMap = new bool[Width][];
                    for (int x = 0; x < Width; x++) {
                        _tilesMap[x] = new bool[Height];
                        for (int y = 0; y < Height; y++) {
                            _tilesMap[x][y] = (GetTileAt(x, y).Type == TileType.Ocean);
                        }
                    }
                }
                return _tilesMap;
            }
        }

        public WorldGraph WorldGraph { get; protected set; }

        public void RegisterOnCreateProjectileCallback(Action<Projectile> cb) {
            cbCreateProjectile += cb;
        }

        public void UnregisterOnCreateProjectileCallback(Action<Projectile> cb) {
            cbCreateProjectile -= cb;
        }
        public Vector2 Center => new Vector2(Width / 2, Height / 2);

        private Action<Unit> cbUnitCreated;
        private Action<Worker> cbWorkerCreated;
        private Action<Tile> cbTileChanged;
        private Action<Crate> cbCrateSpawn;
        private Action<Crate> cbCrateDespawned;

        private Action<Unit, IWarfare> cbAnyUnitDestroyed;

#endregion RuntimeOrOther

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class.
    /// Used in the GameState!
    /// </summary>
    public World(Tile[] addTiles, bool isIslandEditor = true) {
            this.Tiles = new Tile[Width * Height];
            foreach (Tile t in addTiles) {
                if (t != null)
                    SetTileAt(t.X, t.Y, t);
            }
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    if (GetTileAt(x, y) == null)
                        SetTileAt(x, y, new Tile(x, y));
                }
            }
            SetupWorld();
            //whole world IS 1 Island -- so add all tiles to single island
            if (isIslandEditor) {
                Islands.Add(new Island(addTiles));
            }
        }

        public void LoadTiles(Tile[] tiles, int width, int height) {
            Tiles = tiles;
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    if (GetTileAt(x, y) == null)
                        SetTileAt(x, y, new Tile(x, y));
                }
            }
        }
        public void Load() {
            WorldGraph = new WorldGraph();
            foreach (Unit u in Units) {
                u.Load();
                u.RegisterOnDestroyCallback(OnUnitDestroy);
                u.RegisterOnCreateProjectileCallback(OnCreateProjectile);
                cbUnitCreated?.Invoke(u);
            }
            _unitBuildId = Units.Max(u => u.BuildID);
            foreach (Crate c in Crates) {
                cbCrateSpawn?.Invoke(c);
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="World"/> class. Used in the Editor!
        /// </summary>
        [JsonConstructor]
        public World() {
            Current = this;
            Crates ??= new List<Crate>();
            Projectiles ??= new List<Projectile>();
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

        public void Update(float deltaTime) {
            foreach (Island i in Islands) {
                i.Update(deltaTime);
            }
            for (int pos = Crates.Count - 1; pos >= 0; pos--) {
                Crates[pos].Update(deltaTime);
            }
        }

        public IEnumerable<GEventable> GetShipUnits() {
            List<GEventable> list = new List<GEventable>(Units);
            list.RemoveAll(x => ((Unit)x).IsShip);
            return list;
        }

        public IEnumerable<GEventable> GetLandUnits() {
            List<GEventable> list = new List<GEventable>(Units);
            list.RemoveAll(x => ((Unit)x).IsShip == false);
            return list;
        }

        public void FixedUpdate(float deltaTime) {
            for (int i = Units.Count - 1; i >= 0; i--) {
                Units[i].Update(deltaTime);
                if (Units[i].IsDestroyed) {
                    Units.RemoveAt(i);
                }
            }
            for (int i = Projectiles.Count - 1; i >= 0; i--) {
                Projectiles[i].Update(deltaTime);
            }
        }

        public void CreateIsland(MapGenerator.IslandData islandStruct) {
            Island island = new Island(islandStruct);
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
        private Tile GetTileClampedAt(Vector2 v) {
            return GetTileAt(Mathf.Clamp(v.x, 0, Width - 1), Mathf.Clamp(v.y, 0, Height - 1));
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
            Unit unit = prefabUnit.Clone(playerNumber, startTile, _unitBuildId++);
            Units.Add(unit);
            unit.RegisterOnDestroyCallback(OnUnitDestroy);
            unit.RegisterOnCreateProjectileCallback(OnCreateProjectile);
            cbUnitCreated?.Invoke(unit);
            return unit;
        }

        public void OnCreateProjectile(Projectile pro) {
            cbCreateProjectile?.Invoke(pro);
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
            if (u.Inventory != null) {
                foreach (Item i in u.Inventory.GetAllItemsAndRemoveThem()) {
                    CreateItemOnMap(i, u.PositionVector);
                }
            }
            cbAnyUnitDestroyed?.Invoke(u, warfare);
        }

        public void CreateItemOnMap(Item i, Vector2 toSpawnPosition) {
            Vector2 randomFactor = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
            if (GetTileClampedAt((toSpawnPosition + randomFactor)).Type != TileType.Ocean) {
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

        public void RegisterCrateSpawned(Action<Crate> onSpawned) {
            cbCrateSpawn += onSpawned;
        }

        public void UnregisterCrateSpawned(Action<Crate> onSpawned) {
            cbCrateSpawn -= onSpawned;
        }

        public void RegisterCrateDespawned(Action<Crate> onDespawned) {
            cbCrateDespawned += onDespawned;
        }

        public void UnregisterCrateDespawned(Action<Crate> onDespawned) {
            cbCrateDespawned -= onDespawned;
        }

        public void RegisterAnyUnitDestroyed(Action<Unit, IWarfare> onAnyUnitDestroyed) {
            cbAnyUnitDestroyed += onAnyUnitDestroyed;
        }

        public void UnregisterUnitDestroyed(Action<Unit, IWarfare> onAnyUnitDestroyed) {
            cbAnyUnitDestroyed -= onAnyUnitDestroyed;
        }

        // we dont need this right now because str cant be build on Ocean tiles only
        // on shore tiles
        public void ChangeWorldGraph(Tile t, bool b) {
            TilesMap[t.X][t.Y] = b;
        }

        public Fertility GetFertility(string ID) {
            return idToFertilities[ID];
        }
        public Tile GetRandomOceanTile() {
            int x = UnityEngine.Random.Range(0, Width);
            int y = UnityEngine.Random.Range(0, Height);
            while (TilesMap[x][y] == false) {
                x = UnityEngine.Random.Range(0, Width);
                y = UnityEngine.Random.Range(0, Height);
            }
            return World.Current.GetTileAt(x, y);
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

#endregion callbacks

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

#endregion igeventable

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

        public class WorldDamage : IWarfare {
            public int PlayerNumber => GameData.WorldNumber;
            private float Damage;
            public float CurrentDamage => Damage;
            public float MaximumDamage => Damage;

            public WorldDamage(float Damage) {
                this.Damage = Damage;
            }

            public DamageType DamageType => PrototypController.Instance.GetWorldDamageType();

            public float MaximumHealth => throw new NotImplementedException();

            public float CurrentHealth => throw new NotImplementedException();

            public bool IsDestroyed => throw new NotImplementedException();

            public Vector2 CurrentPosition => throw new NotImplementedException();

            public Vector2 NextDestinationPosition => throw new NotImplementedException();

            public Vector2 LastMovement => throw new NotImplementedException();

            public ArmorType ArmorType => throw new NotImplementedException();

            public float Speed => throw new NotImplementedException();

            public float Width => throw new NotImplementedException();

            public float Height => throw new NotImplementedException();

            public float Rotation => throw new NotImplementedException();

            public float GetCurrentDamage(ArmorType armorType) {
                return CurrentDamage;
            }

            public bool GiveAttackCommand(ITargetable warfare, bool overrideCurrent = false) {
                return false;
            }

            public void GoIdle() {
                return;
            }

            public bool IsAttackableFrom(IWarfare warfare) {
                throw new NotImplementedException();
            }

            public void TakeDamageFrom(IWarfare warfare) {
                throw new NotImplementedException();
            }

            public uint GetBuildID() {
                return 0;
            }
        }

        public Queue<Tile> GetTilesQueue(Queue<Vector2> q) {
            Queue<Tile> tiles = new Queue<Tile>();
            foreach (Vector2 v in q) {
                tiles.Enqueue(GetTileAt(v));
            }
            return tiles;
        }

        public void CreateIslands(List<MapGenerator.IslandData> doneIslands) {
            foreach (var item in doneIslands) {
                CreateIsland(item);
            }
            WorldGraph = new WorldGraph();
        }

        public Unit GetUnitFromBuildID(uint buildID) {
            return Units.Find(u => u.BuildID == buildID);
        }
    }
}