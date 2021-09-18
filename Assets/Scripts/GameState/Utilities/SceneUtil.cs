using UnityEngine;
using UnityEngine.SceneManagement;

namespace Andja.Utility {
    public static class SceneUtil {
        static bool SkipGameData = false;
        public static void ChangeToGameStateLoadScreen(bool deleteAll = false, bool skipGameData = false) {
            SkipGameData = skipGameData;
            if (deleteAll)
                DeleteAllGameObjects();
            SceneManager.LoadScene("GameStateLoadingScreen");
        }

        public static void ChangeToEditorLoadScreen(bool deleteAll = false) {
            if (deleteAll)
                DeleteAllGameObjects();
            Editor.EditorController.IsEditor = true;
            SceneManager.LoadScene("EditorLoadingScreen");
        }

        public static void ChangeToGameStateScreen() {
            SceneManager.LoadScene("GameState");
        }

        public static void ChangeToMainMenuScreen(bool deleteAll = false) {
            if (deleteAll)
                DeleteAllGameObjects();
            SceneManager.LoadScene("MainMenu");
        }

        private static void DeleteAllGameObjects() {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject go in allObjects)
                if (go.activeInHierarchy || SkipGameData && go != GameData.Instance.gameObject)
                    GameObject.Destroy(go);
            SkipGameData = false;
        }
    }
}