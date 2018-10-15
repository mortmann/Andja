using UnityEngine;
using System.Collections;

//TODO REMOVE
using UnityEngine.EventSystems;

public class KeyboardController : MonoBehaviour {

	// Use this for initialization
	UIController UIC=> UIController.Instance;
	MouseController MouseC => MouseController.Instance;
    BuildController BuildC => BuildController.Instance;

    void Start () {
		new InputHandler ();
	}
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.Escape)) {
			MouseC.Escape ();
			BuildC.Escape ();
            UIC.Escape (BuildC.BuildState!=BuildStateModes.None);
		} 
		if(UIController.IsTextFieldFocused()){
			return;
		}
		if(UIC.IsPauseMenuOpen()){
			return;
		}
		if (InputHandler.GetButtonDown (InputName.BuildMenu)) {
            UIC.ShowBuildMenu();
		}
		if(InputHandler.GetButtonDown (InputName.TradeMenu)){
            UIC.ToggleTradeMenu ();
		}
		if(InputHandler.GetButtonDown (InputName.Offworld)){
            UIC.ToggleOffWorldMenu ();
		}
		if(InputHandler.GetButtonDown (InputName.TogglePause)){
			WorldController.Instance.TogglePause ();
		}
		if(InputHandler.GetButtonDown (InputName.Console)){
            UIC.ToggleConsole ();
		}
//		if(Input.GetKeyDown (KeyCode.Alpha1)){
//			WorldController.Instance.OnClickChangeTimeMultiplier (1);
//		}
		if (InputHandler.GetButtonDown (InputName.Rotate)) {
			if(BuildC.toBuildStructure != null){
				BuildC.toBuildStructure.RotateStructure ();
			}
		}
        if (InputHandler.GetButtonDown(InputName.Screenshot)) {
            ScreenCapture.CaptureScreenshot("screenshot_"+ System.DateTime.Now.ToString("dd_MM_yyyy-hh_mm_ss_ff")+".jpg");
        }
        if (Application.isEditor){
			if(Input.GetKey (KeyCode.LeftShift)){
				if(EventSystem.current.IsPointerOverGameObject ()==false){
					FindObjectOfType<HoverOverScript> ().DebugTileInfo (MouseC.GetTileUnderneathMouse ());
				} 
			}
			if(Input.GetKeyUp (KeyCode.LeftShift)){
                FindObjectOfType<HoverOverScript>().Unshow();
            }
		}

	}

}
