using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum BrushTypes {Square,Round}

public class EditorController : MonoBehaviour {


	public static EditorController Instance { get; protected set; }
	public EditorIsland editorIsland;
	public static int width= 100;
	public static int height= 100;
	public static Climate climate = Climate.Middle;

	public bool changeTileType;
	public TileType selectedTileType = TileType.Dirt;
	public int structureID;
	public int structureStage;
	public bool DestroyBuilding=false;
	public BrushTypes brushType = BrushTypes.Square;
	public float randomChange=100;
	public string spriteName;
	static string loadsavegame;
	int brushSize=1;
	Action<int,EditorTile> cbStructureCreated;
	Action<EditorTile> cbStructureDestroyed;
	public bool IsModal; // If true, a modal dialog box is open so normal inputs should be ignored.
	// Use this for initialization
	void OnEnable() {
		if (Instance != null) {
			Debug.LogError("There should never be two world controllers.");
		}
		Instance = this;

		if (loadsavegame!=null) {
			CreateWorldFromSaveFile (loadsavegame);
			loadsavegame = null;
		} else {
			editorIsland = new EditorIsland (width,height,climate);
		}
		Camera.main.transform.position = new Vector3(width / 2, height / 2, Camera.main.transform.position.z);
	}
	public void NewIsland(int w,int h, Climate clim){
		width = w;
		height = h;
		climate = clim;
		SceneManager.LoadScene( "IslandEditor" );
	}

	void Update(){
		if(Input.GetMouseButton (0)){
			if(EventSystem.current.IsPointerOverGameObject ()){
				return;
			}
			if(changeTileType){
				ChangeTileType ();
			} else {
				CreateStructure ();
			}
		}
	}
	public void ChangeTileType(){
		EditorTile t = GetTileAtWorldCoord (Camera.main.ScreenToWorldPoint(Input.mousePosition));
		ChangeTileTypeForTile (t);
		switch (brushType) {
		case BrushTypes.Square:
			SquareBrush(ChangeTileTypeForTile,t);
			break;
		case BrushTypes.Round:
			RoundBrush(ChangeTileTypeForTile,t);
			break;
		default:
			throw new ArgumentOutOfRangeException ();
		} 
	}
	private void SquareBrush(Action<EditorTile> action, EditorTile t){
		for (int x = -Mathf.FloorToInt ((float)brushSize / 2f); x < Mathf.CeilToInt ((float)brushSize / 2f); x++) {
			for (int y = -Mathf.FloorToInt ((float)brushSize / 2f); y < Mathf.CeilToInt ((float)brushSize / 2f); y++) {
				RandomModifier (action,GetTileAtWorldCoord (t.X+x,t.Y+y));
			}
		}
	}
	private void RoundBrush(Action<EditorTile> action, EditorTile t){
		List<EditorTile> temp = new List<EditorTile> ();
		float x=0;
		float y=0;
		float radius = brushSize + 1f;
		for (float a = 0; a < 360; a += 0.5f) {
			x = t.X + radius * Mathf.Cos (a);
			y = t.Y + radius * Mathf.Sin (a);
			//			GameObject go = new GameObject ();
			//			go.transform.position = new Vector3 (x, y);
			//			go.AddComponent<SpriteRenderer> ().sprite = Resources.Load<Sprite> ("Debug");
			x = Mathf.RoundToInt (x);
			y = Mathf.RoundToInt (y);
			for (int i = 0; i < brushSize; i++) {
				EditorTile circleTile = GetTileAtWorldCoord (Mathf.RoundToInt (x),Mathf.RoundToInt ( y));
				if (temp.Contains (circleTile) == false) {
					temp.Add (circleTile);
				}
			}
		}
		List<EditorTile> tempInner= new List<EditorTile>();
		//like flood fill the inner circle
		Queue<EditorTile> tilesToCheck = new Queue<EditorTile> ();
		tilesToCheck.Enqueue (t);
		while (tilesToCheck.Count > 0) {
			EditorTile et = tilesToCheck.Dequeue ();
			if (temp.Contains (et) == false && tempInner.Contains (et) == false) {
				tempInner.Add (et);
				EditorTile[] ns = et.GetNeighbours (false);
				foreach (EditorTile t2 in ns) {
					tilesToCheck.Enqueue (t2);
				}
			}
		}
		foreach(EditorTile item in tempInner){
			RandomModifier (action,item);
		}
	}
	private void RandomModifier(Action<EditorTile> action,EditorTile et){
		if(randomChange==100){
			action (et);
		}
		if (Input.GetMouseButtonDown (0)||Input.GetKey (KeyCode.LeftShift)) {
			float f = UnityEngine.Random.Range (0, 100);
			if (f <= randomChange) {
				action (et);
			}
		}
	}



	private void ChangeTileTypeForTile(EditorTile t){
		TileType oldType=t.Type;
		t.Type = selectedTileType;
		t.SpriteName = spriteName;
		if(selectedTileType==TileType.Ocean){
			foreach(EditorTile n in t.GetNeighbours (true)){
				n.Type = TileType.Shore;
			}
		} 
		if(selectedTileType != TileType.Shore){
			
			if(oldType ==TileType.Ocean || oldType ==TileType.Shore){
				foreach(EditorTile n in t.GetNeighbours (true)){
					if(n==null){
						continue;
					}
					List<EditorTile> et = new List<EditorTile> (n.GetNeighbours (true));
					if(et.Find (x=>x!=null && x.Type == TileType.Ocean)!=null){;
						n.Type = TileType.Shore;
						n.SpriteName = "Shore";
					} else {
						n.Type = selectedTileType;
						n.SpriteName = spriteName;
					}
				}
			}
		}
	}
	public void SetDestroyMode(bool destroy){
		DestroyBuilding = destroy;
	}
	public void CreateStructure(){
		if(Input.GetMouseButton(0)){
			EditorTile et = GetTileAtWorldCoord (Camera.main.ScreenToWorldPoint (Input.mousePosition));
			if(DestroyBuilding){
				switch (brushType) {
				case BrushTypes.Square:
					SquareBrush (DestroyStructureOnTile,et);
					break;
				case BrushTypes.Round:
					RoundBrush(DestroyStructureOnTile,et);
					break;
				default:
					throw new ArgumentOutOfRangeException ();
				} 

			} else {
				switch (brushType) {
				case BrushTypes.Square:
					SquareBrush (CreateStructureOnTile,et);
					break;
				case BrushTypes.Round:
					RoundBrush(CreateStructureOnTile,et);
					break;
				default:
					throw new ArgumentOutOfRangeException ();
				} 
			}
		}
	}
	public void DestroyStructureOnTile(EditorTile et){
		editorIsland.RemoveStructure (et);
		if (cbStructureDestroyed != null)
			cbStructureDestroyed (et);
	}
	public void ChangeBrushType(int type){
		brushType = (BrushTypes)type;
	}
	private void CreateStructureOnTile(EditorTile et){
		if(editorIsland.structures.ContainsKey (et)){
			return;
		}
		if(Tile.IsBuildType (et.Type)==false){
			return;
		}
		editorIsland.AddStructure(structureID,structureStage,et);
		if(cbStructureCreated!=null)
			cbStructureCreated(structureID,et);
	}
	public void setBrushSize(int size){
		brushSize = size;
	}
	public void ChangeBuild(bool type){
		changeTileType = type;
	}
	public void RegisterOnStructureCreated(Action<int,EditorTile> strs){
		cbStructureCreated += strs;
	}
	public void UnregisterOnStructureCreated(Action<int,EditorTile> strs){
		cbStructureCreated -= strs;
	}
	public void RegisterOnStructureDestroyed(Action<EditorTile> strs){
		cbStructureDestroyed += strs;
	}
	public void UnregisterOnStructureDestroyed(Action<EditorTile> strs){
		cbStructureDestroyed -= strs;
	}

	internal EditorTile GetTileAtWorldCoord(Vector3 currFramePosition) {
		if (currFramePosition.x >= editorIsland.width ||currFramePosition.y >= editorIsland.height ) {
			return null;
		}
		if (currFramePosition.x < 0 || currFramePosition.y < 0) {
			return null;
		}

		return editorIsland.tiles[Mathf.FloorToInt(currFramePosition.x+0.5f), Mathf.FloorToInt(currFramePosition.y+0.5f)];
	}
	internal EditorTile GetTileAtWorldCoord(int x , int y) {
		if (x >= editorIsland.width ||y >= editorIsland.height ) {
			return null;
		}
		if (x < 0 || y < 0) {
			return null;
		}
		return editorIsland.tiles[x, y];
	}
	public void OnBrushRandomChange(float f){
		this.randomChange = f;
	}
	/// 
	/// 
	/// SAVING FEATURES
	/// 
	/// 


	public void SaveWorld(string savename) {
		Debug.Log("SaveWorld button was clicked.");
		XmlSerializer serializer = new XmlSerializer( typeof(EditorIsland) );

		TextWriter writer = new StringWriter();
		serializer.Serialize(writer, editorIsland);

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
		string filePath = System.IO.Path.Combine(GetSaveGamesPath (),savename+".isl") ;
		File.WriteAllText( filePath, writer.ToString() );

	}
	public void LoadWorld(string name) {
		Debug.Log("LoadWorld button was clicked.");
		loadsavegame=name;

//		if(quickload){
//			GameDataHolder gdh = GameDataHolder.Instance;
//			gdh.loadsavegame = "QuickSave";//TODO CHANGE THIS TO smth not hardcoded
//		}
		// Reload the scene to reset all data (and purge old references)
		SceneManager.LoadScene( "IslandEditor" );
	}
	void CreateWorldFromSaveFile(string savegamename) {
		Debug.Log("CreateWorldFromSaveFile");
		// Create a world from our save file data.

		XmlSerializer serializer = new XmlSerializer( typeof(EditorIsland) );
		string saveGameText = File.ReadAllText( System.IO.Path.Combine( GetSaveGamesPath (), savegamename+".isl" ) );

		TextReader reader = new StringReader( saveGameText );
//		Debug.Log (reader.ToString () + " "+saveGameText); 
		editorIsland = (EditorIsland)serializer.Deserialize(reader);
		reader.Close();

		// Center the Camera
		Camera.main.transform.position = new Vector3( editorIsland.width/2, editorIsland.height/2, Camera.main.transform.position.z );
		Debug.Log ("LOAD ENDED");
	}
	public string GetSaveGamesPath(){
		return System.IO.Path.Combine(Application.dataPath.Replace ("/Assets","") , "islands");
	}
}
