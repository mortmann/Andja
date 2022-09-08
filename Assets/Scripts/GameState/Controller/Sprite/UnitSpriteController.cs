using Andja.Model;
using Andja.Model.Components;
using Andja.Utility;
using DigitalRuby.AdvancedPolygonCollider;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Controller {

    public class UnitSpriteController : MonoBehaviour {
        public static UnitSpriteController Instance;

        private Dictionary<string, Sprite> _unitSprites;
        public Dictionary<Unit, GameObject> unitGameObjectMap;
        public GameObject unitPathPrefab;
        public GameObject unitCirclePrefab;
        public Dictionary<Crate, GameObject> crateGameObjectMap;
        public Dictionary<Projectile, GameObject> projectileGameObjectMap;

        private Unit _circleUnit;
        private const string CircleGOName = "buildrange_circle_gameobject";
        public Material SpriteHighlightMaterial;

        public void Awake() {
            Instance = this;
        }

        // Use this for initialization
        public void Start() {
            Setup();
        }

        public void Setup() {
            unitGameObjectMap = new Dictionary<Unit, GameObject>();
            crateGameObjectMap = new Dictionary<Crate, GameObject>();
            projectileGameObjectMap = new Dictionary<Projectile, GameObject>();
            LoadSprites();
            World.Current.RegisterUnitCreated(OnUnitCreated);
            World.Current.RegisterCrateSpawned(OnCrateSpawned);
            World.Current.RegisterCrateDespawned(OnCrateDespawned);

            foreach (var item in World.Current.Units.Where(item => item.IsDead == false)) {
                OnUnitCreated(item);
            }
            foreach (Crate c in World.Current.Crates) {
                OnCrateSpawned(c);
            }
            foreach (Projectile pro in World.Current.Projectiles) {
                OnProjectileCreated(pro);
            }
            World.Current.RegisterOnCreateProjectileCallback(OnProjectileCreated);
            BuildController.Instance.RegisterBuildStateChange(OnBuildStateChange);
        }

        public void OnUnitCreated(Unit unit) {
            // This creates a new GameObject and adds it to our scene.
            GameObject go = new GameObject();
            go.layer = LayerMask.NameToLayer("Unit");
            go.name = unit.PlayerNumber + ":" + (unit.IsShip?"S":"U") + unit.PlayerSetName ?? unit.Name;
            GameObject lineGo = Instantiate(unitPathPrefab);
            lineGo.transform.SetParent(go.transform);
            // Add our tile/GO pair to the dictionary.
            unitGameObjectMap.Add(unit, go);
            go.AddComponent<SpriteOutline>().enabled = false;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Units";
            sr.sprite = _unitSprites[unit.Data.spriteBaseName];
            sr.material = SpriteHighlightMaterial;
            //go.transform.SetParent(this.transform, true);
            go.AddComponent<ITargetableHoldingScript>().Holding = unit;
            go.GetComponent<SpriteOutline>().PlayerNumber = unit.PlayerNumber;
            Rigidbody2D r2d = go.AddComponent<Rigidbody2D>();
            r2d.gravityScale = 0;
            AdvancedPolygonCollider apc = go.AddComponent<AdvancedPolygonCollider>();
            apc.AlphaTolerance = 128;
            apc.Scale = 1f;
            apc.DistanceThreshold = 4;
            StartCoroutine(UpdateHitbox(apc));
            //BoxCollider2D col = unit_go.AddComponent<BoxCollider2D>();
            //col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit,
            //                        sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);

            //u.width = sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit;
            //u.height = sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit;
            unit.RegisterOnChangedCallback(OnUnitChanged);
            unit.RegisterOnDestroyCallback(OnUnitDestroy);
            if (FogOfWarController.FogOfWarOn) {
                if (unit.IsOwnedByCurrentPlayer()) {
                    FogOfWarController.Instance.AddUnitFogModule(go, unit);
                }
                if (FogOfWarController.IsFogOfWarAlways) {
                    if (unit.IsOwnedByCurrentPlayer() == false) {
                        // boom this should make one part of fog always work
                        sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; 
                    }
                }
            }
            // Register our callback so that our GameObject gets updated whenever
            // the object's into changes.
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
            if (apc == null)
                yield return null;
            //somehow it sometimes get's here when its null so i added null check for spriterenderer
            apc.RecalculatePolygon();
            yield return null;
        }

        public void OnProjectileCreated(Projectile projectile) {
            GameObject proGo = new GameObject {
                name = "Projectile"
            };
            SpriteRenderer sr = proGo.AddComponent<SpriteRenderer>();
            if (FogOfWarController.FogOfWarOn) {
                if (FogOfWarController.IsFogOfWarAlways) {
                    sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
            }
            sr.sortingLayerName = "Units";
            sr.sprite = _unitSprites["cannonball_1"];
            projectile.RegisterOnDestroyCallback(OnProjectileDestroy);
            if (projectile.HasHitbox) {
                BoxCollider2D col = proGo.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit,
                                        sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
            }
            proGo.AddComponent<ProjectileHoldingScript>().Projectile = projectile;
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
            GameObject charGo = unitGameObjectMap[c];
            if (c is Ship ship) {
                charGo.SetActive(ship.isOffWorld == false);
                //change this so it does use the rigidbody to move
                //char_go.transform.position = new Vector3(c.X, c.Y, 0);
                //Quaternion q = char_go.transform.rotation;
                //q.eulerAngles = new Vector3(0, 0, c.Rotation);
                //char_go.transform.rotation = q;
            }
            else {
                charGo.transform.position = new Vector3(c.X, c.Y, 0);
                Quaternion q = charGo.transform.rotation;
                q.eulerAngles = new Vector3(0, 0, c.Rotation);
                charGo.transform.rotation = q;
            }
        }

        private void OnUnitDestroy(Unit c, IWarfare warfare) {
            if (unitGameObjectMap.ContainsKey(c) == false) {
                //Debug.LogError("OnUnitDestroy -- trying to change visuals for character not in our map.");
                return;
            }
            GameObject charGo = unitGameObjectMap[c];
            Destroy(charGo);
            unitGameObjectMap.Remove(c);
            c.UnregisterOnChangedCallback(OnUnitChanged);
        }

        private void OnCrateSpawned(Crate c) {
            //TODO: create a prefab?
            GameObject go = new GameObject();
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (FogOfWarController.FogOfWarOn) {
                if (FogOfWarController.IsFogOfWarAlways) {
                    sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
            }
            sr.sprite = _unitSprites["Crate"];
            go.AddComponent<CrateHoldingScript>().thisCrate = c;
            go.transform.SetParent(this.transform);
            go.name = "Crate";
            go.layer = LayerMask.NameToLayer("Unit");
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
            _unitSprites = new Dictionary<string, Sprite>();
            Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Units/");
            foreach (Sprite s in sprites) {
                _unitSprites[s.name] = s;
            }
            Sprite[] custom = ModLoader.LoadSprites(SpriteType.Unit);
            if (custom == null)
                return;
            foreach (Sprite s in custom) {
                _unitSprites[s.name] = s;
            }
        }

        public void OnDestroy() {
            World.Current.UnregisterUnitCreated(OnUnitCreated);
        }

        private void OnBuildStateChange(BuildStateModes bsm) {
            if (MouseController.Instance.MouseUnitState != MouseUnitState.Build)
                return;
            if (bsm != BuildStateModes.Build) {
                RemoveBuildCircle();
                return;
            }
            CreateBuildCircle();
        }

        private void RemoveBuildCircle() {
            if (_circleUnit == null) {
                return; // can be because cheats
            }
            if (unitGameObjectMap.ContainsKey(_circleUnit) == false) {
                return;//maybe it has been destroyed or other bug calls this function twice or cheats cause to call this without create
            }
            GameObject go = unitGameObjectMap[_circleUnit].transform.Find(CircleGOName).gameObject;
            Destroy(go);
        }

        private void CreateBuildCircle() {
            Unit circleUnit = MouseController.Instance.SelectedUnit;
            if (circleUnit == null) {
                return;
            }
            Transform parent = unitGameObjectMap[circleUnit].transform;
            GameObject go = Instantiate(unitCirclePrefab);
            go.name = CircleGOName;
            //buildrange is radius
            go.transform.localScale = new Vector3(circleUnit.BuildRange, circleUnit.BuildRange);
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0, 0, -0.5f);
            _circleUnit = circleUnit;
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
            if (Application.isPlaying == false)
                return;
            foreach (Unit unit in units) {
                if(unitGameObjectMap.ContainsKey(unit) == false) {
                    continue;
                }
                unitGameObjectMap[unit].GetComponent<SpriteOutline>().enabled = false;
            }
        }
    }
}