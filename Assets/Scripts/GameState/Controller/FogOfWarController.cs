using Andja.FogOfWar;
using Andja.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.Controller {
    /// <summary>
    /// Off and Unknown are working (should atleast)
    /// Always has lighter shadow but does conceal but no working save
    /// Always does maybe have a performance problem... 
    /// --because non hitbox structures cant be destroyed when out of view
    /// --also because hitboxes are involved with deciding what is visible
    /// </summary>
    public enum FogOfWarStyle { Off, Unknown, Always }

    public class FogOfWarController : MonoBehaviour {
        public Camera MainCamera;
        public Camera SecondaryCamera;
        public Canvas FogOfWarCanvas;
        public RawImage FogImage;
        private SpriteRenderer _visibleTilesRenderer;
        public static FogOfWarController Instance;
        public GameObject UnitFogModulePrefab;
        public GameObject StructureFogModulePrefab;
        private bool _visibleTilesApply;
        private Dictionary<uint, FogOfWarStructureData> _fogStructures;
        public static bool FogOfWarOn => GameData.FogOfWarStyle != FogOfWarStyle.Off;

        public static bool IsFogOfWarAlways => GameData.FogOfWarStyle == FogOfWarStyle.Always;

        private static FogOfWarSave _fogOfWarSaveData;

        public void Awake() {
            Instance = this;
        }

        public void Start() {
            switch (GameData.FogOfWarStyle) {
                case FogOfWarStyle.Off:
                    gameObject.SetActive(false);
                    return;

                case FogOfWarStyle.Unknown:
                    FogImage.material.SetFloat("_RedWeight", 2f);
                    FogImage.material.SetFloat("_BlueWeight", 0f);
                    break;

                case FogOfWarStyle.Always:
                    FogImage.material.SetFloat("_RedWeight", 1.5f);
                    FogImage.material.SetFloat("_BlueWeight", 0.5f);
                    Texture2D visibleTiles = new Texture2D(GameData.Width, GameData.Height);
                    visibleTiles.filterMode = FilterMode.Point;
                    _fogStructures = new Dictionary<uint, FogOfWarStructureData>();
                    GameObject go = new GameObject();
                    go.name = "VisibleTiles";
                    go.transform.SetParent(transform);
                    //go.transform.position = new Vector3(World.Current.Width, World.Current.Height) / 2;
                    go.layer = LayerMask.NameToLayer("FogOfWar Secondary");
                    _visibleTilesRenderer = go.AddComponent<SpriteRenderer>();
                    _visibleTilesRenderer.sprite = Sprite.Create(visibleTiles, new Rect(0, 0, visibleTiles.width, visibleTiles.height), new Vector2(0.5f, 0.5f), 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            transform.position = new Vector2(GameData.Width, GameData.Height) / 2;
            if (_fogOfWarSaveData != null) {
                LoadSave();
            } 
            MainCamera.orthographicSize = (Mathf.Max(GameData.Width, GameData.Height)) / 2f;
            SecondaryCamera.orthographicSize = (Mathf.Max(GameData.Width, GameData.Height)) / 2f;
            FogImage.GetComponent<RectTransform>().sizeDelta = new Vector2(GameData.Width, GameData.Height);
            foreach (Island isl in PlayerController.CurrentPlayer.GetIslandList()) {
                AddIslandFogModule(isl);
            }
            PlayerController.CurrentPlayer.RegisterCityCreated(OnCityCreated);
            World.Current.RegisterTileChanged(OnTileChanged);
            foreach (Tile t in World.Current.Tiles) {
                OnTileChanged(t);
            }
        }

        internal void RemoveFogOfWarStructure(uint buildID) {
            _fogStructures.Remove(buildID);
        }

        private void OnTileChanged(Tile tile) {
            if (IsFogOfWarAlways == false)
                return;
            Color color = new Color(0, 0, 0, 0);
            if (tile.City?.IsCurrentPlayerCity() == true) {
                color = new Color(0, 0, 1, 1);
            }
            _visibleTilesRenderer.sprite.texture.SetPixel(tile.X, tile.Y, color);
            _visibleTilesApply = true;
        }

        public void LateUpdate() {
            if (_visibleTilesApply == false) return;
            _visibleTilesRenderer.sprite.texture.Apply();
            _visibleTilesApply = false;
        }

        private void OnCityCreated(ICity obj) {
            AddIslandFogModule(obj.Island);
        }

        /// <summary>
        /// Add to Player Unit a Fog Module.
        /// </summary>
        /// <param name="gameObject"></param>
        public void AddUnitFogModule(GameObject gameObject, Unit unit) {
            if (unit.IsOwnedByCurrentPlayer() == false)
                return;
            GameObject module = Instantiate(UnitFogModulePrefab);
            module.transform.localScale = new Vector3(unit.AttackRange * 2, unit.AttackRange * 2);
            module.transform.SetParent(gameObject.transform, false);
            module.transform.localPosition = Vector3.zero;
        }
        public void AddStructureFogModule(GameObject gameObject, Structure structure) {
            FogOfWarStructure fws = gameObject.AddComponent<FogOfWarStructure>();
            if (_fogStructures.ContainsKey(structure.buildID)) {
                fws.Set(_fogStructures[structure.buildID]);
            } else {
                fws.Link(structure);
            }
            AddBoxCollider(gameObject, structure.HasHitbox == false);
            _fogStructures[structure.buildID] = fws.Data;
        }

        private void AddBoxCollider(GameObject gameObject, bool isTrigger) {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
            col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit, sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
            col.isTrigger = isTrigger;
        }

        private void AddIslandFogModule(Island island) {
            GameObject go = new GameObject("IslandFogReveal");
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(island.Width + 10, island.Height + 10);
            col.isTrigger = true;
            go.transform.localPosition = island.Center;
            Texture2D tex = new Texture2D(island.Width + 10, island.Height + 10);
            for (int x = 0; x < island.Width + 10; x++) {
                for (int y = 0; y < island.Height + 10; y++) {
                    tex.SetPixel(x, y, new Color(1, 1, 1));
                }
            }
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1);
            GameObject fog = Instantiate(StructureFogModulePrefab);
            fog.transform.SetParent(go.transform);
            fog.transform.localPosition = Vector3.zero;
            fog.GetComponentInChildren<SpriteMask>().sprite = sprite;
            foreach (var v in fog.GetComponentsInChildren<SpriteRenderer>()) {
                v.sprite = sprite;
            }
        }

        public void OnDisable() {
            Instance = null;
        }

        public FogOfWarSave GetFogOfWarSave() {
            if(_fogStructures != null) {
                return new FogOfWarSave() {
                    image = Convert.ToBase64String(SaveController.Zip(Convert.ToBase64String(GetFogOfWarImageBytes()))),
                    fogOfWarStructures = new List<FogOfWarStructureData>(_fogStructures.Values),
                };
            }
            return new FogOfWarSave() {
                image = Convert.ToBase64String(SaveController.Zip(Convert.ToBase64String(GetFogOfWarImageBytes()))),
            };
        }

        public void LoadSave() {
            if(_fogOfWarSaveData.fogOfWarStructures != null) {
                foreach (var item in _fogOfWarSaveData.fogOfWarStructures) {
                    if (BuildController.Instance.BuildIdToStructure.ContainsKey(item.buildID)) {
                        continue;
                    }
                    GameObject go = new GameObject();
                    go.layer = LayerMask.NameToLayer("FogOfWar"); //TODO: test this 
                    go.transform.SetParent(this.transform, true);
                    go.name = "FogOfWarStructure_";
                    FogOfWarStructure fws = go.AddComponent<FogOfWarStructure>();
                    fws.Set(item);
                    if (fws.structure != null) {
                        AddBoxCollider(go, fws.structure.HasHitbox == false);
                    }
                    else {
                        AddBoxCollider(go, true);
                    }
                }
            }
            SetFogOfWarImageBytes(Convert.FromBase64String(SaveController.Unzip(Convert.FromBase64String(_fogOfWarSaveData.image))));
        }

        private byte[] GetFogOfWarImageBytes() {
            Texture mainTexture = FogImage.texture;
            Texture2D texture2D = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.RGBA32, false);
            RenderTexture renderTexture = new RenderTexture(mainTexture.width, mainTexture.height, 32);
            Graphics.Blit(mainTexture, renderTexture);
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();
            return texture2D.EncodeToPNG();
        }

        private void SetFogOfWarImageBytes(byte[] data) {
            try {
                Texture2D t2d = new Texture2D(World.Current.Width, World.Current.Height);
                t2d.LoadImage(data);
                t2d.Apply();
                Sprite s = Sprite.Create(t2d, new Rect(0, 0, t2d.width, t2d.height), new Vector2(0.5f, 0.5f),
                    t2d.width / (float)World.Current.Width);
                GameObject go = new GameObject();
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = s;
                go.layer = LayerMask.NameToLayer("FogOfWar Main");
                go.transform.position = new Vector2(World.Current.Width, World.Current.Height) / 2f;
                StartCoroutine(DeleteGO(go));
            }
            catch (Exception e) {
                Debug.Log(e.Message);
                GameData.FogOfWarStyle = FogOfWarStyle.Off;
                gameObject.SetActive(false);
            }
        }

        private IEnumerator DeleteGO(GameObject go) {
            yield return new WaitForEndOfFrame();
            Destroy(go);
        }

        internal static void SetSaveFogData(FogOfWarSave fws) {
            if (fws == null)
                return;
            _fogOfWarSaveData = fws;
            GameData.FogOfWarStyle = fws.fogOfWarStructures != null ? FogOfWarStyle.Always : FogOfWarStyle.Unknown;
        }

    }
    [Newtonsoft.Json.JsonObject]
    public class FogOfWarSave : BaseSaveData {
        public string image;
        public List<FogOfWarStructureData> fogOfWarStructures;
    }
}