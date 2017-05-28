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
		FileStream saveStream = File.Create (System.IO.Path.Combine( GetSaveGamesPath() , name + ".sav" ));
		StreamWriter writer = new StreamWriter (saveStream);
		writer.WriteLine (Regex.Replace(gdh.GetSaveGameData(), @"\s+", " "));
		writer.WriteLine (Regex.Replace(pc.GetSavePlayerData(), @"\s+", " "));
		writer.WriteLine (Regex.Replace(wc.GetSaveWorldData(), @"\s+", " "));
		writer.WriteLine (Regex.Replace(ec.GetSaveGameEventData(), @"\s+", " "));
		writer.WriteLine (Regex.Replace(cc.GetSaveCamera(), @"\s+", " "));
		writer.Flush ();
		writer.Close ();
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
		FileStream readStream = File.OpenRead (System.IO.Path.Combine( GetSaveGamesPath() , name + ".sav" ));
		StreamReader reader = new StreamReader (readStream);
		gdh.LoadGameData(reader.ReadLine ()); // gamedata
		pc.LoadPlayerData(reader.ReadLine ()); // player
		wc.LoadWorldData (reader.ReadLine ()); // world
		ec.LoadGameEventData(reader.ReadLine ()); // event
		cc.LoadSaveCameraData(reader.ReadLine ()); // camera
		reader.Close ();

		if(wasPaused == false){
			wc.IsPaused = false;
		}
	}


}
