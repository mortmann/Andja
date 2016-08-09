using UnityEngine;
using System.Collections;

public class OptionsToggle : MonoBehaviour {
	public GameObject graphics;
	public GameObject sound;
	public void ToggleGraphics(){
		graphics.SetActive (true);
		sound.SetActive (false);
	}
	public void ToggleSound(){
		graphics.SetActive (false);
		sound.SetActive (true);
	}
}
