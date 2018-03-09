﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;

public class CameraController : MonoBehaviour {
	public static int maxZoomLevel = 25;
	public bool devCameraZoom = false;
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
	public HashSet<Tile> tilesToAddCheckInCameraView;
	public HashSet<Tile> tilesToRemoveCheckInCameraView;
	HashSet<Tile> tilesTempInCameraView;

	public HashSet<Structure> structureCurrentInCameraView;
	public Rect CameraViewRange;
	Vector2 showBounds = new Vector2 ();

	public static CameraController Instance;
	void Awake () {
		if (Instance != null) {
			Debug.LogError ("There should never be two SaveController.");
		}
		Instance = this;
	}
	void Start() {
		tilesCurrentInCameraView = new HashSet<Tile> ();
		structureCurrentInCameraView = new HashSet<Structure> ();
		tilesToAddCheckInCameraView = new HashSet<Tile> ();
		tilesToRemoveCheckInCameraView = new HashSet<Tile> ();
		tilesTempInCameraView = new HashSet<Tile> ();

		if(WorldController.Instance == null ||WorldController.Instance.isLoaded == false){
			Camera.main.transform.position = new Vector3 (World.current.Width / 2, World.current.Height / 2, Camera.main.transform.position.z);
		}
		middle = Camera.main.ScreenToWorldPoint (new Vector3 (Camera.main.pixelWidth/2, Camera.main.pixelHeight/2));
		lower = Camera.main.ScreenToWorldPoint (Vector3.zero);
		upper = Camera.main.ScreenToWorldPoint (new Vector3 (Camera.main.pixelWidth, Camera.main.pixelHeight));
		middle = Camera.main.ScreenToWorldPoint (new Vector3 (Camera.main.pixelWidth/2, Camera.main.pixelHeight/2));
		World w = World.current;
		showBounds.x = w.Width;
		showBounds.y = w.Height;




	}

	void Update () {
		
		if(UIController.Instance!=null && UIController.Instance.IsPauseMenuOpen()){
			return;
		}
		if(EditorUIController.Instance!=null && EditorUIController.Instance.IsPauseMenuOpen()){
			return;
		}
		Vector3 cameraMove = new Vector3(0,0);
		currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		currFramePosition.z = 0;
		UpdateZoom();
		zoomLevel= Mathf.Clamp(Camera.main.orthographicSize - 2,minZoomLevel,maxZoomLevel);
		cameraMove += UpdateKeyboardCameraMovement ();
		cameraMove += UpdateMouseCameraMovement ();

		lower = Camera.main.ScreenToWorldPoint (Vector3.zero);
		upper = Camera.main.ScreenToWorldPoint (new Vector3 (Camera.main.pixelWidth, Camera.main.pixelHeight));
		middle = Camera.main.ScreenToWorldPoint (new Vector3 (Camera.main.pixelWidth/2, Camera.main.pixelHeight/2));

		middleTile = World.current.GetTileAt (middle.x,middle.y);
		findNearestIsland ();

		Vector3 newLower = cameraMove + lower;
		Vector3 newUpper = cameraMove + upper;
		if(newUpper.x>showBounds.x ){
			if(cameraMove.x > 0){
				cameraMove.x = Mathf.Clamp(cameraMove.x, 0, showBounds.x-upper.x);
			}
		}
		if(newLower.x<0){//Camera.main.orthographicSize/divide
			if(cameraMove.x < 0){
				cameraMove.x = Mathf.Clamp(cameraMove.x, 0, -lower.x);
			}
		}
		if(newUpper.y>showBounds.y ){//Camera.main.orthographicSize/divide
			if(cameraMove.y > 0){
				cameraMove.y = Mathf.Clamp(cameraMove.y, 0, showBounds.y-upper.y);
			}
		}
		if(newLower.y<0){
			if(cameraMove.y < 0){
				cameraMove.y = Mathf.Clamp(cameraMove.y, 0, -lower.y);
			}
		}
		Camera.main.transform.Translate (cameraMove);
		lastFramePosition = Camera.main.ScreenToWorldPoint( Input.mousePosition );
		lastFramePosition.z = 0;

		Rect oldViewRange = new Rect (CameraViewRange);

		int mod = 2+ (int)zoomLevel/2 ;//TODO: optimize this
		int lX = (int)lower.x - mod;
		int uX = (int)upper.x + mod;
		int lY = (int)lower.y - mod;
		int uY = (int)upper.y + mod;
		CameraViewRange = new Rect (lX,lY,uX-lX,uY-lY);

		tilesTempInCameraView.Clear();
		tilesToRemoveCheckInCameraView.Clear ();
		tilesToAddCheckInCameraView.Clear ();
		structureCurrentInCameraView.Clear ();
		TileSpriteController tsc = TileSpriteController.Instance;
		for (int x = Mathf.FloorToInt(Mathf.Min(oldViewRange.xMin,CameraViewRange.xMin)); x < Mathf.CeilToInt(Mathf.Max(oldViewRange.xMax,CameraViewRange.xMax)); x++) {
			for (int y=Mathf.FloorToInt(Mathf.Min(oldViewRange.yMin,CameraViewRange.yMin)); y < Mathf.CeilToInt(Mathf.Max(oldViewRange.yMax,CameraViewRange.yMax)); y++) {
				Tile tile_data = World.current.GetTileAt(x, y);
				if(tile_data==null
					|| tile_data.Type == TileType.Ocean ){
					continue;
				}

				bool isInNew = CameraViewRange.Contains (tile_data.vector);
				bool isInOld = oldViewRange.Contains (tile_data.vector);
				if(isInNew && isInOld){
					continue;
				}
				if(isInNew==false && isInOld==false){
					continue;
				}
				tilesTempInCameraView.Add (tile_data); 
				if(isInOld == false && isInNew){
					tsc.SpawnTile (tile_data);
				} else
				if(isInNew == false){
					tsc.DespawnTile (tile_data);
					continue;
				}

				if(tile_data.Structure!=null){
					structureCurrentInCameraView.Add (tile_data.Structure);
				}
			}
		}
		//Whatever needs timing here

		tilesCurrentInCameraView.Clear ();
		tilesCurrentInCameraView.UnionWith (tilesTempInCameraView);
		tilesTempInCameraView.Clear ();
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
		if(devCameraZoom){
			
		}
		Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoomLevel, devCameraZoom? 4*maxZoomLevel : maxZoomLevel);

	}
	public Vector3 UpdateKeyboardCameraMovement(){
		if(UIController.IsTextFieldFocused()){
			return Vector3.zero;
		}
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

	public CameraSave GetSaveCamera(){
		CameraSave cs = new CameraSave ();
		cs.orthographicSize = Camera.main.orthographicSize;
		cs.pos = Camera.main.transform.position;
		return cs;
	}
	public void LoadSaveCameraData(CameraSave cs){
		Camera.main.transform.position = cs.pos;
		Camera.main.orthographicSize = cs.orthographicSize;
	}
}
[Serializable]
public class CameraSave {
	public Vector3 pos;
	public float orthographicSize;
}