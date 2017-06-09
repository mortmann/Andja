using UnityEngine;
using System.Collections;

public class PauseMenu : MonoBehaviour {

	public Transform startMenu;
	void OnEnable(){
		foreach(Transform t in transform){
			if(t != startMenu){
				t.gameObject.SetActive (false);
			} 
		}
		startMenu.gameObject.SetActive (true);
	}



}
