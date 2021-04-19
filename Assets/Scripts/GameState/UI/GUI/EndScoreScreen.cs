using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Andja.UI {

    public class EndScoreScreen : MonoBehaviour {
        public Button closeButton;

        private void Start() {
            closeButton.onClick.AddListener(OnCloseButton);
        }

        private void OnCloseButton() {
            SceneManager.LoadScene("MainMenu");
        }

        private void Update() {
        }
    }
}