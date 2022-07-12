using Andja.Controller;
using Andja.Editor;
using Andja.Model;
using Andja.Model.Generator;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Andja {
    /// <summary>
    /// Handles loading percentage and clears the console for less spam.
    /// </summary>
    public enum LoadingType { None, Editor, Game }
    public class Loading : MonoBehaviour {
        private AsyncOperation aso;
        public Text percentText;
        public Slider percentBar;
        public int percantage = 0;
        public LoadingType Type;
        internal static bool IsLoading = false;
        private Stopwatch loadingStopWatch;
        private bool AsyncLoadDebug = false;

        private float SceneLoadingProgress {
            get {
                if (aso == null)
                    return 0;
                return Mathf.Clamp(aso.progress / 0.9f, 0, 1);
            }
        }

        // Use this for initialization
        private void Awake() {
            if (Application.isEditor)
                ClearConsole();
            EditorController.IsEditor = Type == LoadingType.Editor;
            IsLoading = true;
            loadingStopWatch = new Stopwatch();
            loadingStopWatch.Start();
        }

        private void Update() {
            if (aso == null && Application.isEditor == false) {
                if (EditorController.IsEditor) {
                    aso = SceneManager.LoadSceneAsync("IslandEditor");
                    aso.allowSceneActivation = false;
                }
                else {
                    if (string.IsNullOrEmpty(GameData.Instance.LoadSaveGame) == false
                        && SaveController.Instance.DoesGameSaveExist(GameData.Instance.LoadSaveGame) == false) {
                        UI.Menu.MainMenuInfo.AddInfo(UI.Menu.MainMenuInfo.InfoTypes.SaveFileError, 
                            GameData.Instance.LoadSaveGame + " Save does not exist!");
                        Utility.SceneUtil.ChangeToMainMenuScreen(true);
                        Destroy(FindObjectOfType<MasterController>().gameObject);
                        Destroy(FindObjectOfType<MapGenerator>().gameObject);
                        return;
                    }
                    aso = SceneManager.LoadSceneAsync("GameState");
                    aso.allowSceneActivation = false;
                }
            }
            if (EditorController.IsEditor == false) {
                if (SaveController.IsLoadingSave) {
                    float mapGenValue = MapGenerator.Instance != null ? MapGenerator.Instance.GeneratedProgressPercantage : 1;
                    percantage = (int)(99 * (SceneLoadingProgress * 0.3f
                        + mapGenValue * 0.2f
                        + SaveController.Instance.loadingPercentage * 0.2f
                        + TileSpriteController.CreationPercentage * 0.3));
                }
                else {
                    percantage = (int)(100 * (SceneLoadingProgress * 0.7f + MapGenerator.Instance.GeneratedProgressPercantage * 0.3f));
                }
                SetPercantage(percantage);
                //First wait for MapGeneration
                if (MapGenerator.Instance != null && MapGenerator.Instance.IsDone == false) {
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
                if (Application.isEditor && aso == null)
                    aso = SceneManager.LoadSceneAsync("GameState");
                aso.allowSceneActivation = true;
            }
            else {
                percantage = (int)(SceneLoadingProgress * 100);
                if (MapGenerator.Instance != null) {
                    percantage = (int)(MapGenerator.Instance.GeneratedProgressPercantage * 100 * 0.7f + percantage * 0.3f);
                    SetPercantage(percantage);
                }
                else
                    SetPercantage(percantage);
                if (EditorController.Generate && MapGenerator.Instance.IsDone == false) {
                    return;
                }
                if (Application.isEditor && aso == null)
                    aso = SceneManager.LoadSceneAsync("IslandEditor");
                aso.allowSceneActivation = true;
            }
        }

        private void SetPercantage(int percantage) {
            percentText.text = percantage + "%";
            percentBar.value = percantage / 100f;
        }

        private static void ClearConsole() {
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");

            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            clearMethod.Invoke(null, null);
        }

        public void OnDestroy() {
            loadingStopWatch?.Stop();
            UnityEngine.Debug.Log("Loading took " + loadingStopWatch.ElapsedMilliseconds + "ms " +
                "(" + loadingStopWatch.Elapsed.TotalSeconds + "s)! ");
            IsLoading = false;
            Type = LoadingType.None;
        }
    }
}