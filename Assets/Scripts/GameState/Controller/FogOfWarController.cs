using Andja.Model;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.Controller {
    /// <summary>
    /// Off and Unknown are working (should atleast)
    /// Always has lighter shadow but does not conceal anything
    /// </summary>
    public enum FogOfWarStyle { Off, Unknown, Always }

    public class FogOfWarController : MonoBehaviour {
        public Camera MainCamera;
        public Camera SecondaryCamera;
        public Canvas FogOfWarCanvas;
        public RawImage FogImage;
        private SpriteRenderer visibleTilesRenderer;
        public static FogOfWarController Instance;
        public GameObject UnitFogModulePrefab;
        private bool visibleTilesApply;

        public static bool FogOfWarOn => GameData.FogOfWarStyle != FogOfWarStyle.Off;

        private void Awake() {
            Instance = this;
        }

        private void Start() {
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
                    Texture2D visibleTiles = new Texture2D(World.Current.Width, World.Current.Height);
                    visibleTiles.filterMode = FilterMode.Point;
                    GameObject go = new GameObject();
                    go.name = "VisibleTiles";
                    go.transform.SetParent(transform);
                    //go.transform.position = new Vector3(World.Current.Width, World.Current.Height) / 2;
                    go.layer = LayerMask.NameToLayer("FogOfWar Secondary");
                    visibleTilesRenderer = go.AddComponent<SpriteRenderer>();
                    visibleTilesRenderer.sprite = Sprite.Create(visibleTiles, new Rect(0, 0, visibleTiles.width, visibleTiles.height), new Vector2(0.5f, 0.5f), 1);
                    break;
            }
            transform.position = new Vector2(World.Current.Width, World.Current.Height) / 2;
            if (SaveController.Instance.FogOfWarData.Length > 0)
                SetFogOfWarBytes(SaveController.Instance.FogOfWarData);
            MainCamera.orthographicSize = (Mathf.Max(World.Current.Width, World.Current.Height)) / 2;
            SecondaryCamera.orthographicSize = (Mathf.Max(World.Current.Width, World.Current.Height)) / 2;
            FogImage.GetComponent<RectTransform>().sizeDelta = new Vector2(World.Current.Width, World.Current.Height);
            foreach (Island isl in PlayerController.CurrentPlayer.GetIslandList()) {
                AddIslandFogModule(isl);
            }
            PlayerController.CurrentPlayer.RegisterCityCreated(OnCityCreated);
            World.Current.RegisterTileChanged(OnTileChanged);
            foreach (Tile t in World.Current.Tiles) {
                OnTileChanged(t);
            }
        }

        private void OnTileChanged(Tile tile) {
            if (GameData.FogOfWarStyle == FogOfWarStyle.Unknown)
                return;
            Color color = new Color(0, 0, 0, 0);
            if (tile.City?.IsCurrPlayerCity() == true) {
                color = new Color(0, 0, 1, 1);
            }
            visibleTilesRenderer.sprite.texture.SetPixel(tile.X, tile.Y, color);
            visibleTilesApply = true;
        }

        private void LateUpdate() {
            if (visibleTilesApply) {
                visibleTilesRenderer.sprite.texture.Apply();
                visibleTilesApply = false;
            }
        }

        private void OnCityCreated(City obj) {
            AddIslandFogModule(obj.Island);
        }

        /// <summary>
        /// Add to Player Unit a Fog Module.
        /// </summary>
        /// <param name="gameObject"></param>
        public void AddUnitFogModule(GameObject gameObject, Unit unit) {
            if (unit.IsPlayer() == false)
                return;
            GameObject module = Instantiate(UnitFogModulePrefab);
            module.transform.localScale = new Vector3(unit.AttackRange * 2, unit.AttackRange * 2);
            module.transform.SetParent(gameObject.transform, false);
            module.transform.localPosition = Vector3.zero;
        }

        private void AddIslandFogModule(Island island) {
            GameObject go = new GameObject("IslandFogReveal");
            go.transform.localPosition = island.Center;
            Texture2D tex = new Texture2D(island.Width + 10, island.Height + 10);

            for (int x = 0; x < island.Width + 10; x++) {
                for (int y = 0; y < island.Height + 10; y++) {
                    tex.SetPixel(x, y, new Color(1, 1, 1));
                }
            }
            tex.Apply();
            //SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            //sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1);
            //go.layer = LayerMask.NameToLayer("FogOfWar Main");
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1);

            GameObject fog = Instantiate(UnitFogModulePrefab);
            fog.transform.SetParent(go.transform);
            fog.transform.localPosition = Vector3.zero;
            fog.GetComponentInChildren<SpriteMask>().sprite = sprite;
            foreach(var v in fog.GetComponentsInChildren<SpriteRenderer>()){
                v.sprite = sprite;
            }
            //go.AddComponent<Mask>();
            //DeleteGO(go);
        }

        private void OnDisable() {
            Instance = null;
        }

        internal byte[] GetFogOfWarBytes() {
            Texture mainTexture = FogImage.texture;
            Texture2D texture2D = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.RGBA32, false);
            RenderTexture renderTexture = new RenderTexture(mainTexture.width, mainTexture.height, 32);
            Graphics.Blit(mainTexture, renderTexture);
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();
            return texture2D.EncodeToPNG();
        }

        internal void SetFogOfWarBytes(byte[] data) {
            try {
                Texture2D t2d = new Texture2D(World.Current.Width, World.Current.Height);
                t2d.LoadImage(data);
                t2d.Apply();
                Sprite s = Sprite.Create(t2d, new Rect(0, 0, t2d.width, t2d.height),new Vector2(0.5f,0.5f),
                    t2d.width / (float)World.Current.Width);
                GameObject go = new GameObject();
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = s;
                go.layer = LayerMask.NameToLayer("FogOfWar Main");
                go.transform.position = new Vector2(World.Current.Width, World.Current.Height) / 2f;
                StartCoroutine(DeleteGO(go));
            }
            catch(System.Exception e) {
                Debug.Log(e.Message);
                GameData.FogOfWarStyle = FogOfWarStyle.Off;
                gameObject.SetActive(false);
            }
        }

        private IEnumerator DeleteGO(GameObject go) {
            yield return new WaitForEndOfFrame();
            Destroy(go);
        }
    }
}