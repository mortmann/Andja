using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using System.Linq;

public enum MouseState { Idle,Drag, Path, Single, Unit,Destroy };

public class MouseController : MonoBehaviour {

    public static MouseController Instance { get; protected set; }

    public GameObject greenTileCursorPrefab;
	public GameObject redTileCursorPrefab;
	GameObject previewGO;
	// The world-position of the mouse last frame.
	Vector3 lastFramePosition;
	Vector3 currFramePosition;
	StructureSpriteController ssc;

	HashSet<Tile> _highlightTiles;
	HashSet<Tile> HighlightTiles {
		get { return _highlightTiles; }
		set {
			if (value == null) {
				foreach (Tile t in _highlightTiles) {
					if (t == null) {
						continue;
					}
					t.TileState = TileMark.Reset;
				}
				return;
			} 
			_highlightTiles = value;
		}
	}
	// The world-position start of our left-mouse drag operation
	Vector3 dragStartPosition;
	List<GameObject> previewGameObjects;

	BuildController bmc;
	UIController uic;

	//substate for unit
	bool patrolCommandToAdd;

	protected Structure _structure;
	public Structure structure {
		get{ 
			return _structure;
		}
		set {
			GameObject.Destroy (previewGO);
			previewGO = null;
			HighlightTiles = null;
			_structure = value;
		}
	}

	public MouseState mouseState = MouseState.Idle;
    private Vector3 pathStartPosition;
    private Path_AStar path;
	private Unit _selectedUnit;
	private Unit SelectedUnit  {
		get { return _selectedUnit;}
		set { 
			patrolCommandToAdd = false; 
			_selectedUnit = value;
		}
	} 
    // Use this for initialization
    void Start () {
        if (Instance != null) {
            Debug.LogError("There should never be two mouse controllers.");
        }
        Instance = this;
        previewGameObjects = new List<GameObject>();
		bmc = GameObject.FindObjectOfType<BuildController>();
		bmc.RegisterStructureCreated (ResetBuilding);
		uic = GameObject.FindObjectOfType<UIController> ();
		_highlightTiles = new HashSet<Tile> ();
		ssc = GameObject.FindObjectOfType<StructureSpriteController> ();
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
		if(currFramePosition.y < 0 || currFramePosition.x < 0){
			return;
		} 
		RemovePrefabs ();
        //UpdateCursor();
		if (mouseState == MouseState.Drag||mouseState == MouseState.Destroy) { 
            UpdateDragging();
        } else
        if (mouseState == MouseState.Path) {
            UpdatePathBetweenTiles();
		} else
		if (mouseState == MouseState.Single) {
			UpdateSingle();
		} else 
		if (mouseState == MouseState.Unit && SelectedUnit != null) {
            UpdateUnit();
		} 
        if (Input.GetMouseButtonDown(0)) {
			
			if( EventSystem.current.IsPointerOverGameObject() ) {
				return;
			}
//			Debug.Log (GetTileUnderneathMouse ().toString ()); 
			//mouse press decide what it hit 
			RaycastHit2D hit = Physics2D.Raycast(new Vector2(currFramePosition.x, currFramePosition.y), Vector2.zero, 200);
			DecideWhatUIToShow (hit);
        }
		 
		// Save the mouse position from this frame
		// We don't use currFramePosition because we may have moved the camera.
		lastFramePosition = Camera.main.ScreenToWorldPoint( Input.mousePosition );
		lastFramePosition.z = 0;
	}
	private void DecideWhatUIToShow(RaycastHit2D hit){
		if (hit) {
//			Debug.Log (hit.transform.name); 
			if (hit.transform.GetComponent<UnitHoldingScript> () != null) {
				mouseState = MouseState.Unit;
				SelectedUnit=hit.transform.GetComponent<UnitHoldingScript> ().unit;
				uic.OpenUnitUI (SelectedUnit);
			} else {
				SelectedUnit = null;
			}
			if (SelectedUnit == null) {
				Tile t = GetTileUnderneathMouse ();
				if (t.Structure != null) {
					uic.OpenStructureUI (t.Structure);
				} else {
					Debug.Log ("tile " + t.toString ()); 
				}
			}
		} else {
			if (EventSystem.current.IsPointerOverGameObject ()) {
				return;
			}
			uic.CloseInfoUI ();
		}
	}
	private void UpdateSingle() {
		// If we're over a UI element, then bail out from this.
		if (EventSystem.current.IsPointerOverGameObject ()) {
			HighlightTiles = null; 
			return;
		}
		if (structure == null) {
			HighlightTiles = null; 
			return;
		}
		ShowSinglePreview (GetTileUnderneathMouse());
		if (Input.GetMouseButtonDown (0)) {
			List<Tile> structureTiles = structure.GetBuildingTiles (GetTileUnderneathMouse().X, GetTileUnderneathMouse().Y);
			Build (structureTiles);
		}
	}
	private void ShowSinglePreview(Tile tile){
		List<Tile> structureTiles = structure.GetBuildingTiles (tile.X, tile.Y);
		ShowHighlightOnTiles ();
		ShowPreviewStructureOnTiles (tile);

		foreach (Tile t in structureTiles) {
			if(t==null){
				continue;
			}	
			//not viable city overrides everything
			if(structure.IsTileCityViable (t)==false){
				ShowRedPrefabOnTile (t);
				continue;
			}
			if(structure.mustBeBuildOnMountain == false && structure.mustBeBuildOnShore == false){
				if (structure.correctSpotOnLand (t) == false) {
					ShowRedPrefabOnTile (t);
				} else {
					ShowPrefabOnTile (t);
				}
				continue;
			}
			if (structure.mustBeBuildOnShore && structure.correctSpotOnShore (structureTiles)){
				ShowPrefabOnTile (t);
				continue;
			}
			if(structure.mustBeBuildOnMountain && structure.correctSpotOnMountain (structureTiles)){
				ShowPrefabOnTile (t);
				continue;
			}
			int r = structure.ChangeRotation (tile.X, tile.Y);
			structure.rotated = r;
			ShowRedPrefabOnTile (t);
		}
	}

	public void SetToPatrolMode(){
		patrolCommandToAdd = true;
	}


	public Tile GetTileUnderneathMouse(){
		return World.current.GetTileAt (currFramePosition.x+0.5f,currFramePosition.y+0.5f);
	}

	public void CreatePreviewStructure(){
		previewGO = new GameObject ();
		previewGO.transform.SetParent(this.transform, true);
		previewGO.name="PreviewGO";
		SpriteRenderer sr = previewGO.AddComponent<SpriteRenderer> ();
		sr.sprite = ssc.getStructureSprite (structure);
		sr.sortingLayerName = "StructuresUI";
		sr.color = new Color (sr.color.a, sr.color.b, sr.color.g, 0.5f);
		structure.ExtraBuildUI (previewGO);

	}

	//FIXME this is not optimal 
	// change this to a diffrent way of showing/storing go
	public void ShowPreviewStructureOnTiles(Tile t){
		if(previewGO==null){
			CreatePreviewStructure ();
		}
		previewGO.SetActive (true);

		//this is for extra ui when building like 
		//how effective it is to build there
		//this may move from this place
		structure.UpdateExtraBuildUI (previewGO,t);
		previewGO.transform.position = new Vector3( GetTileUnderneathMouse ().X + (( structure.tileWidth-1 )/2f),
			GetTileUnderneathMouse ().Y + (( structure.tileHeight-1 )/2f), 0);
		previewGO.transform.localRotation = new Quaternion(structure.rotated,0,0,0);
	}
	public void RemovePrefabs(){
		while(previewGameObjects.Count > 0) {
//			Tile t = World.current.GetTileAt (previewGameObjects[0].transform.position.x,previewGameObjects[0].transform.position.y);
//			t.TileState = TileMark.Reset;
			GameObject go = previewGameObjects[0];
			previewGameObjects.RemoveAt(0);
			SimplePool.Despawn (go);
		}
		//so it has to be "moved" to be visible
		if(previewGO!=null)
			previewGO.SetActive (false);
	}
	void ShowRedPrefabOnTile(Tile t){
		if(t == null) {
			return;
		}

		// Display the building hint on top of this tile position
		GameObject go = SimplePool.Spawn( redTileCursorPrefab, new Vector3(t.X, t.Y, 0), Quaternion.identity );
		go.transform.SetParent(this.transform, true);
		previewGameObjects.Add(go);
	}
	void ShowPrefabOnTile(Tile t){
		if(t == null) {
			return;
		}
		
		// Display the building hint on top of this tile position
		GameObject go = SimplePool.Spawn( greenTileCursorPrefab, new Vector3(t.X, t.Y, 0), Quaternion.identity );
		go.transform.SetParent(this.transform, true);
		previewGameObjects.Add(go);
	}
	void ShowHighlightOnTiles(){
		if (structure.buildingRange == 0) {
			return;
		}
		if (HighlightTiles == null) {
			HighlightTiles = new HashSet<Tile> ();
		}

		HashSet<Tile> temp = new HashSet<Tile>(HighlightTiles);
		HighlightTiles.Clear ();
		HighlightTiles = new HashSet<Tile> (structure.GetInRangeTiles (GetTileUnderneathMouse()));
//		Debug.Log (temp.Count + " " + highlightTiles.Count);
		foreach (Tile t in temp) {
			if (t == null || HighlightTiles.Contains (t) == true) {
				continue;
			}
			t.TileState = TileMark.Reset;
		}
		foreach(Tile t in HighlightTiles){
			if (t == null) {
				continue;
			}
			t.TileState = TileMark.Highlight;
		}
	}
	void RemoveHighliteOnTiles(){
		if(HighlightTiles == null){
			return;
		}
		foreach(Tile t in HighlightTiles){
			t.TileState = TileMark.Reset;
		}
	}
    private void UpdateUnit() {
		// If we're over a UI element, then bail out from this.
		if( EventSystem.current.IsPointerOverGameObject() ) {
			return;
		}
        if (Input.GetMouseButtonDown(0)) {
			if(SelectedUnit.playerNumber!=PlayerController.Instance.currentPlayerNumber){
				mouseState = MouseState.Idle;
				return;
			}

			RaycastHit2D hit = Physics2D.Raycast(new Vector2(currFramePosition.x, currFramePosition.y), Vector2.zero, 200);
			if(hit.transform!=null && hit.transform.gameObject.GetComponent<UnitHoldingScript >()!=null){
				SelectedUnit.GiveAttackCommand (hit.transform.gameObject.GetComponent<UnitHoldingScript >().unit,true);

			}
			if (hit.transform != null && hit.transform.gameObject.GetComponent<UnitHoldingScript > () == null) {
				Tile t = GetTileUnderneathMouse ();
				if(t.Structure!=null&&t.Structure is MarketBuilding){
					SelectedUnit.GiveAttackCommand (t.Structure,true);
				}
			}
			if(patrolCommandToAdd){
				SelectedUnit.AddPatrolCommand (currFramePosition.x, currFramePosition.y);
				patrolCommandToAdd = false;
			} else {
				SelectedUnit.AddMovementCommand(currFramePosition.x, currFramePosition.y);
			}
            mouseState = MouseState.Idle;
        }
    }

    void UpdateDragging() {
		// If we're over a UI element, then bail out from this.
		if( EventSystem.current.IsPointerOverGameObject() ) {
			return;
		}

		// Start Drag
		if( Input.GetMouseButtonDown(0) ) {
			dragStartPosition = currFramePosition;
		}

		int start_x = Mathf.FloorToInt( dragStartPosition.x + 0.5f );
		int end_x =   Mathf.FloorToInt( currFramePosition.x + 0.5f );
		int start_y = Mathf.FloorToInt( dragStartPosition.y + 0.5f );
		int end_y =   Mathf.FloorToInt( currFramePosition.y + 0.5f );
		
		// We may be dragging in the "wrong" direction, so flip things if needed.
		if(end_x < start_x) {
			int tmp = end_x;
			end_x = start_x;
			start_x = tmp;
		}
		if(end_y < start_y) {
			int tmp = end_y;
			end_y = start_y;
			start_y = tmp;
		}
		HashSet<Tile> tiles = new HashSet<Tile> ();
		if( Input.GetMouseButton(0) ) {
			// Display a preview of the drag area
			for (int x = start_x; x <= end_x; x+=structure.tileWidth) {
				for (int y = start_y; y <= end_y; y+=structure.tileHeight) {
					if(tiles.Contains (World.current.GetTileAt (x,y))==false)
						tiles.Add (World.current.GetTileAt (x,y));
				}
			}
		}
		if(tiles.Count==0){
			tiles.Add (GetTileUnderneathMouse ());
		}
		foreach (Tile item in tiles) {
			if(mouseState == MouseState.Destroy){
				ShowRedPrefabOnTile (item);
			} else {
				ShowSinglePreview (item);				
			}
		}
		// End Drag
		if( Input.GetMouseButtonUp(0) ) {
			List<Tile> ts = new List<Tile> ();
			// Loop through all the tiles
			for (int x = start_x; x <= end_x; x+=structure.tileWidth) {
				for (int y = start_y; y <= end_y; y+=structure.tileHeight) {
					ts.Add (WorldController.Instance.world.GetTileAt(x, y));
				}
			}
			if(structure == null){
				return;
			}

			if(ts != null) {
				if (mouseState == MouseState.Destroy) {
					bmc.DestroyStructureOnTiles (ts);	
				} else {
					Build (ts, true);
				}
			}
		}
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
            Tile pathStartTile = WorldController.Instance.world.GetTileAt(start_x, start_y);  
            
            if (pathStartTile == null || pathStartTile.myIsland == null) {
                return;
            }
            int end_x = Mathf.FloorToInt(currFramePosition.x + 0.5f);
            int end_y = Mathf.FloorToInt(currFramePosition.y + 0.5f);
            Tile pathEndTile = WorldController.Instance.world.GetTileAt(end_x, end_y);
            if (pathEndTile == null ) {
                return;
            }
            if (pathStartTile.myIsland != null && pathEndTile.myIsland != null) {
                path = new Path_AStar(pathStartTile.myIsland, pathStartTile, pathEndTile);
            }
            if(path.path == null) {
                return;
            } 
            foreach (Tile t in path.path) {
				ShowSinglePreview (t);
            }
        }
        // End path
        if (Input.GetMouseButtonUp(0)) {
            // Loop through all the tiles
			Build( new List<Tile>(path.path),true );
        }

      
    }
	void Build(List<Tile> t,bool single = false){
		bmc.BuildOnTile (t,single,PlayerController.Instance.currentPlayerNumber);
	}

	public void ResetBuilding(Structure structure){
		GameObject.Destroy (previewGO);
		previewGO = null;
		structure = null;
		HighlightTiles = null;
	}


	/// <summary>
	/// what to on escape press 
	///  - set tobuildstructure to null
	///  - set mousestate to drag
	/// </summary>
	public void Escape(){
		ResetBuilding (null);
		bmc.ResetBuild ();
		this.mouseState = MouseState.Idle;
	}


}
