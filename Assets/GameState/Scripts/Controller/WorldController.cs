using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;

public class WorldController : MonoBehaviour {
    public static WorldController Instance { get; protected set; }
    static WorldSaveState save;

    // The world and tile data
    public World World { get; protected set; }

	public OffworldMarket offworldMarket;

	public float timeMultiplier = 1;
	private bool _isPaused = false;
	public bool IsPaused {
		get {
			return  _isPaused || IsModal;
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
    void Awake() {
		if (Instance != null) {
			Debug.LogError ("There should never be two world controllers.");
		}
		Instance = this;

		GameDataHolder gdh = GameDataHolder.Instance;
		offworldMarket = new OffworldMarket ();
		if (SaveController.IsLoadingSave) {
//			SaveController.Instance.LoadGameState (gdh.loadsavegame);
//			gdh.loadsavegame = null;
		} else {
			if (gdh != null) {
				MapGenerator mg = FindObjectOfType<MapGenerator> ();
                Dictionary<Tile,Structure> tileToStructure = mg.GetStructures();
                this.World = mg.GetWorld();
                BuildController.Instance.PlaceWorldGeneratedStructure(tileToStructure);

                isLoaded = false;
			} 
		}
        if (save != null) {
            LoadWorldData();
            save = null;
        }
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
		offworldMarket = save.offworld;
		// Create a world from our save file data.
		World = save.world;
        World.SetTiles(MapGenerator.Instance.GetTiles(),GameDataHolder.Instance.Width,GameDataHolder.Instance.Height);

        List<MapGenerator.IslandStruct> structs = MapGenerator.Instance.GetIslandStructs();
        foreach(MapGenerator.IslandStruct s in structs) {
            Debug.Log(s.y +" " + (s.y + s.Width) + " " + s.y + " " + (s.y + s.Height));
        }
        foreach (Island island in World.IslandList) {
            MapGenerator.IslandStruct thisStruct = structs.Find(s =>
                    island.StartTile.X >= s.x && (s.x + s.Width) >= island.StartTile.X &&
                    island.StartTile.Y >= s.y && (s.y + s.Height) >= island.StartTile.Y
            );
            structs.Remove(thisStruct);
            if (thisStruct.Tiles == null)
                Debug.LogError("thisStruct.Tiles is null " + island.StartTile.X + " " + island.StartTile.Y);
            island.SetTiles(thisStruct.Tiles);
        }
        MapGenerator.Instance.Destroy();

        //Now turn the loaded World into a playable World
        List<Structure> loadedStructures = new List<Structure>();
		foreach (Island island in World.IslandList) {
			loadedStructures.AddRange(island.Load ());
		}
		loadedStructures.Sort ((x, y) => x.buildID.CompareTo(y.buildID) );
		BuildController.Instance.PlaceAllLoadedStructure (loadedStructures);
		if(loadWorker!=null){
			GameObject.FindObjectOfType<WorkerSpriteController> ().loadedWorker = loadWorker;
		}
		Debug.Log ("LOAD ENDED");
	}

    internal static void SetWorldData(WorldSaveState world) {
        save = world;
    }

	List<Worker> loadWorker;
	public void AddWorkerForLoad(Worker w){
		if(loadWorker==null){
			loadWorker = new List<Worker> ();
		}
		loadWorker.Add (w);
	}

}

public class WorldSaveState {
	public OffworldMarket offworld;
	public World world;
}