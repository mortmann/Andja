using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Diagnostics;

public class Loading : MonoBehaviour {
    AsyncOperation aso;
    public Text percentText;
    public bool loadEditor;
    internal static bool IsLoading = true;
    private Stopwatch loadingStopWatch;

    float SceneLoadingProgress {
        get {
            if (aso == null)
                return 0;
            return Mathf.Clamp(0.01f + (1.1f * aso.progress), 0, 1);
        }
    }
    // Use this for initialization
    void Start() {
        IsLoading = true;
        loadingStopWatch = new Stopwatch();
        loadingStopWatch.Start();
    }

    void Update() {
        if(aso == null) {
            if (loadEditor)
                aso = SceneManager.LoadSceneAsync("IslandEditor");
            else {
                aso = SceneManager.LoadSceneAsync("GameState");
                aso.allowSceneActivation = false;
            }
        }
        int percantage = 0;
        if (loadEditor == false) {
            if (SaveController.IsLoadingSave) {
                percantage = (int)(100 * (SceneLoadingProgress * 0.3f
                    + MapGenerator.Instance.PercantageProgress * 0.2f
                    + SaveController.Instance.loadingPercantage * 0.2f
                    + TileSpriteController.CreationPercantage * 0.3));
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
            if (SaveController.IsLoadingSave && SaveController.Instance.IsDone == false) {
                return;
            }
            if (TileSpriteController.CreationDone == false)
                return;
            UnityEngine.Debug.Log("Load Async after " + loadingStopWatch.ElapsedMilliseconds + "ms (" + loadingStopWatch.Elapsed.TotalSeconds + "s)! ");

            aso.allowSceneActivation = true;
        }
        else {
            percantage = (int)(SceneLoadingProgress);
            percentText.text = percantage + "%";
        }

    }
    public void OnDestroy() {
        loadingStopWatch.Stop();
        UnityEngine.Debug.Log("Loading took " + loadingStopWatch.ElapsedMilliseconds + "ms (" + loadingStopWatch.Elapsed.TotalSeconds + "s)! ");
        IsLoading = false;
    }
}
