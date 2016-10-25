using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GS_MouseSensitivity : MonoBehaviour {

	public InputField input;
	Slider slider;
	// Use this for initialization
	void Start () {
		slider = GetComponent<Slider> ();

		input.onValueChanged.AddListener (OnInputValueChange);
		input.text="100";
		slider.onValueChanged.AddListener (OnSliderValueChange);
	}

	void OnSliderValueChange(float value) {
		input.text=""+value;
	}
	void OnInputValueChange(string value) {
		if(value==""){
			return;
		}
		slider.value = float.Parse (value);
	}

}
