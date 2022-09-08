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
        private Dictionary<ExtraBuildUI, GameObject> _extraStructureBuildUIPrefabs;
        private GameObject _highlightGO;

        /// <summary>
        /// If it is in either build or destroy mode
        /// </summary>
        bool IsInBuildDestoyMode => MouseState == (MouseState.BuildDrag | MouseState.BuildPath | MouseState.BuildSingle | MouseState.Destroy);

        private bool _displayDragRectangle;

        /// <summary>
        /// The world-position of the mouse last frame.
        /// </summary>
        private Vector3 _lastFramePosition;
        /// <summary>
        /// The world-position of the mouse last frame with TileOffset.
        /// </summary>
        private Vector3 _currentFramePositionOffset => _currentFramePosition 
                                                    + new Vector3(TileSpriteController.offset, TileSpriteController.offset, 0);
        private Vector3 _lastFrameGuiPosition;
        private Vector3 _currentFramePosition;
        private Vector3 _dragStartPosition;
        private Vector3 _pathStartPosition;

        public static bool Autorotate = true;

        /// <summary>
        ///  is true if smth is overriding the current states and commands for units
        /// </summary>
        public static bool OverrideCurrentSetting => InputHandler.ShiftKey == false; // TODO: better name

        public Vector2 MapClampedMousePosition =>
            new Vector2(Mathf.Clamp(_currentFramePosition.x, 0, World.Current.Width),
                Mathf.Clamp(_currentFramePosition.y, 0, World.Current.Height));

        private HashSet<Tile> _destroyTiles;
        private Dictionary<Tile, TilePreview> _tileToPreviewGO;
        private Dictionary<Tile, StructurePreview> _tileToStructurePreview;
        private GameObject _singleStructurePreview;
        private Structure _selectedStructure;
        private BuildPathAgent _buildPathAgent;
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
                    ResetStructurePreviews();
                _toBuildstructure = value;
            }
        }

        public Item[] NeededItemsToBuild;
        public int NeededBuildCost;

        public MouseState MouseState { get; protected set; } = MouseState.Idle;
        public MouseUnitState MouseUnitState { get; protected set; } = MouseUnitState.None;

        private Unit _selectedUnit;
        public List<Unit> selectedUnitGroup;
        private Rect _drawRect;
        private bool _mouseStateIdleLeftMouseDown;
        private PathJob _buildPathJob;

        public Unit SelectedUnit {
            get => _selectedUnit;
            protected set {
                _selectedUnit?.UnregisterOnDestroyCallback(OnUnitDestroy);
                _selectedUnit = value;
                SetMouseUnitState(_selectedUnit == null ? MouseUnitState.None : MouseUnitState.Normal);
            }
        }

        public IGEventable CurrentlySelectedIGEventable {
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
            _tileToPreviewGO = new Dictionary<Tile, TilePreview>();
            _tileToStructurePreview = new Dictionary<Tile, StructurePreview>();
            _destroyTiles = new HashSet<Tile>();
            _extraStructureBuildUIPrefabs = new Dictionary<ExtraBuildUI, GameObject>();
            foreach (ExtraStructureBuildUI esbu in extraStructureBuildUIPrefabsEditor) {
                _extraStructureBuildUIPrefabs[esbu.Type] = esbu.Prefab;
            }
            _buildPathAgent = new BuildPathAgent(PlayerController.currentPlayerNumber);
            PlayerController.Instance.cbPlayerChange += (a,b)=>{ _buildPathAgent = new BuildPathAgent(PlayerController.currentPlayerNumber); };
        }

        /// <summary>
        /// Gets the mouse position in world space.
        /// </summary>
        public Vector3 GetMousePosition() {
            return _currentFramePositionOffset;
        }

        /// <summary>
        /// Gets the mouse position in world space.
        /// </summary>
        public Vector3 GetLastMousePosition() {
            return _lastFramePosition;
        }

        private System.Diagnostics.Stopwatch _stopWatch;

        public void Update() {
            if (EditorController.IsEditor == false && PlayerController.Instance.GameOver)
                return;
            _stopWatch = new System.Diagnostics.Stopwatch();
            _stopWatch.Start();

            _currentFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _currentFramePosition.z = 0;
            if (_currentFramePosition.y < 0 || _currentFramePosition.x < 0) {
                return;
            }
            UpdateMouseStates();
            if (EditorController.IsEditor == false) {
                UpdateDragBoxSelect();
            }
            else {
                UpdateEditorStuff();
            }

            if (InputHandler.GetMouseButtonDown(InputMouse.Secondary) && MouseState != MouseState.Idle
                && MouseState != MouseState.Unit && MouseState != MouseState.UnitGroup) {
                ResetBuild();
                SetMouseState(MouseState.Idle);
            }

            // Save the mouse position from this frame
            // We don't use currFramePosition because we may have moved the camera.
            _lastFrameGuiPosition = Input.mousePosition;
            _lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _lastFramePosition.z = 0;
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
            switch (state) {
                case MouseState.Idle:
                    ChangeCursorType(CursorType.Pointer);
                    break;

                case MouseState.BuildDrag:
                case MouseState.BuildPath:
                case MouseState.BuildSingle:
                    ChangeCursorType(CursorType.Build);
                    break;

                case MouseState.Unit:
                    break;

                case MouseState.UnitGroup:
                    break;

                case MouseState.Copy:
                    ChangeCursorType(CursorType.Copy);
                    break;

                case MouseState.Destroy:
                    ChangeCursorType(CursorType.Destroy);
                    break;

                case MouseState.DragSelect:
                    break;

                case MouseState.Upgrade:
                    ChangeCursorType(CursorType.Upgrade);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
            switch (MouseState) {
                case MouseState.Idle:
                    break;

                case MouseState.BuildDrag:
                case MouseState.BuildPath:
                case MouseState.BuildSingle:
                    NeededItemsToBuild = null;
                    NeededBuildCost = 0;
                    UI.Model.IslandInfoUI.Instance.ResetAddons();
                    break;

                case MouseState.Unit:
                    break;

                case MouseState.UnitGroup:
                    break;

                case MouseState.Copy:
                    break;

                case MouseState.Destroy:
                    break;

                case MouseState.DragSelect:
                    break;

                case MouseState.Upgrade:
                    NeededItemsToBuild = null;
                    NeededBuildCost = 0;
                    UI.Model.IslandInfoUI.Instance.ResetAddons();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            switch (MouseState) {
                case MouseState.Idle:
                    if(EventSystem.current.IsPointerOverGameObject() == false && InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                        //If some clicks down onto a ui and then goes off it and releases the mouse 
                        //we do not want to open or close where the mouse ends up 
                        _mouseStateIdleLeftMouseDown = true;
                    }
                    if (_mouseStateIdleLeftMouseDown && InputHandler.GetMouseButtonUp(InputMouse.Primary) 
                            && EditorController.IsEditor == false) {
                        //mouse press decide what it hit
                        DecideWhatUIToShow(MouseRayCast());
                    }
                    break;

                case MouseState.BuildDrag:
                    UpdateBuildDragging();
                    break;

                case MouseState.BuildPath:
                    UpdateBuildPath();
                    break;

                case MouseState.BuildSingle:
                    UpdateSingle();
                    break;

                case MouseState.Unit:
                    if (SelectedUnit == null) {
                        SetMouseState(MouseState.Idle);
                        return;
                    }
                    UpdateUnit();
                    break;

                case MouseState.DragSelect:
                    UpdateDragSelect();
                    break;

                case MouseState.Destroy:
                    UpdateDestroy();
                    break;

                case MouseState.UnitGroup:
                    UpdateUnitGroup();
                    break;

                case MouseState.Copy:
                    UpdateCopyState();
                    break;

                case MouseState.Upgrade:
                    UpdateUpgradeState();
                    break;
            }
        }
        /// <summary>
        /// Responsible for detecting a drag not in Build/Destroy Mode 
        /// </summary>
        private void UpdateDragBoxSelect() {
            if (IsInBuildDestoyMode == false)
                return;
            if (InputHandler.GetMouseButton(InputMouse.Primary) && _displayDragRectangle == false) {
                if (EventSystem.current.IsPointerOverGameObject() == false && ShortcutUI.Instance.IsDragging == false) {
                    float sqrdist = (Input.mousePosition - _lastFrameGuiPosition).sqrMagnitude;
                    if (sqrdist > 5) {
                        _dragStartPosition = _currentFramePosition;
                        //SetMouseState(MouseState.DragSelect);
                        _displayDragRectangle = true;
                    }
                }
            }
            if (_displayDragRectangle)
                UpdateDragSelect();
        }

        private void UpdateCopyState() {
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) == false) return;
            Tile t = GetTileUnderneathMouse();
            if (t.Structure == null)
                return;
            if (t.Structure.CanBeBuild == false)
                return;
            BuildController.Instance.StartStructureBuild(t.Structure.ID);
        }

        private void UpdateUpgradeState() {
            Tile t = GetTileUnderneathMouse();
            NeededItemsToBuild = null;
            NeededBuildCost = 0;
            if (t.Structure == null)
                return;
            if (t.Structure.CanBeUpgraded == false)
                return;
            Structure upgradeTo = null;
            foreach (string item in t.Structure.CanBeUpgradedTo) {
                if (t.Structure is HomeStructure == false && PlayerController.CurrentPlayer.HasStructureUnlocked(item) == false)
                    continue;
                upgradeTo = PrototypController.Instance.GetStructure(item);
                NeededItemsToBuild = upgradeTo.BuildingItems?.CloneArrayWithCounts();
                NeededBuildCost = upgradeTo.BuildCost;
                break;
            }
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) && upgradeTo != null) {
                BuildController.Instance.BuildOnTile(upgradeTo, t.Structure.Tiles, PlayerController.currentPlayerNumber, false);
            }
        }
        public void UnselectStuff(bool escape = false) {
            UnselectUnit();
            UnselectUnitGroup();
            UnselectStructure();
            if(escape == false)
                UIController.Instance.CloseMouseUnselect();
        }

        private void UpdateDestroy() {
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                _dragStartPosition = _currentFramePositionOffset;
            }
            int startX = Mathf.FloorToInt(_dragStartPosition.x);
            int endX = Mathf.FloorToInt(_currentFramePositionOffset.x);
            int startY = Mathf.FloorToInt(_dragStartPosition.y);
            int endY = Mathf.FloorToInt(_currentFramePositionOffset.y);
            if (InputHandler.GetMouseButton(InputMouse.Primary)) {
                List<Tile> tiles = GetTilesStructures(startX, endX, startY, endY);
                foreach (Tile t in _destroyTiles.Except(tiles).ToArray()) {
                    SimplePool.Despawn(_tileToPreviewGO[t].gameObject);
                    _tileToPreviewGO.Remove(t);
                    _destroyTiles.Remove(t);
                }
                foreach (Tile t in tiles) {
                    if (_destroyTiles.Contains(t))
                        continue;
                    ShowTilePrefabOnTile(t, TileHighlightType.Red);
                    _destroyTiles.Add(t);
                }
            }

            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) == false) return;
            List<Tile> ts = new List<Tile>(GetTilesStructures(startX, endX, startY, endY));
            if (ts.Count > 0) {
                bool isGod = EditorController.IsEditor || IsGod; //TODO: add cheat to set this
                BuildController.Instance.DestroyStructureOnTiles(ts, PlayerController.CurrentPlayer, isGod);
            }
            ResetBuild(false);
        }

        private void UpdateUnitGroup() {
            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                switch (MouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;

                    case MouseUnitState.Normal:
                        UnselectUnitGroup();
                        break;

                    case MouseUnitState.Patrol:
                        selectedUnitGroup.ForEach(x => x.AddPatrolCommand(MapClampedMousePosition.x, MapClampedMousePosition.y));
                        break;

                    case MouseUnitState.Build:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            CheckUnitCursor();
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary) == false) return;
            Transform hit = MouseRayCast();
            if (hit == null) {
                switch (MouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;

                    case MouseUnitState.Normal:
                        selectedUnitGroup.ForEach(x => x.GiveMovementCommand(MapClampedMousePosition.x,
                            MapClampedMousePosition.y, OverrideCurrentSetting));
                        break;

                    case MouseUnitState.Patrol:
                        SetMouseUnitState(MouseUnitState.Normal);
                        break;
                    case MouseUnitState.Build:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else {
                ITargetableHoldingScript targetableHoldingScript = hit.GetComponent<ITargetableHoldingScript>();
                if (targetableHoldingScript != null) {
                    selectedUnitGroup.ForEach(x =>
                        x.GiveAttackCommand(hit.gameObject.GetComponent<ITargetableHoldingScript>().Holding,
                            OverrideCurrentSetting));
                }
                else if (hit.GetComponent<CrateHoldingScript>() != null) {
                    //TODO: maybe nearest? other logic? air distance??
                    selectedUnitGroup[0].TryToAddCrate(hit.GetComponent<CrateHoldingScript>().thisCrate);
                }
                else if (targetableHoldingScript == null) {
                    Tile t = GetTileUnderneathMouse();
                    switch (t.Structure) {
                        case null:
                            return;
                        case ICapturable ic:
                            selectedUnitGroup.ForEach(x => x.GiveCaptureCommand(ic, OverrideCurrentSetting));
                            break;
                        case TargetStructure ts:
                            selectedUnitGroup.ForEach(x => x.GiveAttackCommand(ts, OverrideCurrentSetting));
                            break;
                    }
                }
            }
        }
        public void ShowError(MapErrorMessage message) {
            ShowError(message, _currentFramePosition);
        }
        public void ShowError(MapErrorMessage message, Vector3 position) {
            TextMeshPro text = SimplePool.Spawn(fadeOutTextPrefab, position, Quaternion.identity).GetComponent<TextMeshPro>();
            text.fontSize = Mathf.Max(8.333f * (CameraController.Instance.zoomLevel / CameraController.MaxZoomLevel), 2);
            text.text = UILanguageController.Instance.GetTranslation(message);
            //text.transform.SetParent(transform);
            StartCoroutine(DespawnFade(text));
        }

        private IEnumerator DespawnFade(TextMeshPro text) {
            //while(text.color.a>0) {
            yield return new WaitForSeconds(1f);
            //}
            SimplePool.Despawn(text.gameObject);
        }
        /// <summary>
        /// Calculates the selectbox and the interaction on exit.
        /// </summary>
        private void UpdateDragSelect() {
            // End Drag
            if (InputHandler.GetMouseButton(InputMouse.Primary) == false) {
                Vector3 v1 = _dragStartPosition;
                Vector3 v2 = _lastFramePosition;
                v1.z = 0;
                v2.z = 0;
                Vector3 min = Vector3.Min(v1, v2);
                Vector3 max = Vector3.Max(v1, v2);
                Vector3 dimensions = max - min;
                Collider2D[] c2d = Physics2D.OverlapBoxAll(min + dimensions / 2, dimensions, 0);
                if (OverrideCurrentSetting)
                    selectedUnitGroup.Clear();
                foreach (Collider2D c in c2d) {
                    ITargetableHoldingScript target = c.GetComponent<ITargetableHoldingScript>();
                    if (target == null)
                        continue;
                    if (target.IsUnit == false)
                        continue;
                    if (target.Holding.PlayerNumber != PlayerController.currentPlayerNumber) continue;
                    Unit u = ((Unit)target.Holding);
                    if (selectedUnitGroup.Contains(u) == false)
                        selectedUnitGroup.Add(u);
                }
                if (selectedUnitGroup.Count > 1)
                    SelectUnitGroup(selectedUnitGroup);
                else if (selectedUnitGroup.Count == 1)
                    SelectUnit(selectedUnitGroup[0]);
                else {
                    SetMouseState(MouseState.Idle);// nothing selected
                    UnselectStuff();
                }
                _drawRect = Rect.zero;
                _displayDragRectangle = false;
            }

            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            // Drag already started

            Vector3 screenPosition1 = Camera.main.WorldToScreenPoint(_dragStartPosition);
            Vector3 screenPosition2 = _lastFrameGuiPosition;
            screenPosition1.y = Screen.height - screenPosition1.y;
            screenPosition2.y = Screen.height - screenPosition2.y;

            // Calculate corners
            var topLeft = Vector3.Min(screenPosition1, screenPosition2);
            var bottomRight = Vector3.Max(screenPosition1, screenPosition2);
            // Create Rect
            _drawRect = Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
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
            if (_displayDragRectangle)
                Util.DrawScreenRectBorder(_drawRect, 2, new Color(0.9f, 0.9f, 0.9f, 0.9f));
        }
        /// <summary>
        /// OnClick on Map. If it hits unit or structure. UIControllers decides then which UI.
        /// If nothing close UI(s).
        /// </summary>
        /// <param name="hit"></param>
        private void DecideWhatUIToShow(Transform hit) {
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
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
            SetMouseState(MouseState.Unit);
            SetMouseUnitState(MouseUnitState.Normal);
            SelectedUnit = unit;
            SelectedUnit.RegisterOnDestroyCallback(OnUnitDestroy);
            UIController.Instance.OpenUnitUI(SelectedUnit);
            UIDebug(SelectedUnit);
        }

        public void SelectUnitGroup(List<Unit> units) {
            if(units.Count == 1) {
                SelectUnit(units[0]);
                return;
            }
            SetMouseState(MouseState.UnitGroup);
            SetMouseUnitState(MouseUnitState.Normal);
            selectedUnitGroup = units;
            selectedUnitGroup.ForEach(x => x.RegisterOnDestroyCallback(OnUnitDestroy));
            UIController.Instance.OpenUnitGroupUI(selectedUnitGroup);
        }
        /// <summary>
        /// Single Structure Build Mode updating. 
        /// </summary>
        private void UpdateSingle() {
            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            if (ToBuildStructure == null) {
                return;
            }
            UpdateSinglePreview();
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary) == false) return;
            List<Tile> structureTiles = ToBuildStructure.GetBuildingTiles(GetTileUnderneathMouse());
            Build(structureTiles);
        }
        /// <summary>
        /// Change the location and rotation of a single preview.
        /// </summary>
        private void UpdateSinglePreview() {
            if (_singleStructurePreview == null)
                _singleStructurePreview = CreatePreviewStructure();
            float x = ToBuildStructure.TileWidth / 2f - TileSpriteController.offset;
            float y = ToBuildStructure.TileHeight / 2f - TileSpriteController.offset;
            _singleStructurePreview.transform.position = new Vector3(GetTileUnderneathMouse().X + x,
                                                       GetTileUnderneathMouse().Y + y, 0);
            _singleStructurePreview.transform.eulerAngles = new Vector3(0, 0, 360 - ToBuildStructure.Rotation);
            List<Tile> tiles = ToBuildStructure.GetBuildingTiles(GetTileUnderneathMouse());
            foreach (Tile tile in _tileToPreviewGO.Keys.Except(tiles).ToArray()) {
                SimplePool.Despawn(_tileToPreviewGO[tile].gameObject);
                _tileToPreviewGO.Remove(tile);
            }
            UpdateStructurePreview(tiles, 1);
            NeededItemsToBuild = ToBuildStructure.BuildingItems?.CloneArrayWithCounts();
            NeededBuildCost = ToBuildStructure.BuildCost;
        }
        /// <summary>
        /// Calculates for the selected structure where and how the structures fit in the dragged rectangle.
        /// </summary>
        private void UpdateBuildDragging() {
            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            // Start Drag
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                _dragStartPosition = _currentFramePositionOffset;
                if (_singleStructurePreview != null) {
                    SimplePool.Despawn(_singleStructurePreview);
                    _singleStructurePreview = null;
                }
            }
            int startX = Mathf.FloorToInt(_dragStartPosition.x);
            int endX = Mathf.FloorToInt(_currentFramePositionOffset.x);
            int startY = Mathf.FloorToInt(_dragStartPosition.y);
            int endY = Mathf.FloorToInt(_currentFramePositionOffset.y);
            List<Tile> ts = GetTilesStructures(startX, endX, startY, endY);
            if (InputHandler.GetMouseButton(InputMouse.Primary)) {
                // Display a preview of the drag area
                UpdateMultipleStructurePreviews(ts);
            }
            else {
                UpdateSinglePreview();
            }
            // End Drag
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) == false) return;
            Build(ts, true, true);
            ResetStructurePreviews();
        }
        /// <summary>
        /// Update the the Pathfinding with the start and end position and then display foreach position a prefab.
        /// </summary>
        private void UpdateBuildPath() {
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            // Start Path
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                _pathStartPosition = _currentFramePositionOffset;
                if (_singleStructurePreview != null) {
                    ResetStructurePreviews();
                }
            }
            if (InputHandler.GetMouseButton(InputMouse.Primary)) {
                int startX = Mathf.FloorToInt(_pathStartPosition.x);
                int startY = Mathf.FloorToInt(_pathStartPosition.y);
                Tile pathStartTile = World.Current.GetTileAt(startX, startY);

                if (pathStartTile == null || pathStartTile.Island == null) {
                    return;
                }
                int endX = Mathf.FloorToInt(_currentFramePositionOffset.x);
                int endY = Mathf.FloorToInt(_currentFramePositionOffset.y);
                Tile pathEndTile = World.Current.GetTileAt(endX, endY);
                if (pathEndTile == null) {
                    return;
                }
                if (pathStartTile.Island != null && pathEndTile.Island != null &&
                        (_buildPathJob == null || _buildPathJob.End != pathEndTile.Vector2)) {
                    _buildPathJob = new PathJob(_buildPathAgent, pathStartTile.Island.Grid, pathStartTile.Vector2, pathEndTile.Vector2);
                    PathfindingThreadHandler.EnqueueJob(_buildPathJob, null, true);
                    if(_buildPathJob.Path != null)
                        UpdateMultipleStructurePreviews(World.Current.GetTilesQueue(_buildPathJob.Path));
                }
            }
            else {
                UpdateSinglePreview();
            }
            // End path
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) == false) return;
            ResetStructurePreviews();
            if (_buildPathJob == null || _buildPathJob.Status != JobStatus.Done) {
                return;
            }
            Build(World.Current.GetTilesQueue(_buildPathJob.Path).ToList(), true);
        }
        /// <summary>
        /// Updates which previews are to be deleted and where to create new ones.
        /// </summary>
        /// <param name="tiles"></param>
        private void UpdateMultipleStructurePreviews(IEnumerable<Tile> tiles) {
            foreach (Tile tile in _tileToStructurePreview.Keys.Except(tiles).ToArray()) {
                SimplePool.Despawn(_tileToStructurePreview[tile].gameObject);
                foreach (Tile t in _tileToStructurePreview[tile].tiles) {
                    SimplePool.Despawn(_tileToPreviewGO[t].gameObject);
                    _tileToPreviewGO.Remove(t);
                }
                _tileToStructurePreview.Remove(tile);
            }
            foreach (Tile tile in tiles) {
                if (_tileToStructurePreview.ContainsKey(tile)) continue;
                StructurePreview preview = new StructurePreview(
                    tile,
                    CreatePreviewStructure(tile),
                    ToBuildStructure.GetBuildingTiles(tile), _tileToStructurePreview.Count + 1
                );
                _tileToStructurePreview[tile] = preview;
            }
            NeededItemsToBuild = ToBuildStructure.BuildingItems?.CloneArrayWithCounts(tiles.Count());
            NeededBuildCost = ToBuildStructure.BuildCost * tiles.Count();
            foreach (StructurePreview preview in _tileToStructurePreview.Values) {
                if (ToBuildStructure is RoadStructure) {
                    string sprite = ToBuildStructure.SpriteName + RoadStructure.UpdateOrientation(preview.tile, tiles);
                    preview.spriteRenderer.sprite = StructureSpriteController.Instance.GetStructureSprite(sprite);
                }
                UpdateStructurePreview(preview.tiles, preview.number);
            }
        }
        /// <summary>
        /// Updates the Preview Tiles based on if the player has enough ressources, monemy or if the enough citytiles
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="number"></param>
        private void UpdateStructurePreview(List<Tile> tiles, int number) {
            if (EditorController.IsEditor) {
                UpdateStructurePreviewTiles(tiles, true);
                return;
            }
            bool hasEnoughResources = PlayerController.CurrentPlayer.HasEnoughMoney(ToBuildStructure.BuildCost * number);
            if(MouseUnitState == MouseUnitState.Build) {
                hasEnoughResources &= SelectedUnit.Inventory.HasEnoughOfItems(ToBuildStructure.BuildingItems, times: number) == true;
            } else {
                hasEnoughResources &= tiles[0].Island?.FindCityByPlayer(PlayerController.currentPlayerNumber)?
                            .HasEnoughOfItems(ToBuildStructure.BuildingItems, number) == true;
            }
            UpdateStructurePreviewTiles(tiles, hasEnoughResources);
        }
        /// <summary>
        /// Updates the Preview Tiles red/green bassed on override or if it can be build on that tile
        /// if <paramref name="dontOverrideTile"/> is false it will make it red - if true it does not influenz it
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="dontOverrideTile"></param>
        private void UpdateStructurePreviewTiles(List<Tile> tiles, bool dontOverrideTile) {
            Dictionary<Tile, bool> tileToCanBuild = ToBuildStructure.CheckForCorrectSpot(tiles);
            if (MouseState == MouseState.BuildSingle && Autorotate) {
                int i = 0;
                while(tileToCanBuild.ContainsValue(false) && i < 4) {
                    ToBuildStructure.Rotate();
                    tiles = ToBuildStructure.GetBuildingTiles(GetTileUnderneathMouse());
                    //TODO: think about a not so ugly solution for autorotate
                    tileToCanBuild = ToBuildStructure.CheckForCorrectSpot(tiles);
                    i++;
                }
            }
            dontOverrideTile &= EditorController.IsEditor || ToBuildStructure.InCityCheck(tiles, PlayerController.currentPlayerNumber);
            foreach (Tile tile in tiles) {
                bool specialTileCheck = true;
                if (MouseUnitState == MouseUnitState.Build) {
                    specialTileCheck = SelectedUnit.IsTileInBuildRange(tile);
                }
                bool canBuild = dontOverrideTile && specialTileCheck && tileToCanBuild[tile];
                canBuild &= EditorController.IsEditor || Structure.IsTileCityViable(tile, PlayerController.currentPlayerNumber);
                canBuild &= tile.Island != null && tile.Island.HasNegativeEffect == false;
                ShowTilePrefabOnTile(tile, canBuild ? TileHighlightType.Green : TileHighlightType.Red);
            }
        }

        private void UIDebug(object obj) {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift)
                        && SaveController.DebugModeSave) {
                UIController.Instance.ShowDebugForObject(obj);
            }
        }

        public GameObject CreatePreviewStructure(Tile tile = null) {
            Vector3 position = Vector3.zero;
            if (tile != null) {
                position.x = ((float)ToBuildStructure.TileWidth) / 2f - TileSpriteController.offset;
                position.y = ((float)ToBuildStructure.TileHeight) / 2f - TileSpriteController.offset;
                position += tile.Vector;
            }
            GameObject previewGO = SimplePool.Spawn(structurePreviewRendererPrefab, position, Quaternion.Euler(0, 0, 360 - ToBuildStructure.Rotation));
            //previewGO.transform.SetParent(this.transform, true);
            if (ToBuildStructure.ExtraBuildUITyp != ExtraBuildUI.None) {
                if (_extraStructureBuildUIPrefabs.ContainsKey(ToBuildStructure.ExtraBuildUITyp) == false)
                    Debug.LogError(ToBuildStructure.ExtraBuildUITyp + " ExtraBuildPreview has no Prefab assigned!");
                else {
                    GameObject extra = Instantiate(_extraStructureBuildUIPrefabs[ToBuildStructure.ExtraBuildUITyp]);
                    extra.transform.SetParent(previewGO.transform);
                }
            }

            SpriteRenderer sr = previewGO.GetComponent<SpriteRenderer>();
            sr.sprite = StructureSpriteController.Instance.GetStructureSprite(ToBuildStructure);
            AddRangeHighlight(previewGO);
            return previewGO;
        }
        /// <summary>
        /// Decides if new prefab tile needs to be spawned or despwaned.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="type"></param>
        private void ShowTilePrefabOnTile(Tile t, TileHighlightType type) {
            if (_tileToPreviewGO.ContainsKey(t)) {
                if (_tileToPreviewGO[t].HighlightType == type) {
                    return;
                }
                else {
                    SimplePool.Despawn(_tileToPreviewGO[t].gameObject);
                    _tileToPreviewGO.Remove(t);
                }
            }

            GameObject go = type switch {
                TileHighlightType.Green => greenTileCursorPrefab,
                TileHighlightType.Red => redTileCursorPrefab,
                _ => null
            };
            go = SimplePool.Spawn(go, new Vector3(t.X + 0.5f, t.Y + 0.5f, 0), Quaternion.identity);
            // Display the building hint on top of this tile position
            //go.transform.SetParent(this.transform, true);
            _tileToPreviewGO.Add(t, new TilePreview(type, go));
        }

        private void AddRangeHighlight(GameObject parent) {
            if (ToBuildStructure.StructureRange == 0)
                return;
            int range = ToBuildStructure.StructureRange * 2; // cause its the radius
            int width = range + ToBuildStructure.TileWidth;
            int height = range + ToBuildStructure.TileHeight;
            GetHighlightGameObject(width, height, ToBuildStructure.PrototypeTiles).transform.SetParent(parent.transform);
        }

        private GameObject GetHighlightGameObject(int width, int height, IEnumerable<Tile> tiles) {
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
        /// <summary>
        /// IF unit is selected update based on mouse interactions, e.g. move/attack/patrol.
        /// </summary>
        private void UpdateUnit() {
            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            //TEMPORARY FOR TESTING
            if (Input.GetKeyDown(KeyCode.U)) {
                if (SelectedUnit.IsShip) {
                    ((Ship)SelectedUnit).ShotAtPosition(_currentFramePosition);
                }
            }
            Transform hit = MouseRayCast();
            CheckUnitCursor();
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary)) {
                switch (MouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;

                    case MouseUnitState.Normal:
                        //TODO: Better way?
                        if (hit) {
                            ITargetableHoldingScript iths = hit.GetComponent<ITargetableHoldingScript>();
                            if (iths != null) {
                                if (iths.Holding == SelectedUnit) {
                                    return;
                                }
                            }
                        }
                        UnselectUnit();
                        break;

                    case MouseUnitState.Patrol:
                        SelectedUnit.AddPatrolCommand(MapClampedMousePosition.x, MapClampedMousePosition.y);
                        break;

                    case MouseUnitState.Build:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (InputHandler.GetMouseButtonDown(InputMouse.Secondary) == false) return;
            if (SelectedUnit.PlayerNumber != PlayerController.currentPlayerNumber) {
                SetMouseState(MouseState.Idle);
                return;
            }

            if (hit == null) {
                switch (MouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;

                    case MouseUnitState.Normal:
                        SelectedUnit.GiveMovementCommand(MapClampedMousePosition.x, MapClampedMousePosition.y,
                            OverrideCurrentSetting);
                        break;

                    case MouseUnitState.Patrol:
                        SetMouseUnitState(MouseUnitState.Normal);
                        break;

                    case MouseUnitState.Build:
                        ResetBuild();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else {
                ITargetableHoldingScript targetableHoldingScript = hit.GetComponent<ITargetableHoldingScript>();
                if (targetableHoldingScript != null) {
                    SelectedUnit.GiveAttackCommand(targetableHoldingScript.Holding, OverrideCurrentSetting);
                }
                else if (hit.GetComponent<CrateHoldingScript>() != null) {
                    SelectedUnit.GivePickUpCrateCommand(hit.GetComponent<CrateHoldingScript>().thisCrate,
                        OverrideCurrentSetting);
                }
                else if (targetableHoldingScript == null) {
                    Tile t = GetTileUnderneathMouse();
                    switch (t.Structure) {
                        case null:
                            return;
                        case ICapturable structure:
                            SelectedUnit.GiveCaptureCommand(structure, OverrideCurrentSetting);
                            break;
                        case TargetStructure tStructure:
                            SelectedUnit.GiveAttackCommand(tStructure, OverrideCurrentSetting);
                            break;
                    }
                }
            }
        }

        private void CheckUnitCursor() {
            if (SelectedUnit.IsOwnedByCurrentPlayer() == false)
                return;
            if (MouseUnitState == MouseUnitState.Build) return;
            Transform hit = MouseRayCast();
            bool attackAble = false;
            if (hit) {
                ITargetableHoldingScript iths = hit.GetComponent<ITargetableHoldingScript>();
                if (iths != null) {
                    attackAble = PlayerController.Instance.ArePlayersAtWar(PlayerController.currentPlayerNumber, iths.PlayerNumber);
                    if (SelectedUnit != iths.Holding
                        && PlayerController.currentPlayerNumber == iths.PlayerNumber
                        && SelectedUnit.IsUnit == iths.IsUnit) {
                        ChangeCursorType(CursorType.Escort);
                        return;
                    }
                }
            }
            Structure str = GetTileUnderneathMouse()?.Structure;
            if (str is TargetStructure) {
                attackAble = PlayerController.Instance.ArePlayersAtWar(PlayerController.currentPlayerNumber, str.PlayerNumber);
            }
            ChangeCursorType(attackAble ? CursorType.Attack : CursorType.Pointer);
        }

        public void UnselectUnit(bool closeUI = true) {
            if (SelectedUnit == null)
                return;
            SelectedUnit.UnregisterOnDestroyCallback(OnUnitDestroy);
            SelectedUnit = null;
            UnselectStructure();
            if (closeUI)
                UIController.Instance.CloseInfoUI();
            SetMouseState(MouseState.Idle);
            SetMouseUnitState(MouseUnitState.None);
        }
        /// <summary>
        /// Calculates for the given rectangle which tiles are the buildtiles for selected structure.
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="endX"></param>
        /// <param name="startY"></param>
        /// <param name="endY"></param>
        /// <returns></returns>
        private List<Tile> GetTilesStructures(int startX, int endX, int startY, int endY) {
            int width = 1;
            int height = 1;
            List<Tile> tiles = new List<Tile>();
            if (ToBuildStructure != null) {
                width = ToBuildStructure.TileWidth;
                height = ToBuildStructure.TileHeight;
            }
            if (endX >= startX && endY >= startY) {
                for (int x = startX; x <= endX; x += width) {
                    for (int y = startY; y <= endY; y += height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            else
            if (endX > startX && endY <= startY) {
                for (int x = startX; x <= endX; x += width) {
                    for (int y = startY; y >= endY; y -= height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            else
            if (endX <= startX && endY > startY) {
                for (int x = startX; x >= endX; x -= width) {
                    for (int y = startY; y <= endY; y += height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            else
            if (endX <= startX && endY <= startY) {
                for (int x = startX; x >= endX; x -= width) {
                    for (int y = startY; y >= endY; y -= height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            return tiles;
        }
        /// <summary>
        /// Send the build command to the buildcontroller based on what the player has selected.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="single"></param>
        /// <param name="previewOrder"></param>
        private void Build(List<Tile> t, bool single = false, bool previewOrder = false) {
            if (EditorController.IsEditor) {
                EditorController.Instance.BuildOn(t, single);
            }
            else {
                if (MouseUnitState == MouseUnitState.Build) {
                    BuildController.Instance.CurrentPlayerBuildOnTile(t, single, PlayerController.currentPlayerNumber, false, SelectedUnit);
                }
                else {
                    if (previewOrder) {
                        foreach (StructurePreview sp in _tileToStructurePreview.Values.OrderBy(x => x.number)) {
                            BuildController.Instance.CurrentPlayerBuildOnTile(sp.tiles, false, PlayerController.currentPlayerNumber, false);
                        }
                    }
                    else {
                        BuildController.Instance.CurrentPlayerBuildOnTile(t, single, PlayerController.currentPlayerNumber, false);
                    }
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
            ResetStructurePreviews();
            NeededBuildCost = 0;
            NeededItemsToBuild = null;
            _destroyTiles.Clear();
            ToBuildStructure = null;
            if (MouseUnitState == MouseUnitState.Build) {
                UnselectUnit();
            }
        }

        public void ResetStructurePreviews() {
            foreach (Tile tile in _tileToStructurePreview.Keys) {
                foreach (Transform t in _tileToStructurePreview[tile].gameObject.transform) {
                    Destroy(t.gameObject);
                }
                SimplePool.Despawn(_tileToStructurePreview[tile].gameObject);
            }
            _tileToStructurePreview.Clear();
            foreach (Tile t in _tileToPreviewGO.Keys) {
                SimplePool.Despawn(_tileToPreviewGO[t].gameObject);
            }
            _tileToPreviewGO.Clear();
            if (_singleStructurePreview == false) return;
            SimplePool.Despawn(_singleStructurePreview);
            foreach (Transform t in _singleStructurePreview.transform) {
                Destroy(t.gameObject);
            }

            _singleStructurePreview = null;
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
            return World.Current.GetTileAt(_currentFramePositionOffset);
        }

        private Transform MouseRayCast() {
            return Physics2D.Raycast(new Vector2(_currentFramePosition.x, _currentFramePosition.y), Vector2.zero, 200).transform;
        }

        /// <summary>
        /// what to on escape press
        ///  - set tobuildstructure to null
        ///  - set mousestate to drag
        /// </summary>
        public void Escape() {
            _dragStartPosition = _currentFramePosition;
            UnselectStuff(true);
            ResetBuild();
            SetMouseState(MouseState.Idle);
            SetMouseUnitState(MouseUnitState.None);
            ChangeCursorType(CursorType.Pointer);
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

        [Serializable]
        public struct ExtraStructureBuildUI {
            public ExtraBuildUI Type;
            public GameObject Prefab;
        }

        private class TilePreview {
            public readonly GameObject gameObject;
            public readonly TileHighlightType HighlightType;

            public TilePreview(TileHighlightType type, GameObject gameObject) {
                HighlightType = type;
                this.gameObject = gameObject;
            }
        }

        private class StructurePreview {
            public readonly GameObject gameObject;
            public readonly List<Tile> tiles;
            public readonly Tile tile;
            public readonly int number;
            public readonly SpriteRenderer spriteRenderer;

            public StructurePreview(Tile tile, GameObject gameObject, List<Tile> tiles, int number) {
                this.tile = tile;
                this.gameObject = gameObject;
                spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                this.tiles = tiles;
                this.number = number;
            }
        }
    }
}