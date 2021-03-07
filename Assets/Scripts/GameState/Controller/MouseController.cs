using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections;

public enum MouseState { Idle, BuildDrag, BuildPath, BuildSingle, Unit, UnitGroup, Destroy, DragSelect };
public enum MouseUnitState { None, Normal, Patrol, Build };
public enum TileHighlightType { Green, Red }
public class MouseController : MonoBehaviour {

    public static MouseController Instance { get; protected set; }
    public GameObject structurePreviewRendererPrefab;
    public GameObject greenTileCursorPrefab;
    public GameObject redTileCursorPrefab;
    public ExtraStructureBuildUI[] extraStructureBuildUIPrefabsEditor;
    Dictionary<ExtraBuildUI, GameObject> ExtraStructureBuildUIPrefabs;
    GameObject highlightGO;

    // The world-position of the mouse last frame.
    Vector3 lastFramePosition;
    Vector3 CurrFramePositionOffset => currFramePosition + new Vector3(TileSpriteController.offset, TileSpriteController.offset, 0);

    Vector3 lastFrameGUIPosition;

    Vector3 currFramePosition;
    private Vector3 dragStartPosition;

    private Vector3 pathStartPosition;

    public static bool autorotate = true;
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
    HashSet<Tile> destroyTiles;
    Dictionary<Tile, TilePreview> tileToPreviewGO;
    Dictionary<Tile, StructurePreview> tileToStructurePreview;
    GameObject singleStructurePreview;
    private Structure SelectedStructure;
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
            if (SelectedStructure != null)
                return SelectedStructure;
            if (CameraController.Instance.nearestIsland != null)
                return CameraController.Instance.nearestIsland;
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
        tileToPreviewGO = new Dictionary<Tile, TilePreview>();
        tileToStructurePreview = new Dictionary<Tile, StructurePreview>();
        destroyTiles = new HashSet<Tile>();
        ExtraStructureBuildUIPrefabs = new Dictionary<ExtraBuildUI, GameObject>();
        foreach (ExtraStructureBuildUI esbu in extraStructureBuildUIPrefabsEditor) {
            ExtraStructureBuildUIPrefabs[esbu.Type] = esbu.Prefab;
        }
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


    // Update is called once per frame
    void Update() {
        if (EditorController.IsEditor==false && PlayerController.GameOver)
            return;
        currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePosition.z = 0;
        if (currFramePosition.y < 0 || currFramePosition.x < 0) {
            return;
        }
        UpdateMouseStates();
        if (EditorController.IsEditor == false) {
            UpdateDragBox();
        } else {
            UpdateEditorStuff();
        }

        if (Input.GetMouseButtonDown(1) && mouseState != MouseState.Idle 
            && mouseState != MouseState.Unit && mouseState != MouseState.UnitGroup) {
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
                UpdateBuildPath();
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
                UpdateDestroy();
                break;
            case MouseState.UnitGroup:
                UpdateUnitGroup();
                break;
        }
    }

    private void UpdateDestroy() {
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }
        if (Input.GetMouseButtonDown(0)) {
            dragStartPosition = CurrFramePositionOffset;
        }
        int start_x = Mathf.FloorToInt(dragStartPosition.x);
        int end_x = Mathf.FloorToInt(CurrFramePositionOffset.x);
        int start_y = Mathf.FloorToInt(dragStartPosition.y);
        int end_y = Mathf.FloorToInt(CurrFramePositionOffset.y);
        if (Input.GetMouseButton(0)) {
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
        if (Input.GetMouseButtonUp(0)) {
            List<Tile> ts = new List<Tile>(GetTilesStructures(start_x, end_x, start_y, end_y));
            if (ts != null) {
                bool isGod = EditorController.IsEditor; //TODO: add cheat to set this
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
        HighlightUnits(unit);
    }
    private void SelectUnitGroup(List<Unit> units) {
        HighlightUnits(units.ToArray());
        mouseState = MouseState.UnitGroup;
        mouseUnitState = MouseUnitState.Normal;
        selectedUnitGroup = units;
        selectedUnitGroup.ForEach(x=>x.RegisterOnDestroyCallback(OnUnitDestroy));
        UIController.Instance.OpenUnitGroupUI(selectedUnitGroup.ToArray());
    }
    private void HighlightUnits(params Unit[] units) {
        UnitSpriteController.Instance.Highlight(units);
    }
    private void DehighlightUnits(params Unit[] units) {
        UnitSpriteController.Instance.Highlight(units);
    }
    private void UpdateSingle() {
        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }
        if (ToBuildStructure == null) {
            return;
        }
        UpdateSinglePreview();
        if (Input.GetMouseButtonDown(0)) {
            List<Tile> structureTiles = ToBuildStructure.GetBuildingTiles(GetTileUnderneathMouse());
            Build(structureTiles);
        }
    }
    void UpdateSinglePreview() {
        if(singleStructurePreview == null)
            singleStructurePreview = CreatePreviewStructure();
        float x = ((float)ToBuildStructure.TileWidth) / 2f - TileSpriteController.offset;
        float y = ((float)ToBuildStructure.TileHeight) / 2f - TileSpriteController.offset;
        singleStructurePreview.transform.position = new Vector3(GetTileUnderneathMouse().X + x,
                                                   GetTileUnderneathMouse().Y + y, 0);
        singleStructurePreview.transform.eulerAngles = new Vector3(0, 0, 360 - ToBuildStructure.rotation);
        List<Tile> tiles = ToBuildStructure.GetBuildingTiles(GetTileUnderneathMouse());
        foreach(Tile tile in tileToPreviewGO.Keys.Except(tiles).ToArray()) {
            SimplePool.Despawn(tileToPreviewGO[tile].gameObject);
            tileToPreviewGO.Remove(tile);
        }
        UpdateStructurePreview(tiles, 1);
    }

    void UpdateBuildDragging() {
        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }
        // Start Drag
        if (Input.GetMouseButtonDown(0)) {
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
        if (Input.GetMouseButton(0)) {
            // Display a preview of the drag area
            UpdateMultipleStructurePreviews(ts);
        }
        else {
            UpdateSinglePreview();
        }
        // End Drag
        if (Input.GetMouseButtonUp(0)) {
            Build(ts, true, true);
            ResetStructurePreviews();
        }
    }
    void UpdateBuildPath() {
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }
        // Start Path
        if (Input.GetMouseButtonDown(0)) {
            pathStartPosition = CurrFramePositionOffset;
            if (singleStructurePreview != null) {
                ResetStructurePreviews();
            }
        }
        if (Input.GetMouseButton(0)) {
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
                    (path == null || path.endTiles == null || path.endTiles.Contains(pathEndTile) == false)) {
                path = new Path_AStar(pathStartTile.Island, pathStartTile, pathEndTile, false, 
                                        Path_Heuristics.Manhattan, true, PlayerController.currentPlayerNumber);
            }
            if (path.path == null) {
                return;
            }
            UpdateMultipleStructurePreviews (path.path);
        }
        else {
            UpdateSinglePreview();
        }
        // End path
        if (Input.GetMouseButtonUp(0)) {
            if (path == null || path.path == null) {
                return;
            }
            ResetStructurePreviews();
            Build(new List<Tile>(path.path), true);
        }
    }
    void UpdateMultipleStructurePreviews(IEnumerable<Tile> tiles) {
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
        foreach (StructurePreview preview in tileToStructurePreview.Values) {
            if (ToBuildStructure is RoadStructure) {
                string sprite = ToBuildStructure.SpriteName + RoadStructure.UpdateOrientation(preview.tile, tiles);
                preview.spriteRenderer.sprite = StructureSpriteController.Instance.GetStructureSprite(sprite);
            }
            UpdateStructurePreview(preview.tiles, preview.number);
        }
    }
    void UpdateStructurePreview(List<Tile> tiles, int number) {
        bool ownCityTileCount = ToBuildStructure.InCityCheck(tiles, PlayerController.currentPlayerNumber);
        bool hasEnoughResources = 
            tiles[0].Island?.FindCityByPlayer(PlayerController.currentPlayerNumber)?.HasEnoughOfItems(ToBuildStructure.BuildingItems, number) == true 
            && PlayerController.CurrentPlayer.HasEnoughMoney(ToBuildStructure.BuildCost * number);
        UpdateStructurePreviewTiles(tiles, ownCityTileCount && hasEnoughResources);
    }
    void UpdateStructurePreviewTiles(List<Tile> tiles, bool overrideTile) {
        Dictionary<Tile, bool> tileToCanBuild = ToBuildStructure.CheckForCorrectSpot(tiles);
        foreach (Tile tile in tiles) {
            bool specialTileCheck = true;
            if (mouseUnitState == MouseUnitState.Build) {
                if (Vector2.Distance(tile.Vector2, SelectedUnit.PositionVector2) > SelectedUnit.BuildRange) {
                    specialTileCheck = false;
                }
            }
            bool canBuild = overrideTile && specialTileCheck && tileToCanBuild[tile] 
                        && Structure.IsTileCityViable(tile, PlayerController.currentPlayerNumber);
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
        previewGO.transform.SetParent(this.transform, true);
        if(ToBuildStructure.ExtraBuildUITyp!=ExtraBuildUI.None) {
            if (ExtraStructureBuildUIPrefabs.ContainsKey(ToBuildStructure.ExtraBuildUITyp) == false)
                Debug.LogError(ToBuildStructure.ExtraBuildUITyp + " ExtraBuildPreview has no Prefab assigned!");
            else {
                GameObject extra = Instantiate(ExtraStructureBuildUIPrefabs[ToBuildStructure.ExtraBuildUITyp]);
                extra.transform.SetParent(previewGO.transform);
            }
        }
        
        SpriteRenderer sr = previewGO.GetComponent<SpriteRenderer>();
        sr.sprite = StructureSpriteController.Instance.GetStructureSprite(ToBuildStructure);
        if(EditorController.IsEditor==false)
            TileSpriteController.Instance.AddDecider(TileCityDecider, true);
        AddRangeHighlight(previewGO);
        return previewGO;
    }

    void ShowTilePrefabOnTile(Tile t, TileHighlightType type) {
        if (tileToPreviewGO.ContainsKey(t)) {
            if (tileToPreviewGO[t].HighlightType == type) {
                return;
            } else {
                SimplePool.Despawn(tileToPreviewGO[t].gameObject);
                tileToPreviewGO.Remove(t);
            }
        }
        GameObject go = null;
        switch (type){
            case TileHighlightType.Green:
                go = greenTileCursorPrefab;
                break;
            case TileHighlightType.Red:
                go = redTileCursorPrefab;
                break;
        }
        go = SimplePool.Spawn(go, new Vector3(t.X + 0.5f, t.Y + 0.5f, 0), Quaternion.identity);
        // Display the building hint on top of this tile position
        go.transform.SetParent(this.transform, true);
        tileToPreviewGO.Add(t, new TilePreview(t,type,go));
    }
    void AddRangeHighlight(GameObject parent) {
        if (ToBuildStructure.StructureRange == 0)
            return;
        int range = ToBuildStructure.StructureRange * 2; // cause its the radius
        int width = range + ToBuildStructure.TileWidth;
        int height = range + ToBuildStructure.TileWidth;
        GetHighlightGameObject(width, height, ToBuildStructure.PrototypeTiles).transform.SetParent(parent.transform);
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
        Color32[] temp = tex.GetPixels32();
        tex.filterMode = FilterMode.Point;
        sr.sortingLayerName = "Structures";
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1);
        // offset based on even or uneven so it is centered properly
        // its working now?!? -- but leaving it in if its makes problems in the future
        // nope? 0 not working again
        if (ToBuildStructure != null && ToBuildStructure.TileWidth != ToBuildStructure.TileHeight) {
            float xoffset = 0;
            float yoffset = 0;
            if (ToBuildStructure.TileWidth % 3 == ToBuildStructure.TileHeight % 3) {
                xoffset = ToBuildStructure.TileWidth % 3 == 0 ? 0f : 0.5f;
                yoffset = ToBuildStructure.TileHeight % 3 == 0 ? 0f : 0.5f;
            }
            else {
                xoffset = ToBuildStructure.TileWidth % 2 == 0 ? 0f : -0.5f;
                yoffset = ToBuildStructure.TileHeight % 2 == 0 ? 0f : -0.5f;
            }
            highGO.transform.localPosition = new Vector3(xoffset, yoffset);
        }
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
        DehighlightUnits(SelectedUnit);
        SelectedUnit = null;
        SelectedStructure = null;
        UIController.Instance.CloseInfoUI();
        mouseState = MouseState.Idle;
        mouseUnitState = MouseUnitState.None;
    }

    private List<Tile> GetTilesStructures(int start_x, int end_x, int start_y, int end_y) {
        int width = 1;
        int height = 1;
        List<Tile> tiles = new List<Tile>();
        if (ToBuildStructure != null) {
            width = ToBuildStructure.TileWidth;
            height = ToBuildStructure.TileHeight;
        }
        // We may be dragging in the "wrong" direction, so flip things if needed.
        //if (end_x < start_x) {
        //    int tmp = end_x;
        //    end_x = start_x;
        //    start_x = tmp;
        //}
        //if (end_y < start_y) {
        //    int tmp = end_y;
        //    end_y = start_y;
        //    start_y = tmp;
        //}
        if (end_x >= start_x && end_y >= start_y) {
            for (int x = start_x; x <= end_x; x += width) {
                for (int y = start_y; y <= end_y; y += height) {
                    tiles.Add(World.Current.GetTileAt(x, y));
                }
            }
        } else
        if (end_x > start_x && end_y <= start_y) {
            for (int x = start_x; x <= end_x; x += width) {
                for (int y = start_y; y >= end_y; y -= height) {
                    tiles.Add(World.Current.GetTileAt(x, y));
                }
            }
        } else
        if (end_x <= start_x && end_y > start_y) {
            for (int x = start_x; x >= end_x; x -= width) {
                for (int y = start_y; y <= end_y; y += height) {
                    tiles.Add(World.Current.GetTileAt(x, y));
                }
            }
        } else
        if (end_x <= start_x && end_y <= start_y) {
            for (int x = start_x; x >= end_x; x -= width) {
                for (int y = start_y; y >= end_y; y -= height) {
                    tiles.Add(World.Current.GetTileAt(x, y));
                }
            }
        }
        return tiles;
    }

    void Build(List<Tile> t, bool single = false, bool previewOrder = false) {
        if(EditorController.IsEditor) {
            EditorController.Instance.BuildOn(t, single);
        } else {
            if (mouseUnitState == MouseUnitState.Build) {
                BuildController.Instance.CurrentPlayerBuildOnTile(t, single, PlayerController.currentPlayerNumber, false, SelectedUnit);
            }
            else {
                if(previewOrder) {
                    foreach(StructurePreview sp in tileToStructurePreview.Values.OrderBy(x=>x.number)) {
                        BuildController.Instance.CurrentPlayerBuildOnTile(sp.tiles, false, PlayerController.currentPlayerNumber, false);
                    }
                } else {
                    BuildController.Instance.CurrentPlayerBuildOnTile(t, single, PlayerController.currentPlayerNumber, false);
                }
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
        if(BuildController.Instance.BuildState != BuildStateModes.None)
            BuildController.Instance.ResetBuild();
        ResetStructurePreviews();
        destroyTiles.Clear();
        ToBuildStructure = null;
        if(mouseUnitState == MouseUnitState.Build) {
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
        if(singleStructurePreview) {
            SimplePool.Despawn(singleStructurePreview);
            foreach (Transform t in singleStructurePreview.transform) {
                Destroy(t.gameObject);
            }
            singleStructurePreview = null;
        }
    }
    internal void ClearUnitGroup() {
        if (selectedUnitGroup == null)
            return;
        DehighlightUnits(selectedUnitGroup.ToArray());
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
    public Tile GetTileUnderneathMouse() {
        return World.Current.GetTileAt(CurrFramePositionOffset);
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
    class TilePreview {
        public Tile tile;
        public GameObject gameObject;
        public TileHighlightType HighlightType;

        public TilePreview(Tile t, TileHighlightType type, GameObject gameObject) {
            tile = t;
            HighlightType = type;
            this.gameObject = gameObject;
        }
    }
    class StructurePreview {
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
