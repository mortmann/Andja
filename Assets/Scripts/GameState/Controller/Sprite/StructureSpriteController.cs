using Andja.Editor;
using Andja.FogOfWar;
using Andja.Model;
using Andja.Model.Components;
using Andja.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Controller {

    public class StructureSpriteController : MonoBehaviour {
        private const string NoSpriteName = "nosprite";
        public static StructureSpriteController Instance { get; protected set; }
        private Dictionary<Structure, GameObject> _structureGameObjectMap;
        private Dictionary<Structure, GameObject> _structureExtraUiMap;
        private Dictionary<Route, TextMesh> _routeToTextMesh;
        private static readonly string EffectFilePath = "Textures/Effects/Structures/";
        public static Dictionary<string, Sprite> StructureSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, StructureSprite> StructureToVariants = new Dictionary<string, StructureSprite>();
        public Sprite circleSprite;
        public Sprite upgradeSprite;
        public Sprite unitCircleSprite;
        public Dictionary<string, EffectSprite> effectToSprite;
        public bool RoadDebug = false;
        public Material ShadowMaterial;
        private void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two StructureSpriteController.");
            }
            Instance = this;
            LoadEffectSprites();
            BuildController.Instance.RegisterStructureCreated(OnBuildStrucutureCreated);
        }

        private void Start() {
            _structureGameObjectMap = new Dictionary<Structure, GameObject>();
            _structureExtraUiMap = new Dictionary<Structure, GameObject>();
            if (BuildController.Instance.LoadedStructures != null) {
                foreach (Structure str in BuildController.Instance.BuildIdToStructure.Values) {
                    OnBuildStrucutureCreated(str, true);
                }
            }

            if (EditorController.IsEditor) {
                EditorController.Instance.RegisterOnStructureDestroyed(OnTileStructureDestroyed);
            }
        }

        private void Update() {
            //no destroying gameobjects...
            if (FogOfWarController.IsFogOfWarAlways)
                return;
            List<Structure> ts = new List<Structure>(_structureGameObjectMap.Keys);
            HashSet<Structure> inView = new HashSet<Structure>(CameraController.Instance.structureCurrentInCameraView);
            foreach (Structure str in ts) {
                if (inView.Contains(str) == false) {
                    if (str.HasHitbox)
                        // TODO: check performance impact 
                        //-- if we need to remove those aswell 
                        //-- cant do it if fogAlways it needs them for visible detection
                        continue; 
                    Destroy(_structureGameObjectMap[str]);
                    _structureGameObjectMap.Remove(str);
                }
                else {
                    inView.Remove(str); // already exist as a gameobject so no need to check to create it
                }
            }
            //inView should only contain structures that dont exist as gameobject
            foreach (Structure str in inView) {
                if (str.HasHitbox == false) { //only structures without hitbox need to dynamically be created
                    CreateStructureGameObject(str);
                }
            }
        }

        public void OnBuildStrucutureCreated(Structure structure, bool onLoad) {
            if (FogOfWarController.IsFogOfWarAlways == false && structure.HasHitbox == false)
                return;
            CreateStructureGameObject(structure);
        }

        public void CreateStructureGameObject(Structure structure) {
            GameObject go = new GameObject();
            go.layer = LayerMask.NameToLayer("Structure");
            structure.RegisterOnChangedCallback(OnStructureChanged);
            structure.RegisterOnDestroyCallback(OnStructureDestroyed);
            structure.RegisterOnExtraUICallback(OnStructureExtraUI);
            structure.RegisterOnEffectChangedCallback(OnStructureEffectChange);
            float x = ((float)structure.TileWidth) / 2f - TileSpriteController.offset;
            float y = ((float)structure.TileHeight) / 2f - TileSpriteController.offset;
            Tile t = structure.BuildTile;
            go.transform.position = new Vector3(t.X + x, t.Y + y,-1);
            go.transform.transform.eulerAngles = new Vector3(0, 0, 360 - structure.Rotation);
            go.transform.SetParent(this.transform, true);
            go.name = structure.SmallName + "_" + structure.Tiles[0].ToString();
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.material = ShadowMaterial;
            sr.sortingLayerName = structure.SortingLayer;
            _structureGameObjectMap.Add(structure, go);
            if (structure is RoadStructure roadStructure) {
                roadStructure.RegisterOnRoadCallback(OnRoadChange);
                //sr.sortingLayerName = "Road";
                if (RoadDebug) {
                    AddRoadDebug(go, roadStructure);
                }
            }

            SetSpriteRendererStructureSprite(go, structure);

            if (structure is OutputStructure outputStructure && outputStructure.ContactRange > 0) {
                GameObject goContact = new GameObject();
                CircleCollider2D cc2d = goContact.AddComponent<CircleCollider2D>();
                cc2d.radius = outputStructure.ContactRange
                                    + (outputStructure.Width+ outputStructure.Height) / 2;
                cc2d.isTrigger = true;
                goContact.transform.SetParent(go.transform);
                goContact.transform.localPosition = Vector3.zero;
                ContactColliderScript c = goContact.AddComponent<ContactColliderScript>();
                c.contact = outputStructure;
                goContact.name = "ContactCollider";
            }
            if (structure.Effects != null) {
                foreach (Effect e in structure.Effects) {
                    OnStructureEffectChange(structure, e, true);
                }
            }
            if(GameData.FogOfWarStyle == FogOfWarStyle.Always) {
                FogOfWarController.Instance.AddStructureFogModule(go, structure);
            } else {
                if (structure.HasHitbox || 
                     structure is GrowableStructure == false && structure is RoadStructure == false) {
                    BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                    col.isTrigger = structure.HasHitbox == false &&
                                    structure is GrowableStructure == false &&
                                    structure is RoadStructure == false;
                    col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit, sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
                }
            }
            //SOUND PART -- IMPORTANT
            SoundController.Instance?.OnStructureGOCreated(structure, go);
        }

        private void AddRoadDebug(GameObject go, RoadStructure road) {
            if (_routeToTextMesh == null)
                _routeToTextMesh = new Dictionary<Route, TextMesh>();
            if (_routeToTextMesh.ContainsKey(road.Route))
                return;
            GameObject gos = new GameObject();
            TextMesh text = gos.AddComponent<TextMesh>();
            text.characterSize = 0.1f;
            text.anchor = TextAnchor.MiddleCenter;
            gos.transform.SetParent(go.transform);
            gos.transform.localPosition = Vector3.zero;
            gos.GetComponent<MeshRenderer>().sortingLayerName = "StructuresUI";
            Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            text.font = ArialFont;
            _routeToTextMesh[road.Route] = text;
            text.text = road.Route.ToString();
        }

        private void OnStructureEffectChange(IGEventable target, Effect effect, bool added) {
            if (string.IsNullOrEmpty(effect.OnMapSpriteName))
                return;
            if (!(target is Structure structure) || _structureGameObjectMap.ContainsKey(structure) == false)
                return;
            GameObject strgo = _structureGameObjectMap[structure];
            if (added == false) {
                EffectAnimator[] effectAnimators = strgo.GetComponentsInChildren<EffectAnimator>();
                if (effectAnimators == null || effectAnimators.Length == 0)
                    return;
                EffectAnimator removeEffect = Array.Find(effectAnimators, x => x.effect.ID == effect.ID);
                if (removeEffect == null)
                    return;
                Destroy(removeEffect.gameObject);
            }
            else {
                GameObject effectGO = new GameObject();
                effectGO.transform.SetParent(strgo.transform);
                effectGO.transform.localPosition = new Vector3(0, 0, 0);
                EffectAnimator ea = effectGO.AddComponent<EffectAnimator>();
                if (effectToSprite.ContainsKey(effect.ID))
                    ea.Show(effectToSprite[effect.ID].Get(target.GetID()), "Structures", effect, strgo.GetComponent<SpriteRenderer>());
            }
        }

        public void OnStructureChanged(Structure structure) {
            if (structure == null) {
                Debug.LogError("Structure change and its empty?");
                return;
            }
            if (_structureGameObjectMap.ContainsKey(structure) == false) {
                return;
            }
            if(FogOfWarController.IsFogOfWarAlways &&
                FogOfWarStructure.IsStructureVisible(_structureGameObjectMap[structure]) == false) {
                return;
            }
            SetSpriteRendererStructureSprite(_structureGameObjectMap[structure], structure);
        }

        public void OnStructureExtraUI(Structure structure, bool show) {
            if (show) {
                if (_structureExtraUiMap.ContainsKey(structure))
                    return;
                GameObject extraUI = null;
                switch (structure.ExtraUITyp) {
                    case ExtraUI.None:
                        return;

                    case ExtraUI.Range:
                        extraUI = CreateRange(structure);
                        break;

                    case ExtraUI.Efficiency:
                        extraUI = Instantiate(Resources.Load<GameObject>("Prefabs/GamePrefab/SpriteSlider"));
                        break;

                    case ExtraUI.Upgrade:
                        extraUI = CreateUpgrade(structure);
                        break;
                }
                if (extraUI == null)
                    Debug.LogError("No Extra UI to Show was created for type " + structure.ExtraUITyp);
                _structureExtraUiMap.Add(structure, extraUI);
            }
            else {
                //Not showing it anymore so delete it
                if (_structureExtraUiMap.ContainsKey(structure) == false) return;
                Destroy(_structureExtraUiMap[structure]);
                _structureExtraUiMap.Remove(structure);
            }
        }

        private GameObject CreateUpgrade(Structure structure) {
            GameObject go = new GameObject {
                name = "UpgradeUI",
                transform = {
                    position = _structureGameObjectMap[structure].transform.position,
                    localScale = new Vector3(1, 1, 0)
                }
            };
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = upgradeSprite;
            sr.sortingLayerName = "StructuresUI";
            go.transform.SetParent(_structureGameObjectMap[structure].transform);
            return go;
        }

        private GameObject CreateRange(Structure structure) {
            GameObject go = new GameObject {
                name = "RangeUI",
                transform = {
                    position = _structureGameObjectMap[structure].transform.position,
                    localScale = new Vector3(
                        ((OutputStructure)structure).ContactRange * 2,
                        ((OutputStructure)structure).ContactRange * 2,
                        0)
                }
            };
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circleSprite;
            sr.sortingLayerName = "StructuresUI";
            go.transform.SetParent(_structureGameObjectMap[structure].transform, true);
            return go;
        }

        private void SetSpriteRendererStructureSprite(GameObject go, Structure str) {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (StructureSprites.ContainsKey(str.GetSpriteName())) {
                sr.sprite = StructureSprites[str.GetSpriteName()];
            }
            else {
                Debug.LogWarning("Missing Structure Sprite " + str.GetSpriteName());
                sr.sprite = StructureSprites[NoSpriteName];
                go.transform.localScale = new Vector3(str.TileWidth, str.TileHeight, 1);
                go.transform.localRotation = Quaternion.identity;
            }
        }
        public static string GetRandomVariant(string id, string climate) {
            return StructureToVariants.ContainsKey(id) ? "_" + StructureToVariants[id].GetRandomVariant(climate) : "";
        }
        public Sprite GetSprite(string name) {
            return StructureSprites.ContainsKey(name) ? StructureSprites[name] : StructureSprites[NoSpriteName];
        }

        private void OnTileStructureDestroyed(Tile t) {
            OnStructureDestroyed(t.Structure, null);
        }

        private void OnStructureDestroyed(Structure structure, IWarfare destroyer) {
            if (structure == null)
                return;
            if (_structureGameObjectMap.ContainsKey(structure) == false) {
                return;
            }
            GameObject go = _structureGameObjectMap[structure];
            structure.UnregisterOnChangedCallback(OnStructureChanged);
            structure.UnregisterOnDestroyCallback(OnStructureDestroyed);
            structure.UnregisterOnExtraUICallback(OnStructureExtraUI); 
            _structureGameObjectMap.Remove(structure);
            //SOUND PART -- IMPORTANT
            SoundController.Instance.OnStructureGODestroyed(structure, go);
            if (FogOfWarStructure.IsStructureVisible(go) == false)
                return;
            Destroy(go);
        }

        public void OnRoadChange(RoadStructure road) {
            Structure s = road;
            SetSpriteRendererStructureSprite(_structureGameObjectMap[s], s);
            if (!RoadDebug || road.Route == null) return;
            AddRoadDebug(_structureGameObjectMap[s], road);
            List<Route> temp = new List<Route>(_routeToTextMesh.Keys);
            foreach (var r in temp.Where(r => r.Tiles.Count == 0)) {
                Destroy(_routeToTextMesh[r]);
                _routeToTextMesh.Remove(r);
            }
        }
        private void LoadEffectSprites() {
            effectToSprite = new Dictionary<string, EffectSprite>();
            Sprite[] sprites = Resources.LoadAll<Sprite>(EffectFilePath);
            Sprite[] custom = ModLoader.LoadSprites(SpriteType.Structure);
            List<Sprite> all = new List<Sprite>(sprites);
            if (custom != null)
                all.AddRange(custom);
            foreach (Sprite s in all) {
                string[] names = s.name.Split('_');
                if(effectToSprite.ContainsKey(names[0]) == false) {
                    effectToSprite[names[0]] = new EffectSprite();
                }
                string first = null;
                string second = null;
                if(names.Length >= 3) {
                    second = names[2];
                    first = names[1];
                }
                int.TryParse(second ?? first, out int num);
                effectToSprite[names[0]].Add(s, first, num);
            }
        }
        public static void LoadSprites() {
            StructureSprites = new Dictionary<string, Sprite>();
            Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Structures/");
            foreach (Sprite s in sprites) {
                StructureSprites[s.name] = s;
            }
            Sprite[] custom = ModLoader.LoadSprites(SpriteType.Structure);
            if (custom == null)
                return;
            foreach (Sprite s in custom) {
                StructureSprites[s.name] = s;
            }
            foreach (string name in StructureSprites.Keys) {
                string[] splits = name.Split('_');
                string id = splits[0];
                if (splits.Length < 3)
                    continue;
                if (StructureToVariants.ContainsKey(id) == false)
                    StructureToVariants[id] = new StructureSprite();
                StructureToVariants[id].climateToVariants ??= new Dictionary<string, List<string>>();
                if (StructureToVariants[id].climateToVariants.ContainsKey(splits[1]) == false) {
                    StructureToVariants[id].climateToVariants[splits[1]] = new List<string>();
                }
                StructureToVariants[id].climateToVariants[splits[1]].Add(splits[2]);
            }
        }

        public Sprite GetStructureSprite(Structure str) {
            if (StructureSprites.ContainsKey(str.GetSpriteName()) == false) {
                //FIXME this should be active in future
                //fornow there arent many sprites anyway
                //			Debug.LogError ("No Structure Sprite for that Name!");
                return null;
            }
            return GetSprite(str.GetSpriteName());
        }

        public Sprite GetStructureSprite(string sprite) {
            return GetSprite(sprite);
        }

        public GameObject GetGameObject(Structure str) {
            return _structureGameObjectMap.ContainsKey(str) == false ? null : _structureGameObjectMap[str];
        }

        public void OnDestroy() {
            Instance = null;
        }
        public class StructureSprite {
            public Sprite Default;
            public Dictionary<string, List<string>> climateToVariants = new Dictionary<string, List<string>>();

            internal string GetRandomVariant(string climate) {
                return climateToVariants[climate][UnityEngine.Random.Range(0, climateToVariants[climate].Count)]; 
            }
        }
        public class EffectSprite {
            public Dictionary<string, List<Sprite>> structureToSprites = new Dictionary<string, List<Sprite>>();
            public List<Sprite> Default = new List<Sprite>();

            internal void Add(Sprite sprite, string name, int num) {
                if(string.IsNullOrEmpty(name)) {
                    if (num < Default.Count - 1) {
                        Default.Insert(num, sprite);
                    }
                    else {
                        Default.Add(sprite);
                    }
                    return;
                } 
                if (structureToSprites.ContainsKey(name) == false)
                    structureToSprites[name] = new List<Sprite>(); 
                List<Sprite> s = structureToSprites[name];
                if (num < s.Count - 1) {
                    s.Insert(num, sprite);
                }
                else {
                    s.Add(sprite);
                }
            }

            internal Sprite[] Get(string name) {
                return structureToSprites.ContainsKey(name) == false ? Default?.ToArray() : structureToSprites[name].ToArray();
            }
        } 
    }
}