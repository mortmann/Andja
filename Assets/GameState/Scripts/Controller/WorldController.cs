using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;

public class WorldController : MonoBehaviour {
    public static WorldController Instance { get; protected set; }

    // The world and tile data
    public World World { get; protected set; }

	public OffworldMarket offworldMarket;

	public float timeMultiplier = 1;
	private bool _isPaused = false;
	public bool IsPaused {
		get {
			return  _isPaused || IsModal || Loading.IsLoading;
		}
		set {
			_isPaused = value;
		}
	}
	public float DeltaTime { get { return Time.deltaTime * timeMultiplier;}}
	public float FixedDeltaTime { get { return Time.fixedDeltaTime * timeMultiplier;}}

	public bool IsModal; // If true, a modal dialog box is open so normal inputs should be ignored.

	public bool isLoaded = true;
    // Use this for initialization
    void OnEnable() {
        Debug.Log("Intializing World Controller");
        if (Instance != null) {
			Debug.LogError ("There should never be two world controllers.");
		} else {
            Instance = this;
        }
    }

    public void SetGeneratedWorld(World world, Dictionary<Tile,Structure> tileToStructure) {
        this.World = world;
        if(SaveController.IsLoadingSave == false)
            BuildController.Instance.PlaceWorldGeneratedStructure(tileToStructure);
        isLoaded = false;
        offworldMarket = new OffworldMarket();
    }

    void OnDestroy() {
        Instance = null;
    }
    // Update is called once per frame
    void Update() {
		if (World == null || IsPaused) {
			return;
		}
        World.Update(Time.deltaTime * timeMultiplier);
    }
	void FixedUpdate (){
		if (World == null || IsPaused) {
			return;
		}
		World.Fixedupdate(Time.fixedDeltaTime * timeMultiplier);
	}

	public void TogglePause(){
		if(IsPaused){
			OnClickChangeTimeMultiplier (0);
		} else {
			OnClickChangeTimeMultiplier (-1);
		}
	}
	public void OnClickChangeTimeMultiplier(int multi){
		switch(multi){
		case -1:
			IsPaused = !IsPaused; 
			break;
		case 0:
			IsPaused = !IsPaused; 
			break;
		case 1:
			timeMultiplier = 0.5f;
			IsPaused = false;
			break;
		case 2:
			timeMultiplier = 0.75f;
			IsPaused = false;
			break;
		case 3:
			timeMultiplier = 1.5f;
			IsPaused = false;
			break;
		case 4:
			timeMultiplier = 2;
			IsPaused = false;
			break;
		}
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
		if(quickload){
			GameDataHolder gdh = GameDataHolder.Instance;
			gdh.loadsavegame = "QuickSave";//TODO CHANGE THIS TO smth not hardcoded
		}
		// set to loadscreen to reset all data (and purge old references)
		SceneManager.LoadScene( "GameStateLoadingScreen" );
	}
	public void LoadWorldData() {
		// Create a world from our save file data.
        World.LoadData(MapGenerator.Instance.GetTiles(),GameDataHolder.Instance.Width,GameDataHolder.Instance.Height);
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
            island.SetTiles(thisStruct.Tiles);
            island.Placement = thisStruct.GetPosition();
        }
        MapGenerator.Instance.Destroy();

        //Now turn the loaded World into a playable World
        List<Structure> loadedStructures = new List<Structure>();
		foreach (Island island in World.IslandList) {
			loadedStructures.AddRange(island.Load ());
		}
		loadedStructures.Sort ((x, y) => x.buildID.CompareTo(y.buildID) );
		BuildController.Instance.PlaceAllLoadedStructure (loadedStructures);
		Debug.Log ("LOAD ENDED");
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