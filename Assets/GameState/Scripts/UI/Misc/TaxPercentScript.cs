using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class TaxPercentScript : MonoBehaviour {
	Slider mySlider;
	public Text knobText;
	// Use this for initialization
	void Start () {
		mySlider = gameObject.GetComponent<Slider> ();
	}
	
	// Update is called once per frame
	void Update () {
		knobText.text = 2*Mathf.FloorToInt(mySlider.value*100) + "%";
	}
}
