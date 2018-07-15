using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;


public class SaveController : MonoBehaviour {

	public static SaveController Instance;
    public static bool IsLoadingSave = false;
    public bool IsDone = false;
    public float loadingPercantage = 0;

    //TODO autosave here
    const string SaveFileVersion = "0.1.3";
    GameDataHolder GDH => GameDataHolder.Instance;
    WorldController WC => WorldController.Instance;
    EventController EC => EventController.Instance;
    CameraController CC => CameraController.Instance;
    PlayerController PC => PlayerController.Instance;

    void Awake () {
		if (Instance != null) {
			Debug.LogError ("There should never be two SaveController.");
		}
		Instance = this;
	}
	// Use this for initialization
	void Start () { 
		if (GDH!=null && GDH.loadsavegame!=null && GDH.loadsavegame.Length > 0) {
            ClearConsole();
            Debug.Log("LOADING SAVEGAME " + GDH.loadsavegame);
            IsLoadingSave = true;
            StartCoroutine(LoadGameState (GDH.loadsavegame));
			GDH.loadsavegame = null;
		} else {
            IsLoadingSave = false;
        }
	}
    static void ClearConsole() {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");

        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

        clearMethod.Invoke(null, null);
    }

    public void Update(){
		//autosave every soandso 
		//maybe option to choose frequenzy
	}

	public void SaveGameState(string name = "autosave"){
		//first pause the world so nothing changes and we can save an 
		bool wasPaused = WC.IsPaused;
		if(wasPaused==false){
			WC.IsPaused = true;
		}
		string path = System.IO.Path.Combine (GetSaveGamesPath (), name + ".sav");

        SaveState savestate = new SaveState {
            safefileversion = (SaveFileVersion),
            gamedata = (GDH.GetSaveGameData()),
            pcs = (PC.GetSavePlayerData()),
            world = (WC.GetSaveWorldData()),
            ges = (EC.GetSaveGameEventData()),
            camera = (CC.GetSaveCamera())
        };

        System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(savestate,Formatting.Indented,
				new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore,
					PreserveReferencesHandling = PreserveReferencesHandling.Objects, 
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
					TypeNameHandling = TypeNameHandling.Auto
				}
			) 
		);

		if(wasPaused == false){
			WC.IsPaused = false;
		}
	}

    private static List<Worker> loadWorker;
    public static void AddWorkerForLoad(Worker w) {
        if (loadWorker == null) {
            loadWorker = new List<Worker>();
        }
        loadWorker.Add(w);
    }
    public static List<Worker> GetLoadWorker() {
        if (loadWorker == null)
            return null;
        List<Worker> tempLoadWorker = new List<Worker>(loadWorker);
        loadWorker = null;
        return tempLoadWorker;
    }
    public string GetSaveGamesPath(){
		//TODO FIXME change this to documentspath
		return System.IO.Path.Combine(Application.dataPath.Replace ("/Assets","") , "saves");
	}

	public IEnumerator LoadGameState(string name = "autosave"){
		//first pause the world so nothing changes and we can save an 		
		string alllines = System.IO.File.ReadAllText (System.IO.Path.Combine (GetSaveGamesPath (), name + ".sav"));
		SaveState state = JsonConvert.DeserializeObject<SaveState> (alllines,new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				PreserveReferencesHandling = PreserveReferencesHandling.Objects, 
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				TypeNameHandling = TypeNameHandling.Auto
			});
		if(SaveFileVersion!=state.safefileversion){
			Debug.LogError ("Mismatch of SaveFile Versions " + state.safefileversion + " & " + SaveFileVersion);
            yield break;
		}
		PrototypController.Instance.LoadFromXML ();

		GDH.LoadGameData(state.gamedata); // gamedata
        while (MapGenerator.Instance.IsDone == false)
            yield return null;
        loadingPercantage += 0.2f;
        PlayerController.SetPlayerData(state.pcs); // player
        loadingPercantage += 0.2f;
        WorldController.SetWorldData(state.world); // world
        loadingPercantage += 0.2f;
        EventController.SetGameEventData(state.ges); // event
        loadingPercantage += 0.2f;
        CameraController.SetSaveCameraData(state.camera); // camera
        loadingPercantage += 0.2f;

        IsDone = true;
        yield return null;
	}
	[Serializable]
	public class SaveState {
		public string safefileversion;
		public GameData gamedata;
		public WorldSaveState world;
		public PlayerControllerSave pcs;
		public GameEventSave ges;
		public CameraSave camera;
	}
}

