using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Concurrent;
/// <summary>
/// Sprite Names should follow this rules
/// HAVE TO BE SAME SIZE! -> for now 32px
/// HAVE TO BE RGBA32 BIT!
/// NO COMPRESSION!
/// AND FOLLOW THIS NAMEING RULE:
/// climate_tiletyp_version_connection
/// if climate == null:
/// tiletyp_version_connectionSameType
/// there could be in future:
/// climate_tiletyp_2ndTileType_version_connectionSameType_connection2ndType
/// </summary>
public class TileSpriteController : MonoBehaviour {
    public static TileSpriteController Instance { get; protected set; }

    public static Dictionary<string, Sprite> nameToSprite;
    private static Dictionary<TileType, Dictionary<string, List<string>>> typeTotileSpriteNames;

    public Sprite noSprite;
    public Sprite emptySprite;

    public GameObject karoOverlay;
    public GameObject tilePrefab;
    GameObject darkLayer;
    public GameObject waterLayer;
    public GameObject water;

    public Sprite darkLayerSprite;

    public Material waterMaterial;
    public Material darkMaterial;
    Material clearMaterial;
    public Material highlightMaterial;

    private static Dictionary<Vector2, Texture2D> islandToMaskTexture;
    private static Dictionary<Vector2, GameObject> islandPosToTilemap;

    private static int createdIslands = 0;
    private static int numberOfIslands = 0;
    public static float CreationPercantage => numberOfIslands == 0 ? 0 : createdIslands / numberOfIslands;
    public static bool CreationDone => createdIslands == numberOfIslands;
    public static bool CreationStarted = false;
    private static System.Diagnostics.Stopwatch islandSpriteStopWatch;
    private static Dictionary<Island, SpriteMask> islandToCityMask;
    private static Dictionary<Island, SpriteMask> islandToCustomMask;
    private static Dictionary<Island, GameObject> islandToGameObject;

    private bool apply;
    private Tilemap editorTilemap;
    private static Dictionary<string, TileBase> nameToBaseTile;
    private GameObject editor_island_tilemap;

    public delegate TileMark TileDecider(Tile tile);
    public event TileDecider TileDeciderFunc;

    public enum TileSpriteClimate { cold, middle, warm, all }

    // The pathfinding graph used to navigate our world map.
    World World {
        get { return World.Current; }
    }
    public static float offset = 0;
    // Use this for initialization
    void OnEnable() {
        if (Instance != null) {
            Debug.LogError("There should never be two tile controllers.");
        }
        Instance = this;
        water = Instantiate(waterLayer);
        //DarkLayer probably gonna be changed
        if (EditorController.IsEditor == false) {
            water.transform.position = new Vector3((World.Width / 2) - offset, (World.Height / 2) - offset, 0.1f);
            Vector3 size = new Vector3(6 + World.Width / 10, 0.1f, 6 + World.Height / 10);
            Vector2 tile = new Vector2(6 + World.Width, 6 + World.Height);
            water.transform.localScale = size;
            //water.GetComponent<Renderer>().material = waterMaterial;
            Renderer wr = water.GetComponent<Renderer>();
            wr.material.mainTextureScale = tile;
            darkLayer = new GameObject();
            darkLayer.transform.position = new Vector3((World.Width / 2) - offset, (World.Height / 2) - offset, 0);
            SpriteRenderer darksr = darkLayer.AddComponent<SpriteRenderer>();
            darksr.sprite = darkLayerSprite;
            darksr.sortingLayerName = "DarkLayer";
            darksr.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            darkLayer.transform.localScale = new Vector3(1.25f * World.Width, 1.25f * World.Height, 0);
            darkLayer.name = "DarkLayer";
            darkLayer.transform.SetParent(this.transform);
            darkLayer.SetActive(true);

            islandToCityMask = new Dictionary<Island, SpriteMask>();
            islandToGameObject = new Dictionary<Island, GameObject>();
            foreach (Island i in World.Current.Islands) {
                Vector2 key = i.Placement;
                GameObject islandGO = Instantiate(islandPosToTilemap[key]);
                Destroy(islandPosToTilemap[key]);
                islandToGameObject[i] = islandGO;
                islandGO.transform.position = key - new Vector2(offset, offset);
                islandGO.layer = LayerMask.NameToLayer("Islands");
                islandGO.name = "Island " + i.StartTile.Vector2; 
                //Now we create the masks for the islands 
                GameObject cityMaskGameobject = new GameObject("IslandCityMask");
                cityMaskGameobject.transform.parent = islandGO.transform;
                cityMaskGameobject.SetActive(false);
                cityMaskGameobject.transform.localPosition = Vector3.zero;
                SpriteMask sm = cityMaskGameobject.AddComponent<SpriteMask>();
                sm.isCustomRangeActive = true;
                sm.sortingLayerName = "DarkLayer";
                sm.frontSortingLayerID = 638755707; // UI Layer even tho its some strange number
                Texture2D masktex = Instantiate<Texture2D>(islandToMaskTexture[i.Placement]);
                sm.sprite = Sprite.Create(masktex, new Rect(0, 0, masktex.width, masktex.height), Vector2.zero, 1);
                sm.alphaCutoff = 1;
                islandToCityMask.Add(i, sm);
            }
            World.RegisterTileChanged(OnTileChanged);

            foreach (Island i in World.Current.Islands) {
                City c = i.FindCityByPlayer(PlayerController.currentPlayerNumber);
                if (c == null) {
                    continue;
                }
                foreach (Tile t in c.Tiles) {
                    OnTileChanged(t);
                }
            }
        }
        else {
            LoadSprites();
            CreateBaseTiles();
        }

        //BuildController.Instance.RegisterBuildStateChange (OnBuildStateChance);

    }
    public void EditorFix() {
        //if (editor_island_tilemap != null)
        //    Destroy(editor_island_tilemap);
        editor_island_tilemap = new GameObject();
        editor_island_tilemap.name = "EditorIsland TileMap";
        editor_island_tilemap.transform.position = new Vector3(-offset, -offset, 0);
        editorTilemap = editor_island_tilemap.AddComponent<Tilemap>();
        Grid g = editor_island_tilemap.AddComponent<Grid>();
        g.cellSize = new Vector3(1, 1, 0);
        g.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        g.cellLayout = GridLayout.CellLayout.Rectangle;
        TilemapRenderer trr = editor_island_tilemap.AddComponent<TilemapRenderer>();
        trr.sortingLayerName = "Tile";
        editorTilemap.size = new Vector3Int(EditorController.Width, EditorController.Height, 0);
        water.transform.position = new Vector3((World.Width / 2) - offset, (World.Height / 2) - offset, 0.1f);
        water.transform.localScale = new Vector3(World.Width / 10, 0.1f, World.Height / 10);
        water.GetComponent<Renderer>().material = waterMaterial;
        water.GetComponent<Renderer>().material.mainTextureScale = new Vector2(World.Width, World.Height);

        foreach (Tile t in World.Current.Tiles) {
            ChangeEditorTile(t);
        }
        World.RegisterTileChanged(ChangeEditorTile);

    }
    public void Update() {
        if (apply) {
            foreach (SpriteMask sm in islandToCityMask.Values) {
                sm.sprite.texture.Apply();
                apply = false;
            }
            if (islandToCustomMask != null) {
                foreach (SpriteMask sm in islandToCustomMask.Values) {
                    sm.sprite.texture.Apply();
                    apply = false;
                }
            }
        }
        
    }
    private static void CreateBaseTiles() {
        nameToBaseTile = new Dictionary<string, TileBase>();
        foreach (string name in nameToSprite.Keys) {
            if (name.Contains("animated")) {
                //AnimatedTile tileBase = ScriptableObject.CreateInstance<AnimatedTile>();
                //tileBase.m_AnimatedSprites = tests.ToArray();
                //tileBase.m_MinSpeed = 0.05f;
                //tileBase.m_MaxSpeed = 0.05f;
                //nameToBaseTile[name] = tileBase;
            }
            else {
                UnityEngine.Tilemaps.Tile tileBase = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                tileBase.sprite = nameToSprite[name];
                tileBase.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
                nameToBaseTile[name] = tileBase;
            }
        }
    }

    public static void CreateIslandSprites(List<MapGenerator.IslandStruct> islands) {
        if (EditorController.IsEditor)
            return;
        CreationStarted = true;
        numberOfIslands = islands.Count;
        LoadSprites();
        islandSpriteStopWatch = new System.Diagnostics.Stopwatch();
        islandSpriteStopWatch.Start();
        islandPosToTilemap = new Dictionary<Vector2, GameObject>();
        islandToMaskTexture = new Dictionary<Vector2, Texture2D>();
        CreateBaseTiles();

        foreach (MapGenerator.IslandStruct i in islands) {
            int islandWidth = (i.Width + 1);
            int islandHeight = (i.Height + 1);
            int xTileOffset = i.x;
            int yTileOffset = i.y;

            GameObject island_tilemap = new GameObject();
            Tilemap tilemap = island_tilemap.AddComponent<Tilemap>();
            Grid g = island_tilemap.AddComponent<Grid>();
            g.cellSize = new Vector3(1, 1, 0);
            g.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            g.cellLayout = GridLayout.CellLayout.Rectangle;
            TilemapRenderer trr = island_tilemap.AddComponent<TilemapRenderer>();
            trr.sortingLayerName = "Tile";
            tilemap.size = new Vector3Int(i.Width, i.Height, 0);
            
            DontDestroyOnLoad(tilemap.gameObject);

            Texture2D masktexture = null;

            masktexture = new Texture2D(islandWidth, islandHeight, TextureFormat.Alpha8, false, true);
            masktexture.SetPixels32(new Color32[(islandWidth) * (islandHeight)]);
            masktexture.filterMode = FilterMode.Point;
            foreach (Tile tile_data in i.Tiles) {
                if (tile_data.Type == TileType.Ocean)
                    continue;
                //TILEMAP
                int x = (int)(tile_data.X - xTileOffset);
                int y = (int)(tile_data.Y - yTileOffset);
                if(tile_data.SpriteName==null||nameToBaseTile.ContainsKey(tile_data.SpriteName)==false) {
                    Debug.Log("Missing "+ tile_data.Type + " tilesprite " + tile_data.SpriteName);
                }
                string temp = nameToBaseTile.ContainsKey(tile_data.SpriteName) ? tile_data.SpriteName : "nosprite" ;
                tilemap.SetTile(new Vector3Int( x, y, 0 ) , nameToBaseTile[temp]);
                //MASK TEXTURE
                masktexture.SetPixel(x, y, new Color32(128, 128, 128, 128));
            }
            tilemap.RefreshAllTiles();
            islandPosToTilemap[i.GetPosition()] = island_tilemap;
            DontDestroyOnLoad(island_tilemap);
            
            masktexture.Apply();
            islandToMaskTexture.Add(i.GetPosition(), masktexture);
            createdIslands++;
        }

        islandSpriteStopWatch.Stop();
        Debug.Log("Island Visuals " + islandSpriteStopWatch.ElapsedMilliseconds + "ms (" + islandSpriteStopWatch.Elapsed.TotalSeconds + "s)! ");
    }

    private Sprite GetSpriteForName(string spriteName) {
        if (nameToSprite.ContainsKey(spriteName)) {
            return nameToSprite[spriteName];
        }
        return nameToSprite["nosprite"];
    }

    void OnTileChanged(Tile tile_data) {
        int x = (int)(tile_data.X - tile_data.Island.Placement.x);
        int y = (int)(tile_data.Y - tile_data.Island.Placement.y);
        if (tile_data.City.PlayerNumber == PlayerController.currentPlayerNumber) {
            islandToCityMask[tile_data.Island].sprite.texture.SetPixel(x, y, new Color32(128, 128, 128, 255));
        }
        apply = true;
        if (TileDeciderFunc != null && islandToCustomMask != null) {
            darkLayer.SetActive(true);

            TileMark tm = TileDeciderFunc(tile_data);
            switch (tm) {
                case TileMark.None:
                    islandToCustomMask[tile_data.Island].sprite.texture.SetPixel(x, y, new Color32(128, 128, 128, 255));
                    break;
                case TileMark.Highlight:
                    break;
                case TileMark.Dark:
                    islandToCustomMask[tile_data.Island].sprite.texture.SetPixel(x, y, new Color32(128, 128, 128, 0));
                    break;
            }
        }
        else {
            //sr.material = clearMaterial;
            if (darkLayer != null)
                darkLayer.SetActive(false);
        }

    }

    public void ChangeEditorTile(Tile tile_data) {
        if (EditorController.IsEditor==false) {
            return;
        }
        if(String.IsNullOrEmpty(tile_data.SpriteName)) {
            editorTilemap.SetTile(new Vector3Int(tile_data.X, tile_data.Y, 0), null);
        } else {
            string temp = nameToBaseTile.ContainsKey(tile_data.SpriteName) ? tile_data.SpriteName : "nosprite";
            if (nameToBaseTile.ContainsKey(tile_data.SpriteName) == false)
                Debug.Log(tile_data.SpriteName);
            editorTilemap.SetTile(new Vector3Int(tile_data.X, tile_data.Y, 0), nameToBaseTile[temp]);
        }
    }

    TileMark TileCityDecider(Tile t) {
        if (t.City.IsCurrPlayerCity()) {
            return TileMark.None;
        }
        else {
            return TileMark.Dark;
        }
    }

   public static void LoadSprites() {
        nameToSprite = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/TileSprites/");
        foreach (Sprite s in sprites) {
            nameToSprite.Add(s.name, s);
        }
        Sprite[] custom = ModLoader.LoadSprites(SpriteType.Tile);
        if (custom != null) {
            foreach (Sprite s in custom) {
                nameToSprite[s.name] = s;
            }
        }
        typeTotileSpriteNames = new Dictionary<TileType, Dictionary<string, List<string>>>();

        foreach (string s in TileSpriteController.nameToSprite.Keys) {
            string part = s.Split('_')[0].ToLower();
            string climateIdentifier = TileSpriteClimate.all.ToString();
            //if the first identifier is a climate
            try {
                Climate climate = (Climate)Enum.Parse(typeof(Climate), part, true);
                climateIdentifier = climate.ToString();
                part = s.Split('_')[1].ToLower();
            }
            catch {

            }

            TileType type;
            try {
                type = (TileType)Enum.Parse(typeof(TileType), part, true);
            }
            catch {
                continue;
            }

            //			Debug.Log (type + " / " + s.name);
            if (typeTotileSpriteNames.ContainsKey(type) == false) {
                Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
                typeTotileSpriteNames.Add(type, dict);
            }
            if (typeTotileSpriteNames[type].ContainsKey(climateIdentifier)) {
                typeTotileSpriteNames[type][climateIdentifier].Add(s);
            }
            else {
                typeTotileSpriteNames[type].Add(climateIdentifier, new List<string> { s });
            }
        }
    }
    void OnDestroy() {
        World.UnregisterTileChanged(OnTileChanged);
        createdIslands = 0;
        Instance = null;
    }
    public void AddDecider(TileDecider addDeciderFunc, bool isCityDecider = false) {
        this.TileDeciderFunc += addDeciderFunc;
        if (TileDeciderFunc != null || TileDeciderFunc.GetInvocationList().Length > 0)
            darkLayer.SetActive(true);
        if (isCityDecider) {
            islandToCityMask.ToList().ForEach(x => x.Value.gameObject.SetActive(true));
        }
        else {
            islandToCustomMask = new Dictionary<Island, SpriteMask>();
            foreach (Island i in World.Current.Islands) {
                GameObject cityMaskGameobject = new GameObject("IslandCustomMask " + addDeciderFunc.Method.Name);
                cityMaskGameobject.transform.parent = islandToGameObject[i].transform;
                cityMaskGameobject.transform.localPosition = -new Vector3(0, 0);
                SpriteMask sm = cityMaskGameobject.AddComponent<SpriteMask>();
                sm.isCustomRangeActive = true;
                sm.sortingLayerName = "DarkLayer";
                sm.frontSortingLayerID = 638755707; // UI Layer even tho its some strange number
                Texture2D masktex = Instantiate<Texture2D>(islandToMaskTexture[i.Placement]);
                sm.sprite = Sprite.Create(masktex, new Rect(0, 0, masktex.width, masktex.height), Vector2.zero, 1);
                sm.alphaCutoff = 1;

                islandToCustomMask.Add(i, sm);
                //TODO: call this again or just ontilechanged when the corresponding decider needs updating 
                i.Tiles.ForEach(x => OnTileChanged(x));

            }
        }
    }
    public void ResetDecider() {
        //Debug.Log ("RESET");
        TileDeciderFunc = null;
        darkLayer.SetActive(false);
    }
    public void RemoveDecider(TileDecider removeFunc, bool isCityDecider = false) {
        if (EditorController.IsEditor)
            return;
        TileDeciderFunc -= removeFunc;
        if (TileDeciderFunc == null || TileDeciderFunc.GetInvocationList().Length == 0)
            darkLayer.SetActive(false);

        if (isCityDecider) {
            islandToCityMask.ToList().ForEach(x => x.Value.gameObject.SetActive(false));
        }
        else {
            if (islandToCustomMask == null)
                return;
            foreach (SpriteMask sm in islandToCustomMask.Values) {
                if (sm == null)
                    continue;
                Destroy(sm.gameObject);
            }
        }
    }
    public static List<string> GetSpriteNamesForType(TileType type, Climate climate, string spriteAddon = null) {
        string climateString = climate.ToString();
        if (typeTotileSpriteNames == null)
            LoadSprites();
        if (typeTotileSpriteNames.ContainsKey(type) == false)
            return null;
        if(type == TileType.Shore) {
            //TODO: FIX this -- For now only one type of shore
            return new List<string> { type.ToString().ToLower()+spriteAddon};
            //if (typeTotileSpriteNames.ContainsKey(type) || typeTotileSpriteNames[type].ContainsKey(climateString)) {
            //    return null;
            //}
            //return typeTotileSpriteNames[type][climateString]?.Where(x => x == type + spriteAddon).ToList();
        }
        if (typeTotileSpriteNames[type].ContainsKey(climateString) == false) {
            if (typeTotileSpriteNames[type].ContainsKey(TileSpriteClimate.all.ToString()) == false) {
                return null;
            }
            return typeTotileSpriteNames[type][TileSpriteClimate.all.ToString()];
        }
        return typeTotileSpriteNames[type][climateString];
    }

}
