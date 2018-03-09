using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour {
	AsyncOperation aso;
	public Text percentText;
	public bool loadEditor;
	float sceneLoadingProgress {
		get { 
			if (aso == null)
				return 0;
			return Mathf.Clamp (1f+(1.1f*aso.progress) * 100,0,100); }
	}
	// Use this for initialization
	void Start () {
		if(loadEditor)
			aso = SceneManager.LoadSceneAsync("IslandEditor");
	}
	
	void Update(){
		int percantage =(int)( sceneLoadingProgress * 0.7f + MapGenerator.Instance.percantageProgress * 0.3f );
		percentText.text =  percantage + "%";
		if(MapGenerator.Instance.isDone&&aso == null)
			aso = SceneManager.LoadSceneAsync ("GameState");
		
	}
}
