using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections;

public enum MouseState { Idle, BuildDrag, BuildPath, BuildSingle, Unit, UnitGroup, Destroy, DragSelect };
public enum MouseUnitState { None, Normal, Patrol, Build };

public class MouseController : MonoBehaviour {

    public static MouseController Instance { get; protected set; }

    public GameObject greenTileCursorPrefab;
    public GameObject redTileCursorPrefab;
    public ExtraStructureBuildUI[] extraStructureBuildUIPrefabsEditor;
    Dictionary<ExtraBuildUI, GameObject> ExtraStructureBuildUIPrefabs;
    GameObject previewGO;
    GameObject highlightGO;

    // The world-position of the mouse last frame.
    Vector3 lastFramePosition;
    Vector3 currFramePositionOffset => currFramePosition + new Vector3(TileSpriteController.offset, TileSpriteController.offset, 0);

    Vector3 lastFrameGUIPosition;

    Vector3 currFramePosition;
    private Vector3 dragStartPosition;

    private Vector3 pathStartPosition;

    StructureSpriteController ssc;
    public static bool autorotate = true;
    /// <summary>
    ///  is true if smth is overriding the current states and commands for units
    /// </summary>
    public static bool OverrideCurrentSetting => InputHandler.ShiftKey == false; // TODO: better name
    private HashSet<Tile> _highlightTiles;
    HashSet<Tile> HighlightTiles {
        get { return _highlightTiles; }
        set {
            _highlightTiles = value;
        }
    }
    public Vector2 MapClampedMousePosition {
        get {
            return new Vector2(Mathf.Clamp(currFramePosition.x, 0, World.Current.Width), 
                               Mathf.Clamp(currFramePosition.y, 0, World.Current.Height));
        }
    }

    // The world-position start of our left-mouse drag operation
    List<GameObject> previewGameObjects;

    private Structure SelectedStructure;
    protected Structure _toBuildstructure;
    public Structure ToBuildStructure {
        get {
            return _toBuildstructure;
        }
        set {
            GameObject.Destroy(previewGO);
            _toBuildstructure = value;
        }
    }

    public MouseState mouseState = MouseState.Idle;
    public MouseUnitState mouseUnitState = MouseUnitState.None;

    private Path_AStar path;

    private Unit _selectedUnit;
    private List<Unit> selectedUnitGroup;
    private Rect draw_rect;

    public Unit SelectedUnit {
        get { return _selectedUnit; }
        protected set {
            if (_selectedUnit != null)
                _selectedUnit.UnregisterOnDestroyCallback(OnUnitDestroy);
            _selectedUnit = value;
            if (_selectedUnit == null) {
                mouseUnitState = MouseUnitState.None;
            }
            else {
                mouseUnitState = MouseUnitState.Normal;
            }
        }
    }


    public IGEventable CurrentlySelectedIGEventable {
        get {
            if (SelectedUnit != null)
                return SelectedUnit;
            if(SelectedStructure != null)
                return SelectedStructure;
            return null;
        }
    }
    public void OnEnable() {
        if (Instance != null) {
            Debug.LogError("There should never be two mouse controllers.");
        }
        Instance = this;

    }

    void Start() {
        selectedUnitGroup = new List<Unit>(); 
        previewGameObjects = new List<GameObject>();
        BuildController.Instance.RegisterStructureCreated(ResetBuild);
        _highlightTiles = new HashSet<Tile>();
        ssc = GameObject.FindObjectOfType<StructureSpriteController>();
        ExtraStructureBuildUIPrefabs = new Dictionary<ExtraBuildUI, GameObject>();
        foreach (ExtraStructureBuildUI esbu in extraStructureBuildUIPrefabsEditor) {
            ExtraStructureBuildUIPrefabs[esbu.Type] = esbu.Prefab;
        }
    }

    /// <summary>
    /// Gets the mouse position in world space.
    /// </summary>
    public Vector3 GetMousePosition() {
        return currFramePositionOffset;
    }
    /// <summary>
    /// Gets the mouse position in world space.
    /// </summary>
    public Vector3 GetLastMousePosition() {
        return lastFramePosition;
    }


    // Update is called once per frame
    void Update() {
        if (PlayerController.GameOver)
            return;
        currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePosition.z = 0;
        if (currFramePosition.y < 0 || currFramePosition.x < 0) {
            return;
        }
        RemovePrefabs();
        UpdateMouseStates();
        if (EditorController.IsEditor == false) {
            UpdateDragBox();
        } else {
            UpdateEditorStuff();
        }

        if (Input.GetMouseButtonDown(1) && mouseState != MouseState.Unit && mouseState != MouseState.UnitGroup) {
            ResetBuild(null);
            mouseState = MouseState.Idle;
        }

        // Save the mouse position from this frame
        // We don't use currFramePosition because we may have moved the camera.
        lastFrameGUIPosition = Input.mousePosition;
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    private void UpdateEditorStuff() {
        if(highlightGO != null) {
            Tile t = GetTileUnderneathMouse();
            if (t == null)
                return;
            highlightGO.transform.position = new Vector3(t.X,t.Y, 0) + EditorController.Instance.BrushOffset;
        }
    }

    private void UpdateDragBox() {
        if (Input.GetMouseButtonDown(0)
            && mouseState == MouseState.Idle) {
            if (EventSystem.current.IsPointerOverGameObject() == false && ShortcutUI.Instance.IsDragging == false) {
                float sqrdist = (Input.mousePosition - lastFrameGUIPosition).sqrMagnitude;
                if (sqrdist > 5) {
                    dragStartPosition = currFramePosition;
                    mouseState = MouseState.DragSelect;
                    UpdateDragSelect(); // update the rect so no ghosts
                }
            }
        }
    }

    public void UpdateMouseStates() {
        switch (mouseState) {
            case MouseState.Idle:
                if (Input.GetMouseButtonUp(0)&&EditorController.IsEditor==false) {
                    //mouse press decide what it hit 
                    DecideWhatUIToShow(MouseRayCast());
                }
                break;
            case MouseState.BuildDrag:
                UpdateBuildDragging();
                break;
            case MouseState.BuildPath:
                UpdatePathBetweenTiles();
                break;
            case MouseState.BuildSingle:
                UpdateSingle();
                break;
            case MouseState.Unit:
                if (SelectedUnit == null) {
                    mouseState = MouseState.Idle;
                    return;
                }
                UpdateUnit();
                break;
            case MouseState.DragSelect:
                UpdateDragSelect();
                break;
            case MouseState.Destroy:
                UpdateBuildDragging();
                break;
            case MouseState.UnitGroup:
                UpdateUnitGroup();
                break;
        }
    }

    private void UpdateUnitGroup() {
        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }
        if (Input.GetMouseButtonDown(0)) {
            switch (mouseUnitState) {
                case MouseUnitState.None:
                    Debug.LogWarning("MouseController is in the wrong state!");
                    break;
                case MouseUnitState.Normal:
                    ClearUnitGroup();
                    break;
                case MouseUnitState.Patrol:
                    selectedUnitGroup.ForEach(x=>x.AddPatrolCommand(MapClampedMousePosition.x, MapClampedMousePosition.y));
                    break;
                case MouseUnitState.Build:
                    Debug.LogWarning("MouseController is in the wrong state!");
                    break;
            }
        }
        if (Input.GetMouseButtonDown(1)) {
            Transform hit = MouseRayCast();
            if (hit == null) {
                switch (mouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;
                    case MouseUnitState.Normal:
                        selectedUnitGroup.ForEach(x => x.GiveMovementCommand(MapClampedMousePosition.x, MapClampedMousePosition.y, OverrideCurrentSetting));
                        break;
                    case MouseUnitState.Patrol:
                        mouseUnitState = MouseUnitState.Normal;
                        break;
                }
            }
            else {
                ITargetableHoldingScript targetableHoldingScript = hit.GetComponent<ITargetableHoldingScript>();
                if (targetableHoldingScript != null) {
                    selectedUnitGroup.ForEach(x =>x.GiveAttackCommand(hit.gameObject.GetComponent<ITargetableHoldingScript>().Holding, OverrideCurrentSetting));
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
                            selectedUnitGroup.ForEach(x =>x.GiveCaptureCommand((ICapturable)t.Structure, OverrideCurrentSetting));
                        }
                        else
                        if (t.Structure is TargetStructure) {
                            selectedUnitGroup.ForEach(x =>x.GiveAttackCommand((TargetStructure)t.Structure, OverrideCurrentSetting));
                        }
                        return;
                    }
                }
            }
        }
    }

    private void UpdateDragSelect() {
        // End Drag
        if (Input.GetMouseButton(0)==false) {
            Vector3 v1 = dragStartPosition;
            Vector3 v2 = lastFramePosition;
            v1.z = 0;
            v2.z = 0;
            Vector3 min = Vector3.Min(v1, v2);
            Vector3 max = Vector3.Max(v1, v2);
            Vector3 dimensions = max - min;
            Collider2D[] c2d = Physics2D.OverlapBoxAll(min + dimensions/2, dimensions, 0);
            if(OverrideCurrentSetting)
                selectedUnitGroup.Clear();
            foreach (Collider2D c in c2d) {
                ITargetableHoldingScript target = c.GetComponent<ITargetableHoldingScript>();
                if (target == null)
                    continue;
                if (target.IsUnit == false)
                    continue;
                if (target.Holding.PlayerNumber == PlayerController.currentPlayerNumber) {
                    Unit u = ((Unit)target.Holding);
                    if (selectedUnitGroup.Contains(u)==false)
                        selectedUnitGroup.Add(u);
                }
            }
            if (selectedUnitGroup.Count > 1)
                SelectUnitGroup(selectedUnitGroup);
            else if (selectedUnitGroup.Count == 1)
                SelectUnit(selectedUnitGroup[0]);
            else {
                mouseState = MouseState.Idle; // nothing selected
                UnselectUnit();
                ClearUnitGroup();
            }
            draw_rect = Rect.zero;
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

    internal void SetEditorHighlight(int size, List<Tile> toHighlightTiles, bool active) {
        if (toHighlightTiles == null)
            return;
        if (highlightGO != null)
            Destroy(highlightGO);
        HighlightTiles = new HashSet<Tile>(toHighlightTiles);
        highlightGO = GetHighlightGameObject(size, size, toHighlightTiles);
        highlightGO.GetComponent<SpriteRenderer>().sortingLayerName = "StructuresUI";
        highlightGO.SetActive(active);
    }

    public void OnGUI() {
        if(mouseState == MouseState.DragSelect) {
            Util.DrawScreenRectBorder(draw_rect, 2, new Color(0.9f, 0.9f, 0.9f,0.9f));
        }
    }
    private void DecideWhatUIToShow(Transform hit) {
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }
        if (hit != null) {
            ITargetableHoldingScript targetableHoldingScript = hit.GetComponent<ITargetableHoldingScript>();
            if (targetableHoldingScript != null && targetableHoldingScript.IsUnit) {
                SelectUnit((Unit) targetableHoldingScript.Holding);
            }
            else
            if (SelectedUnit == null) {
                Tile t = GetTileUnderneathMouse();
                if (t.Structure != null) {
                    UIDebug(t.Structure);
                    UIController.Instance.OpenStructureUI(t.Structure);
                    SelectedStructure = t.Structure;
                }
            }
        }
        else {
            UIDebug(GetTileUnderneathMouse());
            if (mouseState != (MouseState.Unit | MouseState.UnitGroup) ) {
                UIController.Instance.CloseInfoUI();
                SelectedUnit = null;
                SelectedStructure = null;
            }
        }
    }

    private void SelectUnit(Unit unit) {
        if (SelectedUnit == unit)
            return;
        mouseState = MouseState.Unit;
        mouseUnitState = MouseUnitState.Normal;
        SelectedUnit = unit;
        SelectedUnit.RegisterOnDestroyCallback(OnUnitDestroy);
        UIController.Instance.OpenUnitUI(SelectedUnit);
        UIDebug(SelectedUnit);
    }
    private void SelectUnitGroup(List<Unit> units) {
        mouseState = MouseState.UnitGroup;
        mouseUnitState = MouseUnitState.Normal;
        selectedUnitGroup = units;
        selectedUnitGroup.ForEach(x=>x.RegisterOnDestroyCallback(OnUnitDestroy));
        UIController.Instance.OpenUnitGroupUI(selectedUnitGroup.ToArray());
    }
    private void UpdateSingle() {
        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject()) {
            HighlightTiles = null;
            return;
        }
        if (ToBuildStructure == null) {
            HighlightTiles = null;
            return;
        }
        ShowSinglePreview(GetTileUnderneathMouse());
        if (Input.GetMouseButtonDown(0)) {
            List<Tile> structureTiles = ToBuildStructure.GetBuildingTiles(GetTileUnderneathMouse().X, GetTileUnderneathMouse().Y);
            Build(structureTiles);
        }
    }

    private void UIDebug(object obj) {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift)
                    && SaveController.DebugModeSave) {
            UIController.Instance.ShowDebugForObject(obj);
        }
    }
    private void ShowSinglePreview(Tile tile) {
        if (tile == null) {
            return;
        }
        int tempTest = ToBuildStructure.rotation;
        Dictionary<Tile, bool> tileToCanBuild = null;
        if (autorotate) {
            for (int r = 0; r < 4; r++) {
                ToBuildStructure.AddTimes90ToRotate(r);
                List<Tile> structureTiles = ToBuildStructure.GetBuildingTiles(tile.X, tile.Y);
                tileToCanBuild = ToBuildStructure.CheckForCorrectSpot(structureTiles);
                if (tileToCanBuild.Values.ToList().Contains(false) == false) {
                    break;
                }

            }
        } else {
            List<Tile> structureTiles = ToBuildStructure.GetBuildingTiles(tile.X, tile.Y);
            tileToCanBuild = ToBuildStructure.CheckForCorrectSpot(structureTiles);
        }

        ShowPreviewStructureOnTiles(tile);
        ShowHighlightOnTiles();

        if (tileToCanBuild.Values.ToList().Contains(false)) {
            //TODO fix this temporary fix
            // it is so that previews dont spinn like crazy BUT find better way todo this
            ToBuildStructure.rotation = tempTest;
        }
        foreach (Tile t in tileToCanBuild.Keys) {
            if (t == null) {
                continue;
            }
            //not viable city overrides everything
            if (ToBuildStructure.IsTileCityViable(t, PlayerController.currentPlayerNumber) == false
                && EditorController.IsEditor==false) {
                ShowRedPrefabOnTile(t);
                continue;
            }
            if(mouseUnitState == MouseUnitState.Build) {
                if(Vector2.Distance(t.Vector2, SelectedUnit.PositionVector2) > SelectedUnit.BuildRange) {
                    ShowRedPrefabOnTile(t);
                    continue;
                }
            }
            if (tileToCanBuild[t]) {
                ShowPrefabOnTile(t);
            }
            else {
                ShowRedPrefabOnTile(t);
            }
        }

    }

    public Tile GetTileUnderneathMouse() {
        return World.Current.GetTileAt(currFramePositionOffset);
    }

    public void CreatePreviewStructure() {
        previewGO = new GameObject();
        previewGO.transform.SetParent(this.transform, true);
        previewGO.name = "PreviewGO";
        if(ToBuildStructure.ExtraBuildUITyp!=ExtraBuildUI.None) {
            if (ExtraStructureBuildUIPrefabs.ContainsKey(ToBuildStructure.ExtraBuildUITyp) == false)
                Debug.LogError(ToBuildStructure.ExtraBuildUITyp + " ExtraBuildPreview has no Prefab assigned!");
            else {
                GameObject extra = Instantiate(ExtraStructureBuildUIPrefabs[ToBuildStructure.ExtraBuildUITyp]);
                extra.transform.SetParent(previewGO.transform);
            }
        }
        SpriteRenderer sr = previewGO.AddComponent<SpriteRenderer>();
        sr.sprite = ssc.GetStructureSprite(ToBuildStructure);
        sr.sortingLayerName = "StructuresUI";
        sr.color = new Color(sr.color.a, sr.color.b, sr.color.g, 0.5f);
        if(EditorController.IsEditor==false)
            TileSpriteController.Instance.AddDecider(TileCityDecider, true);
    }

    //FIXME this is not optimal 
    // change this to a diffrent way of showing/storing go
    public void ShowPreviewStructureOnTiles(Tile t) {
        if (previewGO == null) {
            CreatePreviewStructure();
        }
        previewGO.SetActive(true);

        //this is for extra ui when building like 
        //how effective it is to build there
        //this may move from this place
        if (EditorController.IsEditor == false)
            ToBuildStructure.UpdateExtraBuildUI(previewGO, t);
        float x = ((float)ToBuildStructure.TileWidth) / 2f - TileSpriteController.offset;
        float y = ((float)ToBuildStructure.TileHeight) / 2f - TileSpriteController.offset;
        previewGO.transform.position = new Vector3(GetTileUnderneathMouse().X + x,
                                                   GetTileUnderneathMouse().Y + y, 0);
        previewGO.transform.eulerAngles = new Vector3(0, 0, 360 - ToBuildStructure.rotation);
    }
    public void RemovePrefabs() {
        while (previewGameObjects.Count > 0) {
            //			Tile t = World.current.GetTileAt (previewGameObjects[0].transform.position.x,previewGameObjects[0].transform.position.y);
            //			t.TileState = TileMark.Reset;
            GameObject go = previewGameObjects[0];
            previewGameObjects.RemoveAt(0);
            SimplePool.Despawn(go);
        }
        //so it has to be "moved" to be visible
        if (previewGO != null)
            previewGO.SetActive(false);
    }
    void ShowRedPrefabOnTile(Tile t) {
        if (t == null) {
            return;
        }
        // Display the building hint on top of this tile position
        GameObject go = SimplePool.Spawn(redTileCursorPrefab, new Vector3(t.X + 0.5f, t.Y + 0.5f, 0), Quaternion.identity);
        go.transform.SetParent(this.transform, true);
        previewGameObjects.Add(go);
    }
    void ShowPrefabOnTile(Tile t) {
        if (t == null) {
            return;
        }
        // Display the building hint on top of this tile position
        GameObject go = SimplePool.Spawn(greenTileCursorPrefab, new Vector3(t.X + 0.5f, t.Y + 0.5f, 0), Quaternion.identity);
        go.transform.SetParent(this.transform, true);
        previewGameObjects.Add(go);
    }
    void ShowHighlightOnTiles() {
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }
        if (ToBuildStructure.StructureRange == 0) {
            return;
        }
        if (highlightGO != null)
            return;
        HighlightTiles = new HashSet<Tile>(ToBuildStructure.PrototypeTiles);

        int range = ToBuildStructure.StructureRange * 2; // cause its the radius
        int width = range + ToBuildStructure.TileWidth;
        int height = range + ToBuildStructure.TileWidth;

        highlightGO = GetHighlightGameObject(width, height, HighlightTiles);
        // offset based on even or uneven so it is centered properly
        // its working now?!? -- but leaving it in if its makes problems in the future
        // nope? 0 not working again
        float xoffset = 0;
        float yoffset = 0;
        if(ToBuildStructure.TileWidth != ToBuildStructure.TileHeight ) {
            if(ToBuildStructure.TileWidth % 3 == ToBuildStructure.TileHeight % 3) {
                xoffset = ToBuildStructure.TileWidth % 3 == 0 ? 0f : -0.5f;
                yoffset = ToBuildStructure.TileHeight % 3 == 0 ? 0f : -0.5f;
            } else {
                xoffset = ToBuildStructure.TileWidth % 2 == 0 ? 0f : 0.5f;
                yoffset = ToBuildStructure.TileHeight % 2 == 0 ? 0f : 0.5f;
            }
        }
        highlightGO.transform.parent = previewGO.transform;
        highlightGO.transform.localPosition = new Vector3(xoffset, yoffset);
    }

    GameObject GetHighlightGameObject(int width, int height, IEnumerable<Tile> tiles) {
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
        if (Input.GetMouseButtonUp(0)) {
            switch (mouseUnitState) {
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
        if (Input.GetMouseButtonDown(1)) {
            if (SelectedUnit.playerNumber != PlayerController.currentPlayerNumber) {
                mouseState = MouseState.Idle;
                return;
            }
            Transform hit = MouseRayCast();
            if (hit == null) {
                switch (mouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;
                    case MouseUnitState.Normal:
                        SelectedUnit.GiveMovementCommand(MapClampedMousePosition.x, MapClampedMousePosition.y, OverrideCurrentSetting);
                        break;
                    case MouseUnitState.Patrol:
                        mouseUnitState = MouseUnitState.Normal;
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

    private void UnselectUnit() {
        if (SelectedUnit != null)
            SelectedUnit.UnregisterOnDestroyCallback(OnUnitDestroy);
        SelectedUnit = null;
        SelectedStructure = null;
        UIController.Instance.CloseInfoUI();
        mouseState = MouseState.Idle;
        mouseUnitState = MouseUnitState.None;
    }

    void UpdateBuildDragging() {
        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }

        // Start Drag
        if (Input.GetMouseButtonDown(0)) {
            dragStartPosition = currFramePositionOffset;
        }

        int start_x = Mathf.FloorToInt(dragStartPosition.x);
        int end_x = Mathf.FloorToInt(currFramePositionOffset.x);
        int start_y = Mathf.FloorToInt(dragStartPosition.y);
        int end_y = Mathf.FloorToInt(currFramePositionOffset.y);

        // We may be dragging in the "wrong" direction, so flip things if needed.
        if (end_x < start_x) {
            int tmp = end_x;
            end_x = start_x;
            start_x = tmp;
        }
        if (end_y < start_y) {
            int tmp = end_y;
            end_y = start_y;
            start_y = tmp;
        }
        
        HashSet<Tile> tiles = new HashSet<Tile>();
        if (Input.GetMouseButton(0)) {
            // Display a preview of the drag area
            tiles = new HashSet<Tile>(GetTilesStructures(start_x, end_x, start_y, end_y));
        }
        if (tiles.Count == 0) {
            tiles.Add(GetTileUnderneathMouse());

        }
        foreach (Tile item in tiles) {
            if (mouseState == MouseState.Destroy) {
                ShowRedPrefabOnTile(item);
            }
            else {
                ShowSinglePreview(item);
            }
        }
        // End Drag
        if (Input.GetMouseButtonUp(0)) {
            List<Tile> ts = new List<Tile>(GetTilesStructures(start_x, end_x, start_y, end_y));
            if (ts != null) {
                if (mouseState == MouseState.Destroy) {
                    bool isGod = EditorController.IsEditor; //TODO: add cheat to set this
                    BuildController.Instance.DestroyStructureOnTiles(ts, PlayerController.Instance?.CurrPlayer, isGod);
                }
                else {
                    if (ToBuildStructure == null) {
                        return;
                    }
                    Build(ts, true);
                }
            }
        }
    }

    private IEnumerable<Tile> GetTilesStructures(int start_x, int end_x, int start_y, int end_y) {
        int width = 1;
        int height = 1;
        List<Tile> tiles = new List<Tile>();
        if (ToBuildStructure != null) {
            width = ToBuildStructure.TileWidth;
            height = ToBuildStructure.TileHeight;
        }
        for (int x = start_x; x <= end_x; x += width) {
            for (int y = start_y; y <= end_y; y += height) {
                if (tiles.Contains(World.Current.GetTileAt(x, y)) == false)
                    tiles.Add(World.Current.GetTileAt(x, y));
            }
        }
        return tiles;
    }


    void UpdatePathBetweenTiles() {
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }
        // Start Path
        if (Input.GetMouseButtonDown(0)) {
            pathStartPosition = currFramePositionOffset;
        }
        if (Input.GetMouseButton(0)) {
            int start_x = Mathf.FloorToInt(pathStartPosition.x);
            int start_y = Mathf.FloorToInt(pathStartPosition.y);
            Tile pathStartTile = World.Current.GetTileAt(start_x, start_y);

            if (pathStartTile == null || pathStartTile.Island == null) {
                return;
            }
            int end_x = Mathf.FloorToInt(currFramePositionOffset.x);
            int end_y = Mathf.FloorToInt(currFramePositionOffset.y);
            Tile pathEndTile = World.Current.GetTileAt(end_x, end_y);
            if (pathEndTile == null) {
                return;
            }
            if (pathStartTile.Island != null && pathEndTile.Island != null) {
                path = new Path_AStar(pathStartTile.Island, pathStartTile, pathEndTile, false, Path_Heuristics.Manhattan);
            }
            if (path.path == null) {
                return;
            }
            foreach (Tile t in path.path) {
                ShowSinglePreview(t);
            }
        }
        // End path
        if (Input.GetMouseButtonUp(0)) {
            if (path == null || path.path == null) {
                return;
            }
            Build(new List<Tile>(path.path), true);
        }
    }

    void Build(List<Tile> t, bool single = false) {
        if(EditorController.IsEditor) {
            EditorController.Instance.BuildOn(t, single);
        } else {
            if (mouseUnitState == MouseUnitState.Build) {
                BuildController.Instance.CurrentPlayerBuildOnTile(t, single, PlayerController.currentPlayerNumber, false, SelectedUnit);
            }
            else {
                BuildController.Instance.CurrentPlayerBuildOnTile(t, single, PlayerController.currentPlayerNumber, false);
            }
        }
    }
    public void BuildFromUnit() {
        mouseUnitState = MouseUnitState.Build;
        BuildController.Instance.SettleFromUnit(SelectedUnit);
    }
    public void SetToPatrolMode() {
        mouseUnitState = MouseUnitState.Patrol;
    }
    public void ResetBuild(Structure structure, bool loading = false) {
        if (loading) {
            return;// there is no need to call any following
        }
        TileSpriteController.Instance.RemoveDecider(TileCityDecider);
        if(BuildController.Instance.BuildState != BuildStateModes.None)
            BuildController.Instance.ResetBuild();
        GameObject.Destroy(previewGO);
        previewGO = null;
        ToBuildStructure = null;
        HighlightTiles = null;
        if(mouseUnitState == MouseUnitState.Build) {
            UnselectUnit();
        }
    }

    internal void ClearUnitGroup() {
        if (selectedUnitGroup == null)
            return;
        selectedUnitGroup.ForEach(x => x.UnregisterOnDestroyCallback(OnUnitDestroy));
        selectedUnitGroup.Clear();
        UIController.Instance.CloseInfoUI();
        mouseState = MouseState.Idle;
        mouseUnitState = MouseUnitState.None;
        selectedUnitGroup.Clear();
    }
    internal void RemoveUnitFromGroup(Unit unit) {
        selectedUnitGroup.Remove(unit);
        if (selectedUnitGroup.Count == 0) {
            UIController.Instance.CloseUnitGroupUI();
            MouseController.Instance.ClearUnitGroup();
        }
        if (selectedUnitGroup.Count == 1) {
            UIController.Instance.CloseUnitGroupUI();
            UIController.Instance.OpenUnitUI(selectedUnitGroup[0]);
            SelectUnit(selectedUnitGroup[0]);
            selectedUnitGroup.Clear();
        }
        unit.UnregisterOnDestroyCallback(OnUnitDestroy);
    }
    private void OnUnitDestroy(Unit unit, IWarfare warfare) {
        if (SelectedUnit == unit) {
            mouseState = MouseState.Idle;
            _selectedUnit = null;
        }
        else {
            if (selectedUnitGroup.Contains(unit))
                selectedUnitGroup.Remove(unit);
        }
    }
    internal void StopUnit() {
        //if null or not player unit return without doing anything
        if (SelectedUnit == null || SelectedUnit.IsPlayerUnit() == false) {
            return;
        }
        SelectedUnit.GoIdle();
    }
    void OnDestroy() {
        Instance = null;
    }
    Transform MouseRayCast() {
        return Physics2D.Raycast(new Vector2(currFramePosition.x, currFramePosition.y), Vector2.zero, 200).transform;
    }
    /// <summary>
    /// what to on escape press 
    ///  - set tobuildstructure to null
    ///  - set mousestate to drag
    /// </summary>
    public void Escape() {
        dragStartPosition = currFramePosition;
        UnselectUnit();
        ClearUnitGroup();
        ResetBuild(null);
        mouseState = MouseState.Idle;
        mouseUnitState = MouseUnitState.None;
    }

    TileMark TileCityDecider(Tile t) {
        if (t == null) {
            return TileMark.None;
        }
        if (HighlightTiles != null && HighlightTiles.Contains(t)) {
            return TileMark.Highlight;
        }
        else if (t.City != null && t.City.IsCurrPlayerCity()) {
            return TileMark.None;
        }
        else {
            return TileMark.Dark;
        }
    }
    [Serializable]
    public struct ExtraStructureBuildUI {
        public ExtraBuildUI Type;
        public GameObject Prefab;
    }

}
