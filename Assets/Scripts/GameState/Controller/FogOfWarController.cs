using Andja.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.Controller {

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

            for (int x = (-island.Width / 2) - 5; x < (island.Width / 2) + 5; x++) {
                for (int y = (-island.Height / 2) - 5; y < (island.Height / 2) + 5; y++) {
                    tex.SetPixel(x, y, new Color(1, 0, 0));
                }
            }
            tex.Apply();
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1);
            go.layer = LayerMask.NameToLayer("FogOfWar Main");
        }

        private void OnDisable() {
            Instance = null;
        }

        internal byte[] GetFogOfWarBytes() {
            return ((Texture2D)FogImage.texture).GetRawTextureData();
        }

        internal void SetFogOfWarBytes(byte[] data) {
            try {
                ((Texture2D)FogImage.texture).LoadRawTextureData(data);
            }
            catch {
                GameData.FogOfWarStyle = FogOfWarStyle.Off;
                gameObject.SetActive(false);
            }
        }
    }
}