using Andja.Editor;
using Andja.Model;
using Andja.Model.Generator;
using Andja.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Tile = Andja.Model.Tile;

namespace Andja.Controller {

    /// <summary>
    /// Sprite Names should follow this rules
    /// HAVE TO BE SAME SIZE! -> for now 32px
    /// HAVE TO BE RGBA32 BIT!
    /// NO COMPRESSION!
    /// 
    /// Tile Sprites have the following Name for now:
    /// Starting with climate_ or all_
    /// Followed by the type_
    /// Then by all sorrounding types condensed into 4 eg. wwww_ (all water)
    /// At the end if there is any variation then it goes here.
    /// TODO: Rethink the nameing scheme 
    /// </summary>
    public class TileSpriteController : MonoBehaviour {
        public static TileSpriteController Instance { get; protected set; }
        public Material tileMapRendererBlending;
        private static Dictionary<string, Sprite> nameToSprite;
        private static Dictionary<Climate, ClimateSprites> climateTileSprites;
        public Sprite noSprite;
        public Sprite emptySprite;

        public GameObject karoOverlay;
        private GameObject darkLayer;
        public GameObject oceanPrefab;
        public GameObject oceanInstance;

        public Sprite darkLayerSprite;

        public Material waterMaterial;
        public Material darkMaterial;
        private Material clearMaterial;
        public Material highlightMaterial;
        public Material tileMapMaterial;
        private static Dictionary<City, CityMaskTexture> cityToMaskTexture;
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
        private World World {
            get { return World.Current; }
        }

        public static float offset = 0;

        // Use this for initialization
        private void OnEnable() {
            if (Instance != null) {
                Debug.LogError("There should never be two tile controllers.");
            }
            Instance = this;
            oceanInstance = Instantiate(oceanPrefab);
            //DarkLayer probably gonna be changed
            if (EditorController.IsEditor == false) {
                oceanInstance.transform.position = new Vector3((World.Width / 2) - offset, (World.Height / 2) - offset, 0.1f);
                Vector3 size = new Vector3(6 + World.Width / 10, 0.1f, 6 + World.Height / 10);
                Vector2 tile = new Vector2(6 + World.Width, 6 + World.Height);
                oceanInstance.transform.localScale = size;
                //water.GetComponent<Renderer>().material = waterMaterial;
                Renderer wr = oceanInstance.GetComponent<Renderer>();
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
                darkLayer.SetActive(false);

                islandToCityMask = new Dictionary<Island, SpriteMask>();
                islandToGameObject = new Dictionary<Island, GameObject>();
                islandToCustomMask = new Dictionary<Island, SpriteMask>();
                cityToMaskTexture = new Dictionary<City, CityMaskTexture>();

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
                    TilemapRenderer trr = islandGO.GetComponent<TilemapRenderer>();
                    trr.material = tileMapMaterial;

                    foreach (City city in i.Cities) {
                        OnCityCreated(city);
                    }

                    GameObject MaskGameobject = new GameObject("IslandCustomMask ");
                    MaskGameobject.transform.parent = islandToGameObject[i].transform;
                    MaskGameobject.transform.localPosition = -new Vector3(0, 0);
                    SpriteMask csm = MaskGameobject.AddComponent<SpriteMask>();
                    csm.isCustomRangeActive = true;
                    csm.sortingLayerName = "DarkLayer";
                    csm.frontSortingLayerID = 638755707; // UI Layer even tho its some strange number
                    Texture2D cmasktex = Instantiate<Texture2D>(islandToMaskTexture[i.Placement]);
                    csm.sprite = Sprite.Create(cmasktex, new Rect(0, 0, masktex.width, masktex.height), Vector2.zero, 1);
                    csm.alphaCutoff = 1;
                    islandToCustomMask.Add(i, csm);
                }
                World.RegisterTileChanged(OnTileChanged);

                foreach (Island i in World.Current.Islands) {
                    foreach (var c in i.Cities) {
                        foreach (Tile t in c.Tiles) {
                            OnTileChanged(t);
                        }
                    }
                }
            }
            else {
                LoadSprites();
                CreateBaseTiles();
            }
            BuildController.Instance.RegisterCityCreated(OnCityCreated);
            //BuildController.Instance.RegisterBuildStateChange (OnBuildStateChance);
            PlayerController.Instance.cbPlayerChange += OnPlayerChange;
            OnPlayerChange(null, PlayerController.CurrentPlayer);
        }

        private void OnCityCreated(City city) {
            CityMaskTexture cityMask = new CityMaskTexture {
                city = city,
                texture = Instantiate<Texture2D>(islandToMaskTexture[city.Island.Placement])
            };
            cityToMaskTexture.Add(city, cityMask);
        }

        private void OnPlayerChange(Player o, Player n) {
            foreach (Island island in World.Islands) {
                City city = island.FindCityByPlayer(n.Number);
                if (city != null) {
                    Texture2D tex = cityToMaskTexture[city].texture;
                    tex.Apply();
                    islandToCityMask[island].sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1);
                    //foreach (var item in city.Tiles) {
                    //    OnTileChanged(item);
                    //}
                } else {
                    islandToCityMask[island].sprite = null;
                }
            }
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
            oceanInstance.transform.position = new Vector3((World.Width / 2) - offset, (World.Height / 2) - offset, 0.1f);
            oceanInstance.transform.localScale = new Vector3(World.Width / 10, 0.1f, World.Height / 10);
            oceanInstance.GetComponent<Renderer>().material = waterMaterial;
            oceanInstance.GetComponent<Renderer>().material.mainTextureScale = new Vector2(World.Width, World.Height);
            Texture2D tex = new Texture2D(World.Width, World.Height);
            Color[] colors = tex.GetPixels();
            for (int y = 0; y < World.Height; y++) {
                for (int x = 0; x < World.Width; x++) {
                    //float b = ((((float)x + (float)y) / ((float)World.Width + (float)World.Height)));
                    Tile t = World.GetTileAt(x, y);
                    if (t == null) {
                        continue;
                    }
                    colors[x + y * World.Width] = new Color(1, 1, 1, t.Moisture);
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
            foreach (var item in cityToMaskTexture.Values) {
                item.CheckApply();
            }
            if (apply) {
                if (islandToCustomMask != null) {
                    foreach (SpriteMask sm in islandToCustomMask.Values) {
                        sm.sprite?.texture.Apply();
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
                    if (tile_data.SpriteName == null || nameToBaseTile.ContainsKey(tile_data.SpriteName) == false) {
                        Debug.Log("Missing " + tile_data.Type + " tilesprite " + tile_data.SpriteName);
                    }
                    string temp = nameToBaseTile.ContainsKey(tile_data.SpriteName) ? tile_data.SpriteName : "nosprite";
                    tilemap.SetTile(new Vector3Int(x, y, 0), nameToBaseTile[temp]);

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

        private void OnTileChanged(Tile tile_data) {
            if (darkLayer == null) {
                return;
            }
            int x = (int)(tile_data.X - tile_data.Island.Placement.x);
            int y = (int)(tile_data.Y - tile_data.Island.Placement.y);
            cityToMaskTexture[tile_data.City].SetPixel(x, y, new Color32(128, 128, 128, 255));
            foreach (var item in tile_data.Island.Cities) {
                if(item != tile_data.City)
                    cityToMaskTexture[item].SetPixel(x, y, new Color32(128, 128, 128, 0));
            }
            if (TileDeciderFunc != null && islandToCustomMask != null) {
                apply = true;
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
            if (EditorController.IsEditor == false) {
                return;
            }
            if (tile_data == null)
                return;
            if (String.IsNullOrEmpty(tile_data.SpriteName)) {
                editorTilemap.SetTile(new Vector3Int(tile_data.X, tile_data.Y, 0), null);
            }
            else {
                string temp = nameToBaseTile.ContainsKey(tile_data.SpriteName) ? tile_data.SpriteName : "nosprite";
                if (nameToBaseTile.ContainsKey(tile_data.SpriteName) == false)
                    Debug.Log(tile_data.SpriteName);
                editorTilemap.SetTile(new Vector3Int(tile_data.X, tile_data.Y, 0), nameToBaseTile[temp]);
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
                    foreach (Climate c in Enum.GetValues(typeof(Climate)))
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

        private void OnDestroy() {
            World.UnregisterTileChanged(OnTileChanged);
            createdIslands = 0;
            Instance = null;
        }

        public void AddDecider(TileDecider addDeciderFunc, bool isCityDecider = false) {
            if (isCityDecider && MouseController.Instance.SelectedStructure != null)
                return;
            if (TileDeciderFunc != null && TileDeciderFunc.GetInvocationList().Contains(addDeciderFunc))
                return;
            this.TileDeciderFunc += addDeciderFunc;
            if (TileDeciderFunc != null || TileDeciderFunc.GetInvocationList().Length > 0)
                darkLayer.SetActive(true);
            if (isCityDecider) {
                islandToCityMask.ToList().ForEach(x => x.Value.gameObject.SetActive(true));
            }
            else {
                foreach (Island i in World.Current.Islands) {
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
                //if (islandToCustomMask == null)
                //    return;
                //foreach (SpriteMask sm in islandToCustomMask.Values) {
                //    if (sm == null)
                //        continue;
                //    Destroy(sm.gameObject);
                //}
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
        public static List<string> GetAllSpriteNamesForType(TileType type, Climate climate) {
            if (climateTileSprites == null)
                LoadSprites();
            if (type == TileType.Ocean)
                return null;
            if (climateTileSprites[climate].typeTree.ContainsKey(type) == false)
                return null;
            return climateTileSprites[climate].typeTree[type].GetAll();
        }
        private class ClimateSprites {
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

        private class TileTypeSprites {
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
                    newAddon = new string(new char[] { f, f, f, f });
                    if (connectionToSprite.ContainsKey(newAddon) == false)
                        return new List<string>() { "nosprite" };
                    return connectionToSprite[newAddon];
                }
                return connectionToSprite[spriteAddon];
            }
        }
        class CityMaskTexture {
            public City city;
            public Texture2D texture;
            public bool apply;
            public void CheckApply() {
                if (apply) {
                    texture.Apply();
                    apply = false;
                }
            }

            internal void SetPixel(int x, int y, Color32 color32) {
                texture.SetPixel(x, y, color32);
                apply = true;
            }
        }

        public static System.Collections.Concurrent.ConcurrentBag<Vector2> positions = new System.Collections.Concurrent.ConcurrentBag<Vector2>();
        public static System.Collections.Concurrent.ConcurrentDictionary<Vector2, float> positionsCost = new System.Collections.Concurrent.ConcurrentDictionary<Vector2, float>();
        private void OnDrawGizmos() {
            if (Application.isPlaying) {
                //foreach (Pathfinding.WorldNode n in World.Current.WorldGraph.Nodes) {
                //    if (n == null)
                //        continue;
                //    foreach (Pathfinding.WorldEdge e in n.Edges) {
                //        Gizmos.color = Color.white;
                //        Gizmos.DrawLine(new Vector2(n.x + 0.5f, n.y + 0.5f), new Vector2(e.Node.x + 0.5f, e.Node.y + 0.5f));
                //    }
                foreach (var t in positions) {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(new Vector3(t.x + 0.5f, t.y + 0.5f), 0.5f);
                }
                foreach (var t in positionsCost) {
                    UnityEditor.Handles.Label(new Vector3(t.Key.x + 0.5f, t.Key.y + 0.5f), t.Value +"");
                }
            }
        }

    }
}