using UnityEditor;
using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System;
public class SaveController : MonoBehaviour {

	public static SaveController Instance;
	//TODO autosave here
	const string SaveFileVersion = "0.1.1";
	WorldController wc;
	EventController ec;
	CameraController cc;
	GameDataHolder gdh;
	PlayerController pc;
	void Awake () {
		if (Instance != null) {
			Debug.LogError ("There should never be two SaveController.");
		}
		Instance = this;
	}
	// Use this for initialization
	void Start () {
		wc = WorldController.Instance;
		ec = EventController.Instance;
		cc = CameraController.Instance;
		gdh = GameDataHolder.Instance;
		pc = PlayerController.Instance;
		if (gdh!=null && gdh.loadsavegame!=null && gdh.loadsavegame.Length > 0) {
			LoadGameState (gdh.loadsavegame);
			gdh.loadsavegame = null;
		}
//		Item[] items = new Item[4];
//		items [0] = new Item (1, "test", ItemType.Build, 12);
//		items [1] = new Item (1, "test", ItemType.Build, 53);
//		items [2] = new Item (2, "test2", ItemType.Intermediate, 12);
//		items [3] = new Item (3, "test3", ItemType.Luxury, 12);
//		TradeRoute.Trade t = new TradeRoute.Trade (World.current.islandList [0].myCities [0], items, items);
//
//		string json =  (JsonConvert.SerializeObject(t,
//			new JsonSerializerSettings
//			{
//				NullValueHandling = NullValueHandling.Ignore,
//				PreserveReferencesHandling = PreserveReferencesHandling.Objects 
//			}) );
//		TradeRoute.Trade t2 = JsonConvert.DeserializeObject<TradeRoute.Trade> (json);
//		Debug.Log (t2.getting[0]);
//		Debug.Log (t2.getting[1]);
//		Debug.Log (t2.giving[0]);
//		Debug.Log (t2.giving[1]);
//

//		LoadGameState ("sae");
	}
	public void Update(){
		//autosave every soandso 
		//maybe option to choose frequenzy

	}

	public void SaveGameState(string name = "autosave"){
		//first pause the world so nothing changes and we can save an 
		bool wasPaused = wc.IsPaused;
		if(wasPaused==false){
			wc.IsPaused = true;
		}
		string path = System.IO.Path.Combine (GetSaveGamesPath (), name + ".sav");

		SaveState savestate = new SaveState ();
		savestate.safefileversion = (SaveFileVersion);
		savestate.gamedata = (gdh.GetSaveGameData());
		savestate.pcs =  (pc.GetSavePlayerData ());
		savestate.world =  (wc.GetSaveWorldData ());
		savestate.ges =  (ec.GetSaveGameEventData ());
		savestate.camera = (cc.GetSaveCamera ());


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
			wc.IsPaused = false;
		}
	}

	public string GetSaveGamesPath(){
		//TODO FIXME change this to documentspath
		return System.IO.Path.Combine(Application.dataPath.Replace ("/Assets","") , "saves");
	}

	public void LoadGameState(string name = "autosave"){
		if(wc==null){
			return;
		}
		//first pause the world so nothing changes and we can save an 
		bool wasPaused = wc.IsPaused;
		if(wasPaused==false){
			wc.IsPaused = true;
		}
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
			return;
		}
		PrototypController.Instance.LoadFromXML ();

		gdh.LoadGameData(state.gamedata); // gamedata
		pc.LoadPlayerData(state.pcs); // player
		wc.LoadWorldData (state.world); // world
		ec.LoadGameEventData(state.ges); // event
		cc.LoadSaveCameraData(state.camera); // camera


		if(wasPaused == false){
			wc.IsPaused = false;
		}
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

