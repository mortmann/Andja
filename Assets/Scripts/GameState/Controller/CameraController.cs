using Andja.Editor;
using Andja.Model;
using Andja.UI.Menu;
using Andja.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.Controller {

    public class CameraController : MonoBehaviour {
        public static int MaxZoomLevel => devCameraZoom ? 100 : 25;
        public static bool devCameraZoom = false;
        public static int minZoomLevel = 3;

        float Height => 2f * Camera.main.orthographicSize;
        float Width => Height * Camera.main.aspect;

        private SpriteRenderer MiniMapCameraShadow;
        public Camera MiniMapCamera;
        private Vector3 lastFramePosition;
        private Vector3 currFramePosition;
        public Vector3 upper = new Vector3(1, 1);
        public Vector3 lower = new Vector3();
        public Vector3 middle = new Vector3();
        public Island nearestIsland;

        /// <summary>
        /// X = Ocean Y = Land Z = Height (wind)
        /// </summary>
        public Vector3 SoundAmbientValues;

        public float zoomLevel;

        public HashSet<Tile> tilesCurrentInCameraView;
        public HashSet<Structure> structureCurrentInCameraView;

        public Rect CameraViewRange;
        private Vector2 showBounds = new Vector2();
        public static CameraController Instance;
        private CameraSave cameraSave;

        private void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two CameraController.");
            }
            Instance = this;
#if UNITY_EDITOR
            Debug.Log("LOCKED FRAMERATE 100");
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 100;
#endif
        }

        private void Start() {
            tilesCurrentInCameraView = new HashSet<Tile>();
            structureCurrentInCameraView = new HashSet<Structure>();
            if (EditorController.IsEditor) {
                showBounds.x = EditorController.Width;
                showBounds.y = EditorController.Height;
            }
        }

        public void GameScreenSetup() {
            MiniMapCameraShadow = Camera.main.gameObject.GetComponentInChildren<SpriteRenderer>();
            MiniMapCamera = GameObject.FindGameObjectWithTag("MiniMapCamera").GetComponent<Camera>();
            MiniMapCamera.orthographicSize = World.Current.Width / 2;
            MiniMapCamera.rect = new Rect(0, 0, World.Current.Width, World.Current.Height);
            MiniMapCamera.transform.position = new Vector3(World.Current.Width / 2, World.Current.Height / 2, Camera.main.transform.position.z);
            if (cameraSave == null) {
                if (WorldController.Instance.SpawningRect == null) {
                    Camera.main.transform.position = new Vector3(World.Current.Width / 2, World.Current.Height / 2, Camera.main.transform.position.z);
                }
                else {
                    Structure str = PlayerController.CurrentPlayer.AllStructures?.FirstOrDefault(x => x is WarehouseStructure);
                    if (str == null) {
                        Camera.main.transform.position = new Vector3(WorldController.Instance.SpawningRect.center.x,
                                    WorldController.Instance.SpawningRect.center.y, Camera.main.transform.position.z);
                        Camera.main.orthographicSize = WorldController.Instance.SpawningRect.size.magnitude / 2;
                    }
                    else {
                        Camera.main.transform.position = new Vector3(str.Center.x, str.Center.y, Camera.main.transform.position.z);
                        Camera.main.orthographicSize = 15;
                    }
                }
            }
            else {
                Camera.main.transform.position = cameraSave.pos.Vec;
                Camera.main.orthographicSize = cameraSave.orthographicSize;
            }
            middle = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2));
            lower = Camera.main.ScreenToWorldPoint(Vector3.zero);
            upper = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));
            middle = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2));
            showBounds.x = World.Current.Width;
            showBounds.y = World.Current.Height;
        }

        private void Update() {
            //DO not move atall when Menu is Open
            if (PauseMenu.IsOpen || Loading.IsLoading) {
                return;
            }

            Vector3 cameraMove = new Vector3(0, 0);
            currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            currFramePosition.z = 0;
            UpdateZoom();
            zoomLevel = Mathf.Clamp(Camera.main.orthographicSize - 2, minZoomLevel, MaxZoomLevel);
            if (EditorController.IsEditor == false) {
                MiniMapCameraShadow.transform.localScale = new Vector3(Width, Height);
            }
            cameraMove += UpdateKeyboardCameraMovement();
            cameraMove += UpdateMouseCameraMovement();

            Vector3 leftBottom = Camera.main.ScreenToWorldPoint(Vector3.zero);
            Vector3 leftTop = Camera.main.ScreenToWorldPoint(new Vector3(0, Camera.main.pixelHeight));
            Vector3 rightBottom = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0));
            Vector3 rightTop = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));

            lower = new Vector3(Mathf.Min(leftBottom.x, leftTop.x, rightBottom.x, rightTop.x),
                                Mathf.Min(leftBottom.y, leftTop.y, rightBottom.y, rightTop.y),
                                Mathf.Min(leftBottom.z, leftTop.z, rightBottom.z, rightTop.z));
            upper = new Vector3(Mathf.Max(leftBottom.x, leftTop.x, rightBottom.x, rightTop.x),
                                Mathf.Max(leftBottom.y, leftTop.y, rightBottom.y, rightTop.y),
                                Mathf.Max(leftBottom.z, leftTop.z, rightBottom.z, rightTop.z));

            middle = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2));

            Vector3 newLower = cameraMove + lower;
            Vector3 newUpper = cameraMove + upper;
            if (newUpper.x > showBounds.x) {
                if (cameraMove.x > 0) {
                    cameraMove.x = Mathf.Clamp(cameraMove.x, 0, showBounds.x - upper.x);
                }
            }
            if (newLower.x < 0) {
                if (cameraMove.x < 0) {
                    cameraMove.x = Mathf.Clamp(cameraMove.x, 0, -lower.x);
                }
            }
            if (newUpper.y > showBounds.y) {
                if (cameraMove.y > 0) {
                    cameraMove.y = Mathf.Clamp(cameraMove.y, 0, showBounds.y - upper.y);
                }
            }
            if (newLower.y < 0) {
                if (cameraMove.y < 0) {
                    cameraMove.y = Mathf.Clamp(cameraMove.y, 0, -lower.y);
                }
            }
            Camera.main.transform.Translate(cameraMove);
            lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lastFramePosition.z = 0;

            Rect oldViewRange = new Rect(CameraViewRange);
            int mod = 2 + (int)zoomLevel / 2;//TODO: optimize this
            int lX = (int)lower.x - mod;
            int uX = (int)upper.x + mod;
            int lY = (int)lower.y - mod;
            int uY = (int)upper.y + mod;
            CameraViewRange = new Rect(lX, lY, uX - lX, uY - lY);

            tilesCurrentInCameraView.Clear();
            structureCurrentInCameraView.Clear();

            SoundAmbientValues = new Vector3();
            bool islandInview = false;
            for (int x = Mathf.FloorToInt(Mathf.Min(oldViewRange.xMin, CameraViewRange.xMin)); x < Mathf.CeilToInt(Mathf.Max(oldViewRange.xMax, CameraViewRange.xMax)); x++) {
                for (int y = Mathf.FloorToInt(Mathf.Min(oldViewRange.yMin, CameraViewRange.yMin)); y < Mathf.CeilToInt(Mathf.Max(oldViewRange.yMax, CameraViewRange.yMax)); y++) {
                    Tile tile_data = World.Current.GetTileAt(x, y);
                    if (tile_data == null
                        || tile_data.Type == TileType.Ocean) {
                        SoundAmbientValues.x++;
                        continue;
                    }
                    SoundAmbientValues.y++;
                    bool isInNew = CameraViewRange.Contains(tile_data.Vector);
                    bool isInOld = oldViewRange.Contains(tile_data.Vector);
                    if (isInNew == false && isInOld == false) {
                        continue;
                    }

                    if (isInNew) {
                        islandInview = true;
                        nearestIsland = tile_data.Island;
                        if (EditorController.IsEditor) {
                            tilesCurrentInCameraView.Add(tile_data);
                        }
                        if (tile_data.Structure != null) {
                            structureCurrentInCameraView.Add(tile_data.Structure);
                        }
                    }
                }
            }
            if (islandInview == false)
                nearestIsland = null;
            float tileCount = CameraViewRange.width * CameraViewRange.height;
            SoundAmbientValues.x /= tileCount;
            SoundAmbientValues.y /= tileCount;
            SoundAmbientValues.z = Mathf.Clamp((MaxZoomLevel - zoomLevel) / MaxZoomLevel, 0, 1f);
        }

        private void OnDrawGizmos() {
            Vector3 a = Camera.main.ScreenToWorldPoint(Vector3.zero);
            Vector3 b = Camera.main.ScreenToWorldPoint(new Vector3(0, Camera.main.pixelHeight));
            Vector3 c = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0));
            Vector3 d = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));

            var plane = new Plane(a, c, d);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(a, 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(b, 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(b, c);
            Gizmos.DrawWireSphere(c, 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(c, d);
            Gizmos.DrawWireSphere(d, 0.1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(lower, upper);
            Gizmos.DrawWireSphere(lower, 0.1f);
            Gizmos.DrawWireSphere(upper, 0.1f);

            Gizmos.color = plane.GetSide(transform.position) ? Color.cyan : Color.red;
            Gizmos.DrawLine(plane.ClosestPointOnPlane(transform.position), transform.position);
        }

        internal void SetSaveCameraData(CameraSave camera) {
            this.cameraSave = camera;
        }

        private Vector3 UpdateMouseCameraMovement() {
            // Handle screen panning
            if (Input.GetMouseButton(1) || Input.GetMouseButton(2)) {   // Right or Middle Mouse Button
                return lastFramePosition - currFramePosition;
            }
            return Vector3.zero;
        }

        public void UpdateZoom() {
            if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus)) {
                Camera.main.orthographicSize -= Camera.main.orthographicSize * 0.1f;
            }
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) {
                Camera.main.orthographicSize += Camera.main.orthographicSize * 0.1f;
            }

            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoomLevel, MaxZoomLevel);
        }

        public Vector3 UpdateKeyboardCameraMovement() {
            if (UIController.IsTextFieldFocused()) {
                return Vector3.zero;
            }
            if (Mathf.Abs(Input.GetAxis("Horizontal")) == 0 && Mathf.Abs(Input.GetAxis("Vertical")) == 0) {
                return Vector3.zero;
            }
            float Horizontal = 0;
            if (Input.GetAxis("Horizontal") < 0) {
                Horizontal = -1;
            }
            if (Input.GetAxis("Horizontal") > 0) {
                Horizontal = 1;
            }
            float Vertical = 0;
            if (Input.GetAxis("Vertical") < 0) {
                Vertical = -1;
            }
            if (Input.GetAxis("Vertical") > 0) {
                Vertical = 1;
            }
            float zoomMultiplier = Mathf.Clamp(Camera.main.orthographicSize - 2, 1, 4f) * 10;
            return new Vector3(zoomMultiplier * Horizontal * Time.deltaTime, zoomMultiplier * Vertical * Time.deltaTime, 0);
        }

        private void OnDestroy() {
            Instance = null;
        }

        public void MoveCameraToPosition(Vector2 pos) {
            Camera.main.transform.position = new Vector3(pos.x, pos.y, Camera.main.transform.position.z);
            currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lastFramePosition = currFramePosition;
        }

        public CameraSave GetSaveCamera() {
            CameraSave cs = new CameraSave {
                orthographicSize = Camera.main.orthographicSize,
                pos = Camera.main.transform.position
            };
            return cs;
        }
    }

    [Serializable]
    public class CameraSave : BaseSaveData {
        public SeriaziableVector3 pos;
        public float orthographicSize;
    }
}