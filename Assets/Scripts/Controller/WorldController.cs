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
		if (world == null) {
			return;
		}
        world.update(Time.deltaTime);
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
//		Debug.Log( writer.ToString() );
		System.IO.File.WriteAllText("C:\\Users\\%USERPROFIL%\\Desktop\\Unity\\save.xml", writer.ToString());
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
		Debug.Log( reader.ToString() );
		world = (World)serializer.Deserialize(reader);
		Debug.Log ("World " + world);
		reader.Close();
		// Center the Camera
		Camera.main.transform.position = new Vector3( world.Width/2, world.Height/2, Camera.main.transform.position.z );
		Debug.Log ("LOAD ENDED");
	}
}