using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

using UnityEngine.EventSystems;
using Newtonsoft.Json;

public enum BrushTypes {Square,Round}

public class EditorController : MonoBehaviour {
	public const string islandSaveFileVersion = "i_0.0.2";
	public static bool IsEditor = false;

	public static EditorController Instance { get; protected set; }
	public MapGenerator mapGenerator;

	public World world;
	Dictionary<Tile,Structure> tileToStructure;

	public static int width  = 100;
	public static int height = 100;
	public static Climate climate = Climate.Middle;
	static System.IO.FileInfo loadsavegame;

	public bool changeTileType;
	public TileType selectedTileType = TileType.Dirt;

	public Structure structure;

	public int structureStage;
	public bool DestroyBuilding=false;
	public BrushTypes brushType = BrushTypes.Square;
	public float randomChange=100;
	public string spriteName;
	int brushSize=1;

    public IslandSizeTypes IslandSize {
        get { return Island.GetSizeTyp(width, height); }
    }

	Action<Structure> cbStructureCreated;
	Action<Tile> cbStructureDestroyed;

    public bool IsModal; // If true, a modal dialog box is open so normal inputs should be ignored.

    // Use this for initialization
    void OnEnable() {
		if (Instance != null) {
			Debug.LogError("There should never be two world controllers.");
		}
		tileToStructure = new Dictionary<Tile, Structure> ();
		IsEditor = true;
		Instance = this;
		new InputHandler ();
		MapGenerator mg = FindObjectOfType<MapGenerator> ();
		if (loadsavegame != null) {
			CreateFromSave (loadsavegame);
			loadsavegame = null;
		} else if (mg != null){
			world = new World (mg.GetTiles (), mg.Width, mg.Height);
		}
		else {
			world = new World (width, height);
		}
        foreach (Climate climate in Enum.GetValues(typeof(Climate))) {
            foreach (IslandSizeTypes size in Enum.GetValues(typeof(IslandSizeTypes))) {
                if(System.IO.Directory.Exists(GetPathToIsland(size,climate)) == false){
                    System.IO.Directory.CreateDirectory(GetPathToIsland(size, climate));
                }
            }
        }
        Camera.main.transform.position = new Vector3(width / 2, height / 2, Camera.main.transform.position.z);
	}
	public IEnumerator NewIsland(int w,int h, Climate clim){
		GameObject go = Instantiate (mapGenerator.gameObject);
		MapGenerator mg = go.GetComponent<MapGenerator> ();
		mg.EditorGenerate(w, h, new MapGenerator.IslandGenInfo (new MapGenerator.Range(w, w) , new MapGenerator.Range(h,h) , clim));
		mg.Generate ();
        width = w;
        height = h;
        climate = clim;
		while(mg.IsDone == false){
			yield return null;
		}
		DontDestroyOnLoad (go);
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
        if (t == null)
            return;
		if(t.Type == selectedTileType){
			return;
		}
		if(t.Type == TileType.Ocean){
			World.Current.SetTileAt (t.X, t.Y, new LandTile (t.X,t.Y));
		} 
        t = World.Current.GetTileAt(t.X, t.Y);

        if (selectedTileType == TileType.Ocean){
			if(t.Structure!=null){
				DestroyStructureOnTile (t);
			}
			World.Current.SetTileAt (t.X, t.Y, new Tile (t.X,t.Y));
		}
		t.Type = selectedTileType;
        if(t.Type == TileType.Shore) {
            foreach (Tile neigh in t.GetNeighbours()) {
                if (neigh.Type != TileType.Shore) {
                    continue;
                }
                neigh.SpriteName = "shore" + GetSpriteAddonForTile(neigh);
                World.Current.OnTileChanged(neigh);
            }
            t.SpriteName = "shore" + GetSpriteAddonForTile(t);
        } else {
            t.SpriteName = spriteName;
        }
        World.Current.OnTileChanged (t);
	}

    private string GetSpriteAddonForTile(Tile t){
        //FOR now only Shore is rotating to face the other tiles
        if(t.Type != TileType.Shore) {
            return "";
        }
        string connectOrientation = "";
        Tile[] neig = t.GetNeighbours();

        connectOrientation = "_";
        int neighbours = 0;
        if (neig[0] != null && neig[0].Type == TileType.Shore) {
            connectOrientation += "N";
            neighbours++;
        }
        if (neig[1] != null && neig[1].Type == TileType.Shore) {
            connectOrientation += "E";
            neighbours++;
        }
        if (neig[2] != null && neig[2].Type == TileType.Shore) {
            connectOrientation += "S";
            neighbours++;
        }
        if (neig[3] != null && neig[3].Type == TileType.Shore) {
            connectOrientation += "W";
            neighbours++;
        }
        if (neighbours > 0) {
            connectOrientation += "_";
            if (neig[0] != null && neig[0].Type != TileType.Shore && neig[0].Type != TileType.Ocean) {
                connectOrientation += "N";
            } 
            if (neig[1] != null && neig[1].Type != TileType.Shore && neig[1].Type != TileType.Ocean) {
                connectOrientation += "E";
            }
            if (neig[2] != null && neig[2].Type != TileType.Shore && neig[2].Type != TileType.Ocean) {
                connectOrientation += "S";
            }
            if (neig[3] != null && neig[3].Type != TileType.Shore && neig[3].Type != TileType.Ocean) {
                connectOrientation += "W";
            }
        }
        return connectOrientation;
    }

	public void SetDestroyMode(bool destroy){
		DestroyBuilding = destroy;
	}
	public void CreateStructure(){
		if(Input.GetMouseButtonDown(0)){
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
        cbStructureDestroyed?.Invoke(et);
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
		Structure toPlace = structure.Clone ();
		//Set Variables of Structure somehow
		if (toPlace is Growable)
			((Growable)toPlace).currentStage = structureStage;

		PlaceStructureOnTile (toPlace, et);
	}
	void PlaceStructureOnTile(Structure toPlace, Tile placeOn){
		placeOn.Structure = toPlace;
		toPlace.BuildTile = placeOn;
		tileToStructure.Add(placeOn, toPlace);
        cbStructureCreated?.Invoke(toPlace);
    } 
	public void SetBrushSize(int size){
		brushSize = size;
	}
	public void ChangeBuild(bool type){
		changeTileType = type;
	}

	public void SetAge(int age){
		if (structure is Growable)
			((Growable)structure).currentStage = age;
	}
	public void SetStructure(int id){
		structure = PrototypController.Instance.structurePrototypes [id];
	}
	public void RegisterOnStructureCreated(Action<Structure> strs){
		cbStructureCreated += strs;
	}
	public void UnregisterOnStructureCreated(Action<Structure> strs){
		cbStructureCreated -= strs;
	}
	public void RegisterOnStructureDestroyed(Action<Tile> strs){
		cbStructureDestroyed += strs;
	}
	public void UnregisterOnStructureDestroyed(Action<Tile> strs){
		cbStructureDestroyed -= strs;
	}

	internal Tile GetTileAtWorldCoord(Vector3 currFramePosition) {
		return World.Current.GetTileAt (currFramePosition.x+0.5f, currFramePosition.y+0.5f);
	}
	internal Tile GetTileAtWorldCoord(int x , int y) {
		return World.Current.GetTileAt (x, y);
	}
	public void OnBrushRandomChange(float f){
		this.randomChange = f;
	}
	/// 
	/// 
	/// SAVING FEATURES
	/// 
	/// 

	public void SaveIslandState(string name = "autosave"){
        string path = GetPathToIsland(IslandSize, climate);
        path = System.IO.Path.Combine (path, name + ".isl");

		SaveIsland savestate = GetSaveState();

		System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(savestate,//Formatting.Indented,
			new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            }
		) 
		);
	}

    public static string GetPathToIsland(IslandSizeTypes islandSize, Climate islandClimate) {
        string path = System.IO.Path.Combine(GetSaveGamesPath(), islandClimate.ToString());
        path = System.IO.Path.Combine(path, islandSize.ToString());
        return path;
    }
	public void LoadIsland(System.IO.FileInfo file ){
		loadsavegame = file;
		MenuController.instance.ChangeToEditorLoadScreen ();
	}
	private void CreateFromSave(System.IO.FileInfo file){
		LoadSaveState (LoadIsland(file.FullName));
	}
    public static SaveIsland LoadIsland(string location) {
        string alllines = System.IO.File.ReadAllText(location);
        SaveIsland state = JsonConvert.DeserializeObject<SaveIsland>(alllines, new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        });
        if (islandSaveFileVersion != state.version) {
            Debug.LogError("Mismatch of SaveFile Versions " + state.version + " & " + islandSaveFileVersion);
            return null;
        }
        return state;
    }
	void LoadSaveState(SaveIsland load){
		world = new World(load.tiles,load.Width,load.Height);
		tileToStructure = new Dictionary<Tile, Structure> ();
		foreach(Structure s in load.structures){
			PlaceStructureOnTile (s, s.BuildTile);
		}
	}
	public SaveIsland GetSaveState(){
		HashSet<Tile> toSave = new HashSet<Tile> (world.Tiles);
		toSave.RemoveWhere(x=>x.Type == TileType.Ocean);
		return new SaveIsland (tileToStructure, toSave.ToArray() , width,height, climate);
	}
	public static string GetSaveGamesPath(){
		return System.IO.Path.Combine(Application.dataPath.Replace ("/Assets","") , "islands");
	}
	[JsonObject]
	public class SaveIsland {
		[JsonPropertyAttribute] public string version = islandSaveFileVersion;
		[JsonPropertyAttribute] public int Width;
        [JsonPropertyAttribute] public int Height;
        [JsonPropertyAttribute] public Climate climate;
        [JsonPropertyAttribute(TypeNameHandling = TypeNameHandling.Auto)] public List<Structure> structures;
        [JsonPropertyAttribute(TypeNameHandling = TypeNameHandling.None)] public Tile[] tiles;

        public SaveIsland(){
			
		}
		public SaveIsland(Dictionary<Tile,Structure> tileToStructure,Tile[] tiles,int Width,int Height, Climate climate){
			this.Width=Width;
			this.Height=Height;
            this.climate = climate;
            this.structures = new List<Structure>(tileToStructure.Values);
			this.tiles = tiles;
		}
 	}
}
