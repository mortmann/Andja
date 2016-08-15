using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class GS_Resolution : MonoBehaviour {
	Dropdown dp;
	Resolution[] resolutions;
	// Use this for initialization
	void Start () {
		dp = GetComponent<Dropdown> ();
		List<string> resses = new List<string> ();
		resolutions = Screen.resolutions;
		foreach (Resolution res in resolutions) {
			resses.Add(res.width + "x" + res.height + "@" +res.refreshRate);
		}
		dp.AddOptions (resses);
		dp.value = resolutions.Length-1;

	}
	public void OnChange(){
		Screen.SetResolution (resolutions[dp.value].width,resolutions[dp.value].height,Screen.fullScreen,resolutions[dp.value].refreshRate);

	
	}

}
