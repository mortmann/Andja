using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour {
	AsyncOperation aso;
	public Text percentText;
	public bool loadEditor;
    internal static bool IsLoading = true;

    float SceneLoadingProgress {
		get { 
			if (aso == null)
				return 0;
			return Mathf.Clamp (0.01f+(1.1f*aso.progress),0,1); }
	}
	// Use this for initialization
	void Start () {
        IsLoading = true;

        if (loadEditor)
			aso = SceneManager.LoadSceneAsync("IslandEditor");
	}
	
	void Update(){
		int percantage = 0;
		if(loadEditor == false){
            if (SaveController.IsLoadingSave) {
                percantage = (int) (100* (SceneLoadingProgress * 0.3f 
                    + MapGenerator.Instance.PercantageProgress * 0.2f 
                    + SaveController.Instance.loadingPercantage * 0.2f
                    + TileSpriteController.SpriteCreationPercantage * 0.3) );
            }
            else {
                percantage = (int)(SceneLoadingProgress * 0.7f + MapGenerator.Instance.PercantageProgress * 0.3f);
            }
            percentText.text = percantage + "%";
            //First wait for MapGeneration
            if (MapGenerator.Instance.IsDone == false) {
                return;
            }
            //Wait for Loading Save to be done when it is loading one
            if(SaveController.IsLoadingSave && SaveController.Instance.IsDone == false) {
                return;
            }
            if (TileSpriteController.SpriteCreationDone == false)
                return;
            if(aso == null)
				aso = SceneManager.LoadSceneAsync ("GameState");
		} else {
			percantage = (int)(SceneLoadingProgress);
            percentText.text = percantage + "%";
        }

    }
    public void OnDestroy() {
        IsLoading = false;
    }
}
