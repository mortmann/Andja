using UnityEngine;
using System.Collections;
using System.IO.Compression;
using System.IO;

public class SaveController : MonoBehaviour {

	//TODO autosave here

	WorldController wc;
	EventController ec;
	CameraController cc;
	GameDataHolder gdh;
	PlayerController pc;

	// Use this for initialization
	void Start () {
		wc = WorldController.Instance;
		ec = EventController.Instance;
		cc = CameraController.Instance;
		gdh = GameDataHolder.Instance;
		pc = PlayerController.Instance;

	}


	public void SaveGameState(string name = "autosave"){




	}

}
