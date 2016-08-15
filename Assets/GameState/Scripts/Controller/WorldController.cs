using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;

public class WorldController : MonoBehaviour {
    public static WorldController Instance { get; protected set; }

    // The world and tile data
    public World world { get; protected set; }
	static string savegamename;

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

	public bool IsModal; // If true, a modal dialog box is open so normal inputs should be ignored.
    // Use this for initialization
    void OnEnable() {
        if (Instance != null) {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;
		if (savegamename!=null) {
			CreateWorldFromSaveFile ();
			savegamename = null;

		} else {
			GameDataHolder gdh = GameObject.FindObjectOfType<GameDataHolder>();
			if (gdh != null) {
				this.world = new World (gdh.width, gdh.height);
				GameObject.Destroy (gdh.gameObject);
			} else
				this.world = new World (1000, 1000);
		}
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    // Update is called once per frame
    void Update() {
		if (world == null || IsPaused) {
			return;
		}
        world.update(Time.deltaTime * timeMultiplier);
    }
	void FixedUpdate (){
		if (world == null || IsPaused) {
			return;
		}
		world.fixedupdate(Time.deltaTime * timeMultiplier);
	}
    internal Tile GetTileAtWorldCoord(Vector3 currFramePosition) {
        return world.GetTileAt(Mathf.FloorToInt(currFramePosition.x), Mathf.FloorToInt(currFramePosition.y));
    }

	public void SaveWorld(string savename) {
		Debug.Log("SaveWorld button was clicked.");
		XmlSerializer serializer = new XmlSerializer( typeof(World) );
		TextWriter writer = new StringWriter();
		serializer.Serialize(writer, world);
		writer.Close();
		// Create/overwrite the save file with the xml text.

		// Make sure the save folder exists.
		if( Directory.Exists(GetSaveGamesPath () ) == false ) {
			// NOTE: This can throw an exception if we can't create the folder,
			// but why would this ever happen? We should, by definition, have the ability
			// to write to our persistent data folder unless something is REALLY broken
			// with the computer/device we're running on.
			Directory.CreateDirectory( GetSaveGamesPath ()  );
		}
		string filePath = System.IO.Path.Combine(GetSaveGamesPath (),savename+".sav") ;
		File.WriteAllText( filePath, writer.ToString() );

	}
	public void LoadWorld(string savegame) {
		Debug.Log("LoadWorld button was clicked.");
		// Reload the scene to reset all data (and purge old references)
		savegamename = savegame;
		SceneManager.LoadScene( SceneManager.GetActiveScene().name );

	}
	void CreateWorldFromSaveFile() {
		Debug.Log("CreateWorldFromSaveFile");
		// Create a world from our save file data.

		XmlSerializer serializer = new XmlSerializer( typeof(World) );
		string saveGameText = File.ReadAllText( System.IO.Path.Combine( GetSaveGamesPath (), savegamename ) );

		TextReader reader = new StringReader( saveGameText );
		Debug.Log (reader.ToString () + " "+saveGameText); 
		world = (World)serializer.Deserialize(reader);
		reader.Close();
		// Center the Camera
		Camera.main.transform.position = new Vector3( world.Width/2, world.Height/2, Camera.main.transform.position.z );
		BuildController.Instance.PlaceAllLoadedStructure ();
		Debug.Log ("LOAD ENDED");
	}
	public void OnClickChangeTimeMultiplier(int multi){
		switch(multi){
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
	public string GetSaveGamesPath(){
		//TODO FIXME change this to documentspath
		return System.IO.Path.Combine(Application.dataPath.Replace ("/Assets","") , "saves");
	}
}