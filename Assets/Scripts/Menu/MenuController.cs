using Andja.Controller;
using Andja.Editor;
using Andja.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class MenuController : MonoBehaviour {
        public static MenuController Instance;

        public GameObject menu;
        public GameObject[] panels;
        public GameObject[] ActiveMainMenu;
        public GameObject[] ActiveNotMainMenu;
        public Button ResumeButton;

        // Used by the buttons which open panels so we can return to the same button
        // after closing the panel.
        [HideInInspector]
        public Button currentButton;

        // Used to determine which game object our mouse pointer is currently hovering over.
        [HideInInspector]
        public GameObject currentMouseOverGameObject;

        public bool IsMainMenu => SceneManager.GetActiveScene().name == "MainMenu";

        private static bool mainMenuOpen = false;
        private bool panelOpen = false;

        private void OnEnable() {
            foreach (Transform t in transform) {
                if (t != menu.transform && t.GetComponent<YesNoDialog>() == null) {
                    t.gameObject.SetActive(false);
                }
            }
            menu.SetActive(true);
            if (IsMainMenu) {
                string lastsave = SaveController.GetLastSaveName();
                if (lastsave == null) {
                    ResumeButton.interactable = false;
                }
                else {
                    ResumeButton.GetComponentInChildren<TextLanguageSetter>().SetNameSuffix(lastsave);
                }
            }

            foreach (GameObject g in ActiveMainMenu) {
                g.SetActive(IsMainMenu);
            }
            foreach (GameObject g in ActiveNotMainMenu) {
                g.SetActive(IsMainMenu == false);
            }
        }

        private void Awake() {
            Instance = this;
        }

        private void Update() {
            if (IsMainMenu)
                return;
            if (InputHandler.GetButtonDown(InputName.Cancel)) {
                if (panelOpen) {
                    HideAllPanels();
                }
                else if (mainMenuOpen) {
                    HideMenu();
                }
                else {
                    ShowMenu();
                }
            }
        }

        public void GenericBackButton() {
            HideAllPanels();
        }

        public void Resume() {
            if (IsMainMenu) {
                LoadSaveGame(SaveController.GetLastSaveName());
            }
            else {
                UIController.Instance?.TogglePauseMenu();
            }
        }

        private void HideAllPanels() {
            foreach (GameObject panel in panels) {
                panel.SetActive(false);
            }
            panelOpen = false;
            ShowMenu();
            currentButton.Select();
        }

        public void ShowPanel(int id) {
            panels[id].SetActive(true);
            if (panels[id].GetComponent<GS_Panel>() == null) {
                Debug.LogWarning("first select is null");
            }
            else
                panels[id].GetComponent<GS_Panel>().SelectFirstElement();
            panelOpen = true;
            HideMenu();
        }

        public void ShowMenu() {
            menu.SetActive(true);
            mainMenuOpen = true;
        }

        public void HideMenu() {
            menu.SetActive(false);
            mainMenuOpen = false;
        }

        public void QuitToMenu() {
            if (SaveController.Instance.UnsavedProgress == false) {
                return;
            }
            FindObjectOfType<YesNoDialog>().Show(YesNoDialogTypes.UnsavedProgress, ChangeToMainMenuScreen, null);
        }

        public void QuitToDesktop() {
            if (SaveController.Instance?.UnsavedProgress == false) {
                return;
            }
            //If we are running in a standalone build of the game
#if UNITY_STANDALONE
            if (IsMainMenu) {
                Application.Quit();
            }
            else {
                //Quit the application
                FindObjectOfType<YesNoDialog>().Show(YesNoDialogTypes.UnsavedProgress, Application.Quit, null);
            }
#endif

            //If we are running in the editor
            //Stop playing the scene
#if UNITY_EDITOR
            if (IsMainMenu) {
                UnityEditor.EditorApplication.isPlaying = false;
            }
            else {
                FindObjectOfType<YesNoDialog>().Show(YesNoDialogTypes.UnsavedProgress, () => {
                    UnityEditor.EditorApplication.isPlaying = false;
                }, null);
            }
#endif
        }

        internal void LoadSaveGame(string selected) {
            GameData.setloadsavegame = selected;
            ChangeToGameStateLoadScreen();
        }

        public void ChangeToGameStateLoadScreen() {
            SceneManager.LoadScene("GameStateLoadingScreen");
        }

        public void ChangeToEditorLoadScreen() {
            EditorController.IsEditor = true;
            SceneManager.LoadScene("EditorLoadingScreen");
        }

        public void ChangeToGameStateScreen() {
            SceneManager.LoadScene("GameState");
        }

        public void ChangeToMainMenuScreen() {
            SceneManager.LoadScene("MainMenu");
        }
    }
}