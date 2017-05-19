using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEditor;

public class SaveController : MonoBehaviour {

	public static SaveController Instance;
	//TODO autosave here

	WorldController wc;
	EventController ec;
	CameraController cc;
	GameDataHolder gdh;
	PlayerController pc;

	// Use this for initialization
	void Start () {
		if (Instance != null) {
			Debug.LogError("There should never be two SaveController.");
		}
		Instance = this;
		wc = WorldController.Instance;
		ec = EventController.Instance;
		cc = CameraController.Instance;
		gdh = GameDataHolder.Instance;
		pc = PlayerController.Instance;
	}
		

	public void SaveGameState(string name = "autosave"){
		//first pause the world so nothing changes and we can save an 
		bool wasPaused = wc.IsPaused;
		if(wasPaused==false){
			wc.IsPaused = true;
		}
		FileStream saveStream = File.Create (Application.dataPath + "/Save/" + name + ".sav");
		StreamWriter writer = new StreamWriter (saveStream);
		writer.WriteLine (wc.GetSaveWorld());
		writer.WriteLine (ec.GetSaveEvent());
		writer.WriteLine (cc.GetSaveCamera());
		writer.WriteLine (gdh.GetSaveData());
		writer.WriteLine (pc.GetPlayerSaveData());
		writer.Flush ();
//		string temp = FileUtil.GetUniqueTempPathInProject ();
//		System.IO.File.WriteAllText(temp +"world.temp", wc.SaveWorld());
//		System.IO.File.WriteAllText(temp +"event.temp", ec.SaveEvent());





		if(wasPaused == false){
			wc.IsPaused = false;
		}
	}


	public void LoadGameState(string name = "autosave"){
		//first pause the world so nothing changes and we can save an 
		bool wasPaused = wc.IsPaused;
		if(wasPaused==false){
			wc.IsPaused = true;
		}
		FileStream saveStream = File.Create (Application.dataPath + "/Save/" + name + ".sav");
		StreamReader reader = new StreamWriter (saveStream);
		reader.ReadLine (); // world
		reader.ReadLine (); // event
		reader.ReadLine (); // camera
		reader.ReadLine (); // gamedata
		reader.ReadLine (); // player

		//		string temp = FileUtil.GetUniqueTempPathInProject ();
		//		System.IO.File.WriteAllText(temp +"world.temp", wc.SaveWorld());
		//		System.IO.File.WriteAllText(temp +"event.temp", ec.SaveEvent());





		if(wasPaused == false){
			wc.IsPaused = false;
		}
	}


}
