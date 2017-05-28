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
    // Use this for initialization
    void OnEnable() {
        if (Instance != null) {
            Debug.LogError("There should never be two world controllers.");
        }
		Instance = this;

		GameDataHolder gdh = GameDataHolder.Instance;

		offworldMarket = new OffworldMarket ();

		if (gdh!=null && gdh.loadsavegame!=null && gdh.loadsavegame.Length > 0) {
			LoadWorldData (gdh.loadsavegame);
			gdh.loadsavegame = null;
		} else {
			if (gdh != null) {
				this.world = new World (gdh.width, gdh.height);
			} else
				this.world = new World (100, 100);
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
		world.fixedupdate(Time.fixedDeltaTime * timeMultiplier);
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
	public String GetSaveWorldData() {
		Debug.Log("SaveWorld button was clicked.");
		XmlSerializer serializer = new XmlSerializer( typeof(World) );
		TextWriter writer = new StringWriter();
		serializer.Serialize(writer, world);
		writer.Close();
		// Create/overwrite the save file with the xml text.
		return writer.ToString();
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
	public void LoadWorldData(string saveGameText) {
		Debug.Log("CreateWorldFromSaveFile " + saveGameText);
		// Create a world from our save file data.

		XmlSerializer serializer = new XmlSerializer( typeof(World) );
		TextReader reader = new StringReader( saveGameText );
		world = (World)serializer.Deserialize(reader);
		reader.Close();
		// Center the Camera
		Camera.main.transform.position = new Vector3( world.Width/2, world.Height/2, Camera.main.transform.position.z );
		BuildController.Instance.PlaceAllLoadedStructure ();
		Debug.Log ("LOAD ENDED");
	}

	public void LoadWorldTilesFromWorldFile(string mapname){
		string path = System.IO.Path.Combine (Application.dataPath.Replace ("/Assets", ""), "maps");
		if(Directory.Exists(path) == false){
			Directory.CreateDirectory (path);
		}
		string lines = File.ReadAllText (System.IO.Path.Combine( path , name + ".map" ));
		//read tiles here
	}
}