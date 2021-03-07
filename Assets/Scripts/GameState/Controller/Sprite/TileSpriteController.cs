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
    public Material tileMapRendererBlending;
    static Dictionary<string, Sprite> nameToSprite;
    static Dictionary<Climate, ClimateSprites> climateTileSprites;
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
    public Material tileMapMaterial;

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

    internal static string GetSpriteForSpecial(TileType type, int x, int y) {
        return type.ToString().ToLower() + "_" + x + "_" + y;
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
        //trr.material = tileMapRendererBlending;
        trr.sortingLayerName = "Tile";
        editorTilemap.size = new Vector3Int(EditorController.Width, EditorController.Height, 0);
        water.transform.position = new Vector3((World.Width / 2) - offset, (World.Height / 2) - offset, 0.1f);
        water.transform.localScale = new Vector3(World.Width / 10, 0.1f, World.Height / 10);
        water.GetComponent<Renderer>().material = waterMaterial;
        water.GetComponent<Renderer>().material.mainTextureScale = new Vector2(World.Width, World.Height);
        Texture2D tex = new Texture2D(World.Width, World.Height);
        Color[] colors = tex.GetPixels();
        for (int y = 0; y < World.Height; y++) {
            for (int x = 0; x < World.Width; x++) {
                //float b = ((((float)x + (float)y) / ((float)World.Width + (float)World.Height)));
                Tile t = World.GetTileAt(x,y);
                if (t == null) {
                    continue;
                }
                colors[x  + y* World.Width] = new Color(1, 1, 1, t.Moisture);
            }
        }
        foreach (Tile t in World.Current.Tiles) {
            ChangeEditorTile(t);
            //if (t != null)
            //    colors[t.X + t.Y * World.Width] = new Color(1, 1, 1, 0);
        }
        tex.SetPixels(colors);
        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        var dirPath = Application.dataPath + "/../SaveImages/";
        if (!System.IO.Directory.Exists(dirPath)) {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        System.IO.File.WriteAllBytes(dirPath + "Image" + ".png", bytes);

        trr.material = tileMapMaterial;
        trr.material.SetTexture("_Saturations", tex);
        trr.material.SetVector("_startPosition", new Vector2(0, 0));
        trr.material.SetVector("_Size", new Vector2(World.Width, World.Height));

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

    public static void CreateIslandSprites(List<MapGenerator.IslandData> islands) {
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

        foreach (MapGenerator.IslandData i in islands) {
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
        if(darkLayer == null) {
            return;
        }
        int x = (int)(tile_data.X - tile_data.Island.Placement.x);
        int y = (int)(tile_data.Y - tile_data.Island.Placement.y);
        if (tile_data.City.PlayerNumber == PlayerController.currentPlayerNumber) {
            islandToCityMask[tile_data.Island].sprite.texture.SetPixel(x, y, new Color32(128, 128, 128, 255));
        }
        apply = true;
        if (TileDeciderFunc != null && islandToCustomMask != null) {
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
    }

    public void ChangeEditorTile(Tile tile_data) {
        if (EditorController.IsEditor==false) {
            return;
        }
        if (tile_data == null)
            return;
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
        climateTileSprites = new Dictionary<Climate, ClimateSprites>();
        foreach (Climate c in Enum.GetValues(typeof(Climate)))
            climateTileSprites[c] = new ClimateSprites(c);
        foreach (string s in TileSpriteController.nameToSprite.Keys) {
            string[] parts = s.Split('_');
            if (parts.Length < 4) {
                //Debug.LogError("Tile Sprite in correct formatted - climate_type_connections_variation");
                continue;
            }
            TileType type = (TileType)Enum.Parse(typeof(TileType), parts[1], true);
            string connections = parts[2].ToLower();
            string variation = parts[3].ToLower();
            if (parts[0].ToLower() == "all") {
                foreach(Climate c in Enum.GetValues(typeof(Climate)))
                    climateTileSprites[c].AddSprite(type, connections, s);
            }
            else {
                try {
                    Climate climate = (Climate)Enum.Parse(typeof(Climate), parts[0], true);
                    climateTileSprites[climate].AddSprite(type, connections, s);
                }
                catch {
                    //NO correct climate so skip -- TODO: uncomment -- for now to much spam
                    //Debug.LogError("Tile Sprite in correct formatted Climate - either all or on of the enums (cold,middle,warm)");
                }
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
        TileDeciderFunc = null;
        darkLayer.SetActive(false);
    }
    public void RemoveDecider(TileDecider removeFunc, bool isCityDecider = false) {
        if (EditorController.IsEditor)
            return;
        TileDeciderFunc -= removeFunc;
        if (TileDeciderFunc == null || TileDeciderFunc.GetInvocationList().Length == 0)
            ResetDecider();

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
        if (climateTileSprites == null)
            LoadSprites();
        if (type == TileType.Ocean)
            return null;
        if (climateTileSprites[climate].typeTree.ContainsKey(type) == false)
            return null;
        return climateTileSprites[climate].typeTree[type].GetClosest(spriteAddon);
    }
    class ClimateSprites {
        public Climate climate;
        public Dictionary<TileType, TileTypeSprites> tileTypeSprites = new Dictionary<TileType, TileTypeSprites>();
        public Dictionary<TileType, ClosestTree> typeTree = new Dictionary<TileType, ClosestTree>();
        public ClimateSprites(Climate climate) {
            this.climate = climate;
        }

        public void AddSprite(TileType type, string connections, string name) {
            if (tileTypeSprites.ContainsKey(type) == false) {
                tileTypeSprites[type] = new TileTypeSprites(type);
                typeTree[type] = new ClosestTree(type);
            }
            typeTree[type].Insert(connections, name);
            tileTypeSprites[type].AddSprite(connections, name);
        }

        internal List<string> GetSprites(TileType type, string spriteAddon) {
            if (tileTypeSprites.ContainsKey(type) == false)
                return new List<string>();
            return tileTypeSprites[type].GetSprites(spriteAddon);
        }
    }
    class TileTypeSprites {
        public TileType type;
        public Dictionary<string, List<string>> connectionToSprite = new Dictionary<string, List<string>>();

        public TileTypeSprites(TileType type) {
            this.type = type;
        }

        public void AddSprite(string connections, string name) {
            connections = connections.ToLower();
            if (connectionToSprite.ContainsKey(connections) == false) 
                connectionToSprite[connections] = new List<string>();
            connectionToSprite[connections].Add(name);
        }
        internal List<string> GetSprites(string spriteAddon) {
            if (spriteAddon == null)
                return null;
            if (type == TileType.Dirt)
                Debug.Log("");
            if (connectionToSprite.ContainsKey(spriteAddon) == false) {
                string newAddon = "";
                char f = type.ToString().ToLower()[0];
                foreach (char c in spriteAddon) {
                    if (c == 'o') {
                        newAddon += 'o';
                        continue;
                    }
                    newAddon += c == f ? f : 'a';
                }
                if (connectionToSprite.ContainsKey(newAddon))
                    return connectionToSprite[newAddon];
                newAddon = new string(new char[]{ f, f, f, f });
                if (connectionToSprite.ContainsKey(newAddon) ==false)
                    return new List<string>() { "nosprite" };
                return connectionToSprite[newAddon];
            }
            return connectionToSprite[spriteAddon];
        }
    }
}
