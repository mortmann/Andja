using Andja.Model;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Andja.Utility {

    //just to change it to loading cause nothing works when you start here!
    public class ToLoadingChanger : MonoBehaviour {

        private void OnEnable() {
            if (World.Current != null) {
                Destroy(this);
                return;
            }
            else if (SceneManager.GetActiveScene().name == "GameState")
                AutoGameState();
        }

        public void NewGameStart() {
            GameData.Instance.editorloadsavegame = null;
            SceneUtil.ChangeToGameStateLoadScreen(false);
        }

        public void AutoGameState() {
            SceneUtil.ChangeToGameStateLoadScreen(true);
        }

        public void Editor() {
            SceneUtil.ChangeToEditorLoadScreen(true);
        }
    }
}