using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour {
	public static bool isOpen = false;
	void OnEnable(){
		isOpen = true;
	}
	void OnDisable(){
		isOpen = false;
	}
}
