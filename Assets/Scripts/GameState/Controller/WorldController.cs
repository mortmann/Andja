using System;
using UnityEngine;
using System.Collections.Generic;


public enum GameSpeed { Paused, StopMotion, Slowest, Slow, Normal, Fast, Fastest, LudicrousSpeed }
public class WorldController : MonoBehaviour {
    public static WorldController Instance { get; protected set; }

    public World World { get; protected set; }
    public Rect SpawningRect;
    public Action<GameSpeed> onGameSpeedChange;
    public OffworldMarket offworldMarket;
    public FlyingTrader flyingTrader;
    public Pirate pirate;
    public Action<Unit> cbWorldCreatedUnit;
    public float timeMultiplier = 1;
    private bool _isPaused = false;
    public bool IsPaused {
        get {
            return _isPaused || Loading.IsLoading;
        }
        set {
            _isPaused = value;
        }
    }
    public float DeltaTime { get { return Time.deltaTime * timeMultiplier; } }
    public float FixedDeltaTime { get { return Time.fixedDeltaTime * timeMultiplier; } }

    public bool isLoaded = true;
    public GameSpeed CurrentSpeed {
        get {
            if (timeMultiplier == 0 || IsPaused) return GameSpeed.Paused;
            if (timeMultiplier < 0.5f ) return GameSpeed.StopMotion;
            if (timeMultiplier < 0.75f) return GameSpeed.Slowest;
            if (timeMultiplier < 1f) return GameSpeed.Slow;
            if (timeMultiplier == 1f) return GameSpeed.Normal;
            if (timeMultiplier <= 1.5f) return GameSpeed.Fast;
            if (timeMultiplier <= 2f) return GameSpeed.Fastest;
            return GameSpeed.LudicrousSpeed;
        }
    }

    internal void SetRandomSeed() {
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
    }

    // Use this for initialization
    void OnEnable() {
        Debug.Log("Intializing World Controller");
        if (Instance != null) {
            Debug.LogError("There should never be two world controllers.");
        }
        else {
            Instance = this;
        }
    }
    public void Start() {
        EventController.Instance.RegisterOnEvent(OnEventCreated, OnEventEnded);
    }
    public void SetGeneratedWorld(World world, Dictionary<Tile, Structure> tileToStructure, Rect spawnRect, Vector2[] spawnPoints) {
        this.World = world;
        World.RegisterUnitCreated(OnUnitCreated);
        if (SaveController.IsLoadingSave == false && tileToStructure!=null && tileToStructure.Count>0) {
            BuildController.Instance.PlaceWorldGeneratedStructure(tileToStructure);
            CreatePlayerStarts(spawnPoints);        
            offworldMarket = new OffworldMarket();
            if(GameDataHolder.flyingTraders)
                flyingTrader = new FlyingTrader();
            if(GameDataHolder.pirates)
                pirate = new Pirate();
        }
        SpawningRect = spawnRect;
        isLoaded = false;
    }

    private void CreatePlayerStarts(Vector2[] spawnPoints) {
        StartingLoadout loadout = GameDataHolder.Instance.Loadout;
        if (loadout == null)
            return;
        for (int i = 0; i < PlayerController.Players.Count; i++) {
            Item[] startItems = loadout.GetItemsCopy();
            Player player = PlayerController.Players[i];
            Vector2 shipSpawn = new Vector2(-1,-1);
            //TODO: place structures! -- for now only warehouse?
            if(loadout.Structures!=null && player.IsHuman && Array.Exists(loadout.Structures,x=>x is WarehouseStructure)) {
                //here then place em
                AIPlayer temp = new AIPlayer(player);
                temp.DecideIsland(true);
                if(player.Cities.Count==0) {
                    Debug.LogError("-- Could not fit any Warehouse on the selected island --");
                } else {
                    shipSpawn = player.Cities[0].warehouse.tradeTile.Vector2;
                    player.Cities[0].Inventory.AddItems(startItems);
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
                    }
                }
            }
            
        }
    }

    protected void OnEventCreated(GameEvent ge) {
        World.OnEventCreate(ge);
    }
    protected void OnEventEnded(GameEvent ge) {
        World.OnEventEnded(ge);
    }
    
    // Update is called once per frame
    void Update() {
        if (World == null || IsPaused) {
            return;
        }
        World.Update(DeltaTime);
        offworldMarket.Update(DeltaTime);
        flyingTrader.Update(DeltaTime);
    }
    void FixedUpdate() {
        if (World == null || IsPaused) {
            return;
        }
        World.FixedUpdate(Time.fixedDeltaTime * timeMultiplier);
    }

    public void TogglePause() {
        if (IsPaused) {
            ChangeGameSpeed(GameSpeed.Paused);
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
                SetSpeed(1.5f);
                break;
            case GameSpeed.Fastest:
                SetSpeed(2f);
                break;
        }
    }

    internal void SetSpeed(float speed) {
        timeMultiplier = Mathf.Clamp(speed, 0, 100);
        if (timeMultiplier == 0)
            IsPaused = true;
        else
            IsPaused = false;
        onGameSpeedChange?.Invoke(CurrentSpeed);
    }
    private void OnUnitCreated(Unit unit) {
        cbWorldCreatedUnit?.Invoke(unit);
    }
    public void RegisterWorldUnitCreated(Action<Unit> callbackfunc) {
        cbWorldCreatedUnit += callbackfunc;
    }
    public void UnregisterWorldUnitCreated(Action<Unit> callbackfunc) {
        cbWorldCreatedUnit -= callbackfunc;
    }
    void OnDestroy() {
        Instance = null;
        flyingTrader?.OnDestroy();
        World?.Destroy();
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
        };
        return wss;
    }
    
    public void LoadWorldData() {
        // Create a world from our save file data.
        //World.LoadData(MapGenerator.Instance.GetTiles(), GameDataHolder.Width, GameDataHolder.Height);
        MapGenerator.Instance.Destroy();
        List<MapGenerator.IslandStruct> structs = MapGenerator.Instance.GetIslandStructs();
        foreach (Island island in World.Islands) {
            MapGenerator.IslandStruct thisStruct = structs.Find(s =>
                    island.StartTile.X >= s.x && (s.x + s.Width) >= island.StartTile.X &&
                    island.StartTile.Y >= s.y && (s.y + s.Height) >= island.StartTile.Y
            );
            island.Fertilities = thisStruct.fertilities;
            structs.Remove(thisStruct);
            if (thisStruct.Tiles == null)
                Debug.LogError("thisStruct.Tiles is null " + island.StartTile.X + " " + island.StartTile.Y);

            foreach(string id in thisStruct.Ressources.Keys) {
                if (island.HasRessource(id))
                    continue;
                island.Ressources[id] = thisStruct.Ressources[id];
            }
            island.SetTiles(thisStruct.Tiles);
            island.Placement = thisStruct.GetPosition();
        }
        offworldMarket = new OffworldMarket();
        //Now turn the loaded World into a playable World
        List<Structure> loadedStructures = new List<Structure>();
        foreach (Island island in World.Islands) {
            loadedStructures.AddRange(island.Load());
        }
        loadedStructures.Sort((x, y) => x.buildID.CompareTo(y.buildID));
        BuildController.Instance.PlaceAllLoadedStructure(loadedStructures);
    }

    internal void SetWorldData(WorldSaveState worldsave) {
        World = worldsave.world;
        offworldMarket = worldsave.offworld;
        flyingTrader = worldsave.flyingTrader;
        pirate = worldsave.pirate;
        LoadWorldData();
    }



}

public class WorldSaveState : BaseSaveData {
    public OffworldMarket offworld;
    public World world;
    public FlyingTrader flyingTrader;
    public Pirate pirate;
}