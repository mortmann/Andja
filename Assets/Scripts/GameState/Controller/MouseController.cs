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
        private Dictionary<ExtraBuildUI, GameObject> ExtraStructureBuildUIPrefabs;
        private GameObject highlightGO;

        /// <summary>
        /// If it is in either build or destroy mode
        /// </summary>
        bool IsInBuildDestoyMode => MouseState == (MouseState.BuildDrag | MouseState.BuildPath | MouseState.BuildSingle | MouseState.Destroy);

        private bool DisplayDragRectangle;

        /// <summary>
        /// The world-position of the mouse last frame.
        /// </summary>
        private Vector3 lastFramePosition;
        /// <summary>
        /// The world-position of the mouse last frame with TileOffset.
        /// </summary>
        private Vector3 CurrFramePositionOffset => currFramePosition 
                                                    + new Vector3(TileSpriteController.offset, TileSpriteController.offset, 0);
        private Vector3 lastFrameGUIPosition;
        private Vector3 currFramePosition;
        private Vector3 dragStartPosition;
        private Vector3 pathStartPosition;

        public static bool Autorotate = true;

        /// <summary>
        ///  is true if smth is overriding the current states and commands for units
        /// </summary>
        public static bool OverrideCurrentSetting => InputHandler.ShiftKey == false; // TODO: better name

        public Vector2 MapClampedMousePosition {
            get {
                return new Vector2(Mathf.Clamp(currFramePosition.x, 0, World.Current.Width),
                                   Mathf.Clamp(currFramePosition.y, 0, World.Current.Height));
            }
        }

        private HashSet<Tile> destroyTiles;
        private Dictionary<Tile, TilePreview> tileToPreviewGO;
        private Dictionary<Tile, StructurePreview> tileToStructurePreview;
        private GameObject singleStructurePreview;
        private Structure _selectedStructure;
        private BuildPathAgent BuildPathAgent;
        public Structure SelectedStructure {
            get => _selectedStructure;
            set {
                if (_selectedStructure != null)
                    _selectedStructure.CloseExtraUI();
                _selectedStructure = value;
            }
        }

        protected Structure _toBuildstructure;

        public Structure ToBuildStructure {
            get {
                return _toBuildstructure;
            }
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
        private Rect draw_rect;
        private bool mouseStateIdleLeftMouseDown;
        private PathJob buildPathJob;

        public Unit SelectedUnit {
            get { return _selectedUnit; }
            protected set {
                if (_selectedUnit != null)
                    _selectedUnit.UnregisterOnDestroyCallback(OnUnitDestroy);
                _selectedUnit = value;
                if (_selectedUnit == null) {
                    SetMouseUnitState(MouseUnitState.None);
                }
                else {
                    SetMouseUnitState(MouseUnitState.Normal);
                }
            }
        }

        public IGEventable CurrentlySelectedIGEventable {
            get {
                if (SelectedUnit != null)
                    return SelectedUnit;
                if (SelectedStructure != null)
                    return SelectedStructure;
                if (CameraController.Instance.nearestIsland != null)
                    return CameraController.Instance.nearestIsland;
                return null;
            }
        }

        public bool IsGod { get; set; }

        public void OnEnable() {
            if (Instance != null) {
                Debug.LogError("There should never be two mouse controllers.");
            }
            Instance = this;
        }

        private void Start() {
            selectedUnitGroup = new List<Unit>();
            tileToPreviewGO = new Dictionary<Tile, TilePreview>();
            tileToStructurePreview = new Dictionary<Tile, StructurePreview>();
            destroyTiles = new HashSet<Tile>();
            ExtraStructureBuildUIPrefabs = new Dictionary<ExtraBuildUI, GameObject>();
            foreach (ExtraStructureBuildUI esbu in extraStructureBuildUIPrefabsEditor) {
                ExtraStructureBuildUIPrefabs[esbu.Type] = esbu.Prefab;
            }
            BuildPathAgent = new BuildPathAgent(PlayerController.currentPlayerNumber);
            PlayerController.Instance.cbPlayerChange += (a,b)=>{ BuildPathAgent = new BuildPathAgent(PlayerController.currentPlayerNumber); };
        }

        /// <summary>
        /// Gets the mouse position in world space.
        /// </summary>
        public Vector3 GetMousePosition() {
            return CurrFramePositionOffset;
        }

        /// <summary>
        /// Gets the mouse position in world space.
        /// </summary>
        public Vector3 GetLastMousePosition() {
            return lastFramePosition;
        }
        System.Diagnostics.Stopwatch StopWatch;
        private void Update() {
            if (EditorController.IsEditor == false && PlayerController.GameOver)
                return;
            StopWatch = new System.Diagnostics.Stopwatch();
            StopWatch.Start();

            currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            currFramePosition.z = 0;
            if (currFramePosition.y < 0 || currFramePosition.x < 0) {
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
                ResetBuild(null);
                SetMouseState(MouseState.Idle);
            }

            // Save the mouse position from this frame
            // We don't use currFramePosition because we may have moved the camera.
            lastFrameGUIPosition = Input.mousePosition;
            lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lastFramePosition.z = 0;
        }

        internal void UnselectStructure() {
            SelectedStructure = null;
        }

        internal void SetStructureToBuild(Structure toBuildStructure) {
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
        internal void SetMouseState(MouseState state) {
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
            }
            MouseState = state;
        }

        internal void SetMouseUnitState(MouseUnitState state) {
            MouseUnitState = state;
        }
        /// <summary>
        /// Moves Highlights with offset so that mouse is in the middle.
        /// </summary>
        private void UpdateEditorStuff() {
            if (highlightGO != null) {
                Tile t = GetTileUnderneathMouse();
                if (t == null)
                    return;
                highlightGO.transform.position = new Vector3(t.X, t.Y, 0) + EditorController.Instance.BrushOffset;
            }
        }

        public void UpdateMouseStates() {
            switch (MouseState) {
                case MouseState.Idle:
                    if(EventSystem.current.IsPointerOverGameObject() == false && InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                        //If some clicks down onto a ui and then goes off it and releases the mouse 
                        //we do not want to open or close where the mouse ends up 
                        mouseStateIdleLeftMouseDown = true;
                    }
                    if (mouseStateIdleLeftMouseDown && InputHandler.GetMouseButtonUp(InputMouse.Primary) 
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
            if (InputHandler.GetMouseButton(InputMouse.Primary) && DisplayDragRectangle == false) {
                if (EventSystem.current.IsPointerOverGameObject() == false && ShortcutUI.Instance.IsDragging == false) {
                    float sqrdist = (Input.mousePosition - lastFrameGUIPosition).sqrMagnitude;
                    if (sqrdist > 5) {
                        dragStartPosition = currFramePosition;
                        //SetMouseState(MouseState.DragSelect);
                        DisplayDragRectangle = true;
                    }
                }
            }
            if (DisplayDragRectangle)
                UpdateDragSelect();
        }

        private void UpdateCopyState() {
            if(InputHandler.GetMouseButtonUp(InputMouse.Primary)) {
                Tile t = GetTileUnderneathMouse();
                if (t.Structure == null)
                    return;
                if (t.Structure.CanBeBuild == false)
                    return;
                BuildController.Instance.StartStructureBuild(t.Structure.ID);
            }
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
        internal void UnselectStuff(bool escape = false) {
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
                dragStartPosition = CurrFramePositionOffset;
            }
            int start_x = Mathf.FloorToInt(dragStartPosition.x);
            int end_x = Mathf.FloorToInt(CurrFramePositionOffset.x);
            int start_y = Mathf.FloorToInt(dragStartPosition.y);
            int end_y = Mathf.FloorToInt(CurrFramePositionOffset.y);
            if (InputHandler.GetMouseButton(InputMouse.Primary)) {
                List<Tile> tiles = GetTilesStructures(start_x, end_x, start_y, end_y);
                foreach (Tile t in destroyTiles.Except(tiles).ToArray()) {
                    SimplePool.Despawn(tileToPreviewGO[t].gameObject);
                    tileToPreviewGO.Remove(t);
                    destroyTiles.Remove(t);
                }
                foreach (Tile t in tiles) {
                    if (destroyTiles.Contains(t))
                        continue;
                    ShowTilePrefabOnTile(t, TileHighlightType.Red);
                    destroyTiles.Add(t);
                }
            }
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary)) {
                List<Tile> ts = new List<Tile>(GetTilesStructures(start_x, end_x, start_y, end_y));
                if (ts != null) {
                    bool isGod = EditorController.IsEditor || IsGod; //TODO: add cheat to set this
                    BuildController.Instance.DestroyStructureOnTiles(ts, PlayerController.CurrentPlayer, isGod);
                }
                ResetBuild(null, false);
            }
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
                }
            }
            CheckUnitCursor();
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                Transform hit = MouseRayCast();
                if (hit == null) {
                    switch (MouseUnitState) {
                        case MouseUnitState.None:
                            Debug.LogWarning("MouseController is in the wrong state!");
                            break;

                        case MouseUnitState.Normal:
                            selectedUnitGroup.ForEach(x => x.GiveMovementCommand(MapClampedMousePosition.x, MapClampedMousePosition.y, OverrideCurrentSetting));
                            break;

                        case MouseUnitState.Patrol:
                            SetMouseUnitState(MouseUnitState.Normal);
                            break;
                    }
                }
                else {
                    ITargetableHoldingScript targetableHoldingScript = hit.GetComponent<ITargetableHoldingScript>();
                    if (targetableHoldingScript != null) {
                        selectedUnitGroup.ForEach(x => x.GiveAttackCommand(hit.gameObject.GetComponent<ITargetableHoldingScript>().Holding, OverrideCurrentSetting));
                    }
                    else
                    if (hit.GetComponent<CrateHoldingScript>() != null) {
                        //TODO: maybe nearest? other logic? air distance??
                        selectedUnitGroup[0].TryToAddCrate(hit.GetComponent<CrateHoldingScript>().thisCrate);
                    }
                    else
                    if (targetableHoldingScript == null) {
                        Tile t = GetTileUnderneathMouse();
                        if (t.Structure != null) {
                            if (t.Structure is ICapturable) {
                                selectedUnitGroup.ForEach(x => x.GiveCaptureCommand((ICapturable)t.Structure, OverrideCurrentSetting));
                            }
                            else
                            if (t.Structure is TargetStructure) {
                                selectedUnitGroup.ForEach(x => x.GiveAttackCommand((TargetStructure)t.Structure, OverrideCurrentSetting));
                            }
                            return;
                        }
                    }
                }
            }
        }
        internal void ShowError(MapErrorMessage Message) {
            ShowError(Message, currFramePosition);
        }
        internal void ShowError(MapErrorMessage Message, Vector3 Position) {
            TextMeshPro text = SimplePool.Spawn(fadeOutTextPrefab, Position, Quaternion.identity).GetComponent<TextMeshPro>();
            text.fontSize = Mathf.Max(8.333f * (CameraController.Instance.zoomLevel / CameraController.MaxZoomLevel), 2);
            text.text = UILanguageController.Instance.GetTranslation(Message);
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
                Vector3 v1 = dragStartPosition;
                Vector3 v2 = lastFramePosition;
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
                    if (target.Holding.PlayerNumber == PlayerController.currentPlayerNumber) {
                        Unit u = ((Unit)target.Holding);
                        if (selectedUnitGroup.Contains(u) == false)
                            selectedUnitGroup.Add(u);
                    }
                }
                if (selectedUnitGroup.Count > 1)
                    SelectUnitGroup(selectedUnitGroup);
                else if (selectedUnitGroup.Count == 1)
                    SelectUnit(selectedUnitGroup[0]);
                else {
                    SetMouseState(MouseState.Idle);// nothing selected
                    UnselectStuff();
                }
                draw_rect = Rect.zero;
                DisplayDragRectangle = false;
            }

            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            // Drag already started

            Vector3 screenPosition1 = Camera.main.WorldToScreenPoint(dragStartPosition);
            Vector3 screenPosition2 = lastFrameGUIPosition;
            screenPosition1.y = Screen.height - screenPosition1.y;
            screenPosition2.y = Screen.height - screenPosition2.y;

            // Calculate corners
            var topLeft = Vector3.Min(screenPosition1, screenPosition2);
            var bottomRight = Vector3.Max(screenPosition1, screenPosition2);
            // Create Rect
            draw_rect = Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        internal void SetEditorBrushHighlightActive(bool brushBuild) {
            highlightGO.SetActive(brushBuild);
        }
        /// <summary>
        /// Which tiles will be highlighted. 
        /// Size of the texture.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="toHighlightTiles"></param>
        /// <param name="active"></param>
        internal void SetEditorHighlight(int size, List<Tile> toHighlightTiles, bool active) {
            if (toHighlightTiles == null)
                return;
            if (highlightGO != null)
                Destroy(highlightGO);
            highlightGO = GetHighlightGameObject(size, size, toHighlightTiles);
            highlightGO.GetComponent<SpriteRenderer>().sortingLayerName = "StructuresUI";
            highlightGO.SetActive(active);
        }

        public void OnGUI() {
            if (DisplayDragRectangle)
                Util.DrawScreenRectBorder(draw_rect, 2, new Color(0.9f, 0.9f, 0.9f, 0.9f));
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
                if (targetableHoldingScript != null && targetableHoldingScript.IsUnit) {
                    if (GameData.FogOfWarStyle == FogOfWarStyle.Always && targetableHoldingScript.IsCurrentlyVisible == false) {
                        return;
                    }
                    SelectUnit((Unit)targetableHoldingScript.Holding);
                }
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
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                List<Tile> structureTiles = ToBuildStructure.GetBuildingTiles(GetTileUnderneathMouse());
                Build(structureTiles);
            }
        }
        /// <summary>
        /// Change the location and rotation of a single preview.
        /// </summary>
        private void UpdateSinglePreview() {
            if (singleStructurePreview == null)
                singleStructurePreview = CreatePreviewStructure();
            float x = ((float)ToBuildStructure.TileWidth) / 2f - TileSpriteController.offset;
            float y = ((float)ToBuildStructure.TileHeight) / 2f - TileSpriteController.offset;
            singleStructurePreview.transform.position = new Vector3(GetTileUnderneathMouse().X + x,
                                                       GetTileUnderneathMouse().Y + y, 0);
            singleStructurePreview.transform.eulerAngles = new Vector3(0, 0, 360 - ToBuildStructure.rotation);
            List<Tile> tiles = ToBuildStructure.GetBuildingTiles(GetTileUnderneathMouse());
            foreach (Tile tile in tileToPreviewGO.Keys.Except(tiles).ToArray()) {
                SimplePool.Despawn(tileToPreviewGO[tile].gameObject);
                tileToPreviewGO.Remove(tile);
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
                dragStartPosition = CurrFramePositionOffset;
                if (singleStructurePreview != null) {
                    SimplePool.Despawn(singleStructurePreview);
                    singleStructurePreview = null;
                }
            }
            int start_x = Mathf.FloorToInt(dragStartPosition.x);
            int end_x = Mathf.FloorToInt(CurrFramePositionOffset.x);
            int start_y = Mathf.FloorToInt(dragStartPosition.y);
            int end_y = Mathf.FloorToInt(CurrFramePositionOffset.y);
            List<Tile> ts = GetTilesStructures(start_x, end_x, start_y, end_y);
            if (InputHandler.GetMouseButton(InputMouse.Primary)) {
                // Display a preview of the drag area
                UpdateMultipleStructurePreviews(ts);
            }
            else {
                UpdateSinglePreview();
            }
            // End Drag
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary)) {
                Build(ts, true, true);
                ResetStructurePreviews();
            }
        }
        /// <summary>
        /// Update the the pathfinding with the start and end position and then display foreach position a prefab.
        /// </summary>
        private void UpdateBuildPath() {
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            // Start Path
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                pathStartPosition = CurrFramePositionOffset;
                if (singleStructurePreview != null) {
                    ResetStructurePreviews();
                }
            }
            if (InputHandler.GetMouseButton(InputMouse.Primary)) {
                int start_x = Mathf.FloorToInt(pathStartPosition.x);
                int start_y = Mathf.FloorToInt(pathStartPosition.y);
                Tile pathStartTile = World.Current.GetTileAt(start_x, start_y);

                if (pathStartTile == null || pathStartTile.Island == null) {
                    return;
                }
                int end_x = Mathf.FloorToInt(CurrFramePositionOffset.x);
                int end_y = Mathf.FloorToInt(CurrFramePositionOffset.y);
                Tile pathEndTile = World.Current.GetTileAt(end_x, end_y);
                if (pathEndTile == null) {
                    return;
                }
                if (pathStartTile.Island != null && pathEndTile.Island != null &&
                        (buildPathJob == null || buildPathJob.End != pathEndTile.Vector2)) {
                    buildPathJob = new PathJob(BuildPathAgent, pathStartTile.Island.Grid, pathStartTile.Vector2, pathEndTile.Vector2);
                    PathfindingThreadHandler.EnqueueJob(buildPathJob, null, true);
                    if(buildPathJob.Path != null)
                        UpdateMultipleStructurePreviews(World.Current.GetTilesQueue(buildPathJob.Path));
                }
            }
            else {
                UpdateSinglePreview();
            }
            // End path
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary)) {
                ResetStructurePreviews();
                if (buildPathJob == null || buildPathJob.Status != JobStatus.Done) {
                    return;
                }
                Build(World.Current.GetTilesQueue(buildPathJob.Path).ToList(), true);
            }
        }
        /// <summary>
        /// Updates which previews are to be deleted and where to create new ones.
        /// </summary>
        /// <param name="tiles"></param>
        private void UpdateMultipleStructurePreviews(IEnumerable<Tile> tiles) {
            foreach (Tile tile in tileToStructurePreview.Keys.Except(tiles).ToArray()) {
                SimplePool.Despawn(tileToStructurePreview[tile].gameObject);
                foreach (Tile t in tileToStructurePreview[tile].tiles) {
                    SimplePool.Despawn(tileToPreviewGO[t].gameObject);
                    tileToPreviewGO.Remove(t);
                }
                tileToStructurePreview.Remove(tile);
            }
            foreach (Tile tile in tiles) {
                if (tileToStructurePreview.ContainsKey(tile) == false) {
                    StructurePreview preview = new StructurePreview(
                        tile,
                        CreatePreviewStructure(tile),
                        ToBuildStructure.GetBuildingTiles(tile), tileToStructurePreview.Count + 1
                    );
                    tileToStructurePreview[tile] = preview;
                }
            }
            NeededItemsToBuild = ToBuildStructure.BuildingItems?.CloneArrayWithCounts(tiles.Count());
            NeededBuildCost = ToBuildStructure.BuildCost * tiles.Count();
            foreach (StructurePreview preview in tileToStructurePreview.Values) {
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
                hasEnoughResources &= SelectedUnit.inventory.HasEnoughOfItems(ToBuildStructure.BuildingItems, number) == true;
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
                    ToBuildStructure.RotateStructure();
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
                canBuild &= tile.Island != null && tile.Island.HasNegativEffect == false;
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
            GameObject previewGO = SimplePool.Spawn(structurePreviewRendererPrefab, position, Quaternion.Euler(0, 0, 360 - ToBuildStructure.rotation));
            //previewGO.transform.SetParent(this.transform, true);
            if (ToBuildStructure.ExtraBuildUITyp != ExtraBuildUI.None) {
                if (ExtraStructureBuildUIPrefabs.ContainsKey(ToBuildStructure.ExtraBuildUITyp) == false)
                    Debug.LogError(ToBuildStructure.ExtraBuildUITyp + " ExtraBuildPreview has no Prefab assigned!");
                else {
                    GameObject extra = Instantiate(ExtraStructureBuildUIPrefabs[ToBuildStructure.ExtraBuildUITyp]);
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
            if (tileToPreviewGO.ContainsKey(t)) {
                if (tileToPreviewGO[t].HighlightType == type) {
                    return;
                }
                else {
                    SimplePool.Despawn(tileToPreviewGO[t].gameObject);
                    tileToPreviewGO.Remove(t);
                }
            }
            GameObject go = null;
            switch (type) {
                case TileHighlightType.Green:
                    go = greenTileCursorPrefab;
                    break;

                case TileHighlightType.Red:
                    go = redTileCursorPrefab;
                    break;
            }
            go = SimplePool.Spawn(go, new Vector3(t.X + 0.5f, t.Y + 0.5f, 0), Quaternion.identity);
            // Display the building hint on top of this tile position
            //go.transform.SetParent(this.transform, true);
            tileToPreviewGO.Add(t, new TilePreview(t, type, go));
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
                    ((Ship)SelectedUnit).ShotAtPosition(currFramePosition);
                }
            }
            CheckUnitCursor();
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary)) {
                switch (MouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;

                    case MouseUnitState.Normal:
                        //TODO: Better way?
                        Transform hit = MouseRayCast();
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
                }
            }
            if (InputHandler.GetMouseButtonDown(InputMouse.Secondary)) {
                if (SelectedUnit.playerNumber != PlayerController.currentPlayerNumber) {
                    SetMouseState(MouseState.Idle);
                    return;
                }
                Transform hit = MouseRayCast();
                if (hit == null) {
                    switch (MouseUnitState) {
                        case MouseUnitState.None:
                            Debug.LogWarning("MouseController is in the wrong state!");
                            break;

                        case MouseUnitState.Normal:
                            SelectedUnit.GiveMovementCommand(MapClampedMousePosition.x, MapClampedMousePosition.y, OverrideCurrentSetting);
                            break;

                        case MouseUnitState.Patrol:
                            SetMouseUnitState(MouseUnitState.Normal);
                            break;

                        case MouseUnitState.Build:
                            ResetBuild(null);
                            break;
                    }
                }
                else {
                    ITargetableHoldingScript targetableHoldingScript = hit.GetComponent<ITargetableHoldingScript>();
                    if (targetableHoldingScript != null) {
                        SelectedUnit.GiveAttackCommand(targetableHoldingScript.Holding, OverrideCurrentSetting);
                    }
                    else
                    if (hit.GetComponent<CrateHoldingScript>() != null) {
                        SelectedUnit.GivePickUpCrateCommand(hit.GetComponent<CrateHoldingScript>().thisCrate, OverrideCurrentSetting);
                    }
                    else
                    if (targetableHoldingScript == null) {
                        Tile t = GetTileUnderneathMouse();
                        if (t.Structure != null) {
                            if (t.Structure is ICapturable) {
                                SelectedUnit.GiveCaptureCommand((ICapturable)t.Structure, OverrideCurrentSetting);
                            }
                            else
                            if (t.Structure is TargetStructure) {
                                SelectedUnit.GiveAttackCommand((TargetStructure)t.Structure, OverrideCurrentSetting);
                            }
                            return;
                        }
                    }
                }
            }
        }

        private void CheckUnitCursor() {
            if (SelectedUnit.IsPlayer() == false)
                return;
            if (MouseUnitState != MouseUnitState.Build) {
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
                if (str != null && str is TargetStructure) {
                    attackAble = PlayerController.Instance.ArePlayersAtWar(PlayerController.currentPlayerNumber, str.PlayerNumber);
                }
                if (attackAble)
                    ChangeCursorType(CursorType.Attack);
                else
                    ChangeCursorType(CursorType.Pointer);
            }
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
        /// <param name="start_x"></param>
        /// <param name="end_x"></param>
        /// <param name="start_y"></param>
        /// <param name="end_y"></param>
        /// <returns></returns>
        private List<Tile> GetTilesStructures(int start_x, int end_x, int start_y, int end_y) {
            int width = 1;
            int height = 1;
            List<Tile> tiles = new List<Tile>();
            if (ToBuildStructure != null) {
                width = ToBuildStructure.TileWidth;
                height = ToBuildStructure.TileHeight;
            }
            if (end_x >= start_x && end_y >= start_y) {
                for (int x = start_x; x <= end_x; x += width) {
                    for (int y = start_y; y <= end_y; y += height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            else
            if (end_x > start_x && end_y <= start_y) {
                for (int x = start_x; x <= end_x; x += width) {
                    for (int y = start_y; y >= end_y; y -= height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            else
            if (end_x <= start_x && end_y > start_y) {
                for (int x = start_x; x >= end_x; x -= width) {
                    for (int y = start_y; y <= end_y; y += height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            else
            if (end_x <= start_x && end_y <= start_y) {
                for (int x = start_x; x >= end_x; x -= width) {
                    for (int y = start_y; y >= end_y; y -= height) {
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
                        foreach (StructurePreview sp in tileToStructurePreview.Values.OrderBy(x => x.number)) {
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

        public void ResetBuild(Structure structure, bool loading = false) {
            if (loading) {
                return;// there is no need to call any following
            }
            if (BuildController.Instance.BuildState != BuildStateModes.None)
                BuildController.Instance.ResetBuild();
            ResetStructurePreviews();
            NeededBuildCost = 0;
            NeededItemsToBuild = null;
            destroyTiles.Clear();
            ToBuildStructure = null;
            if (MouseUnitState == MouseUnitState.Build) {
                UnselectUnit();
            }
        }

        public void ResetStructurePreviews() {
            foreach (Tile tile in tileToStructurePreview.Keys) {
                foreach (Transform t in tileToStructurePreview[tile].gameObject.transform) {
                    Destroy(t.gameObject);
                }
                SimplePool.Despawn(tileToStructurePreview[tile].gameObject);
            }
            tileToStructurePreview.Clear();
            foreach (Tile t in tileToPreviewGO.Keys) {
                SimplePool.Despawn(tileToPreviewGO[t].gameObject);
            }
            tileToPreviewGO.Clear();
            if (singleStructurePreview) {
                SimplePool.Despawn(singleStructurePreview);
                foreach (Transform t in singleStructurePreview.transform) {
                    Destroy(t.gameObject);
                }
                singleStructurePreview = null;
            }
        }

        internal void UnselectUnitGroup() {
            if (selectedUnitGroup == null)
                return;
            selectedUnitGroup.ForEach(x => x.UnregisterOnDestroyCallback(OnUnitDestroy));
            selectedUnitGroup.Clear();
            SetMouseState(MouseState.Idle);
            SetMouseUnitState(MouseUnitState.None);
            selectedUnitGroup.Clear();
        }

        internal void RemoveUnitFromGroup(Unit unit) {
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

        internal void StopUnit() {
            //if null or not player unit return without doing anything
            if (SelectedUnit == null || SelectedUnit.IsPlayer() == false) {
                return;
            }
            SelectedUnit.GoIdle();
        }

        private void OnDestroy() {
            Instance = null;
        }

        public Tile GetTileUnderneathMouse() {
            return World.Current.GetTileAt(CurrFramePositionOffset);
        }

        private Transform MouseRayCast() {
            return Physics2D.Raycast(new Vector2(currFramePosition.x, currFramePosition.y), Vector2.zero, 200).transform;
        }

        /// <summary>
        /// what to on escape press
        ///  - set tobuildstructure to null
        ///  - set mousestate to drag
        /// </summary>
        public void Escape() {
            dragStartPosition = currFramePosition;
            UnselectStuff(true);
            ResetBuild(null);
            SetMouseState(MouseState.Idle);
            SetMouseUnitState(MouseUnitState.None);
            ChangeCursorType(CursorType.Pointer);
        }
        internal void SetCopyMode(bool on) {
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
            public Tile tile;
            public GameObject gameObject;
            public TileHighlightType HighlightType;

            public TilePreview(Tile t, TileHighlightType type, GameObject gameObject) {
                tile = t;
                HighlightType = type;
                this.gameObject = gameObject;
            }
        }

        private class StructurePreview {
            public GameObject gameObject;
            public List<Tile> tiles;
            public Tile tile;
            public int number;
            public SpriteRenderer spriteRenderer;

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