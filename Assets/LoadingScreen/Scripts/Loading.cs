using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour {
	AsyncOperation aso;
	public Text percentText;
	public bool loadEditor;
	// Use this for initialization
	void Start () {
		if(loadEditor==false)
			aso = SceneManager.LoadSceneAsync ("GameState");
		else 
			aso = SceneManager.LoadSceneAsync("IslandEditor");
	}
	
	void Update(){
		percentText.text = Mathf.Clamp (1f+(1.1f*aso.progress) * 100,0,100) + "%";
	}
}
