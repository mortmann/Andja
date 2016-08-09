using UnityEngine;
using System.Collections;

public class KeyboardController : MonoBehaviour {

	// Use this for initialization
	UIController uic;
	MouseController mc;
	BuildController bc;
	void Start () {
		uic = GameObject.FindObjectOfType<UIController>();
		mc = GameObject.FindObjectOfType<MouseController>();
		bc = GameObject.FindObjectOfType<BuildController> ();
	}
	// Update is called once per frame
	void Update () {
		if (Input.GetButtonDown ("BuildMenu")) {
			uic.showBuildMenu();
		}
		if(Input.GetKeyDown (KeyCode.Escape)){
			mc.Escape ();
			uic.Escape ();
			bc.Escape ();
		}
		if(Input.GetKeyDown (KeyCode.M)){
			uic.ToggleTradeMenu ();
		}
		if (Input.GetButtonDown ("Rotate")) {
			if(bc.toBuildStructure != null){
				bc.toBuildStructure.RotateStructure ();
			}
		}
	}




}
