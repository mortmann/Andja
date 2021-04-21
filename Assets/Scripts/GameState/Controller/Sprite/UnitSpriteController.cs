using Andja.Model;
using Andja.Model.Components;
using Andja.Utility;
using DigitalRuby.AdvancedPolygonCollider;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Controller {

    public class UnitSpriteController : MonoBehaviour {
        public static UnitSpriteController Instance;

        private Dictionary<string, Sprite> unitSprites;
        public Dictionary<Unit, GameObject> unitGameObjectMap;
        public GameObject unitPathPrefab;
        public GameObject unitCirclePrefab;
        public Dictionary<Crate, GameObject> crateGameObjectMap;
        public Dictionary<Projectile, GameObject> projectileGameObjectMap;

        private Unit circleUnit;
        private const string circleGOname = "buildrange_circle_gameobject";
        private MouseController mouseController;
        public Material SpriteHighlightMaterial;

        private World World {
            get { return World.Current; }
        }

        private void Awake() {
            Instance = this;
        }

        // Use this for initialization
        private void Start() {
            Setup();
        }

        public void Setup() {
            unitGameObjectMap = new Dictionary<Unit, GameObject>();
            crateGameObjectMap = new Dictionary<Crate, GameObject>();
            projectileGameObjectMap = new Dictionary<Projectile, GameObject>();
            LoadSprites();
            World.RegisterUnitCreated(OnUnitCreated);
            World.RegisterCrateSpawned(OnCrateSpawned);
            World.RegisterCrateDespawned(OnCrateDespawned);

            foreach (var item in World.Units) {
                OnUnitCreated(item);
            }
            foreach (Crate c in World.Crates) {
                OnCrateSpawned(c);
            }
            foreach (Projectile pro in World.Projectiles) {
                OnProjectileCreated(pro);
            }
            mouseController = MouseController.Instance;
            World.RegisterOnCreateProjectileCallback(OnProjectileCreated);
            BuildController.Instance.RegisterBuildStateChange(OnBuildStateChange);
        }

        private void Update() {
        }

        public void OnUnitCreated(Unit unit) {
            // This creates a new GameObject and adds it to our scene.
            GameObject go = new GameObject();
            go.name = "U-" + unit.PlayerSetName;
            GameObject line_go = Instantiate(unitPathPrefab);
            line_go.transform.SetParent(go.transform);
            // Add our tile/GO pair to the dictionary.
            unitGameObjectMap.Add(unit, go);
            go.AddComponent<SpriteOutline>().enabled = false;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Units";
            sr.sprite = unitSprites[unit.Data.spriteBaseName];
            sr.material = SpriteHighlightMaterial;
            //go.transform.SetParent(this.transform, true);
            go.AddComponent<ITargetableHoldingScript>().Holding = unit;
            go.GetComponent<SpriteOutline>().PlayerNumber = unit.PlayerNumber;
            Rigidbody2D r2d = go.AddComponent<Rigidbody2D>();
            r2d.gravityScale = 0;
            AdvancedPolygonCollider apc = go.AddComponent<AdvancedPolygonCollider>();
            apc.AlphaTolerance = 20;
            apc.Scale = 1.15f;
            apc.DistanceThreshold = 2;
            StartCoroutine(UpdateHitbox(apc));
            //BoxCollider2D col = unit_go.AddComponent<BoxCollider2D>();
            //col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit,
            //                        sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);

            //u.width = sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit;
            //u.height = sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit;
            // Register our callback so that our GameObject gets updated whenever
            // the object's into changes.
            unit.RegisterOnChangedCallback(OnUnitChanged);
            unit.RegisterOnDestroyCallback(OnUnitDestroy);
            if (FogOfWarController.FogOfWarOn) {
                FogOfWarController.Instance.AddUnitFogModule(go, unit);
            }
            OnUnitChanged(unit);

            //SOUND PART -- IMPORTANT
            SoundController.Instance.OnUnitCreated(unit, go);
        }

        /// <summary>
        /// just so it finds the spriterenderer -- making a prefab would make this prob easier -- just setting sprite then?
        /// </summary>
        /// <param name="apc"></param>
        /// <returns></returns>
        private IEnumerator UpdateHitbox(AdvancedPolygonCollider apc) {
            yield return new WaitForSeconds(0.0001f);
            apc.RecalculatePolygon();
            yield return null;
        }

        private void OnProjectileCreated(Projectile projectile) {
            GameObject pro_go = new GameObject {
                name = "Projectile"
            };
            SpriteRenderer sr = pro_go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Units";
            sr.sprite = unitSprites["cannonball_1"];
            projectile.RegisterOnDestroyCallback(OnProjectileDestroy);
            if (projectile.HasHitbox) {
                BoxCollider2D col = pro_go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit,
                                        sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
            }
            pro_go.AddComponent<ProjectileHoldingScript>().Projectile = projectile;
        }

        private void OnProjectileDestroy(Projectile pro) {
            projectileGameObjectMap.Remove(pro);
            pro.UnregisterOnDestroyCallback(OnProjectileDestroy);
        }

        private void OnUnitChanged(Unit c) {
            if (unitGameObjectMap.ContainsKey(c) == false) {
                Debug.LogError("OnUnitChanged -- trying to change visuals for character not in our map.");
                return;
            }
            GameObject char_go = unitGameObjectMap[c];
            if (c is Ship) {
                if (((Ship)c).isOffWorld) {
                    char_go.SetActive(false);
                }
                else {
                    char_go.SetActive(true);
                }
                //change this so it does use the rigidbody to move
                //char_go.transform.position = new Vector3(c.X, c.Y, 0);
                //Quaternion q = char_go.transform.rotation;
                //q.eulerAngles = new Vector3(0, 0, c.Rotation);
                //char_go.transform.rotation = q;
            }
            else {
                char_go.transform.position = new Vector3(c.X, c.Y, 0);
                Quaternion q = char_go.transform.rotation;
                q.eulerAngles = new Vector3(0, 0, c.Rotation);
                char_go.transform.rotation = q;
            }
        }

        private void OnUnitDestroy(Unit c, IWarfare warfare) {
            if (unitGameObjectMap.ContainsKey(c) == false) {
                Debug.LogError("OnCharacterChanged -- trying to change visuals for character not in our map.");
                return;
            }
            GameObject char_go = unitGameObjectMap[c];
            Destroy(char_go);
            unitGameObjectMap.Remove(c);
            c.UnregisterOnChangedCallback(OnUnitChanged);
        }

        private void OnCrateSpawned(Crate c) {
            //TODO: create a prefab?
            GameObject go = new GameObject();
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = unitSprites["Crate"];
            go.AddComponent<CrateHoldingScript>().thisCrate = c;
            go.transform.SetParent(this.transform);
            go.name = "Crate";
            sr.sortingLayerName = "Units";
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            go.AddComponent<Rigidbody2D>().gravityScale = 0; //TODO: think about if this is good so!
            col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit, sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
            go.transform.position = c.position;
            crateGameObjectMap.Add(c, go);
        }

        private void OnCrateDespawned(Crate c) {
            Destroy(crateGameObjectMap[c]);
            crateGameObjectMap.Remove(c);
        }

        private void LoadSprites() {
            unitSprites = new Dictionary<string, Sprite>();
            Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Units/");
            foreach (Sprite s in sprites) {
                unitSprites[s.name] = s;
            }
            Sprite[] custom = ModLoader.LoadSprites(SpriteType.Unit);
            if (custom == null)
                return;
            foreach (Sprite s in custom) {
                unitSprites[s.name] = s;
            }
        }

        private void OnDestroy() {
            World.UnregisterUnitCreated(OnUnitCreated);
        }

        private void OnBuildStateChange(BuildStateModes bsm) {
            if (bsm != BuildStateModes.Build) {
                RemoveBuildCircle();
                return;
            }
            CreateBuildCircle();
        }

        private void RemoveBuildCircle() {
            if (circleUnit == null) {
                return; // can be because cheats
            }
            if (unitGameObjectMap.ContainsKey(circleUnit) == false) {
                return;//maybe it has been destroyed or other bug calls this function twice or cheats cause to call this without create
            }
            GameObject go = unitGameObjectMap[circleUnit].transform.Find(circleGOname).gameObject;
            Destroy(go);
        }

        private void CreateBuildCircle() {
            Unit u = mouseController.SelectedUnit;
            if (u == null) {
                return;
            }
            Transform parent = unitGameObjectMap[u].transform;
            GameObject go = Instantiate(unitCirclePrefab);
            go.name = circleGOname;
            //buildrange is radius
            go.transform.localScale = new Vector3(u.BuildRange, u.BuildRange);
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0, 0, -0.5f);
            circleUnit = u;
        }

        internal void Highlight(Unit[] units) {
            foreach (Unit unit in units) {
                if (unit == null) {
                    continue;
                }
                unitGameObjectMap[unit].GetComponent<SpriteOutline>().enabled = true;
            }
        }

        internal void Dehighlight(Unit[] units) {
            foreach (Unit unit in units) {
                unitGameObjectMap[unit].GetComponent<SpriteOutline>().enabled = false;
            }
        }
    }
}