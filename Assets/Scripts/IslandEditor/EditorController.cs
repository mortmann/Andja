using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

using UnityEngine.EventSystems;
using Newtonsoft.Json;

public enum BrushTypes { Square, Round }

public class EditorController : MonoBehaviour {
    public static bool IsEditor = false;

    public static EditorController Instance { get; protected set; }
    World world;
    Dictionary<Tile, Structure> tileToStructure;
    Dictionary<string, int[]> Ressources;


    public static int Width = 100;
    public static int Height = 100;
    public static Climate climate = Climate.Middle;
    public static bool generate;
    static SaveIsland loadsavegame;

    public bool changeTileType;
    public TileType selectedTileType = TileType.Dirt;

    public Structure structure;
    public int structureStage;
    public bool DestroyStructure = false;

    public BrushTypes brushType = BrushTypes.Square;
    public float randomChange = 100;
    public string spriteName;
    int brushSize = 1;

    public Size IslandSize {
        get { return Island.GetSizeTyp(Width, Height); }
    }

    Action<Structure> cbStructureCreated;
    Action<Tile> cbStructureDestroyed;

    public bool IsModal; // If true, a modal dialog box is open so normal inputs should be ignored.

    // Use this for initialization
    void OnEnable() {
        if (Instance != null) {
            Debug.LogError("There should never be two EditorController.");
        }
        tileToStructure = new Dictionary<Tile, Structure>();
        Ressources = new Dictionary<string, int[]>();
        IsEditor = true;
        
        Instance = this;
        new InputHandler();
        if (loadsavegame != null) {
            LoadSaveState(loadsavegame);
            loadsavegame = null;
        }
        else if (generate) {
            world = new World(MapGenerator.Instance.GetTiles(), Width, Height);
            MapGenerator.Instance.Destroy();
            generate = false;
        }
        else {
            Tile[] tiles = new Tile[Width * Height];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    tiles[x * Height + y] = new Tile(x, y);
                }
            }
            world = new World(tiles,Width, Height);
        }
        //Destroy(MapGenerator.Instance.gameObject);
        Camera.main.transform.position = new Vector3(Width / 2, Height / 2, Camera.main.transform.position.z);
    }

    internal static MapGenerator.IslandGenInfo[] GetEditorGenInfo() {
        return new MapGenerator.IslandGenInfo[1] { new MapGenerator.IslandGenInfo(new MapGenerator.Range(Width, Width), new MapGenerator.Range(Height, Height), climate) };
    }

    public void NewIsland(int w, int h, Climate clim) {
        Width = w;
        Height = h;
        climate = clim;
        generate = true;
        SceneManager.LoadScene("EditorLoadingScreen");
    }

    void Update() {
        if (Input.GetMouseButton(0)) {
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            if (changeTileType) {
                ChangeTileType();
            }
            else {
                CreateStructure();
            }
        }
    }
    public void ChangeTileType() {
        Tile t = GetTileAtWorldCoord(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        ChangeTileTypeForTile(t);
        switch (brushType) {
            case BrushTypes.Square:
                SquareBrush(ChangeTileTypeForTile, t);
                break;
            case BrushTypes.Round:
                RoundBrush(ChangeTileTypeForTile, t);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    private void SquareBrush(Action<Tile> action, Tile t) {
        for (int x = -Mathf.FloorToInt((float)brushSize / 2f); x < Mathf.CeilToInt((float)brushSize / 2f); x++) {
            for (int y = -Mathf.FloorToInt((float)brushSize / 2f); y < Mathf.CeilToInt((float)brushSize / 2f); y++) {
                RandomModifier(action, GetTileAtWorldCoord(t.X + x, t.Y + y));
            }
        }
    }
    private void RoundBrush(Action<Tile> action, Tile t) {
        List<Tile> temp = new List<Tile>();
        float x = 0;
        float y = 0;
        float radius = brushSize + 1f;
        for (float a = 0; a < 360; a += 0.5f) {
            x = t.X + radius * Mathf.Cos(a);
            y = t.Y + radius * Mathf.Sin(a);
            //			GameObject go = new GameObject ();
            //			go.transform.position = new Vector3 (x, y);
            //			go.AddComponent<SpriteRenderer> ().sprite = Resources.Load<Sprite> ("Debug");
            x = Mathf.RoundToInt(x);
            y = Mathf.RoundToInt(y);
            for (int i = 0; i < brushSize; i++) {
                Tile circleTile = GetTileAtWorldCoord(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
                if (temp.Contains(circleTile) == false) {
                    temp.Add(circleTile);
                }
            }
        }
        List<Tile> tempInner = new List<Tile>();
        //like flood fill the inner circle
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(t);
        while (tilesToCheck.Count > 0) {
            Tile et = tilesToCheck.Dequeue();
            if (temp.Contains(et) == false && tempInner.Contains(et) == false) {
                tempInner.Add(et);
                Tile[] ns = et.GetNeighbours(false);
                foreach (Tile t2 in ns) {
                    tilesToCheck.Enqueue(t2);
                }
            }
        }
        foreach (Tile item in tempInner) {
            RandomModifier(action, item);
        }
    }
    private void RandomModifier(Action<Tile> action, Tile et) {
        if (randomChange == 100) {
            action(et);
        }
        if (Input.GetMouseButtonDown(0) || Input.GetKey(KeyCode.LeftShift)) {
            float f = UnityEngine.Random.Range(0, 100);
            if (f <= randomChange) {
                action(et);
            }
        }
    }

    private void ChangeTileTypeForTile(Tile t) {
        if (t == null)
            return;
        if (t.Type == selectedTileType) {
            return;
        }
        if (t.Type == TileType.Ocean) {
            World.Current.SetTileAt(t.X, t.Y, new LandTile(t.X, t.Y));
        }
        t = World.Current.GetTileAt(t.X, t.Y);

        
        t.Type = selectedTileType;
        if (t.Type == TileType.Shore) {
            t.SpriteName = "shore" + Tile.GetSpriteAddonForTile(t, t.GetNeighbours());
        }
        else {
            t.SpriteName = spriteName;
        }
        if (selectedTileType == TileType.Ocean) {
            if (t.Structure != null) {
                DestroyStructureOnTile(t);
            }
            World.Current.SetTileAt(t.X, t.Y, new Tile(t.X, t.Y));
            t.SpriteName = null;
        }
        foreach (Tile neigh in t.GetNeighbours()) {
            if (neigh.Type != TileType.Shore) {
                continue;
            }
            neigh.SpriteName = "shore" + Tile.GetSpriteAddonForTile(neigh, neigh.GetNeighbours());
            World.Current.OnTileChanged(neigh);
        }
        World.Current.OnTileChanged(t);
    }

    public void SetDestroyMode(bool destroy) {
        DestroyStructure = destroy;
    }
    public void CreateStructure() {
        if (Input.GetMouseButtonDown(0)) {
            Tile et = GetTileAtWorldCoord(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            if (DestroyStructure) {
                switch (brushType) {
                    case BrushTypes.Square:
                        SquareBrush(DestroyStructureOnTile, et);
                        break;
                    case BrushTypes.Round:
                        RoundBrush(DestroyStructureOnTile, et);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
            else {
                switch (brushType) {
                    case BrushTypes.Square:
                        SquareBrush(CreateStructureOnTile, et);
                        break;
                    case BrushTypes.Round:
                        RoundBrush(CreateStructureOnTile, et);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
    public void DestroyStructureOnTile(Tile et) {
        tileToStructure.Remove(et);
        cbStructureDestroyed?.Invoke(et);
    }
    public void ChangeBrushType(int type) {
        brushType = (BrushTypes)type;
    }
    private void CreateStructureOnTile(Tile et) {
        if (structure==null|| tileToStructure.ContainsKey(et)) {
            return;
        }
        if (Tile.IsBuildType(et.Type) == false) {
            return;
        }
        Structure toPlace = structure.Clone();
        //Set Variables of Structure somehow
        if (toPlace is GrowableStructure)
            ((GrowableStructure)toPlace).currentStage = structureStage;

        PlaceStructureOnTile(toPlace, et);
    }
    void PlaceStructureOnTile(Structure toPlace, Tile placeOn) {
        placeOn.Structure = toPlace;
        toPlace.BuildTile = placeOn;
        tileToStructure.Add(placeOn, toPlace);
        cbStructureCreated?.Invoke(toPlace);
    }
    public void SetBrushSize(int size) {
        brushSize = size;
    }
    public void ChangeBuild(bool type) {
        changeTileType = type;
    }

    public void SetAge(int age) {
        if (structure is GrowableStructure)
            ((GrowableStructure)structure).currentStage = age;
    }
    public void SetStructure(string id) {
        structure = PrototypController.Instance.StructurePrototypes[id];
    }
    public void RegisterOnStructureCreated(Action<Structure> strs) {
        cbStructureCreated += strs;
    }
    public void UnregisterOnStructureCreated(Action<Structure> strs) {
        cbStructureCreated -= strs;
    }
    public void RegisterOnStructureDestroyed(Action<Tile> strs) {
        cbStructureDestroyed += strs;
    }
    public void UnregisterOnStructureDestroyed(Action<Tile> strs) {
        cbStructureDestroyed -= strs;
    }
    
    internal void OnRessourceChange(string ID, int amount, bool lower) {
        if (Ressources.ContainsKey(ID) == false)
            Ressources[ID] = new int[2];
        int index = lower ? 0 : 1;
        Ressources[ID][index] = amount;
    }
    internal Tile GetTileAtWorldCoord(Vector3 currFramePosition) {
        return World.Current.GetTileAt(currFramePosition.x + 0.5f, currFramePosition.y + 0.5f);
    }
    internal Tile GetTileAtWorldCoord(int x, int y) {
        return World.Current.GetTileAt(x, y);
    }
    public void OnBrushRandomChange(float f) {
        this.randomChange = f;
    }
    public void OnDestroy() {
        IsEditor = false;
    }
    /// 
    /// 
    /// SAVING FEATURES
    /// 
    /// 
    public void LoadIsland(SaveIsland file) {
        loadsavegame = file;
        MenuController.Instance.ChangeToEditorLoadScreen();
    }
    void LoadSaveState(SaveIsland load) {
        Width = load.Width;
        Height = load.Height;
        world = new World(load.tiles, load.Width, load.Height);
        tileToStructure = new Dictionary<Tile, Structure>();
        Ressources = load.Ressources;
        foreach (Structure s in load.structures) {
            PlaceStructureOnTile(s, s.BuildTile);
        }
    }
    public SaveIsland GetSaveState() {
        HashSet<Tile> toSave = new HashSet<Tile>(world.Tiles);
        toSave.RemoveWhere(x => x.Type == TileType.Ocean);
        return new SaveIsland(tileToStructure, toSave.ToArray(), Width, Height, climate, Ressources);
    }

    [JsonObject]
    public class SaveIsland {
        [JsonPropertyAttribute] public int Width;
        [JsonPropertyAttribute] public int Height;
        [JsonPropertyAttribute] public Climate climate;
        [JsonPropertyAttribute(TypeNameHandling = TypeNameHandling.Auto)] public List<Structure> structures;
        [JsonPropertyAttribute(TypeNameHandling = TypeNameHandling.None)] public LandTile[] tiles;
        [JsonPropertyAttribute] public Dictionary<string, int[]> Ressources;

        [JsonIgnore] public string Name; // for loading in image or similar things
        public SaveIsland() {

        }
        public SaveIsland(Dictionary<Tile, Structure> tileToStructure, Tile[] tiles, int Width, int Height, Climate climate, Dictionary<string, int[]> Ressources) {
            this.Width = Width;
            this.Height = Height;
            this.climate = climate;
            this.structures = new List<Structure>(tileToStructure.Values);
            this.tiles = tiles.Cast<LandTile>().ToArray();
            this.Ressources = new Dictionary<string, int[]>();
            foreach(string id in Ressources.Keys) {
                if (Ressources[id][1] <= 0)
                    continue;
                this.Ressources[id] = Ressources[id];
            }
        }
    }
}
