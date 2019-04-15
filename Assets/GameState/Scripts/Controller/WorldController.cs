using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;


public enum GameSpeed { Paused, StopMotion, Slowest, Slow, Normal, Fast, Fastest, LudicrousSpeed }
public class WorldController : MonoBehaviour {
    public static WorldController Instance { get; protected set; }

    public World World { get; protected set; }
    public Action<GameSpeed> onGameSpeedChange;
    public OffworldMarket offworldMarket;

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
    public void SetGeneratedWorld(World world, Dictionary<Tile, Structure> tileToStructure) {
        this.World = world;
        if (SaveController.IsLoadingSave == false)
            BuildController.Instance.PlaceWorldGeneratedStructure(tileToStructure);
        isLoaded = false;
        offworldMarket = new OffworldMarket();
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
        World.Update(Time.deltaTime * timeMultiplier);
    }
    void FixedUpdate() {
        if (World == null || IsPaused) {
            return;
        }
        World.Fixedupdate(Time.fixedDeltaTime * timeMultiplier);
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

    void OnDestroy() {
        Instance = null;
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
            offworld = offworldMarket
        };
        return wss;
    }
    public void LoadWorld(bool quickload = false) {
        Debug.Log("LoadWorld button was clicked.");
        if (quickload) {
            GameDataHolder gdh = GameDataHolder.Instance;
            gdh.loadsavegame = "QuickSave";//TODO CHANGE THIS TO smth not hardcoded
        }
        // set to loadscreen to reset all data (and purge old references)
        SceneManager.LoadScene("GameStateLoadingScreen");
    }
    public void LoadWorldData() {
        // Create a world from our save file data.
        World.LoadData(MapGenerator.Instance.GetTiles(), GameDataHolder.Instance.Width, GameDataHolder.Instance.Height);
        List<MapGenerator.IslandStruct> structs = MapGenerator.Instance.GetIslandStructs();
        foreach (Island island in World.IslandList) {
            MapGenerator.IslandStruct thisStruct = structs.Find(s =>
                    island.StartTile.X >= s.x && (s.x + s.Width) >= island.StartTile.X &&
                    island.StartTile.Y >= s.y && (s.y + s.Height) >= island.StartTile.Y
            );
            island.myFertilities = thisStruct.fertilities;
            structs.Remove(thisStruct);
            if (thisStruct.Tiles == null)
                Debug.LogError("thisStruct.Tiles is null " + island.StartTile.X + " " + island.StartTile.Y);

            foreach(int id in thisStruct.Ressources.Keys) {
                if (island.HasRessource(id))
                    continue;
                island.Ressources[id] = thisStruct.Ressources[id];
            }
            island.SetTiles(thisStruct.Tiles);
            island.Placement = thisStruct.GetPosition();
        }
        MapGenerator.Instance.Destroy();

        //Now turn the loaded World into a playable World
        List<Structure> loadedStructures = new List<Structure>();
        foreach (Island island in World.IslandList) {
            loadedStructures.AddRange(island.Load());
        }
        loadedStructures.Sort((x, y) => x.buildID.CompareTo(y.buildID));
        BuildController.Instance.PlaceAllLoadedStructure(loadedStructures);
        Debug.Log("LOAD ENDED");
    }

    internal void SetWorldData(WorldSaveState worldsave) {
        World = worldsave.world;
        offworldMarket = worldsave.offworld;
        LoadWorldData();
    }



}

public class WorldSaveState : BaseSaveData {
    public OffworldMarket offworld;
    public World world;
}