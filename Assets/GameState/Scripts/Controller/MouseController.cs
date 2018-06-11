﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections;

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

	bool autorotate = true;
	HashSet<Tile> _highlightTiles;
	HashSet<Tile> HighlightTiles {
		get { return _highlightTiles; }
		set {
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
	bool buildFromUnit;

	protected Structure _structure;
	public Structure structure {
		get{ 
			return _structure;
		}
		set {
			GameObject.Destroy (previewGO);
			ResetBuilding (null);
			_structure = value;
		}
	}

	public MouseState mouseState = MouseState.Idle;
    private Vector3 pathStartPosition;
    
	private Path_AStar path;

	private Unit _selectedUnit;
	public Unit SelectedUnit  {
		get { return _selectedUnit;}
		protected set { 
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
		if (Input.GetMouseButtonDown (1)) {
			ResetBuilding (null);
			mouseState = MouseState.Idle;
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
				if (buildFromUnit == false)
					ResetBuilding (null);
			}
			if (SelectedUnit == null) {
				Tile t = GetTileUnderneathMouse ();
				if (t.Structure != null) {
					uic.OpenStructureUI (t.Structure);
				} else {
					Debug.Log ("tile " + t.ToString ()); 
				}
			}
		} else {
			if (EventSystem.current.IsPointerOverGameObject ()) {
				return;
			}
			if (buildFromUnit == false)
				return;
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
		if(tile == null){
			return;
		}
		int tempTest = structure.rotated;
		Dictionary<Tile,bool> tileToCanBuild = null;
		if(autorotate){
			for(int r = 0; r<4;r++){
				structure.AddTimes90ToRotate (r);
				List<Tile> structureTiles = structure.GetBuildingTiles (tile.X, tile.Y);
				tileToCanBuild = structure.CorrectSpot (structureTiles);
				if(tileToCanBuild.Values.ToList ().Contains (false)==false){
					break;
				}
					
			}
		}


		ShowHighlightOnTiles ();
		ShowPreviewStructureOnTiles (tile);
		if(tileToCanBuild.Values.ToList ().Contains (false)){
			//TODO fix this temporary fix
			// it is so that previews dont spinn like crazy BUT find better way todo this
			structure.rotated = tempTest;
		}
		foreach (Tile t in tileToCanBuild.Keys) {
			if(t==null){
				continue;
			}	
			//not viable city overrides everything
			if(structure.IsTileCityViable (t,PlayerController.currentPlayerNumber)==false){
				ShowRedPrefabOnTile (t);
				continue;
			}
			if (tileToCanBuild[t]) {
				ShowPrefabOnTile (t);
			} else {
				ShowRedPrefabOnTile (t);
			}
		}

	}

	public Tile GetTileUnderneathMouse(){
		return World.Current.GetTileAt (currFramePosition.x+0.5f,currFramePosition.y+0.5f);
	}

	public void CreatePreviewStructure(){
		previewGO = new GameObject ();
		previewGO.transform.SetParent(this.transform, true);
		previewGO.name="PreviewGO";

		SpriteRenderer sr = previewGO.AddComponent<SpriteRenderer> ();

		sr.sprite = ssc.GetStructureSprite (structure);
		sr.sortingLayerName = "StructuresUI";
		sr.color = new Color (sr.color.a, sr.color.b, sr.color.g, 0.5f);
		structure.ExtraBuildUI (previewGO);

		TileSpriteController.Instance.AddDecider (TileCityDecider);

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
		float x = 0;
		float y = 0;
		if (structure.TileWidth> 1) {
			x = 0.5f + ((float)structure.TileWidth) / 2 - 1;
		}
		if (structure.TileHeight> 1) {
			y = 0.5f + ((float)structure.TileHeight) / 2 - 1;
		}
		previewGO.transform.position = new Vector3( GetTileUnderneathMouse ().X + x,
			GetTileUnderneathMouse ().Y + y, 0);
		previewGO.transform.eulerAngles = new Vector3 (0, 0, 360-structure.rotated);
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
		if( EventSystem.current.IsPointerOverGameObject() ) {
			return;
		}
		if (structure.BuildingRange == 0) {
			return;
		}
		HighlightTiles = new HashSet<Tile> (structure.GetInRangeTiles (GetTileUnderneathMouse()));
	}

    private void UpdateUnit() {
		// If we're over a UI element, then bail out from this.
		if( EventSystem.current.IsPointerOverGameObject() ) {
			return;
		}
        if (Input.GetMouseButtonDown(1)) {
			if(SelectedUnit.playerNumber!=PlayerController.currentPlayerNumber){
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
//            mouseState = MouseState.Idle;
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
			tiles = new HashSet<Tile>(GetTilesStructures (start_x,end_x,start_y,end_y));
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
			List<Tile> ts = new List<Tile>(GetTilesStructures (start_x,end_x,start_y,end_y));
			if(ts != null) {
				if (mouseState == MouseState.Destroy) {
					bmc.DestroyStructureOnTiles (ts,PlayerController.Instance.CurrPlayer);	
				} else {
					if(structure == null){
						return;
					}
					Build (ts, true);
				}
			}
		}
	}

	private IEnumerable<Tile> GetTilesStructures(int start_x,int end_x,int start_y,int end_y){
		int width = 1;
		int height = 1;
		List<Tile> tiles = new List<Tile>();
		if(structure!=null){
			width = structure.TileWidth;
			height = structure.TileHeight;
		}
		for (int x = start_x; x <= end_x; x+=width) {
			for (int y = start_y; y <= end_y; y+=height) {
				if(tiles.Contains (World.Current.GetTileAt (x,y))==false)
					tiles.Add (World.Current.GetTileAt (x,y));
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
            Tile pathStartTile = WorldController.Instance.World.GetTileAt(start_x, start_y);  
            
            if (pathStartTile == null || pathStartTile.MyIsland == null) {
                return;
            }
            int end_x = Mathf.FloorToInt(currFramePosition.x + 0.5f);
            int end_y = Mathf.FloorToInt(currFramePosition.y + 0.5f);
            Tile pathEndTile = WorldController.Instance.World.GetTileAt(end_x, end_y);
            if (pathEndTile == null ) {
                return;
            }
            if (pathStartTile.MyIsland != null && pathEndTile.MyIsland != null) {
                path = new Path_AStar(pathStartTile.MyIsland, pathStartTile, pathEndTile,false);
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
			if(path==null || path.path==null){
				return;
			}
			Build( new List<Tile>(path.path),true );
        }

      
    }
	void Build(List<Tile> t,bool single = false){
		Unit temp = null;
		if(buildFromUnit){
			temp = SelectedUnit;
		}
		bmc.BuildOnTile (t,single,PlayerController.currentPlayerNumber,false,temp);
	}
	public void BuildFromUnit(){
		buildFromUnit = true;
		bmc.SettleFromUnit (SelectedUnit);
	}
	public void SetToPatrolMode(){
		patrolCommandToAdd = true;
	}
	public void ResetBuilding(Structure structure,bool loading = false){
		if(loading){
			return;// there is no need to call any following
		}
		TileSpriteController.Instance.RemoveDecider (TileCityDecider);
		GameObject.Destroy (previewGO);

		previewGO = null;
		structure = null;
		HighlightTiles = null;
		if(buildFromUnit){
			SelectedUnit = null;
			buildFromUnit = false;
		}
	}


	/// <summary>
	/// what to on escape press 
	///  - set tobuildstructure to null
	///  - set mousestate to drag
	/// </summary>
	public void Escape(){
		SelectedUnit = null;
		ResetBuilding (null);
		bmc.ResetBuild ();
		this.mouseState = MouseState.Idle;
	}

	TileMark TileCityDecider(Tile t){
		if(t==null){
			return TileMark.None;
		}
		if(HighlightTiles!=null&&HighlightTiles.Contains(t)){
			return TileMark.Highlight;
		} else
			if(t.MyCity!=null&&t.MyCity.IsCurrPlayerCity ()){
			return TileMark.None;
		} else {
			return TileMark.Dark;
		}
	}
}
