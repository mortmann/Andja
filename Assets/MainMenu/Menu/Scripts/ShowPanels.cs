using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShowPanels : MonoBehaviour {

    public static ShowPanels instance;

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

    bool menuOpen = true;
    bool panelOpen = false;

    void Awake() {
        instance = this;
    }

    void Update() {
        if (Input.GetButtonDown("Cancel")) {
            if (panelOpen) {
                HideAllPanels();
            }
            else if (menuOpen) {
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
        panels[id].GetComponent<GS_Panel>().SelectFirstElement();
        panelOpen = true;
        HideMenu();
    }

    public void ShowMenu() {
        menu.SetActive(true);
        menuOpen = true;
    }
    public void HideMenu() {
        menu.SetActive(false);
        menuOpen = false;
    }

    public void Quit() {
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
}
