using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CircleProgressBar : MonoBehaviour {

    public Image fillingCircle;
    public Text percentText;

	// Use this for initialization
	void Start () {
		
	}
	
	public void SetProgress(float amount) {
        percentText.text = Mathf.RoundToInt(amount * 100) + "%";
        fillingCircle.fillAmount = amount;
    }
}
