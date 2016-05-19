using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;

public enum MouseState { Idle,Drag, Path, Single, Unit };

public class MouseController : MonoBehaviour {

    public static MouseController Instance { get; protected set; }

    public GameObject greenTileCursorPrefab;
	public GameObject redTileCursorPrefab;
	public GameObject highlightTileCursorPrefab;
	// The world-position of the mouse last frame.
	Vector3 lastFramePosition;
	Vector3 currFramePosition;
	List<Tile> highlightTiles;
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
			highlightTiles = null;
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
	}

	/// <summary>
	/// Gets the mouse position in world space.
	/// </summary>
	public Vector3 GetMousePosition() {
		return currFramePosition;
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

        UpdateCameraMovement();
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
				Tile t = WorldController.Instance.world.GetTileAt (currFramePosition.x, currFramePosition.y);
				if (t.structures != null) {
					uic.OpenStructureUI (t.structures);
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
			return;
		}
		if (structure == null) {
			return;
		}
		List<Tile> structureTiles = structure.GetBuildingTiles (currFramePosition.x, currFramePosition.y);
		ShowHighliteOnTiles ();
		if (structure.mustBeBuildOnShore) {
			foreach (Tile t in structureTiles) {
				if (t != null) {
					if (structure.correctSpotForOnShore (structureTiles) == false) {
						ShowRedPrefabOnTile (t);
					} else {
						ShowPrefabOnTile (t);
					}
				}
			}
		} else {
			foreach (Tile t in structureTiles) {
				if (t != null) {
					if (Tile.IsBuildType (t.Type) == false) {
						ShowRedPrefabOnTile (t);
					} else {
						ShowPrefabOnTile (t);
					}
				}
			}
		}
//		if (highlightTiles != null) {
//			foreach (Tile t in highlightTiles) {
//				ShowHighliteOnTile (t);
//			}
//		}
		if (Input.GetMouseButtonDown (0)) {
			Build (structureTiles);
		}
	}

	public void RemovePrefabs(){
		while(previewGameObjects.Count > 0) {
			GameObject go = previewGameObjects[0];
			previewGameObjects.RemoveAt(0);
			SimplePool.Despawn (go);
		}
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
	void ShowHighliteOnTiles(){
		World w = WorldController.Instance.world;
		//first time to show highlights
		if(highlightTiles == null){
			highlightTiles = structure.GetInRangeTiles (WorldController.Instance.world.GetTileAt (currFramePosition.x, currFramePosition.y));
			foreach(Tile t in highlightTiles){
				t.isHighlighted = true;
				w.OnTileChanged (t);
			}
			return;
		}
		//all other times to show it
		List<Tile> temp = structure.GetInRangeTiles (WorldController.Instance.world.GetTileAt (currFramePosition.x, currFramePosition.y));
		foreach(Tile t in highlightTiles){
			if(temp.Contains (t) == false){
//				Debug.Log ("changed false");
				t.isHighlighted = false;
				w.OnTileChanged (t);
			}
		}
		foreach(Tile t in temp){
			if(highlightTiles.Contains (t) == false){
				//				Debug.Log ("changed false");
				t.isHighlighted = true;
				w.OnTileChanged (t);
			}
		}
		highlightTiles = temp;
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
		if( Input.GetMouseButton(0) ) {
			// Display a preview of the drag area
			for (int x = start_x; x <= end_x; x++) {
				for (int y = start_y; y <= end_y; y++) {
					Tile t = WorldController.Instance.world.GetTileAt(x, y);
					if(t != null) {
                        if(t.Type != TileType.Water) { 
							ShowPrefabOnTile (t);
                        }
					}
				}
			}
		}

		List<Tile> tiles = new List<Tile> ();
		// End Drag
		if( Input.GetMouseButtonUp(0) ) {
			// Loop through all the tiles
			for (int x = start_x; x <= end_x; x++) {
				for (int y = start_y; y <= end_y; y++) {
					tiles.Add (WorldController.Instance.world.GetTileAt(x, y));
				}
			}
		}
		if(structure == null){
			return;
		}
		if(tiles != null) {
			Build( tiles,true );
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
                    if (t.Type != TileType.Water) {
						ShowPrefabOnTile (t);
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
		highlightTiles = null;
	}

    void UpdateCameraMovement() {
		// Handle screen panning
		if( Input.GetMouseButton(1) || Input.GetMouseButton(2) ) {	// Right or Middle Mouse Button
			Vector3 diff = lastFramePosition - currFramePosition;
			if(Camera.main.transform.position.x>WorldController.Instance.world.Width + Camera.main.orthographicSize/4){
				if(diff.x > 0){
					diff.x = 0;
				}
			}
			if(Camera.main.transform.position.x<- Camera.main.orthographicSize/4){
				if(diff.x < 0){
					diff.x = 0;
				}
			}
			if(Camera.main.transform.position.y>WorldController.Instance.world.Height + Camera.main.orthographicSize/4){
				if(diff.y > 0){
					diff.y = 0;
				}
			}
			if(Camera.main.transform.position.y < - Camera.main.orthographicSize/4){
				if(diff.y < 0){
					diff.y = 0;
				}
			}
			Camera.main.transform.Translate (diff);
		}
		if(EventSystem.current.IsPointerOverGameObject()){
			return;
		}	
		Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");

		Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
	}
	/// <summary>
	/// what to on escape press 
	///  - set tobuildstructure to null
	///  - set mousestate to drag
	/// </summary>
	public void Escape(){
		ResetBuilding (null);
		this.mouseState = MouseState.Idle;


	}


}
