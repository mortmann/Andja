using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

using UnityEngine.EventSystems;
using Newtonsoft.Json;

public enum BrushTypes { Square, Round }
public enum ChangeMode { Tile, Structure }
public class EditorController : MonoBehaviour {
    public static bool IsEditor = false;

    public static EditorController Instance { get; protected set; }
    World world;
    Dictionary<string, Range> Ressources;


    public static int Width = 100;
    public static int Height = 100;
    public static Climate climate = Climate.Middle;
    public static bool Generate = false; 
    static SaveIsland loadsavegame;

    public TileType selectedTileType = TileType.Dirt;

    public Structure structure;
    public int structureStage;
    public bool DestroyStructure = false;
    public ChangeMode changeMode = ChangeMode.Tile;
    public BrushTypes brushType = BrushTypes.Square;
    public float randomChange = 100;
    public string spriteName;
    int brushSize = 1;
    public bool brushBuild;
    public Size IslandSize {
        get { return Island.GetSizeTyp(Width, Height); }
    }
    public Vector3 BrushOffset { get {
            switch (brushType) {
                case BrushTypes.Square:
                    if (brushSize % 2 == 0)
                        return Vector3.zero;
                    return new Vector3(0.5f, 0.5f, 0);
                case BrushTypes.Round:
                    return new Vector3(0, 0, 0);
            }
            return Vector3.zero;
        }
    }

    Action<Structure> cbStructureCreated;
    Action<Tile> cbStructureDestroyed;
    public List<Action<Structure>> SetStructureVariablesList = new List<Action<Structure>>();

    public bool IsModal; // If true, a modal dialog box is open so normal inputs should be ignored.
    private bool dragging;

    // Use this for initialization
    void OnEnable() {
        if (Instance != null) {
            Debug.LogError("There should never be two EditorController.");
        }
        Ressources = new Dictionary<string, Range>();
        IsEditor = true;
        
        Instance = this;
        new InputHandler();
        if (Generate==false&&loadsavegame==null) {
            Tile[] tiles = new Tile[Width * Height];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    tiles[x * Height + y] = new Tile(x, y);
                }
            }
            world = new World(tiles,Width, Height);
        }
        Camera.main.transform.position = new Vector3(Width / 2, Height / 2, Camera.main.transform.position.z);
        SceneManager.activeSceneChanged += OnLevelLoaded;
    }
    internal void ResetSetStructure() {
        SetStructureVariablesList.Clear();
    }

    private void OnLevelLoaded(Scene o, Scene n) {
        if (SceneManager.GetActiveScene().name != "IslandEditor")
            return;
        if (Generate) {
            world = new World(MapGenerator.Instance.GetTiles(), Width, Height,true);
            foreach (Tile t in MapGenerator.Instance.tileToStructure.Keys) {
                Structure str = MapGenerator.Instance.tileToStructure[t];
                BuildController.Instance.EditorBuildOnTile(str, str.GetBuildingTiles(t), false);
            }
            BuildController.Instance.SetLoadedStructures(MapGenerator.Instance.tileToStructure.Values);
            MapGenerator.Instance.Destroy();
            Generate = false;
        }
        if (loadsavegame != null) {
            LoadSaveState(loadsavegame);
            loadsavegame = null;
        }
        TileSpriteController.Instance.EditorFix();
        UpdateHighLights();
    }
    internal static MapGenerator.IslandGenInfo[] GetEditorGenInfo() {
        return new MapGenerator.IslandGenInfo[1] { new MapGenerator.IslandGenInfo(new Range(Width, Width), new Range(Height, Height), climate, true) };
    }

    public void NewIsland(int w, int h, Climate clim, bool random) {
        Width = w;
        Height = h;
        climate = clim;
        Generate = random;
        SceneManager.LoadScene("EditorLoadingScreen");
    }
    public void RandomizeIsland() {
        Generate = true;
        SceneManager.LoadScene("EditorLoadingScreen");
    }
    void Update() {
        if (Input.GetMouseButtonDown(0) || dragging) {
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            dragging = true;
            if (changeMode == ChangeMode.Tile) {
                ChangeTileType();
            }
            else {
                if(brushBuild)
                    CreateStructure();
            }
        }
        if(Input.GetKey(KeyCode.LeftControl)) {
            FindObjectOfType<HoverOverScript>().Show(MouseController.Instance.GetTileUnderneathMouse().Vector.ToString());
        } else {
            FindObjectOfType<HoverOverScript>().Unshow();
        }
        if (Input.GetMouseButtonUp(0)) {
            dragging = false;
        }
        if (InputHandler.GetButtonDown(InputName.Rotate)) {
            if (BuildController.Instance.toBuildStructure != null) {
                BuildController.Instance.toBuildStructure.RotateStructure();
            }
        }
    }
    public void ChangeTileType() {
        Tile t = GetTileAtWorldCoord(Camera.main.ScreenToWorldPoint(Input.mousePosition));
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
        if (t == null)
            return;
        List<Tile> temp = Util.CalculateRectangleTiles(brushSize,brushSize, 0, 0, t.X, t.Y);
        foreach (Tile item in temp) {
            RandomModifier(action, item);
        }
    }
    private void RoundBrush(Action<Tile> action, Tile t) {
        if (t == null)
            return;
        List<Tile> temp = Util.CalculateCircleTiles(brushSize, 0, 0, t.X - brushSize, t.Y - brushSize);
        foreach (Tile item in temp) {
            RandomModifier(action, item);// World.Current.GetTileAt(t.Vector2+item.Vector2-new Vector2(brushSize,brushSize)));
        }
    }
    private void RandomModifier(Action<Tile> action, Tile et) {
        if (randomChange == 100) {
            action(et);
        } else
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
            LandTile landTile = new LandTile(t.X, t.Y);
            landTile.Island = World.Current.Islands[0];
            landTile.City = World.Current.Islands[0].Wilderness;
            World.Current.Islands[0].Tiles.Add(t);
            World.Current.SetTileAt(t.X, t.Y, landTile);
        }
        t = World.Current.GetTileAt(t.X, t.Y);

        if (selectedTileType == TileType.Shore) {
            t.SpriteName = "shore" + Tile.GetSpriteAddonForTile(t, t.GetNeighbours());
        }
        else {
            t.SpriteName = spriteName;
        }
        if (selectedTileType == TileType.Ocean && t.Type != TileType.Ocean) {
            if (t.Structure != null) {
                DestroyStructureOnTile(t);
            }
            World.Current.SetTileAt(t.X, t.Y, new Tile(t.X, t.Y));
            World.Current.Islands[0].Tiles.Remove(t);
            t.SpriteName = null;
        }
        t.Type = selectedTileType;
        foreach (Tile neigh in t.GetNeighbours()) {
            if (neigh!=null&&neigh.Type != TileType.Shore) {
                continue;
            }
            neigh.SpriteName = "shore" + Tile.GetSpriteAddonForTile(neigh, neigh.GetNeighbours());
            World.Current.OnTileChanged(neigh);
        }
        World.Current.OnTileChanged(t);
    }

    public void SetDestroyMode() {
        DestroyStructure = true;
        EditorUIController.Instance.DestroyToggle(DestroyStructure);
        MouseController.Instance.mouseState = MouseState.Destroy;
    }
    public void SetBuildMode() {
        DestroyStructure = false;
        EditorUIController.Instance.DestroyToggle(DestroyStructure);
        MouseController.Instance.mouseState = MouseState.Idle;
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
                        SquareBrush(BrushBuildOn, et);
                        break;
                    case BrushTypes.Round:
                        RoundBrush(BrushBuildOn, et);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
    public void DestroyStructureOnTile(Tile et) {
        BuildController.Instance.DestroyStructureOnTile(et, null, true);
        cbStructureDestroyed?.Invoke(et);
    }
    public void ChangeBrushType(int type) {
        brushType = (BrushTypes)type;
        UpdateHighLights();
    }

    private void UpdateHighLights() {
        //if build structure mode go if brush build is selected otherwise always true
        bool show = changeMode == ChangeMode.Structure ? brushBuild : true;
        switch (brushType) {
            case BrushTypes.Square:
                MouseController.Instance.SetEditorHighlight(brushSize, 
                    Util.CalculateSquareTiles(brushSize), show);
                break;
            case BrushTypes.Round:
                MouseController.Instance.SetEditorHighlight(2*brushSize, 
                    Util.CalculateRangeTiles(brushSize, 0, 0, 0, 0), show);
                break;
        }
    }

    internal void BrushBuildOn(Tile t) {
        BuildOn(structure.GetBuildingTiles(t),true, true);
    }
    internal void BuildOn(List<Tile> tiles, bool foreachTileNewStructure, bool isBrushBuilt = false) {
        if (brushBuild != isBrushBuilt)
            return;
        if(isBrushBuilt==false) {
            float f = UnityEngine.Random.Range(0, 100);
            if (f > randomChange) {
                return;
            }
        }
        if(foreachTileNewStructure==false) {
            Structure toPlace = structure.Clone();
            SetStructureVariablesList.ForEach(x => x?.Invoke(toPlace));
            BuildController.Instance.EditorBuildOnTile(toPlace, tiles, foreachTileNewStructure);
        } else {
            tiles.RemoveAll(x => x.Type == TileType.Ocean);
            foreach(Tile t in tiles) {
                Structure toPlace = structure.Clone();
                SetStructureVariablesList.ForEach(x => x?.Invoke(toPlace));
                BuildController.Instance.EditorBuildOnTile(toPlace, structure.GetBuildingTiles(t), true);
            }
        }
    }
    
    public void SetBrushSize(int size) {
        brushSize = size;
        UpdateHighLights();
    }
    public void ChangeBuild(bool type) {
        changeMode = type ? ChangeMode.Tile : ChangeMode.Structure;
        if(changeMode == ChangeMode.Tile) {
            BuildController.Instance.ResetBuild();
        }
        if(changeMode==ChangeMode.Structure) {
            MouseController.Instance.SetEditorBrushHighlightActive(brushBuild);
        } else {
            MouseController.Instance.SetEditorBrushHighlightActive(true);
        }
    }

    public void SetStructure(string id) {
        structure = PrototypController.Instance.StructurePrototypes[id];
        BuildController.Instance.StartStructureBuild(id,null,structure);
        if(brushBuild) {
            MouseController.Instance.mouseState = MouseState.Idle;
            MouseController.Instance.ToBuildStructure = null;
        }
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
            Ressources[ID] = new Range();
        if(lower)
            Ressources[ID].lower = amount;
        else
            Ressources[ID].upper = amount;
    }
    internal Tile GetTileAtWorldCoord(Vector3 currFramePosition) {
        return World.Current.GetTileAt(currFramePosition.x + TileSpriteController.offset, currFramePosition.y + TileSpriteController.offset);
    }
    internal Tile GetTileAtWorldCoord(int x, int y) {
        return World.Current.GetTileAt(x, y);
    }
    public void OnBrushRandomChange(float f) {
        this.randomChange = f;
    }
    public void OnDestroy() {
        IsEditor = false;
        Instance = null;
        world.Destroy();
        SceneManager.activeSceneChanged -= OnLevelLoaded; //only the newest should get the event
    }
    /// 
    /// 
    /// SAVING FEATURES
    /// 
    /// 
    public void LoadIsland(SaveIsland file) {
        loadsavegame = file;
        Width = loadsavegame.Width;
        Height = loadsavegame.Height;
        climate = loadsavegame.climate;
        MenuController.Instance.ChangeToEditorLoadScreen();
    }
    void LoadSaveState(SaveIsland load) {
        Width = load.Width;
        Height = load.Height;
        world = new World(load.tiles, load.Width, load.Height,true);
        if(load.Resources!=null)
            Ressources = load.Resources;
        foreach (Structure s in load.structures) {
            BuildController.Instance.EditorBuildOnTile(s, s.GetBuildingTiles(s.BuildTile), true);
        }
    }

    internal void SetWorld(World world) {
        this.world = world;
    }

    public SaveIsland GetSaveState() {
        HashSet<Tile> toSave = new HashSet<Tile>(world.Tiles);
        toSave.RemoveWhere(x => x.Type == TileType.Ocean);
        return new SaveIsland(world.Islands[0].Cities[0].Structures, toSave.ToArray(), Width, Height, climate, Ressources);
    }

    [JsonObject]
    public class SaveIsland {
        [JsonPropertyAttribute] public int Width;
        [JsonPropertyAttribute] public int Height;
        [JsonPropertyAttribute] public Climate climate;
        [JsonPropertyAttribute(TypeNameHandling = TypeNameHandling.None)] public LandTile[] tiles;
        [JsonPropertyAttribute(TypeNameHandling = TypeNameHandling.Auto)] public List<Structure> structures;
        [JsonPropertyAttribute] public Dictionary<string, Range> Resources;

        [JsonIgnore] public string Name; // for loading in image or similar things
        public SaveIsland() {

        }
        public SaveIsland(List<Structure> structures, Tile[] tiles, int Width, int Height, Climate climate, Dictionary<string, Range> Ressources) {
            this.Width = Width;
            this.Height = Height;
            this.climate = climate;
            this.structures = new List<Structure>(structures);
            this.tiles = tiles.Cast<LandTile>().ToArray();
            this.Resources = new Dictionary<string, Range>();
            foreach(string id in Ressources.Keys) {
                if (Ressources[id].upper <= 0)
                    continue;
                this.Resources[id] = Ressources[id];
            }
        }
    }

}
