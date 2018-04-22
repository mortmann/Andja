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
		if (Input.GetKeyDown (KeyCode.Escape)) {
			mc.Escape ();
			bc.Escape ();
			uic.Escape (BuildController.Instance.BuildState!=BuildStateModes.None);
		} 
		if(UIController.IsTextFieldFocused()){
			return;
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
		if(InputHandler.GetButtonDown ("Console")){
            
			uic.ToggleConsole ();
		}
//		if(Input.GetKeyDown (KeyCode.Alpha1)){
//			WorldController.Instance.OnClickChangeTimeMultiplier (1);
//		}
		if (InputHandler.GetButtonDown ("Rotate")) {
			if(bc.toBuildStructure != null){
				bc.toBuildStructure.RotateStructure ();
			}
		}
        if (InputHandler.GetButtonDown("Screenshot")) {
            ScreenCapture.CaptureScreenshot("screenshot_"+ System.DateTime.Now.ToString("dd_MM_yyyy-hh_mm_ss_ff")+".jpg");
        }
        if (Application.isEditor){
			if(Input.GetKey (KeyCode.LeftShift)){
				if(EventSystem.current.IsPointerOverGameObject ()==false){
					FindObjectOfType<HoverOverScript> ().DebugInfo (mc.GetTileUnderneathMouse ().ToString ());
					GameObject.Find ("Debug").transform.localPosition = mc.GetMousePosition ();
				} 
			}
			if(Input.GetKeyUp (KeyCode.LeftShift)){
				GameObject.Find ("Debug").transform.localPosition = new Vector3 (-200, -200);

			}
		}

	}




}
