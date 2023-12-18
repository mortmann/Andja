using Andja.Editor;
using Andja.Model;
using Andja.Model.Generator;
using Andja.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.Graphs.Styles;
using Color = UnityEngine.Color;
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
    /// Then by all surrounding types condensed into 4 eg. wwww_ (all water)
    /// At the end if there is any variation then it goes here.
    /// TODO: Rethink the nameing scheme 
    /// </summary>
    public class TileSpriteController : MonoBehaviour {
        private const string NoSpriteName = "nosprite";
        public static TileSpriteController Instance { get; protected set; }
        public Material tileMapRendererBlending;
        private static Dictionary<string, Sprite> _nameToSprite;
        private static Dictionary<Climate, ClimateSprites> _climateTileSprites;

        public GameObject karoOverlay;
        public GameObject oceanPrefab;
        public GameObject oceanInstance;

        public Sprite darkLayerSprite;

        public Material waterMaterial;
        public Material darkMaterial;
        public Material highlightMaterial;
        public Material tileMapMaterial;

        private GameObject _darkLayer;
        private Material _clearMaterial;
        private static Dictionary<ICity, CityMaskTexture> _cityToMaskTexture;
        private static Dictionary<Vector2, Texture2D> _islandToMaskTexture;
        private static Dictionary<Vector2, GameObject> _islandPosToTilemap;
        private static System.Diagnostics.Stopwatch _islandSpriteStopWatch;
        private static Dictionary<IIsland, SpriteMask> _islandToCityMask;
        private static Dictionary<IIsland, GameObject> _islandToGameObject;
        private static Dictionary<Island, SpriteMask> _islandToCustomMask;
        private static int _createdIslands = 0;
        private static int _numberOfIslands = 0;
        private bool _apply;
        private Tilemap _editorTileMap;
        private static Dictionary<string, TileBase> _nameToBaseTile;
        private GameObject _editorIslandTileMap;

        public static float CreationPercentage => _numberOfIslands == 0 ? 0 : _createdIslands / _numberOfIslands;
        public static bool CreationDone => _createdIslands == _numberOfIslands;
        public static bool CreationStarted = false;

        public delegate TileMark TileDecider(Tile tile);

        public event TileDecider TileDeciderFunc;

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
                oceanInstance.transform.position = new Vector3((World.Current.Width / 2) - offset, (World.Current.Height / 2) - offset, 0.1f);
                Vector3 size = new Vector3(6 + World.Current.Width / 10, 0.1f, 6 + World.Current.Height / 10);
                Vector2 tile = new Vector2(6 + World.Current.Width, 6 + World.Current.Height);
                oceanInstance.transform.localScale = size;
                //water.GetComponent<Renderer>().material = waterMaterial;
                Renderer wr = oceanInstance.GetComponent<Renderer>();
                wr.material.mainTextureScale = tile;
                _darkLayer = new GameObject();
                _darkLayer.transform.position = new Vector3((World.Current.Width / 2) - offset, (World.Current.Height / 2) - offset, 0);
                SpriteRenderer darksr = _darkLayer.AddComponent<SpriteRenderer>();
                darksr.sprite = darkLayerSprite;
                darksr.sortingLayerName = "DarkLayer";
                darksr.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                _darkLayer.transform.localScale = new Vector3(1.25f * World.Current.Width, 1.25f * World.Current.Height, 0);
                _darkLayer.name = "DarkLayer";
                _darkLayer.transform.SetParent(this.transform);
                _darkLayer.SetActive(false);

                _islandToCityMask = new Dictionary<IIsland, SpriteMask>();
                _islandToGameObject = new Dictionary<IIsland, GameObject>();
                _islandToCustomMask = new Dictionary<Island, SpriteMask>();
                _cityToMaskTexture = new Dictionary<ICity, CityMaskTexture>();

                foreach (Island i in World.Current.Islands) {
                    Vector2 key = i.Placement;
                    GameObject islandGO = Instantiate(_islandPosToTilemap[key]);
                    Destroy(_islandPosToTilemap[key]);
                    _islandToGameObject[i] = islandGO;
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
                    Texture2D masktex = Instantiate<Texture2D>(_islandToMaskTexture[i.Placement]);
                    sm.sprite = Sprite.Create(masktex, new Rect(0, 0, masktex.width, masktex.height), Vector2.zero, 1);
                    sm.alphaCutoff = 1;
                    _islandToCityMask.Add(i, sm);
                    TilemapRenderer trr = islandGO.GetComponent<TilemapRenderer>();
                    trr.material = tileMapMaterial;

                    foreach (City city in i.Cities) {
                        OnCityCreated(city);
                    }

                    GameObject MaskGameobject = new GameObject("IslandCustomMask ");
                    MaskGameobject.transform.parent = _islandToGameObject[i].transform;
                    MaskGameobject.transform.localPosition = new Vector3(0, 0);
                    SpriteMask csm = MaskGameobject.AddComponent<SpriteMask>();
                    csm.isCustomRangeActive = true;
                    csm.sortingLayerName = "DarkLayer";
                    csm.frontSortingLayerID = 638755707; // UI Layer even tho its some strange number
                    Texture2D cmasktex = Instantiate<Texture2D>(_islandToMaskTexture[i.Placement]);
                    csm.sprite = Sprite.Create(cmasktex, new Rect(0, 0, masktex.width, masktex.height), Vector2.zero, 1);
                    csm.alphaCutoff = 1;
                    _islandToCustomMask.Add(i, csm);
                }
                World.Current.RegisterTileChanged(OnTileChanged);
                BuildController.Instance.RegisterCityCreated(OnCityCreated);
                PlayerController.Instance.cbPlayerChange += OnPlayerChange;
                OnPlayerChange(null, PlayerController.CurrentPlayer);

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
        }

        private void OnCityCreated(ICity city) {
            CityMaskTexture cityMask = new CityMaskTexture {
                city = city,
                texture = Instantiate<Texture2D>(_islandToMaskTexture[city.Island.Placement])
            };
            _cityToMaskTexture.Add(city, cityMask);
        }

        private static void OnPlayerChange(Player o, Player n) {
            foreach (Island island in World.Current.Islands) {
                ICity city = island.FindCityByPlayer(n.Number);
                if (city != null) {
                    Texture2D tex = _cityToMaskTexture[city].texture;
                    tex.Apply();
                    _islandToCityMask[island].sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1);
                } else {
                    _islandToCityMask[island].sprite = null;
                }
            }
        }

        internal static string GetSpriteForSpecial(TileType type, int x, int y) {
            return type.ToString().ToLower() + "_" + x + "_" + y;
        }

        public void EditorFix() {
            //if (editor_island_tilemap != null)
            //    Destroy(editor_island_tilemap);
            _editorIslandTileMap = new GameObject();
            _editorIslandTileMap.name = "EditorIsland TileMap";
            _editorIslandTileMap.transform.position = new Vector3(-offset, -offset, 0);
            _editorTileMap = _editorIslandTileMap.AddComponent<Tilemap>();
            Grid g = _editorIslandTileMap.AddComponent<Grid>();
            g.cellSize = new Vector3(1, 1, 0);
            g.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            g.cellLayout = GridLayout.CellLayout.Rectangle;
            TilemapRenderer trr = _editorIslandTileMap.AddComponent<TilemapRenderer>();
            //trr.material = tileMapRendererBlending;
            trr.sortingLayerName = "Tile";
            _editorTileMap.size = new Vector3Int(EditorController.Width, EditorController.Height, 0);
            oceanInstance.transform.position = new Vector3((World.Current.Width / 2) - offset, (World.Current.Height / 2) - offset, 0.1f);
            oceanInstance.transform.localScale = new Vector3(World.Current.Width / 10, 0.1f, World.Current.Height / 10);
            oceanInstance.GetComponent<Renderer>().material = waterMaterial;
            oceanInstance.GetComponent<Renderer>().material.mainTextureScale = new Vector2(World.Current.Width, World.Current.Height);
            Texture2D tex = new Texture2D(World.Current.Width, World.Current.Height);
            Color[] colors = tex.GetPixels();
            for (int y = 0; y < World.Current.Height; y++) {
                for (int x = 0; x < World.Current.Width; x++) {
                    //float b = ((((float)x + (float)y) / ((float)World.Current.Width + (float)World.Current.Height)));
                    Tile t = World.Current.GetTileAt(x, y);
                    if (t == null) {
                        continue;
                    }
                    colors[x + y * World.Current.Width] = new Color(1, 1, 1, t.Moisture);
                }
            }
            foreach (Tile t in World.Current.Tiles) {
                ChangeEditorTile(t);
                //if (t != null)
                //    colors[t.X + t.Y * World.Current.Width] = new Color(1, 1, 1, 0);
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
            trr.material.SetVector("_Size", new Vector2(World.Current.Width, World.Current.Height));

            World.Current.RegisterTileChanged(ChangeEditorTile);
        }

        public void Update() {
            foreach (var item in _cityToMaskTexture.Values) {
                item.CheckApply();
            }

            if (!_apply) return;
            if (_islandToCustomMask == null) return;
            foreach (SpriteMask sm in _islandToCustomMask.Values) {
                sm.sprite?.texture.Apply();
                _apply = false;
            }
        }

        private static void CreateBaseTiles() {
            _nameToBaseTile = new Dictionary<string, TileBase>();
            foreach (string name in _nameToSprite.Keys) {
                if (name.Contains("animated")) {
                    //AnimatedTile tileBase = ScriptableObject.CreateInstance<AnimatedTile>();
                    //tileBase.m_AnimatedSprites = tests.ToArray();
                    //tileBase.m_MinSpeed = 0.05f;
                    //tileBase.m_MaxSpeed = 0.05f;
                    //nameToBaseTile[name] = tileBase;
                }
                else {
                    UnityEngine.Tilemaps.Tile tileBase = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                    tileBase.sprite = _nameToSprite[name];
                    tileBase.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
                    _nameToBaseTile[name] = tileBase;
                }
            }
        }

        public static void CreateIslandSprites(List<MapGenerator.IslandData> islands) {
            if (EditorController.IsEditor)
                return;
            CreationStarted = true;
            _numberOfIslands = islands.Count;
            LoadSprites();
            _islandSpriteStopWatch = new System.Diagnostics.Stopwatch();
            _islandSpriteStopWatch.Start();
            _islandPosToTilemap = new Dictionary<Vector2, GameObject>();
            _islandToMaskTexture = new Dictionary<Vector2, Texture2D>();
            CreateBaseTiles();

            foreach (MapGenerator.IslandData i in islands) {
                int islandWidth = (i.Width + 1);
                int islandHeight = (i.Height + 1);
                int xTileOffset = i.X;
                int yTileOffset = i.Y;

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
                Texture2D masktexture = new Texture2D(islandWidth, islandHeight, TextureFormat.Alpha8, false, true);
                masktexture.SetPixels32(new Color32[(islandWidth) * (islandHeight)]);
                masktexture.filterMode = FilterMode.Point;
                foreach (Tile tile_data in i.Tiles) {
                    if (tile_data.Type == TileType.Ocean)
                        continue;
                    //TILEMAP
                    int x = (int)(tile_data.X - xTileOffset);
                    int y = (int)(tile_data.Y - yTileOffset);
                    if (tile_data.SpriteName == null || _nameToBaseTile.ContainsKey(tile_data.SpriteName) == false) {
                        Debug.Log("Missing " + tile_data.Type + " tilesprite " + tile_data.SpriteName);
                    }
                    string temp = _nameToBaseTile.ContainsKey(tile_data.SpriteName) ? tile_data.SpriteName : NoSpriteName;
                    tilemap.SetTile(new Vector3Int(x, y, 0), _nameToBaseTile[temp]);

                    //MASK TEXTURE
                    masktexture.SetPixel(x, y, new Color32(128, 128, 128, 128));
                }
                tilemap.RefreshAllTiles();
                _islandPosToTilemap[i.GetPosition()] = island_tilemap;
                DontDestroyOnLoad(island_tilemap);

                masktexture.Apply();
                _islandToMaskTexture.Add(i.GetPosition(), masktexture);
                _createdIslands++;
            }

            _islandSpriteStopWatch.Stop();
            Debug.Log("Island Visuals " + _islandSpriteStopWatch.ElapsedMilliseconds + "ms (" + _islandSpriteStopWatch.Elapsed.TotalSeconds + "s)! ");
        }

        private Sprite GetSpriteForName(string spriteName) {
            return _nameToSprite.ContainsKey(spriteName) ? _nameToSprite[spriteName] : _nameToSprite[NoSpriteName];
        }

        private void OnTileChanged(Tile tile_data) {
            if (_darkLayer == null) {
                return;
            }
            int x = (int)(tile_data.X - tile_data.Island.Placement.x);
            int y = (int)(tile_data.Y - tile_data.Island.Placement.y);
            _cityToMaskTexture[tile_data.City].SetPixel(x, y, new Color32(128, 128, 128, 255));
            foreach (var item in tile_data.Island.Cities.Where(item => item != tile_data.City)) {
                _cityToMaskTexture[item].SetPixel(x, y, new Color32(128, 128, 128, 0));
            }

            if (TileDeciderFunc == null || _islandToCustomMask == null) return;
            _apply = true;
            TileMark tm = TileDeciderFunc(tile_data);
            switch (tm) {
                case TileMark.None:
                    _islandToCustomMask[tile_data.Island].sprite.texture.SetPixel(x, y, new Color32(128, 128, 128, 255));
                    break;

                case TileMark.Highlight:
                    break;

                case TileMark.Dark:
                    _islandToCustomMask[tile_data.Island].sprite.texture.SetPixel(x, y, new Color32(128, 128, 128, 0));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ChangeEditorTile(Tile tile_data) {
            if (EditorController.IsEditor == false) {
                return;
            }
            if (tile_data == null)
                return;
            if (string.IsNullOrEmpty(tile_data.SpriteName)) {
                _editorTileMap.SetTile(new Vector3Int(tile_data.X, tile_data.Y, 0), null);
            }
            else {
                string temp = _nameToBaseTile.ContainsKey(tile_data.SpriteName) ? tile_data.SpriteName : NoSpriteName;
                if (_nameToBaseTile.ContainsKey(tile_data.SpriteName) == false)
                    Debug.Log(tile_data.SpriteName);
                _editorTileMap.SetTile(new Vector3Int(tile_data.X, tile_data.Y, 0), _nameToBaseTile[temp]);
            }
        }

        public static void LoadSprites() {
            _nameToSprite = new Dictionary<string, Sprite>();
            Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/TileSprites/");
            foreach (Sprite s in sprites) {
                _nameToSprite.Add(s.name, s);
            }
            Sprite[] custom = ModLoader.LoadSprites(SpriteType.Tile);
            if (custom != null) {
                foreach (Sprite s in custom) {
                    _nameToSprite[s.name] = s;
                }
            }
            _climateTileSprites = new Dictionary<Climate, ClimateSprites>();
            foreach (Climate c in Enum.GetValues(typeof(Climate)))
                _climateTileSprites[c] = new ClimateSprites(c);
            foreach (string s in TileSpriteController._nameToSprite.Keys) {
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
                        _climateTileSprites[c].AddSprite(type, connections, s);
                }
                else {
                    try {
                        Climate climate = (Climate)Enum.Parse(typeof(Climate), parts[0], true);
                        _climateTileSprites[climate].AddSprite(type, connections, s);
                    }
                    catch {
                        //NO correct Climate so skip -- TODO: uncomment -- for now to much spam
                        //Debug.LogError("Tile Sprite in correct formatted Climate - either all or on of the enums (cold,middle,warm)");
                    }
                }
            }
        }

        public void OnDestroy() {
            World.Current.UnregisterTileChanged(OnTileChanged);
            _createdIslands = 0;
            Instance = null;
        }

        public void AddDecider(TileDecider addDeciderFunc, bool isCityDecider = false) {
            if (isCityDecider && MouseController.Instance.SelectedStructure != null)
                return;
            if (TileDeciderFunc != null && TileDeciderFunc.GetInvocationList().Contains(addDeciderFunc))
                return;
            if (isCityDecider) {
                _islandToCityMask.ToList().ForEach(x => x.Value.gameObject.SetActive(true));
            }
            else {
                this.TileDeciderFunc += addDeciderFunc;
                foreach (Island i in World.Current.Islands) {
                    //TODO: call this again or just ontilechanged when the corresponding decider needs updating
                    i.Tiles.ForEach(x => OnTileChanged(x));
                }
            }
            _darkLayer.SetActive(true);
        }

        public void ResetDecider() {
            TileDeciderFunc = null;
            _darkLayer.SetActive(false);
        }

        public void RemoveDecider(TileDecider removeFunc, bool isCityDecider = false) {
            if (EditorController.IsEditor)
                return;
            TileDeciderFunc -= removeFunc;
            if (TileDeciderFunc == null || TileDeciderFunc.GetInvocationList().Length == 0)
                ResetDecider();

            if (isCityDecider) {
                _islandToCityMask.ToList().ForEach(x => x.Value.gameObject.SetActive(false));
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
            if (_climateTileSprites == null)
                LoadSprites();
            if (type == TileType.Ocean)
                return null;
            return _climateTileSprites[climate].typeTree.ContainsKey(type) == false ? null : _climateTileSprites[climate].typeTree[type].GetClosest(spriteAddon);
        }
        public static List<string> GetAllSpriteNamesForType(TileType type, Climate climate) {
            if (_climateTileSprites == null)
                LoadSprites();
            if (type == TileType.Ocean)
                return null;
            if (_climateTileSprites[climate].typeTree.ContainsKey(type) == false)
                return null;
            return _climateTileSprites[climate].typeTree[type].GetAll();
        }
        private class ClimateSprites {
            public Climate Climate;
            public readonly Dictionary<TileType, TileTypeSprites> tileTypeSprites = new Dictionary<TileType, TileTypeSprites>();
            public readonly Dictionary<TileType, ClosestTree> typeTree = new Dictionary<TileType, ClosestTree>();

            public ClimateSprites(Climate climate) {
                this.Climate = climate;
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
                return tileTypeSprites.ContainsKey(type) == false ? new List<string>() : tileTypeSprites[type].GetSprites(spriteAddon);
            }
        }

        private class TileTypeSprites {
            public readonly TileType type;
            public readonly Dictionary<string, List<string>> connectionToSprite = new Dictionary<string, List<string>>();

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
                if (connectionToSprite.ContainsKey(spriteAddon)) 
                    return connectionToSprite[spriteAddon];
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
                return connectionToSprite.ContainsKey(newAddon) == false ? new List<string>() { NoSpriteName } : connectionToSprite[newAddon];
            }
        }
        class CityMaskTexture {
            public ICity city;
            public Texture2D texture;
            private bool _apply;
            public void CheckApply() {
                if (_apply == false) return;
                _apply = false;
                texture.Apply();
            }
            internal void SetPixel(int x, int y, Color32 color32) {
                texture.SetPixel(x, y, color32);
                _apply = true;
            }
        }

        public static System.Collections.Concurrent.ConcurrentBag<Vector2> positions = new System.Collections.Concurrent.ConcurrentBag<Vector2>();
        public static System.Collections.Concurrent.ConcurrentBag<Vector2> positions2 = new System.Collections.Concurrent.ConcurrentBag<Vector2>();
        public static System.Collections.Concurrent.ConcurrentBag<Vector2> positions3 = new System.Collections.Concurrent.ConcurrentBag<Vector2>();

        public static System.Collections.Concurrent.ConcurrentDictionary<Vector2, float> positionsCost = new System.Collections.Concurrent.ConcurrentDictionary<Vector2, float>();
#if UNITY_EDITOR
        public void OnDrawGizmos() {
            if (Application.isPlaying == false) return;

            //foreach (var n in World.Current.WorldGraph.Nodes) {
            //    if (n == null)
            //        continue;
            //    foreach (var e in n.Edges) {
            //        Gizmos.color = Color.white;
            //        Gizmos.DrawLine(new Vector2(n.x + 0.5f, n.y + 0.5f), new Vector2(e.Node.x + 0.5f, e.Node.y + 0.5f));
            //    }
            //}
            foreach (var t in positions) {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(new Vector3(t.x + 0.5f, t.y + 0.5f), 0.5f);
            }
            foreach (var t in positions2) {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(new Vector3(t.x + 0.5f, t.y + 0.5f), 0.5f);
            }
            foreach (var t in positions3) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(new Vector3(t.x + 0.5f, t.y + 0.5f), 0.5f);
            }
            //var posses = positions.ToArray();
            //for (int i = 0; i < posses.Length - 1; i++) {
            //    Gizmos.DrawLine(new Vector2(posses[i].x + 0.5f, posses[i].y + 0.5f), new Vector2(posses[i+1].x + 0.5f, posses[i + 1].y + 0.5f));
            //}
            //foreach (var t in positionsCost) {
            //    UnityEditor.Handles.Label(new Vector3(t.Key.x + 0.5f, t.Key.y + 0.5f), t.Value +"");
            //}
        }
# endif
    }
}