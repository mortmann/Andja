using UnityEngine;
using System.Collections;

public class OptionsToggle : MonoBehaviour {
	public GameObject graphics;
	public GameObject sound;
	public GameObject controls;
	public void ToggleGraphics(){
		graphics.SetActive (true);
		sound.SetActive (false);
		controls.SetActive (false);
	}
	public void ToggleSound(){
		graphics.SetActive (false);
		sound.SetActive (true);
		controls.SetActive (false);
	}
	public void ToggleControls(){
		graphics.SetActive (false);
		sound.SetActive (false);
		controls.SetActive (true);
	}
}
