using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour {
	public static int maxZoomLevel = 25;
	public static int minZoomLevel = 3;
	Vector3 lastFramePosition;
	Vector3 currFramePosition;
	public Vector3 upper=new Vector3(1,1);
	public Vector3 lower=new Vector3();
	public Vector3 middle=new Vector3();
	Tile middleTile;
	public Island nearestIsland;
	public float zoomLevel;
	public HashSet<Tile> tilesCurrentInCameraView;
	public HashSet<Structure> structureCurrentInCameraView;
	public Rect CameraViewRange;

	public static CameraController Instance;

	void Start() {
		if (Instance != null) {
			Debug.LogError("There should never be two mouse controllers.");
		}
		Instance = this;
		tilesCurrentInCameraView = new HashSet<Tile> ();
		structureCurrentInCameraView = new HashSet<Structure> ();
	}

	void Update () {
		
		if(UIController.Instance!=null && UIController.Instance.IsPauseMenuOpen()){
			return;
		}
		if(EditorUIController.Instance!=null && EditorUIController.Instance.IsPauseMenuOpen()){
			return;
		}
		Vector3 diff = new Vector3(0,0);
		currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		currFramePosition.z = 0;
		UpdateZoom();
		zoomLevel= Mathf.Clamp(Camera.main.orthographicSize - 2,minZoomLevel,maxZoomLevel);
		diff += UpdateKeyboardCameraMovement ();
		diff += UpdateMouseCameraMovement ();

		Vector2 showBounds = new Vector2 ();
		lower = Camera.main.ScreenToWorldPoint (Vector3.zero);

		float lowerX = lower.x;
		float lowerY = lower.y;
		upper = Camera.main.ScreenToWorldPoint (new Vector3 (Camera.main.pixelWidth, Camera.main.pixelHeight));
		float upperX = upper.x;
		float upperY = upper.y;
		if(WorldController.Instance==null){
			showBounds.x = EditorController.Instance.editorIsland.width;
			showBounds.y = EditorController.Instance.editorIsland.height;
		} else {
			if (BuildController.Instance.BuildState != BuildStateModes.None) {
				World.current.checkIfInCamera (lowerX, lowerY, upperX, upperY);
			} else {
				World.current.resetIslandMark ();
			}
			middle = Camera.main.ScreenToWorldPoint (new Vector3 (Camera.main.pixelWidth/2, Camera.main.pixelHeight/2));
			middleTile = World.current.GetTileAt (middle.x,middle.y);
			findNearestIsland ();
			World w = World.current;
			showBounds.x = w.Width;
			showBounds.y = w.Height;
		}

		if(upperX>showBounds.x ){
			if(diff.x > 0){
				diff.x = 0;
			}
		}
		if(lowerX<0){//Camera.main.orthographicSize/divide
			if(diff.x < 0){
				diff.x = 0;
			}
		}
		if(upperY>showBounds.y ){//Camera.main.orthographicSize/divide
			if(diff.y > 0){
				diff.y = 0;
			}
		}
		if(lowerY<0){
			if(diff.y < 0){
				diff.y = 0;
			}
		}
		Camera.main.transform.Translate (diff);
		lastFramePosition = Camera.main.ScreenToWorldPoint( Input.mousePosition );
		lastFramePosition.z = 0;


		tilesCurrentInCameraView.Clear ();
		structureCurrentInCameraView.Clear ();
		int mod = (int)zoomLevel/2;//TODO: optimize this
		int lX = (int)lower.x - 1*mod;
		int uX = (int)upper.x + 3*mod;
		int lY = (int)lower.y - 1*mod;
		int uY = (int)upper.y + 3*mod;
		CameraViewRange = new Rect (lX,lY,uX-lX,uY-lY);
		for (int x = lX; x < uX; x++) {
			for (int y=lY; y < uY; y++) {
				Tile tile_data = World.current.GetTileAt(x, y);
				if(tile_data==null
					||tile_data.Type == TileType.Ocean ){
					continue;
				}
				tilesCurrentInCameraView.Add (tile_data); 
				if(tile_data.Structure!=null){
					//we dont need trees, roads, growables in general or anything like that
					//why tho they use the same structurespritecontroller like every other structure???
//					if(tile_data.Structure.myBuildingTyp==BuildingTyp.Blocking){
						structureCurrentInCameraView.Add (tile_data.Structure);
//					}
				}
			}
		}
	}
	Vector3 UpdateMouseCameraMovement() {
		// Handle screen panning
		if( Input.GetMouseButton(1) || Input.GetMouseButton(2) ) {	// Right or Middle Mouse Button
			return lastFramePosition-currFramePosition;
		}
		return Vector3.zero;
	}
	public void UpdateZoom(){
		if(Input.GetKey (KeyCode.Plus) || Input.GetKey (KeyCode.KeypadPlus)){
			Camera.main.orthographicSize -= Camera.main.orthographicSize * 0.1f;
		}
		if(Input.GetKey (KeyCode.Minus)|| Input.GetKey (KeyCode.KeypadMinus)){
			Camera.main.orthographicSize += Camera.main.orthographicSize * 0.1f;
		}

		if( EventSystem.current.IsPointerOverGameObject() ) {
			return;
		}
		Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");
		Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);

	}
	public Vector3 UpdateKeyboardCameraMovement(){
		if (Mathf.Abs (Input.GetAxis ("Horizontal")) == 0 && Mathf.Abs (Input.GetAxis ("Vertical")) == 0) {
			return Vector3.zero;
		}
		float Horizontal = 0;
		if (Input.GetAxis ("Horizontal") < 0) {
			Horizontal = -1;
		} 
		if(Input.GetAxis ("Horizontal")>0){
			Horizontal = 1;
		}
		float Vertical =  0;
		if(Input.GetAxis ("Vertical")<0){
			Vertical = -1;
		} 
		if(Input.GetAxis ("Vertical")>0){
			Vertical = 1;
		}
		float zoomMultiplier = Mathf.Clamp(Camera.main.orthographicSize - 2,1,4f)*10;
		return new Vector3(zoomMultiplier*Horizontal*Time.deltaTime,zoomMultiplier*Vertical*Time.deltaTime,0);
	}
	public void findNearestIsland(){
		HashSet<Tile> tiles= new HashSet<Tile>();
		Queue<Tile> tilesToCheck = new Queue<Tile>();
		tilesToCheck.Enqueue(middleTile);
		while (tilesToCheck.Count > 0) {

			Tile t = tilesToCheck.Dequeue();
			if (t==null){
				return;
			}
			if(t.myIsland!=null){
				nearestIsland = t.myIsland;
				break;
			}
			if(tiles.Count>100){
				nearestIsland = null; 
				break;
			}
			if (tiles.Contains (t)==false) {
				tiles.Add(t);
				Tile[] ns = t.GetNeighbours();
				foreach (Tile t2 in ns) {
					tilesToCheck.Enqueue(t2);
				}
			}
		}
	}

	public void MoveCameraToPosition(Vector2 pos){
		Camera.main.transform.position = new Vector3 (pos.x, pos.y, Camera.main.transform.position.z);
		currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		lastFramePosition = currFramePosition;

	}
}
