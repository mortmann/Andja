using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using System.Linq;

public enum MouseState { Idle,Drag, Path, Single, Unit };

public class MouseController : MonoBehaviour {

    public static MouseController Instance { get; protected set; }

    public GameObject greenTileCursorPrefab;
	public GameObject redTileCursorPrefab;
	// The world-position of the mouse last frame.
	Vector3 lastFramePosition;
	Vector3 currFramePosition;

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

	protected Structure _structure;
	public Structure structure {
		get{ 
			return _structure;
		}
		set {
			HighlightTiles = null;
			_structure = value;
		}
	}

	public MouseState mouseState = MouseState.Idle;
    private Vector3 pathStartPosition;
    private Path_AStar path;
    private Unit selectedUnit;
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


	public Tile GetMouseOverTile() {
/*		return WorldController.Instance.world.GetTileAt(
			Mathf.FloorToInt(currFramePosition.x), 
			Mathf.FloorToInt(currFramePosition.y)
		);*/
		return WorldController.Instance.GetTileAtWorldCoord( currFramePosition );
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
        if (mouseState == MouseState.Drag) { 
            UpdateDragging();
        } else
        if (mouseState == MouseState.Path) {
            UpdatePathBetweenTiles();
		} else
		if (mouseState == MouseState.Single) {
			UpdateSingle();
		} else 
		if (mouseState == MouseState.Unit && selectedUnit != null) {
            UpdateUnit();
		} 
        if (Input.GetMouseButtonDown(0)) {
			
			if( EventSystem.current.IsPointerOverGameObject() ) {
				return;
			}
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
			selectedUnit = hit.transform.GetComponent<Unit> ();
			if (selectedUnit != null) {
				mouseState = MouseState.Unit;
				uic.OpenUnitUI (selectedUnit);
			}
			if (selectedUnit == null) {
				Tile t = GetTileUnderneathMouse (false);
				if (t.Structure != null) {
					uic.OpenStructureUI (t.Structure);
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
		List<Tile> structureTiles = structure.GetBuildingTiles (GetTileUnderneathMouse().X, GetTileUnderneathMouse().Y);
		ShowHighliteOnTiles ();
		if (structure.mustBeBuildOnShore) {
			foreach (Tile t in structureTiles) {
				if (t != null) {
					if (structure.correctSpotOnShore (structureTiles) == false) {
						int r = structure.ChangeRotation (GetTileUnderneathMouse().X, GetTileUnderneathMouse().Y);
						structure.rotated = r;
						ShowRedPrefabOnTile (t);
					} else {
						ShowPrefabOnTile (t);
					}
				}
			}
		} else 
		if (structure.mustBeBuildOnMountain) {
			foreach (Tile t in structureTiles) {
				if (t != null) {
					if (structure.correctSpotOnMountain (structureTiles) == false) {
						int r = structure.ChangeRotation (GetTileUnderneathMouse().X, GetTileUnderneathMouse().Y);
						structure.rotated = r;
						ShowRedPrefabOnTile (t);
					} else {
						ShowPrefabOnTile (t);
					}
				}
			}
		} else {
			foreach (Tile t in structureTiles) {
				if (t != null) {
					if (structure.correctSpotOnLand (t) == false) {
						ShowRedPrefabOnTile (t);
					} else {
						ShowPrefabOnTile (t);
					}
				}
			}
		}
		if (Input.GetMouseButtonDown (0)) {
			Build (structureTiles);
		}
	}
	public Tile GetTileUnderneathMouse(bool plusOffset = true){
		float xOffset = 0;
		float yOffset = 0;
		if (plusOffset) {
			xOffset = 0.5f;
			yOffset = 0.5f;
		}
		return World.current.GetTileAt (currFramePosition.x+xOffset,currFramePosition.y+yOffset);
	}

	public void RemovePrefabs(){
		while(previewGameObjects.Count > 0) {
			Tile t = World.current.GetTileAt (previewGameObjects[0].transform.position.x,previewGameObjects[0].transform.position.y);
			t.TileState = TileMark.Reset;
			GameObject go = previewGameObjects[0];
			previewGameObjects.RemoveAt(0);
			SimplePool.Despawn (go);
		}
	}
	void ShowRedPrefabOnTile(Tile t){
		if(t == null) {
			return;
		}
		t.TileState = TileMark.None;
		// Display the building hint on top of this tile position
		GameObject go = SimplePool.Spawn( redTileCursorPrefab, new Vector3(t.X, t.Y, 0), Quaternion.identity );
		go.transform.SetParent(this.transform, true);
		previewGameObjects.Add(go);
	}
	void ShowPrefabOnTile(Tile t){
		if(t == null) {
			return;
		}
		t.TileState = TileMark.None;
		// Display the building hint on top of this tile position
		GameObject go = SimplePool.Spawn( greenTileCursorPrefab, new Vector3(t.X, t.Y, 0), Quaternion.identity );
		go.transform.SetParent(this.transform, true);
		previewGameObjects.Add(go);
	}
	void ShowHighliteOnTiles(){
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
            selectedUnit.AddMovementCommand(currFramePosition.x, currFramePosition.y);
            mouseState = MouseState.Drag;
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
		List<Tile> tiles = new List<Tile> ();
		if( Input.GetMouseButton(0) ) {
			// Display a preview of the drag area
			for (int x = start_x; x <= end_x; x+=structure.tileWidth) {
				for (int y = start_y; y <= end_y; y+=structure.tileHeight) {
					tiles.Add (World.current.GetTileAt (x,y));
				}
			}
		}

		foreach (Tile item in tiles) {
			List<Tile> temp = structure.GetBuildingTiles (item.X, item.Y);
			foreach (Tile tile in temp) {
				if(Tile.IsBuildType (tile.Type)){
					ShowPrefabOnTile (tile);
				} else {
					ShowRedPrefabOnTile (tile);
				}				
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
				Build( ts,true );
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
                if (t != null) {
					if (Tile.IsBuildType (t.Type)) {
						ShowPrefabOnTile (t);
					} else {
						ShowRedPrefabOnTile (t);
					}
                }
            }
        }
        // End path
        if (Input.GetMouseButtonUp(0)) {
            // Loop through all the tiles
			Build( new List<Tile>(path.path),true );
        }

      
    }
	void Build(List<Tile> t,bool single = false){
		bmc.BuildOnTile (t,single);
	}

	public void ResetBuilding(Structure structure){
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
