using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections;

public enum MouseState { Idle, Drag, Path, Single, Unit, Destroy };
public enum MouseUnitState { None, Normal, Patrol, Build };

public class MouseController : MonoBehaviour {

    public static MouseController Instance { get; protected set; }

    public GameObject greenTileCursorPrefab;
    public GameObject redTileCursorPrefab;
    GameObject previewGO;
    GameObject highlightGO;

    // The world-position of the mouse last frame.
    Vector3 lastFramePosition;
    Vector3 currFramePosition;
    private Vector3 dragStartPosition;
    private Vector3 pathStartPosition;
    StructureSpriteController ssc;
    public static bool autorotate = true;
    public static bool OverrideCommands => InputHandler.ShiftKey == false;

    private HashSet<Tile> _highlightTiles;
    HashSet<Tile> HighlightTiles {
        get { return _highlightTiles; }
        set {
            _highlightTiles = value;
        }
    }
    // The world-position start of our left-mouse drag operation
    List<GameObject> previewGameObjects;

    BuildController BuildController => BuildController.Instance;
    UIController UIController => UIController.Instance;
    private Structure SelectedStructure;
    protected Structure _toBuildstructure;
    public Structure ToBuildStructure {
        get {
            return _toBuildstructure;
        }
        set {
            GameObject.Destroy(previewGO);
            ResetBuild(null);
            _toBuildstructure = value;
        }
    }

    public MouseState mouseState = MouseState.Idle;
    MouseUnitState mouseUnitState = MouseUnitState.None;

    private Path_AStar path;

    private Unit _selectedUnit;
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

    private void OnUnitDestroy(Unit obj) {
        mouseState = MouseState.Idle;
        _selectedUnit = null;
    }

    // Use this for initialization
    void Start() {
        if (Instance != null) {
            Debug.LogError("There should never be two mouse controllers.");
        }
        Instance = this;
        previewGameObjects = new List<GameObject>();
        BuildController.RegisterStructureCreated(ResetBuild);
        _highlightTiles = new HashSet<Tile>();
        ssc = GameObject.FindObjectOfType<StructureSpriteController>();
    }

    /// <summary>
    /// Gets the mouse position in world space.
    /// </summary>
    public Vector3 GetMousePosition() {
        return currFramePosition;
    }
    /// <summary>
    /// Gets the mouse position in world space.
    /// </summary>
    public Vector3 GetLastMousePosition() {
        return lastFramePosition;
    }


    // Update is called once per frame
    void Update() {

        currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePosition.z = 0;
        if (currFramePosition.y < 0 || currFramePosition.x < 0) {
            return;
        }
        RemovePrefabs();
        //UpdateCursor();

        switch (mouseState) {
            case MouseState.Idle:
                if (Input.GetMouseButtonDown(0)) {
                    //mouse press decide what it hit 
                    DecideWhatUIToShow(Physics2D.Raycast(new Vector2(currFramePosition.x, currFramePosition.y), Vector2.zero, 200));
                }
                break;
            case MouseState.Drag:
                UpdateDragging();
                break;
            case MouseState.Path:
                UpdatePathBetweenTiles();
                break;
            case MouseState.Single:
                UpdateSingle();
                break;
            case MouseState.Unit:
                if (SelectedUnit == null) {
                    mouseState = MouseState.Idle;
                    return;
                }
                UpdateUnit();
                break;
            case MouseState.Destroy:
                UpdateDragging();
                break;
        }
        
        if (Input.GetMouseButtonDown(1) && mouseState != MouseState.Unit) {
            ResetBuild(null);
            mouseState = MouseState.Idle;
        }
        // Save the mouse position from this frame
        // We don't use currFramePosition because we may have moved the camera.
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }
    private void DecideWhatUIToShow(RaycastHit2D hit) {
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }
        if (hit) {
            //Debug.Log (hit.transform.name); 
            ITargetableHoldingScript targetableHoldingScript = hit.transform.GetComponent<ITargetableHoldingScript>();
            if (targetableHoldingScript != null && targetableHoldingScript.IsUnit) {
                mouseState = MouseState.Unit;
                SelectedUnit = (Unit) targetableHoldingScript.Holding;
                SelectedUnit.RegisterOnDestroyCallback(OnUnitDestroy);
                UIController.OpenUnitUI(SelectedUnit);
                UIDebug(SelectedUnit);
            }
            else
            if (SelectedUnit == null) {
                Tile t = GetTileUnderneathMouse();
                if (t.Structure != null) {
                    UIDebug(t.Structure);
                    UIController.OpenStructureUI(t.Structure);
                    SelectedStructure = t.Structure;
                }

            }
        }
        else {
            UIDebug(GetTileUnderneathMouse());
            if (mouseState != MouseState.Unit) {
                UIController.CloseInfoUI();
                SelectedUnit = null;
                SelectedStructure = null;
            }
        }
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
            UIController.ShowDebugForObject(obj);
        }
    }
    private void ShowSinglePreview(Tile tile) {
        if (tile == null) {
            return;
        }
        int tempTest = ToBuildStructure.rotated;
        Dictionary<Tile, bool> tileToCanBuild = null;
        if (autorotate) {
            for (int r = 0; r < 4; r++) {
                ToBuildStructure.AddTimes90ToRotate(r);
                List<Tile> structureTiles = ToBuildStructure.GetBuildingTiles(tile.X, tile.Y);
                tileToCanBuild = ToBuildStructure.CorrectSpot(structureTiles);
                if (tileToCanBuild.Values.ToList().Contains(false) == false) {
                    break;
                }

            }
        }

        ShowPreviewStructureOnTiles(tile);
        StartCoroutine(ShowHighlightOnTiles());

        if (tileToCanBuild.Values.ToList().Contains(false)) {
            //TODO fix this temporary fix
            // it is so that previews dont spinn like crazy BUT find better way todo this
            ToBuildStructure.rotated = tempTest;
        }
        foreach (Tile t in tileToCanBuild.Keys) {
            if (t == null) {
                continue;
            }
            //not viable city overrides everything
            if (ToBuildStructure.IsTileCityViable(t, PlayerController.currentPlayerNumber) == false) {
                ShowRedPrefabOnTile(t);
                continue;
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
        return World.Current.GetTileAt(currFramePosition.x + 0.5f, currFramePosition.y + 0.5f);
    }

    public void CreatePreviewStructure() {
        previewGO = new GameObject();
        previewGO.transform.SetParent(this.transform, true);
        previewGO.name = "PreviewGO";

        SpriteRenderer sr = previewGO.AddComponent<SpriteRenderer>();

        sr.sprite = ssc.GetStructureSprite(ToBuildStructure);
        sr.sortingLayerName = "StructuresUI";
        sr.color = new Color(sr.color.a, sr.color.b, sr.color.g, 0.5f);
        //Structure.GetExtraBuildUIData ();
        //TODO: create extra ui here
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
        ToBuildStructure.UpdateExtraBuildUI(previewGO, t);
        float x = 0;
        float y = 0;
        if (ToBuildStructure.TileWidth > 1) {
            x = 0.5f + ((float)ToBuildStructure.TileWidth) / 2 - 1;
        }
        if (ToBuildStructure.TileHeight > 1) {
            y = 0.5f + ((float)ToBuildStructure.TileHeight) / 2 - 1;
        }
        previewGO.transform.position = new Vector3(GetTileUnderneathMouse().X + x,
                                                    GetTileUnderneathMouse().Y + y, 0);
        previewGO.transform.eulerAngles = new Vector3(0, 0, 360 - ToBuildStructure.rotated);
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
        GameObject go = SimplePool.Spawn(redTileCursorPrefab, new Vector3(t.X, t.Y, 0), Quaternion.identity);
        go.transform.SetParent(this.transform, true);
        previewGameObjects.Add(go);
    }
    void ShowPrefabOnTile(Tile t) {
        if (t == null) {
            return;
        }

        // Display the building hint on top of this tile position
        GameObject go = SimplePool.Spawn(greenTileCursorPrefab, new Vector3(t.X, t.Y, 0), Quaternion.identity);
        go.transform.SetParent(this.transform, true);
        previewGameObjects.Add(go);
    }
    IEnumerator ShowHighlightOnTiles() {
        if (EventSystem.current.IsPointerOverGameObject()) {
            yield break;
        }
        if (ToBuildStructure.StructureRange == 0) {
            yield break;
        }
        if (highlightGO != null)
            yield break;
        HighlightTiles = new HashSet<Tile>(ToBuildStructure.MyPrototypeTiles);
        highlightGO = new GameObject();

        int range = ToBuildStructure.StructureRange * 2; // cause its the radius
        int width = range + ToBuildStructure.TileWidth;
        int height = range + ToBuildStructure.TileWidth;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        tex.SetPixels32(new Color32[width * height]);

        foreach (Tile t in HighlightTiles) {
            tex.SetPixel(t.X, t.Y, new Color32(255, 255, 255, 20));
        }

        tex.filterMode = FilterMode.Point;
        tex.Apply();

        SpriteRenderer sr = highlightGO.AddComponent<SpriteRenderer>();
        // offset based on even or uneven so it is centered properly
        // its working now?!? -- but leaving it in if its makes problems in the future
        float xoffset = 0; // ToBuildStructure.TileWidth % 2 == 0 ? 0f : -0.5f;
        float yoffset = 0; // ToBuildStructure.TileHeight % 2 == 0 ? 0f : -0.5f;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1);
        sr.sortingLayerName = "DarkLayer";
        highlightGO.transform.parent = previewGO.transform;
        highlightGO.transform.localPosition = new Vector3(xoffset, yoffset);
    }


    private void UpdateUnit() {
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
                    SelectedUnit = null;
                    SelectedStructure = null;
                    UIController.CloseInfoUI();
                    mouseState = MouseState.Idle;
                    mouseUnitState = MouseUnitState.None;
                    break;
                case MouseUnitState.Patrol:
                    SelectedUnit.AddPatrolCommand(currFramePosition.x, currFramePosition.y);
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
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(currFramePosition.x, currFramePosition.y), Vector2.zero, 200);
            if (hit.transform == null) {
                switch (mouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;
                    case MouseUnitState.Normal:
                        SelectedUnit.GiveMovementCommand(currFramePosition.x, currFramePosition.y, OverrideCommands);
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
                ITargetableHoldingScript targetableHoldingScript = hit.transform.GetComponent<ITargetableHoldingScript>();
                if (targetableHoldingScript != null) {
                    SelectedUnit.GiveAttackCommand(hit.transform.gameObject.GetComponent<ITargetableHoldingScript>().Holding, OverrideCommands);
                }
                else
                if (hit.transform.GetComponent<CrateHoldingScript>() != null) {
                    SelectedUnit.TryToAddCrate(hit.transform.GetComponent<CrateHoldingScript>().thisCrate);
                }
                else
                if (targetableHoldingScript == null) {
                    Tile t = GetTileUnderneathMouse();
                    if (t.Structure != null) {
                        if (t.Structure is ICapturable) {
                            SelectedUnit.GiveCaptureCommand((ICapturable)t.Structure, OverrideCommands);
                        }
                        else
                        if (t.Structure is TargetStructure) {
                            SelectedUnit.GiveAttackCommand((TargetStructure)t.Structure, OverrideCommands);
                        }
                        return;
                    }
                }
            }
        }
    }

    void UpdateDragging() {
        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }

        // Start Drag
        if (Input.GetMouseButtonDown(0)) {
            dragStartPosition = currFramePosition;
        }

        int start_x = Mathf.FloorToInt(dragStartPosition.x + 0.5f);
        int end_x = Mathf.FloorToInt(currFramePosition.x + 0.5f);
        int start_y = Mathf.FloorToInt(dragStartPosition.y + 0.5f);
        int end_y = Mathf.FloorToInt(currFramePosition.y + 0.5f);

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
                    BuildController.DestroyStructureOnTiles(ts, PlayerController.Instance.CurrPlayer);
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
            pathStartPosition = currFramePosition;
        }
        if (Input.GetMouseButton(0)) {
            int start_x = Mathf.FloorToInt(pathStartPosition.x + 0.5f);
            int start_y = Mathf.FloorToInt(pathStartPosition.y + 0.5f);
            Tile pathStartTile = World.Current.GetTileAt(start_x, start_y);

            if (pathStartTile == null || pathStartTile.MyIsland == null) {
                return;
            }
            int end_x = Mathf.FloorToInt(currFramePosition.x + 0.5f);
            int end_y = Mathf.FloorToInt(currFramePosition.y + 0.5f);
            Tile pathEndTile = World.Current.GetTileAt(end_x, end_y);
            if (pathEndTile == null) {
                return;
            }
            if (pathStartTile.MyIsland != null && pathEndTile.MyIsland != null) {
                path = new Path_AStar(pathStartTile.MyIsland, pathStartTile, pathEndTile, false);
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
            // Loop through all the tiles
            if (path == null || path.path == null) {
                return;
            }
            Build(new List<Tile>(path.path), true);
        }


    }
    void Build(List<Tile> t, bool single = false) {
        if (mouseState == MouseState.Unit && mouseUnitState == MouseUnitState.Build) {
            BuildController.BuildOnTile(t, single, PlayerController.currentPlayerNumber, false, SelectedUnit);
        }
        else {
            BuildController.BuildOnTile(t, single, PlayerController.currentPlayerNumber, false);
        }
    }
    public void BuildFromUnit() {
        mouseUnitState = MouseUnitState.Build;
        BuildController.SettleFromUnit(SelectedUnit);
    }
    public void SetToPatrolMode() {
        mouseUnitState = MouseUnitState.Patrol;

    }
    public void ResetBuild(Structure structure, bool loading = false) {
        if (loading) {
            return;// there is no need to call any following
        }
        TileSpriteController.Instance.RemoveDecider(TileCityDecider);
        GameObject.Destroy(previewGO);
        previewGO = null;
        structure = null;
        HighlightTiles = null;
        if (mouseUnitState == MouseUnitState.Build) {
            SelectedUnit = null;
            mouseUnitState = MouseUnitState.Normal;
        }
    }
    void OnDestroy() {
        Instance = null;
    }

    /// <summary>
    /// what to on escape press 
    ///  - set tobuildstructure to null
    ///  - set mousestate to drag
    /// </summary>
    public void Escape() {
        SelectedUnit = null;
        ResetBuild(null);
        BuildController.ResetBuild();
        this.mouseState = MouseState.Idle;
    }

    TileMark TileCityDecider(Tile t) {
        if (t == null) {
            return TileMark.None;
        }
        if (HighlightTiles != null && HighlightTiles.Contains(t)) {
            return TileMark.Highlight;
        }
        else if (t.MyCity != null && t.MyCity.IsCurrPlayerCity()) {
            return TileMark.None;
        }
        else {
            return TileMark.Dark;
        }
    }
}
