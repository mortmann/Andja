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
	public World world;
	public static int width  = 100;
	public static int height = 100;
	public static Climate climate = Climate.Middle;

	public bool changeTileType;
	public TileType selectedTileType = TileType.Dirt;

	public Structure structure;

	public int structureStage;
	public bool DestroyBuilding=false;
	public BrushTypes brushType = BrushTypes.Square;
	public float randomChange=100;
	public string spriteName;
	static string loadsavegame;
	int brushSize=1;

	Action<Structure,Tile> cbStructureCreated;
	Action<Tile> cbStructureDestroyed;

	Dictionary<Tile,Structure> tileToStructure;

	public bool IsModal; // If true, a modal dialog box is open so normal inputs should be ignored.
	// Use this for initialization
	void OnEnable() {
		if (Instance != null) {
			Debug.LogError("There should never be two world controllers.");
		}
		Instance = this;

		if (loadsavegame!=null) {
//			CreateWorldFromSaveFile (loadsavegame);
			loadsavegame = null;
		} else {
			world = new World (width, height);
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
		Tile t = GetTileAtWorldCoord (Camera.main.ScreenToWorldPoint(Input.mousePosition));
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
	private void SquareBrush(Action<Tile> action,Tile t){
		for (int x = -Mathf.FloorToInt ((float)brushSize / 2f); x < Mathf.CeilToInt ((float)brushSize / 2f); x++) {
			for (int y = -Mathf.FloorToInt ((float)brushSize / 2f); y < Mathf.CeilToInt ((float)brushSize / 2f); y++) {
				RandomModifier (action,GetTileAtWorldCoord (t.X+x,t.Y+y));
			}
		}
	}
	private void RoundBrush(Action<Tile> action, Tile t){
		List<Tile> temp = new List<Tile> ();
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
				Tile circleTile = GetTileAtWorldCoord (Mathf.RoundToInt (x),Mathf.RoundToInt ( y));
				if (temp.Contains (circleTile) == false) {
					temp.Add (circleTile);
				}
			}
		}
		List<Tile> tempInner= new List<Tile>();
		//like flood fill the inner circle
		Queue<Tile> tilesToCheck = new Queue<Tile> ();
		tilesToCheck.Enqueue (t);
		while (tilesToCheck.Count > 0) {
			Tile et = tilesToCheck.Dequeue ();
			if (temp.Contains (et) == false && tempInner.Contains (et) == false) {
				tempInner.Add (et);
				Tile[] ns = et.GetNeighbours (false);
				foreach (Tile t2 in ns) {
					tilesToCheck.Enqueue (t2);
				}
			}
		}
		foreach(Tile item in tempInner){
			RandomModifier (action,item);
		}
	}
	private void RandomModifier(Action<Tile> action,Tile et){
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

	private void ChangeTileTypeForTile(Tile t){
		TileType oldType=t.Type;
		t.Type = selectedTileType;
		t.SpriteName = spriteName;
		if(selectedTileType==TileType.Ocean){
			foreach(Tile n in t.GetNeighbours (true)){
				n.Type = TileType.Shore;
			}
		} 
		if(selectedTileType != TileType.Shore){
			
			if(oldType ==TileType.Ocean || oldType ==TileType.Shore){
				foreach(Tile n in t.GetNeighbours (true)){
					if(n==null){
						continue;
					}
					List<Tile> et = new List<Tile> (n.GetNeighbours (true));
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
			Tile et = GetTileAtWorldCoord (Camera.main.ScreenToWorldPoint (Input.mousePosition));
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
	public void DestroyStructureOnTile(Tile et){
		tileToStructure.Remove (et);
		if (cbStructureDestroyed != null)
			cbStructureDestroyed (et);
	}
	public void ChangeBrushType(int type){
		brushType = (BrushTypes)type;
	}
	private void CreateStructureOnTile(Tile et){
		if(tileToStructure.ContainsKey (et)){
			return;
		}
		if(Tile.IsBuildType (et.Type)==false){
			return;
		}
		tileToStructure.Add(et,structure);
		if(cbStructureCreated!=null)
			cbStructureCreated(structure,et);
	}
	public void setBrushSize(int size){
		brushSize = size;
	}
	public void ChangeBuild(bool type){
		changeTileType = type;
	}

	public void setAge(int age){
		if (structure is Growable)
			((Growable)structure).currentStage = age;
	}
	public void setStructure(int id){
		structure = PrototypController.Instance.structurePrototypes [id];
	}
	public void RegisterOnStructureCreated(Action<Structure,Tile> strs){
		cbStructureCreated += strs;
	}
	public void UnregisterOnStructureCreated(Action<Structure,Tile> strs){
		cbStructureCreated -= strs;
	}
	public void RegisterOnStructureDestroyed(Action<Tile> strs){
		cbStructureDestroyed += strs;
	}
	public void UnregisterOnStructureDestroyed(Action<Tile> strs){
		cbStructureDestroyed -= strs;
	}

	internal Tile GetTileAtWorldCoord(Vector3 currFramePosition) {
		return World.current.GetTileAt (currFramePosition.x, currFramePosition.y);
	}
	internal Tile GetTileAtWorldCoord(int x , int y) {
		return World.current.GetTileAt (x, y);
	}
	public void OnBrushRandomChange(float f){
		this.randomChange = f;
	}
	/// 
	/// 
	/// SAVING FEATURES
	/// 
	/// 


	public string GetSaveGamesPath(){
		return System.IO.Path.Combine(Application.dataPath.Replace ("/Assets","") , "islands");
	}
}
