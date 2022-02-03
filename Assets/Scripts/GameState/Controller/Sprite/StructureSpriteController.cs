using Andja.Editor;
using Andja.FogOfWar;
using Andja.Model;
using Andja.Model.Components;
using Andja.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Controller {

    public class StructureSpriteController : MonoBehaviour {
        public static StructureSpriteController Instance { get; protected set; }
        public Dictionary<Structure, GameObject> structureGameObjectMap;
        public Dictionary<Structure, GameObject> structureExtraUIMap;
        public readonly static string EffectFilePath = "Textures/Effects/Structures/";
        public static Dictionary<string, Sprite> structureSprites = new Dictionary<string, Sprite>();
        public static Dictionary<string, StructureSprite> structureToVariants = new Dictionary<string, StructureSprite>();
        public Sprite circleSprite;
        public Sprite upgradeSprite;
        public Sprite unitCircleSprite;
        public Dictionary<string, EffectSprite> effectToSprite;
        private Dictionary<Route, TextMesh> RouteToTextMesh;
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
            structureGameObjectMap = new Dictionary<Structure, GameObject>();
            structureExtraUIMap = new Dictionary<Structure, GameObject>();
            if (BuildController.Instance.LoadedStructures != null) {
                foreach (Structure str in BuildController.Instance.buildIdToStructure.Values) {
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
            List<Structure> ts = new List<Structure>(structureGameObjectMap.Keys);
            HashSet<Structure> inView = new HashSet<Structure>(CameraController.Instance.structureCurrentInCameraView);
            foreach (Structure str in ts) {
                if (inView.Contains(str) == false) {
                    if (str.HasHitbox)
                        // TODO: check performance impact 
                        //-- if we need to remove those aswell 
                        //-- cant do it if fogAlways it needs them for visible detection
                        continue; 
                    Destroy(structureGameObjectMap[str]);
                    structureGameObjectMap.Remove(str);
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
            go.transform.transform.eulerAngles = new Vector3(0, 0, 360 - structure.rotation);
            go.transform.SetParent(this.transform, true);
            go.name = structure.SmallName + "_" + structure.Tiles[0].ToString();
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.material = ShadowMaterial;
            sr.sortingLayerName = structure.SortingLayer;
            structureGameObjectMap.Add(structure, go);
            if (structure is RoadStructure) {
                ((RoadStructure)structure).RegisterOnRoadCallback(OnRoadChange);
                //sr.sortingLayerName = "Road";
                if (RoadDebug) {
                    AddRoadDebug(go, ((RoadStructure)structure));
                }
            }

            SetSpriteRendererStructureSprite(go, structure);

            if (structure is OutputStructure && ((OutputStructure)structure).ContactRange > 0) {
                GameObject goContact = new GameObject();
                CircleCollider2D cc2d = goContact.AddComponent<CircleCollider2D>();
                cc2d.radius = ((OutputStructure)structure).ContactRange
                                    + (((OutputStructure)structure).Width+ ((OutputStructure)structure).Height) / 2;
                cc2d.isTrigger = true;
                goContact.transform.SetParent(go.transform);
                goContact.transform.localPosition = Vector3.zero;
                ContactColliderScript c = goContact.AddComponent<ContactColliderScript>();
                c.contact = ((OutputStructure)structure);
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
            if (RouteToTextMesh == null)
                RouteToTextMesh = new Dictionary<Route, TextMesh>();
            if (RouteToTextMesh.ContainsKey(road.Route))
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
            RouteToTextMesh[road.Route] = text;
            text.text = road.Route.ToString();
        }

        private void OnStructureEffectChange(IGEventable target, Effect effect, bool added) {
            if (effect.OnMapSpriteName == null || effect.OnMapSpriteName.Length == 0)
                return;
            Structure structure = target as Structure;
            if (structure == null || structureGameObjectMap.ContainsKey(structure) == false)
                return;
            GameObject strgo = structureGameObjectMap[structure];
            if (added == false) {
                EffectAnimator[] effectsanimators = strgo.GetComponentsInChildren<EffectAnimator>();
                if (effectsanimators == null || effectsanimators.Length == 0)
                    return;
                EffectAnimator removeEffect = Array.Find(effectsanimators, x => x.effect.ID == effect.ID);
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
            if (structureGameObjectMap.ContainsKey(structure) == false) {
                return;
            }
            if(FogOfWarController.IsFogOfWarAlways &&
                FogOfWarStructure.IsStructureVisible(structureGameObjectMap[structure]) == false) {
                return;
            }
            SetSpriteRendererStructureSprite(structureGameObjectMap[structure], structure);
        }

        public void OnStructureExtraUI(Structure structure, bool show) {
            if (show) {
                if (structureExtraUIMap.ContainsKey(structure))
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
                structureExtraUIMap.Add(structure, extraUI);
            }
            else {
                //Not showing it anymore so delete it
                if (structureExtraUIMap.ContainsKey(structure)) {
                    Destroy(structureExtraUIMap[structure]);
                    structureExtraUIMap.Remove(structure);
                }
            }
        }

        private GameObject CreateUpgrade(Structure structure) {
            GameObject go = new GameObject {
                name = "UpgradeUI"
            };
            go.transform.position = structureGameObjectMap[structure].transform.position;
            go.transform.localScale = new Vector3(1, 1, 0);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = upgradeSprite;
            sr.sortingLayerName = "StructuresUI";
            go.transform.SetParent(structureGameObjectMap[structure].transform);
            return go;
        }

        private GameObject CreateRange(Structure structure) {
            GameObject go = new GameObject {
                name = "RangeUI"
            };
            go.transform.position = structureGameObjectMap[structure].transform.position;
            go.transform.localScale = new Vector3(
                ((OutputStructure)structure).ContactRange * 2,
                ((OutputStructure)structure).ContactRange * 2, 0);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circleSprite;
            sr.sortingLayerName = "StructuresUI";
            go.transform.SetParent(structureGameObjectMap[structure].transform, true);
            return go;
        }

        private void SetSpriteRendererStructureSprite(GameObject go, Structure str) {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (structureSprites.ContainsKey(str.GetSpriteName())) {
                sr.sprite = structureSprites[str.GetSpriteName()];
            }
            else {
                Debug.LogWarning("Missing Structure Sprite " + str.GetSpriteName());
                sr.sprite = structureSprites["nosprite"];
                go.transform.localScale = new Vector3(str.TileWidth, str.TileHeight, 1);
                go.transform.localRotation = Quaternion.identity;
            }
        }
        public static string GetRandomVariant(string id, string climate) {
            if(structureToVariants.ContainsKey(id))
                return structureToVariants[id].GetRandomVariant(climate);
            return "";
        }
        public Sprite GetSprite(string name) {
            if (structureSprites.ContainsKey(name)) {
                return structureSprites[name];
            }
            else {
                return structureSprites["nosprite"];
            }
        }

        private void OnTileStructureDestroyed(Tile t) {
            OnStructureDestroyed(t.Structure, null);
        }

        private void OnStructureDestroyed(Structure structure, IWarfare destroyer) {
            if (structure == null)
                return;
            if (structureGameObjectMap.ContainsKey(structure) == false) {
                return;
            }
            GameObject go = structureGameObjectMap[structure];
            structure.UnregisterOnChangedCallback(OnStructureChanged);
            structure.UnregisterOnDestroyCallback(OnStructureDestroyed);
            structure.UnregisterOnExtraUICallback(OnStructureExtraUI); 
            structureGameObjectMap.Remove(structure);
            //SOUND PART -- IMPORTANT
            SoundController.Instance.OnStructureGODestroyed(structure, go);
            if (FogOfWarStructure.IsStructureVisible(go) == false)
                return;
            Destroy(go);
        }

        public void OnRoadChange(RoadStructure road) {
            Structure s = road;
            SetSpriteRendererStructureSprite(structureGameObjectMap[s], s);
            if (RoadDebug && road.Route != null) {
                AddRoadDebug(structureGameObjectMap[s], road);
                //RouteToTextMesh[road.Route].text = road.Route.ToString();
                List<Route> temp = new List<Route>(RouteToTextMesh.Keys);
                foreach (Route r in temp) {
                    if (r.Tiles.Count == 0) {
                        Destroy(RouteToTextMesh[r]);
                        RouteToTextMesh.Remove(r);
                    }
                }
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
                string[] name = s.name.Split('_');
                if(effectToSprite.ContainsKey(name[0])==false) {
                    effectToSprite[name[0]] = new EffectSprite();
                }
                string first = null;
                string second = null;
                if(name.Length >= 3) {
                    second = name[2];
                    first = name[1];
                }
                int.TryParse(second ?? first, out int num);
                effectToSprite[name[0]].Add(s, first, num);
            }
        }
        public static void LoadSprites() {
            structureSprites = new Dictionary<string, Sprite>();
            Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Structures/");
            foreach (Sprite s in sprites) {
                structureSprites[s.name] = s;
            }
            Sprite[] custom = ModLoader.LoadSprites(SpriteType.Structure);
            if (custom == null)
                return;
            foreach (Sprite s in custom) {
                structureSprites[s.name] = s;
            }
            foreach (string name in structureSprites.Keys) {
                string[] splits = name.Split('_');
                string id = splits[0];
                if (splits.Length < 3)
                    continue;
                if (structureToVariants.ContainsKey(id) == false)
                    structureToVariants[id] = new StructureSprite();
                if (structureToVariants[id].climateToVariants == null)
                    structureToVariants[id].climateToVariants = new Dictionary<string, List<string>>();
                if (structureToVariants[id].climateToVariants.ContainsKey(splits[1]) == false) {
                    structureToVariants[id].climateToVariants[splits[1]] = new List<string>();
                }
                structureToVariants[id].climateToVariants[splits[1]].Add(splits[2]);
            }
        }

        public Sprite GetStructureSprite(Structure str) {
            if (structureSprites.ContainsKey(str.GetSpriteName()) == false) {
                //FIXME this should be active in future
                //fornow there arent many sprites anyway
                //			Debug.LogError ("No Structure Sprite for that Name!");
                return null;
            }
            return GetSprite(str.GetSpriteName());
        }

        public Sprite GetStructureSprite(String sprite) {
            return GetSprite(sprite);
        }

        public GameObject GetGameObject(Structure str) {
            if (structureGameObjectMap.ContainsKey(str) == false) {
                return null;
            }
            return structureGameObjectMap[str];
        }

        private void OnDestroy() {
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
                if (structureToSprites.ContainsKey(name) == false)
                    return Default?.ToArray();
                return structureToSprites[name].ToArray();
            }
        } 
    }
}