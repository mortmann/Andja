using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Diagnostics;

public class Loading : MonoBehaviour {
    AsyncOperation aso;
    public Text percentText;
    public bool loadEditor;
    internal static bool IsLoading = false;
    private Stopwatch loadingStopWatch;
    bool AsyncLoadDebug = false;
    float SceneLoadingProgress {
        get {
            if (aso == null)
                return 0;
            return Mathf.Clamp(0.01f + (1.1f * aso.progress), 0, 1);
        }
    }
    // Use this for initialization
    void Awake() {
        //if (Application.isEditor)
        //    ClearConsole();
        EditorController.IsEditor = loadEditor;
        IsLoading = true;
        loadingStopWatch = new Stopwatch();
        loadingStopWatch.Start();
    }

    void Update() {
        if (aso == null && Application.isEditor==false) {
            if (loadEditor) {
                aso = SceneManager.LoadSceneAsync("IslandEditor");
                aso.allowSceneActivation = false;
            }
            else {
                if (SaveController.Instance.DoesGameSaveExist(GameDataHolder.Instance.Loadsavegame) == false) {
                    UnityEngine.Debug.LogError(GameDataHolder.Instance.Loadsavegame + " Save does not exist!");
                    SceneManager.LoadScene("MainMenu");
                    Destroy(FindObjectOfType<MasterController>().gameObject);
                    Destroy(FindObjectOfType<MapGenerator>().gameObject);
                    return;
                }
                aso = SceneManager.LoadSceneAsync("GameState");
                aso.allowSceneActivation = false;
            }
        }
        int percantage = 0;
        if (loadEditor == false) {
            if (SaveController.IsLoadingSave) {
                float mapGenValue = MapGenerator.Instance != null ? MapGenerator.Instance.GeneratedProgressPercantage : 1;
                percantage = (int)(100 * (SceneLoadingProgress * 0.3f
                    + mapGenValue * 0.2f
                    + SaveController.Instance.loadingPercantage * 0.2f
                    + TileSpriteController.CreationPercantage * 0.3));
            }
            else {
                percantage = (int)(100 * (SceneLoadingProgress * 0.7f + MapGenerator.Instance.GeneratedProgressPercantage * 0.3f));
            }
            percentText.text = percantage + "%";
            //First wait for MapGeneration
            if (MapGenerator.Instance!=null&&MapGenerator.Instance.IsDone == false) {
                return;
            }
            //Wait for Loading Save to be done when it is loading one
            if (SaveController.IsLoadingSave && SaveController.Instance.IsDone == false) {
                return;
            }
            if (TileSpriteController.CreationDone == false)
                return;
            if (AsyncLoadDebug == false) {
                AsyncLoadDebug = true;
                UnityEngine.Debug.Log("Load Async after " + loadingStopWatch.ElapsedMilliseconds + "ms (" + loadingStopWatch.Elapsed.TotalSeconds + "s)! ");
            }
            if (Application.isEditor && aso==null)
                aso = SceneManager.LoadSceneAsync("GameState");
            aso.allowSceneActivation = true;
        }
        else {
            percantage = (int)(SceneLoadingProgress * 100);
            if (MapGenerator.Instance != null) {
                percantage = (int)(MapGenerator.Instance.GeneratedProgressPercantage * 100 * 0.7f + percantage * 0.3f);
                percentText.text = percantage + "%";
            }
            else
                percentText.text = percantage + "%";
            if (EditorController.Generate && MapGenerator.Instance.IsDone == false) {
                return;
            }
            if (Application.isEditor && aso == null)
                aso = SceneManager.LoadSceneAsync("IslandEditor");
            aso.allowSceneActivation = true;
        }

    }
    static void ClearConsole() {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");

        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

        clearMethod.Invoke(null, null);
    }


    public void OnDestroy() {
        loadingStopWatch?.Stop();
        UnityEngine.Debug.Log("Loading took " + loadingStopWatch.ElapsedMilliseconds + "ms (" + loadingStopWatch.Elapsed.TotalSeconds + "s)! ");
        IsLoading = false;
    }
}
