using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour {
    public static MenuController instance;
    public bool saved;
    public GameObject menu;
    public GameObject[] panels;

    // Used by the buttons which open panels so we can return to the same button
    // after closing the panel.
    [HideInInspector]
    public Button currentButton;

    // Used to determine which game object our mouse pointer is currently hovering over.
    [HideInInspector]
    public GameObject currentMouseOverGameObject;

    // Chromatic abbrevation and vignetting share an image effect so we have
    // some public variables here so each of them can check if the other is
    // active or not before disabling/enabling the image effect component.
    [HideInInspector]
    public bool chromaticEnabled;
    [HideInInspector]
    public bool vignetteEnabled;

    static bool mainMenuOpen = false;
    bool panelOpen = false;
    void OnEnable() {
        foreach (Transform t in transform) {
            if (t != menu.transform) {
                t.gameObject.SetActive(false);
            }
        }
        menu.SetActive(true);
    }
    void Awake() {
        instance = this;
    }

    void Update() {
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

    void HideAllPanels() {
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
    public void Saved() {
        saved = true;
    }
    public void OnDisabled() {
        saved = false;
    }
    public bool hasSaved() {
        return saved;
    }
    public void Quit() {
        if (saved == false) {
            ShowWarning();
            scene = "Close";
            return;
        }
        //If we are running in a standalone build of the game
#if UNITY_STANDALONE
        //Quit the application
        Application.Quit();
#endif

        //If we are running in the editor
#if UNITY_EDITOR
        //Stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public GameObject dialog;
    string scene;

    public void ChangeToGameStateLoadScreen() {
        if (saved == false) {
            ShowWarning();
            scene = "GameStateLoadingScreen";
            return;
        }
        SceneManager.LoadScene("GameStateLoadingScreen");
    }
    public void ChangeToEditorLoadScreen() {
        SceneManager.LoadScene("EditorLoadingScreen");
    }
    public void ChangeToGameStateScreen() {
        SceneManager.LoadScene("GameState");
    }
    public void ChangeToMainMenuScreen() {
        if (saved == false) {
            ShowWarning();
            scene = "MainMenu";
            return;
        }
        SceneManager.LoadScene("MainMenu");
    }
    public void ShowWarning() {
        dialog.SetActive(true);
    }
    public void DialogYesOption() {
        if (scene == "Close") {
            Quit();
            return; // should not be needed
        }
        SceneManager.LoadScene(scene);
    }


}
