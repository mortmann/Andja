using UnityEngine;
using System.Collections;

//TODO REMOVE
using UnityEngine.EventSystems;

public class KeyboardController : MonoBehaviour {

	// Use this for initialization
	UIController uic;
	MouseController mc;
	BuildController bc;





	void Start () {
		new InputHandler ();
		uic = GameObject.FindObjectOfType<UIController>();
		mc = GameObject.FindObjectOfType<MouseController>();
		bc = GameObject.FindObjectOfType<BuildController> ();
	}
	// Update is called once per frame
	void Update () {
		
		if(Input.GetKeyDown (KeyCode.Escape)){
			
			mc.Escape ();
			uic.Escape ();
			bc.Escape ();
		} 
		if(uic.IsPauseMenuOpen()){
			return;
		}
		if (InputHandler.GetButtonDown ("BuildMenu")) {
			uic.showBuildMenu();
		}
		if(InputHandler.GetButtonDown ("TradeMenu")){
			uic.ToggleTradeMenu ();
		}
		if(InputHandler.GetButtonDown ("Offworld")){
			uic.ToggleOffWorldMenu ();
		}
		if(InputHandler.GetButtonDown ("TogglePause")){
			WorldController.Instance.TogglePause ();
		}
//		if(Input.GetKeyDown (KeyCode.Alpha1)){
//			WorldController.Instance.OnClickChangeTimeMultiplier (1);
//		}
		if (Input.GetButtonDown ("Rotate")) {
			if(bc.toBuildStructure != null){
				bc.toBuildStructure.RotateStructure ();
			}
		}

		if(Application.isEditor){
			if(Input.GetKey (KeyCode.LeftShift)){
				if(EventSystem.current.IsPointerOverGameObject ()==false){
					FindObjectOfType<HoverOverScript> ().DebugInfo (mc.GetTileUnderneathMouse ().toString ());
					GameObject.Find ("Debug").transform.localPosition = mc.GetMousePosition ();
				} 
			}
			if(Input.GetKeyUp (KeyCode.LeftShift)){
				GameObject.Find ("Debug").transform.localPosition = new Vector3 (-200, -200);

			}
		}

	}




}
