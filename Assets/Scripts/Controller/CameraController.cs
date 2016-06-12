﻿using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour {
	Vector3 lastFramePosition;
	Vector3 currFramePosition;



	void Start() {
		
	}



	void Update () {
		Vector3 diff = new Vector3(0,0);
		currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		currFramePosition.z = 0;
		UpdateZoom();
		diff += UpdateKeyboardCameraMovement ();
		diff += UpdateMouseCameraMovement ();

		Vector3 lower = Camera.main.ScreenToWorldPoint (Vector3.zero);
		float lowerX = lower.x;
		float lowerY = lower.y;
		Vector3 upper = Camera.main.ScreenToWorldPoint (new Vector3 (Camera.main.pixelWidth, Camera.main.pixelHeight));
		float upperX = upper.x;
		float upperY = upper.y;
		if (BuildController.Instance.BuildState == BuildStateModes.On) {
			World.current.checkIfInCamera (lowerX, lowerY, upperX, upperY);
		} else {
			World.current.resetIslandMark ();
		}
		World w = World.current;
		if(upperX>w.Width ){
			if(diff.x > 0){
				diff.x = 0;
			}
		}
		if(lowerX<0){//Camera.main.orthographicSize/divide
			if(diff.x < 0){
				diff.x = 0;
			}
		}
		if(upperY>w.Height){//Camera.main.orthographicSize/divide
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
		Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
		lastFramePosition = Camera.main.ScreenToWorldPoint( Input.mousePosition );
		lastFramePosition.z = 0;
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

}
