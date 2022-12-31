using Andja.Editor;
using Andja.Model;
using Andja.Model.Components;
using Andja.Pathfinding;
using Andja.UI;
using Andja.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.Controller {

    public enum MouseState { Idle, BuildDrag, BuildPath, BuildSingle, Unit, UnitGroup, Destroy, DragSelect, Copy, Upgrade };

    public enum MouseUnitState { None, Normal, Patrol, Build };

    public enum TileHighlightType { Green, Red }

    public enum CursorType { Pointer, Attack, Escort, Destroy, Build, Copy, Upgrade }

    public enum MapErrorMessage { NoSpace, NotEnoughResources, NotEnoughMoney, NotInCity, Missing,
        NotInRange,
        CanNotBuildHere,
        CanNotDestroy
    }
    /// <summary>
    /// Controls all Mouse Interactions with the Map and Units.
    /// Shows Previews for Building and Destroy.
    /// </summary>
    public class MouseController : MonoBehaviour {
        public static MouseController Instance { get; protected set; }
        public GameObject structurePreviewRendererPrefab;
        public GameObject greenTileCursorPrefab;
        public GameObject redTileCursorPrefab;
        public GameObject fadeOutTextPrefab;

        public ExtraStructureBuildUI[] extraStructureBuildUIPrefabsEditor;
        public Dictionary<ExtraBuildUI, GameObject> ExtraStructureBuildUIPrefabs { get; protected set; }
        private GameObject _highlightGO;

        /// <summary>
        /// If it is in either build or destroy mode
        /// </summary>
        bool IsInBuildDestroyMode => MouseState == MouseState.BuildDrag || 
                                     MouseState == MouseState.BuildPath ||
                                     MouseState == MouseState.BuildSingle ||
                                     MouseState == MouseState.Destroy;

        /// <summary>
        /// The world-position of the mouse last frame.
        /// </summary>
        public Vector3 LastFramePosition { get; protected set; }
        /// <summary>
        /// The world-position of the mouse last frame with TileOffset.
        /// </summary>
        public Vector3 CurrentFramePositionOffset => CurrentFramePosition 
                                                    + new Vector3(TileSpriteController.offset, TileSpriteController.offset, 0);
        public Vector3 LastFrameGuiPosition { get; protected set; }
        public Vector3 CurrentFramePosition { get; protected set; }

        public static bool Autorotate = true;

        /// <summary>
        ///  is true if smth is overriding the current states and commands for units
        /// </summary>
        public static bool OverrideCurrentSetting => InputHandler.ShiftKey == false; // TODO: better name

        public Vector2 MapClampedMousePosition =>
            new Vector2(Mathf.Clamp(CurrentFramePosition.x, 0, World.Current.Width),
                Mathf.Clamp(CurrentFramePosition.y, 0, World.Current.Height));

        private Structure _selectedStructure;

        BaseMouseState ActiveState;
        Dictionary<MouseState, BaseMouseState> typToMouseState;
        public Structure SelectedStructure {
            get => _selectedStructure;
            set {
                _selectedStructure?.CloseExtraUI();
                _selectedStructure = value;
            }
        }

        protected Structure _toBuildstructure;

        public Structure ToBuildStructure {
            get => _toBuildstructure;
            set {
                if (_toBuildstructure != null)
                    ActiveState.Reset();
                _toBuildstructure = value;
            }
        }

        public Item[] NeededItemsToBuild;
        public int NeededBuildCost;

        public MouseState MouseState { get; protected set; } = MouseState.Idle;
        public MouseUnitState MouseUnitState { get; protected set; } = MouseUnitState.None;

        private Unit _selectedUnit;
        public List<Unit> selectedUnitGroup;

        public Unit SelectedUnit {
            get => _selectedUnit;
            protected set {
                _selectedUnit?.UnregisterOnDestroyCallback(OnUnitDestroy);
                _selectedUnit = value;
                SetMouseUnitState(_selectedUnit == null ? MouseUnitState.None : MouseUnitState.Normal);
            }
        }

        public GEventable CurrentlySelectedIGEventable {
            get {
                if (SelectedUnit != null)
                    return SelectedUnit;
                if (SelectedStructure != null)
                    return SelectedStructure;
                return CameraController.Instance.nearestIsland;
            }
        }

        public bool IsGod { get; set; }

        public void OnEnable() {
            if (Instance != null) {
                Debug.LogError("There should never be two mouse controllers.");
            }
            Instance = this;
        }

        public void Start() {
            selectedUnitGroup = new List<Unit>();
            ExtraStructureBuildUIPrefabs = new Dictionary<ExtraBuildUI, GameObject>();
            foreach (ExtraStructureBuildUI esbu in extraStructureBuildUIPrefabsEditor) {
                ExtraStructureBuildUIPrefabs[esbu.Type] = esbu.Prefab;
            }
            SetupMouseStates();
        }

        private void SetupMouseStates() {
            typToMouseState = new Dictionary<MouseState, BaseMouseState> {
                [MouseState.Idle] = new IdleMouseState(),
                [MouseState.Copy] = new CopyMouseState(),
                [MouseState.BuildDrag] = new DragBuildMouseState(),
                [MouseState.BuildSingle] = new SingleBuildMouseState(),
                [MouseState.BuildPath] = new PathBuildMouseState(),
                [MouseState.Unit] = new SingleUnitMouseState(),
                [MouseState.UnitGroup] = new UnitGroupMouseState(),
                [MouseState.Upgrade] = new UpgradeMouseState(),
                [MouseState.DragSelect] = new BoxSelectMouseState(),
                [MouseState.Destroy] = new DestroyBuildMouseState(),
            };
            ActiveState = typToMouseState[MouseState.Idle];
        }

        /// <summary>
        /// Gets the mouse position in world space.
        /// </summary>
        public Vector3 GetMousePosition() {
            return CurrentFramePositionOffset;
        }

        /// <summary>
        /// Gets the mouse position in world space.
        /// </summary>
        public Vector3 GetLastMousePosition() {
            return LastFramePosition;
        }

        public void Update() {
            if (EditorController.IsEditor == false && PlayerController.Instance.GameOver)
                return;

            CurrentFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CurrentFramePosition.Scale(new Vector3(1, 1, 0));
            if (CurrentFramePosition.y < 0 || CurrentFramePosition.x < 0) {
                return;
            }
            UpdateMouseStates();
            if (EditorController.IsEditor == false) {
                CheckDragBoxSelect();
            }
            else {
                UpdateEditorStuff();
            }

            // Save the mouse position from this frame
            // We don't use currFramePosition because we may have moved the camera.
            LastFrameGuiPosition = Input.mousePosition;
            LastFramePosition = CurrentFramePosition;
        }

        public void UnselectStructure() {
            SelectedStructure = null;
        }

        public void SetStructureToBuild(Structure toBuildStructure) {
            ToBuildStructure = toBuildStructure;
            switch (toBuildStructure.BuildTyp) {
                case BuildType.Drag:
                    SetMouseState(MouseState.BuildDrag);
                    break;
                case BuildType.Path:
                    SetMouseState(MouseState.BuildPath);
                    break;
                case BuildType.Single:
                    SetMouseState(MouseState.BuildSingle);
                    break;
            }
            //this has to be here to prevent the previous state to change the values
            NeededItemsToBuild = ToBuildStructure.BuildingItems?.CloneArrayWithCounts();
            NeededBuildCost = ToBuildStructure.BuildCost;
        }

        /// <summary>
        /// Set the MouseState and changes the current Cursor.
        /// </summary>
        /// <param name="state"></param>
        public void SetMouseState(MouseState state) {
            ActiveState.Deactivate();
            ActiveState = typToMouseState[state];
            ActiveState.Activate();
            MouseState = state;
        }

        public void SetMouseUnitState(MouseUnitState state) {
            MouseUnitState = state;
        }
        /// <summary>
        /// Moves Highlights with offset so that mouse is in the middle.
        /// </summary>
        private void UpdateEditorStuff() {
            if (_highlightGO == null) return;
            Tile t = GetTileUnderneathMouse();
            if (t == null)
                return;
            _highlightGO.transform.position = new Vector3(t.X, t.Y, 0) + EditorController.Instance.BrushOffset;
        }

        public void UpdateMouseStates() {
            ActiveState.Update();
        }
        /// <summary>
        /// Responsible for detecting a drag not in Build/Destroy Mode 
        /// </summary>
        private void CheckDragBoxSelect() {
            if (IsInBuildDestroyMode || MouseState == MouseState.DragSelect 
                || EventSystem.current.IsPointerOverGameObject() || ShortcutUI.Instance.IsDragging)
                return;
            if (InputHandler.GetMouseButton(InputMouse.Primary)) {
                float sqrdist = (Input.mousePosition - LastFrameGuiPosition).sqrMagnitude;
                if (sqrdist > 5) {
                    SetMouseState(MouseState.DragSelect);
                }
            }
        }

        public void UnselectStuff(bool escape = false) {
            UnselectUnit();
            UnselectUnitGroup();
            UnselectStructure();
            if(escape == false)
                UIController.Instance.CloseMouseUnselect();
        }

        public void ShowError(MapErrorMessage message) {
            ShowError(message, CurrentFramePosition);
        }

        public void ShowError(MapErrorMessage message, Vector3 position) {
            TextMeshPro text = SimplePool.Spawn(fadeOutTextPrefab, position, Quaternion.identity).GetComponent<TextMeshPro>();
            text.fontSize = Mathf.Max(8.333f * (CameraController.Instance.zoomLevel / CameraController.MaxZoomLevel), 2);
            text.text = UILanguageController.Instance.GetTranslation(message);
            StartCoroutine(DespawnFade(text));
        }

        private IEnumerator DespawnFade(TextMeshPro text) {
            yield return new WaitForSeconds(1f);
            SimplePool.Despawn(text.gameObject);
        }

        public void SetEditorBrushHighlightActive(bool brushBuild) {
            _highlightGO.SetActive(brushBuild);
        }
        /// <summary>
        /// Which tiles will be highlighted. 
        /// Size of the texture.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="toHighlightTiles"></param>
        /// <param name="active"></param>
        public void SetEditorHighlight(int size, List<Tile> toHighlightTiles, bool active) {
            if (toHighlightTiles == null)
                return;
            if (_highlightGO != null)
                Destroy(_highlightGO);
            _highlightGO = GetHighlightGameObject(size, size, toHighlightTiles);
            _highlightGO.GetComponent<SpriteRenderer>().sortingLayerName = "StructuresUI";
            _highlightGO.SetActive(active);
        }

        public void OnGUI() {
            ActiveState.OnGui();
        }
        /// <summary>
        /// OnClick on Map. If it hits unit or structure. UIControllers decides then which UI.
        /// If nothing close UI(s).
        /// </summary>
        /// <param name="hit"></param>
        public void MakeRaycastToCheckWhatTodo() {
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            Transform hit = MouseRayCast();
            ITargetableHoldingScript targetableHoldingScript = hit?.GetComponent<ITargetableHoldingScript>();
            if (targetableHoldingScript != null) {
                if (targetableHoldingScript.IsUnit == false) return;
                if (GameData.FogOfWarStyle == FogOfWarStyle.Always && targetableHoldingScript.IsCurrentlyVisible == false) {
                    return;
                }
                SelectUnit((Unit)targetableHoldingScript.Holding);
            }  else
            if (SelectedUnit == null) {
                if (GameData.FogOfWarStyle == FogOfWarStyle.Always) {
                    if (FogOfWar.FogOfWarStructure.IsStructureVisible(hit.gameObject) == false) {
                        return;
                    }
                }
                Tile t = GetTileUnderneathMouse();
                if (t.Structure != null && 
                    (t.Structure.HasHitbox || t.Structure is RoadStructure == false && t.Structure is GrowableStructure == false)) {
                    UIDebug(t.Structure);
                    UIController.Instance.OpenStructureUI(t.Structure);
                    SelectedStructure = t.Structure;
                } 
                else {
                    UIDebug(GetTileUnderneathMouse());
                    if (MouseState != (MouseState.Unit | MouseState.UnitGroup)) {
                        UnselectStuff();
                    }
                }
            }
            
        }

        public void SelectUnit(Unit unit) {
            if (SelectedUnit == unit)
                return;
            SelectedUnit = unit;
            SetMouseState(MouseState.Unit);
            SetMouseUnitState(MouseUnitState.Normal);
        }

        public void SelectUnitGroup(List<Unit> units) {
            if(units.Count == 1) {
                SelectUnit(units[0]);
                return;
            }
            selectedUnitGroup = units;
            SelectedUnit = null;
            SetMouseState(MouseState.UnitGroup);
            SetMouseUnitState(MouseUnitState.Normal);
        }

        public void UIDebug(object obj) {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift)
                        && SaveController.DebugModeSave) {
                UIController.Instance.ShowDebugForObject(obj);
            }
        }

        public GameObject GetHighlightGameObject(int width, int height, IEnumerable<Tile> tiles) {
            GameObject highGO = new GameObject();
            SpriteRenderer sr = highGO.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            tex.SetPixels32(new Color32[width * height]);
            foreach (Tile t in tiles) {
                if (t == null)
                    continue;
                tex.SetPixel(t.X, t.Y, new Color32(255, 255, 255, 20));
            }
            tex.filterMode = FilterMode.Point;
            sr.sortingLayerName = "Structures";
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1);
            return highGO;
        }

        public void UnselectUnit(bool closeUI = true) {
            if (SelectedUnit == null)
                return;
            SetMouseState(MouseState.Idle);
            SetMouseUnitState(MouseUnitState.None);
            SelectedUnit = null;
            UnselectStructure();
            if (closeUI)
                UIController.Instance.CloseInfoUI();
        }
 
        /// <summary>
        /// Send the build command to the buildcontroller based on what the player has selected.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="single"></param>
        /// <param name="inOrder"></param>
        public void Build(List<Tile> t, bool single = false) {
            if (EditorController.IsEditor) {
                EditorController.Instance.BuildOn(t, single);
            }
            else {
                if (MouseUnitState == MouseUnitState.Build) {
                    BuildController.Instance.CurrentPlayerBuildOnTile(t, single, PlayerController.currentPlayerNumber, false, SelectedUnit);
                }
                else {
                    BuildController.Instance.CurrentPlayerBuildOnTile(t, single, PlayerController.currentPlayerNumber, false);
                }
            }
        }

        public void BuildFromUnit() {
            SetMouseUnitState(MouseUnitState.Build);
            BuildController.Instance.SettleFromUnit(SelectedUnit);
        }

        public void SetToPatrolMode() {
            SetMouseUnitState(MouseUnitState.Patrol);
        }

        public void ResetBuild(bool loading = false) {
            if (loading) {
                return;// there is no need to call any following
            }
            if (BuildController.Instance.BuildState != BuildStateModes.None)
                BuildController.Instance.ResetBuild();
            NeededBuildCost = 0;
            NeededItemsToBuild = null;
            ToBuildStructure = null;
            if (MouseUnitState == MouseUnitState.Build) {
                UnselectUnit();
            }
        }

        public void UnselectUnitGroup() {
            if (selectedUnitGroup == null)
                return;
            selectedUnitGroup.ForEach(x => x.UnregisterOnDestroyCallback(OnUnitDestroy));
            selectedUnitGroup.Clear();
            SetMouseState(MouseState.Idle);
            SetMouseUnitState(MouseUnitState.None);
            selectedUnitGroup.Clear();
        }

        public void RemoveUnitFromGroup(Unit unit) {
            selectedUnitGroup.Remove(unit);
            if (selectedUnitGroup.Count == 0) {
                UIController.Instance.CloseInfoUI();
                UnselectUnitGroup();
            }
            if (selectedUnitGroup.Count == 1) {
                UIController.Instance.CloseInfoUI();
                UIController.Instance.OpenUnitUI(selectedUnitGroup[0]);
                SelectUnit(selectedUnitGroup[0]);
                selectedUnitGroup.Clear();
            }
            unit.UnregisterOnDestroyCallback(OnUnitDestroy);
        }

        private void OnUnitDestroy(Unit unit, IWarfare warfare) {
            if (SelectedUnit == unit) {
                SetMouseState(MouseState.Idle);
                _selectedUnit = null;
            }
            else {
                if (selectedUnitGroup.Contains(unit))
                    selectedUnitGroup.Remove(unit);
            }
        }

        public void StopUnit() {
            //if null or not player unit return without doing anything
            if (SelectedUnit == null || SelectedUnit.IsOwnedByCurrentPlayer() == false) {
                return;
            }
            SelectedUnit.GoIdle();
        }

        public void OnDestroy() {
            Instance = null;
        }

        public Tile GetTileUnderneathMouse() {
            return World.Current.GetTileAt(CurrentFramePositionOffset);
        }

        public Transform MouseRayCast() {
            return Physics2D.Raycast(new Vector2(CurrentFramePosition.x, CurrentFramePosition.y), Vector2.zero, 200).transform;
        }

        /// <summary>
        /// what to on escape press
        ///  - set tobuildstructure to null
        ///  - set mousestate to drag
        /// </summary>
        public void Escape() {
            UnselectStuff(true);
            ResetBuild();
            SetMouseState(MouseState.Idle);
            SetMouseUnitState(MouseUnitState.None);
        }
        public void SetCopyMode(bool on) {
            if(on) {
                Escape();
                SetMouseState(MouseState.Copy);
            }
            else {
                if(MouseState == MouseState.Copy)
                    SetMouseState(MouseState.Idle);
            }
        }

        public static void ChangeCursorType(CursorType type) {
            Sprite s = UISpriteController.GetUISprite("Cursor_" + type.ToString());
            Cursor.SetCursor(s.texture, s.pivot, CursorMode.Auto);
        }

        internal void ResetBuildCost() {
            NeededItemsToBuild = null;
            NeededBuildCost = 0;
        }

        [Serializable]
        public struct ExtraStructureBuildUI {
            public ExtraBuildUI Type;
            public GameObject Prefab;
        }

    }
}