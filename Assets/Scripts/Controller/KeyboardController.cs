using UnityEngine;
using System.Collections;

public class KeyboardController : MonoBehaviour {

	// Use this for initialization
	UIController uic;
	MouseController mc;
	void Start () {
		uic = GameObject.FindObjectOfType<UIController>();
		mc = GameObject.FindObjectOfType<MouseController>();
	}
	// Update is called once per frame
	void Update () {
		if (Input.GetButtonDown ("BuildMenu")) {
			uic.showBuildMenu();
		}
		if(Input.GetKeyDown (KeyCode.Escape)){
			mc.Escape ();
			uic.Escape ();
		}
		if(Input.GetKey (KeyCode.Plus) || Input.GetKey (KeyCode.KeypadPlus)){
			Camera.main.orthographicSize -= Camera.main.orthographicSize * 0.1f;
			Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
		}
		if(Input.GetKey (KeyCode.Minus)|| Input.GetKey (KeyCode.KeypadMinus)){
			Camera.main.orthographicSize += Camera.main.orthographicSize * 0.1f;
			Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
		}
		updateCameraMovement ();	
	}



	public void updateCameraMovement(){
		if (Mathf.Abs (Input.GetAxis ("Horizontal")) == 0 && Mathf.Abs (Input.GetAxis ("Vertical")) == 0) {
			return;
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

		//Needs to change/move this because its double (bad) code 
		// --- The same can be found in MouseController.updateCameraMovement()
		// --- but its slightly diffrent D:
		float zoomMultiplier = Mathf.Clamp(Camera.main.orthographicSize - 2,1,4f);
		Vector3 move = new Vector3(zoomMultiplier*10*Horizontal*Time.deltaTime,zoomMultiplier*10*Vertical*Time.deltaTime,0);

		if(Camera.main.transform.position.x>WorldController.Instance.world.Width + Camera.main.orthographicSize/4){
			if(move.x > 0){
				move.x = 0;
			}
		}
		if(Camera.main.transform.position.x<- Camera.main.orthographicSize/4){
			if(move.x < 0){
				move.x = 0;
			}
		}
		if(Camera.main.transform.position.y>WorldController.Instance.world.Height + Camera.main.orthographicSize/4){
			if(move.y > 0){
				move.y = 0;
			}
		}
		if(Camera.main.transform.position.y < - Camera.main.orthographicSize/4){
			if(move.y < 0){
				move.y = 0;
			}
		}
		Camera.main.transform.Translate (move, Space.World);
	}
}
