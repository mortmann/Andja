using UnityEngine;
using System.Collections;
using System.IO.Compression;
using System.IO;

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








		if(wasPaused == false){
			wc.IsPaused = false;
		}
	}

}
