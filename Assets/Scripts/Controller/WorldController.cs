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
	static bool loadWorld = false;
	public float timeMultiplier = 1;
	public bool isPaused = false;

    // Use this for initialization
    void OnEnable() {
        if (Instance != null) {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;
		if (loadWorld) {
			loadWorld = false;
			CreateWorldFromSaveFile ();
		} else {
			this.world = new World (100, 100);
		}
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    // Update is called once per frame
    void Update() {
		if (world == null || isPaused) {
			return;
		}
        world.update(Time.deltaTime * timeMultiplier);
    }

    internal Tile GetTileAtWorldCoord(Vector3 currFramePosition) {
        return world.GetTileAt(Mathf.FloorToInt(currFramePosition.x), Mathf.FloorToInt(currFramePosition.y));
    }

	public void SaveWorld() {
		Debug.Log("SaveWorld button was clicked.");
		XmlSerializer serializer = new XmlSerializer( typeof(World) );
		TextWriter writer = new StringWriter();
		serializer.Serialize(writer, world);
		writer.Close();
		System.IO.File.WriteAllText(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)+"\\Unity\\save.xml", writer.ToString());
		PlayerPrefs.SetString("SaveGame00", writer.ToString());
	}
	public void LoadWorld() {
		Debug.Log("LoadWorld button was clicked.");
		// Reload the scene to reset all data (and purge old references)
		loadWorld = true;
		SceneManager.LoadScene( SceneManager.GetActiveScene().name );

	}
	void CreateWorldFromSaveFile() {
		Debug.Log("CreateWorldFromSaveFile");
		// Create a world from our save file data.

		XmlSerializer serializer = new XmlSerializer( typeof(World) );
		TextReader reader = new StringReader( PlayerPrefs.GetString("SaveGame00") );

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
			isPaused = !isPaused;
			break;
		case 1:
			timeMultiplier = 0.5f;
			isPaused = false;
			break;
		case 2:
			timeMultiplier = 0.75f;
			isPaused = false;
			break;
		case 3:
			timeMultiplier = 1.5f;
			isPaused = false;
			break;
		case 4:
			timeMultiplier = 2;
			isPaused = false;
			break;
		
		}
	}

}