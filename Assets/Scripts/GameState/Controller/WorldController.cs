using Andja.Model;
using Andja.Model.Generator;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Controller {

    public enum GameSpeed { Paused, StopMotion, Slowest, Slow, Normal, Fast, Fastest, LudicrousSpeed }
    /// <summary>
    /// Handles updating the units, structures, pirates, flyingTraders and offworldmarket.
    /// Controls the deltaTime * GameSpeed aswell as Pausing
    /// RandomState is set here.
    /// </summary>
    public class WorldController : MonoBehaviour {
        public static WorldController Instance { get; protected set; }

        public World World { get; protected set; }
        public Rect SpawningRect;
        public Action<GameSpeed, float> cbGameSpeedChange;
        public OffworldMarket offworldMarket;
        public FlyingTrader flyingTrader;
        public Pirate pirate;
        public Action<Unit> cbWorldCreatedUnit;
        public Action<Unit, IWarfare> cbWorldUnitDestroyed;
        public float TimeMultiplier { get; private set; } = 1;
        private bool _isPaused = false;

        public bool IsPaused {
            get {
                return _isPaused || Loading.IsLoading;
            }
            set {
                _isPaused = value;
            }
        }

        public float DeltaTime { get { return Time.deltaTime * TimeMultiplier; } }
        public float FixedDeltaTime { get { return Time.fixedDeltaTime * TimeMultiplier; } }

        public bool isLoaded = true;

        public GameSpeed CurrentSpeed {
            get {
                if (TimeMultiplier == 0 || IsPaused) return GameSpeed.Paused;
                if (TimeMultiplier < 0.5f) return GameSpeed.StopMotion;
                if (TimeMultiplier < 0.75f) return GameSpeed.Slowest;
                if (TimeMultiplier < 1f) return GameSpeed.Slow;
                if (TimeMultiplier == 1f) return GameSpeed.Normal;
                if (TimeMultiplier <= 2f) return GameSpeed.Fast;
                if (TimeMultiplier <= 4f) return GameSpeed.Fastest;
                return GameSpeed.LudicrousSpeed;
            }
        }

        internal void SetRandomSeed() {
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        }

        // Use this for initialization
        private void OnEnable() {
            Debug.Log("Intializing World Controller");
            if (Instance != null) {
                Debug.LogError("There should never be two world controllers.");
            }
            else {
                Instance = this;
            }
            new Pathfinding.PathfindingThreadHandler();
        }

        public void Start() {
            EventController.Instance.RegisterOnEvent(OnEventCreated, OnEventEnded);
        }

        public void SetGeneratedWorld(World world, Dictionary<Tile, Structure> tileToStructure, Rect spawnRect, Vector2[] spawnPoints) {
            this.World = world;
            World.RegisterUnitCreated(cbWorldCreatedUnit);
            World.RegisterAnyUnitDestroyed(cbWorldUnitDestroyed);
            if (SaveController.IsLoadingSave == false && tileToStructure != null && tileToStructure.Count > 0) {
                BuildController.Instance.PlaceWorldGeneratedStructure(tileToStructure);
                if (GameData.flyingTraders)
                    flyingTrader = new FlyingTrader();
                if (GameData.pirates)
                    pirate = new Pirate();
                offworldMarket = new OffworldMarket();
                CreatePlayerStarts(spawnPoints);
            }
            SpawningRect = spawnRect;
            isLoaded = false;
        }
        private void CreatePlayerStarts(Vector2[] spawnPoints) {
            StartingLoadout loadout = GameData.Instance.Loadout;
            if (loadout == null)
                return;
            for (int i = 0; i < PlayerController.Players.Count; i++) {
                Item[] startItems = loadout.GetItemsCopy();
                Player player = PlayerController.Players[i];
                Vector2 shipSpawn = new Vector2(-1, -1);
                //TODO: place structures! -- for now only warehouse?
                if (loadout.Structures != null && player.IsHuman && Array.Exists(loadout.Structures, x => x is WarehouseStructure)) {
                    //here then place em
                    AIPlayer temp = new AIPlayer(player, true);
                    temp.DecideIsland(true);
                    if (player.Cities.Count == 0) {
                        Debug.LogError("-- Could not fit any Warehouse on the selected island --");
                    }
                    else {
                        shipSpawn = player.Cities[0].warehouse.tradeTile.Vector2;
                        foreach (Item item in startItems) {
                            if(loadout.Units != null && Array.Exists(loadout.Units,x=>x is Ship s && s.CannonItem.ID == item.ID)) {
                                continue;
                            }
                            player.Cities[0].Inventory.AddItem(item);
                        }
                    }
                }
                if (loadout.Units != null) {
                    foreach (Unit prefab in loadout.Units) {
                        if (prefab.IsShip == false) {
                            Debug.LogWarning("Unit is not a ship -- currently not supported to spawn!");
                            continue;
                        }
                        if (shipSpawn.x == -1) {
                            shipSpawn = spawnPoints[i % spawnPoints.Length];
                        }
                        Tile start = World.GetTileAt(shipSpawn);
                        Unit unit = World.CreateUnit(prefab, player, start);
                        foreach (Item item in startItems) {
                            unit.TryToAddItem(item);
                            if (unit is Ship s) {
                                if (item.ID == s.CannonItem.ID) {
                                    s.AddCannonsFromInventory(true);
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Does not change the gamespeed.
        /// </summary>
        internal void Pause() {
            IsPaused = true;
        }

        /// <summary>
        /// Does not change the gamespeed.
        /// </summary>
        internal void Unpause() {
            IsPaused = false;
        }

        protected void OnEventCreated(GameEvent ge) {
            World.OnEventCreate(ge);
        }

        protected void OnEventEnded(GameEvent ge) {
            World.OnEventEnded(ge);
        }

        private void Update() {
            if (World == null || IsPaused) {
                return;
            }
            World.Update(DeltaTime);
            offworldMarket?.Update(DeltaTime);
            flyingTrader?.Update(DeltaTime);
            pirate?.Update(DeltaTime);
        }

        private void FixedUpdate() {
            if (World == null || IsPaused) {
                return;
            }
            World.FixedUpdate(DeltaTime);
        }

        public void TogglePause() {
            if (IsPaused) {
                ChangeGameSpeed(GameSpeed.Normal);
            }
            else {
                ChangeGameSpeed(GameSpeed.Paused);
            }
        }

        public void ChangeGameSpeed(GameSpeed multi) {
            switch (multi) {
                case GameSpeed.Paused:
                    SetSpeed(0f);
                    break;

                case GameSpeed.Slowest:
                    SetSpeed(0.5f);
                    break;

                case GameSpeed.Slow:
                    SetSpeed(0.75f);
                    break;

                case GameSpeed.Normal:
                    SetSpeed(1f);
                    break;

                case GameSpeed.Fast:
                    SetSpeed(2f);
                    break;

                case GameSpeed.Fastest:
                    SetSpeed(4f);
                    break;
            }
        }

        internal void SetSpeed(float speed) {
            if (speed == 0) {
                IsPaused = true;
            }
            else {
                IsPaused = false;
            }
            TimeMultiplier = Mathf.Clamp(speed, 0, 100);
            cbGameSpeedChange?.Invoke(CurrentSpeed, speed);
        }

        public void RegisterWorldUnitCreated(Action<Unit> callbackfunc) {
            cbWorldCreatedUnit += callbackfunc;
        }

        public void UnregisterWorldUnitCreated(Action<Unit> callbackfunc) {
            cbWorldCreatedUnit -= callbackfunc;
        }

        public void RegisterWorldUnitDestroyed(Action<Unit, IWarfare> callbackfunc) {
            cbWorldUnitDestroyed += callbackfunc;
        }

        public void UnregisterWorldUnitDestroyed(Action<Unit, IWarfare> callbackfunc) {
            cbWorldUnitDestroyed -= callbackfunc;
        }

        public void RegisterSpeedChange(Action<GameSpeed, float> callbackfunc) {
            cbGameSpeedChange += callbackfunc;
        }

        public void UnregisterSpeedChange(Action<GameSpeed, float> callbackfunc) {
            cbGameSpeedChange -= callbackfunc;
        }
        private void OnDestroy() {
            Instance = null;
            flyingTrader?.OnDestroy();
            World?.Destroy();
            Pathfinding.PathfindingThreadHandler.Stop();
        }

        ///
        ///
        /// ONLY SAVE/LOAD SUFF UNDERNEATH HERE
        ///

        /// <summary>
        /// Saves the world.
        /// </summary>
        /// <param name="savename">Savename.</param>
        public WorldSaveState GetSaveWorldData() {
            WorldSaveState wss = new WorldSaveState {
                world = World,
                offworld = offworldMarket,
                flyingTrader = flyingTrader,
                pirate = pirate,
                // does not work with newtonsoft - atleast not with out of the box
                RandomSeed = JsonUtility.ToJson(UnityEngine.Random.state)
            };
            return wss;
        }

        public void LoadWorldData(UnityEngine.Random.State state) {
            // Create a world from our save file data.
            //World.LoadData(MapGenerator.Instance.GetTiles(), GameDataHolder.Width, GameDataHolder.Height);
            MapGenerator.Instance.Destroy();
            List<MapGenerator.IslandData> structs = MapGenerator.Instance.GetIslandStructs();
            foreach (Island island in World.Islands) {
                MapGenerator.IslandData thisStruct = structs.Find(s =>
                        island.StartTile.X >= s.x && (s.x + s.Width) >= island.StartTile.X &&
                        island.StartTile.Y >= s.y && (s.y + s.Height) >= island.StartTile.Y
                );
                island.Fertilities = thisStruct.GetFertilities();
                structs.Remove(thisStruct);
                if (thisStruct.Tiles == null)
                    Debug.LogError("thisStruct.Tiles is null " + island.StartTile.X + " " + island.StartTile.Y);

                foreach (string id in thisStruct.Resources.Keys) {
                    if (island.HasResource(id))
                        continue;
                    island.Resources[id] = thisStruct.Resources[id];
                }
                island.SetTiles(thisStruct.Tiles);
                island.Placement = thisStruct.GetPosition();
            }
            World.Load();
            //Now turn the loaded World into a playable World
            List<Structure> loadedStructures = new List<Structure>();
            foreach (Island island in World.Islands) {
                loadedStructures.AddRange(island.Load());
            }
            BuildController.Instance.PlaceAllLoadedStructure(loadedStructures);
            flyingTrader?.Load();
            pirate?.Load();
            PlayerController.Instance.AfterWorldLoad();
            UnityEngine.Random.state = state;
        }

        internal void SetWorldData(WorldSaveState worldsave) {
            World = worldsave.world;
            offworldMarket = worldsave.offworld;
            flyingTrader = worldsave.flyingTrader;
            pirate = worldsave.pirate;
            LoadWorldData(JsonUtility.FromJson<UnityEngine.Random.State>(worldsave.RandomSeed));
        }
    }

    public class WorldSaveState : BaseSaveData {
        public OffworldMarket offworld;
        public World world;
        public FlyingTrader flyingTrader;
        public Pirate pirate;
        public string RandomSeed;
    }
}