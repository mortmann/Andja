using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour {
	AsyncOperation aso;
	public Text percentText;
	public bool loadEditor;
	float SceneLoadingProgress {
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
		int percantage = 0;
		if(loadEditor == false){
            if (SaveController.IsLoadingSave) {
                percantage = (int)(SceneLoadingProgress * 0.40f + MapGenerator.Instance.PercantageProgress * 0.3f + SaveController.Instance.loadingPercantage * 0.3f);
            }
            else {
                percantage = (int)(SceneLoadingProgress * 0.7f + MapGenerator.Instance.PercantageProgress * 0.3f);
            }
            //First wait for MapGeneration
            if (MapGenerator.Instance.IsDone == false) {
                return;
            }
            //Wait for Loading Save to be done when it is loading one
            if(SaveController.IsLoadingSave && SaveController.Instance.IsDone == false) {
                return;
            }
            if(aso == null)
				aso = SceneManager.LoadSceneAsync ("GameState");
		} else {
			percantage = (int)(SceneLoadingProgress);
		}
		percentText.text =  percantage + "%";
		
	}
}
