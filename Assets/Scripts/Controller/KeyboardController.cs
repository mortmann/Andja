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
		if(Input.GetKeyDown (KeyCode.M)){
			uic.OpenTradeMenu ();
		}
	}




}
