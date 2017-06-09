using UnityEditor;
using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
		string[] strings = new string[6];
		strings[0] = (SaveFileVersion);
		strings[1] = (gdh.GetSaveGameData());
		strings[2] =  (pc.GetSavePlayerData ());
		strings[3] =  (wc.GetSaveWorldData ());
		strings[4] =  (ec.GetSaveGameEventData ());
		strings[5] = (cc.GetSaveCamera ());

		System.IO.File.WriteAllText(path, JsonUtil.arrayToJson<string> (strings));
//		FileStream saveStream = File.Create (System.IO.Path.Combine( GetSaveGamesPath() , name + ".sav" ));
//		StreamWriter writer = new StreamWriter (saveStream);
//		writer.WriteLine (Regex.Replace(gdh.GetSaveGameData(), @"\s+", " "));
//		writer.WriteLine (Regex.Replace(pc.GetSavePlayerData(), @"\s+", " "));
//		writer.WriteLine (Regex.Replace(wc.GetSaveWorldData(), @"\s+", " "));
//		writer.WriteLine (Regex.Replace(ec.GetSaveGameEventData(), @"\s+", " "));
//		writer.WriteLine (Regex.Replace(cc.GetSaveCamera(), @"\s+", " "));
//		writer.Flush ();
//		writer.Close ();
//		string temp = FileUtil.GetUniqueTempPathInProject ();
//		System.IO.File.WriteAllText(temp +"world.temp", wc.SaveWorld());
//		System.IO.File.WriteAllText(temp +"event.temp", ec.SaveEvent());

		if(wasPaused == false){
			wc.IsPaused = false;
		}
	}

	public string GetSaveGamesPath(){
		//TODO FIXME change this to documentspath
		return System.IO.Path.Combine(Application.dataPath.Replace ("/Assets","") , "saves");
	}

	public void LoadGameState(string name = "autosave"){
		//first pause the world so nothing changes and we can save an 
		bool wasPaused = wc.IsPaused;
		if(wasPaused==false){
			wc.IsPaused = true;
		}
		string alllines = System.IO.File.ReadAllText (System.IO.Path.Combine (GetSaveGamesPath (), name + ".sav"));
		string[] lines = JsonUtil.getJsonArray<string> (alllines);
		if(SaveFileVersion!=lines[0]){
			Debug.LogError ("Mismatch of SaveFile Versions " + lines[0] + " " + SaveFileVersion);
			return;
		}
		gdh.LoadGameData(lines[1]); // gamedata
		pc.LoadPlayerData(lines[2]); // player
		wc.LoadWorldData (lines[3]); // world
		ec.LoadGameEventData(lines[4]); // event
		cc.LoadSaveCameraData(lines[5]); // camera


		if(wasPaused == false){
			wc.IsPaused = false;
		}
	}


}
