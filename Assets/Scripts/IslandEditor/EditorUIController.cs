using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EditorUIController : MonoBehaviour {

    public Button BuildButton;
    public Button TypeButton;

    public GameObject TypeList;
    public GameObject BuildList;
    public GameObject TypeListExact;
    public GameObject pauseMenu;
    public GameObject BuildListExact;
    public Button BuildDestroyButton;
    public Button BuildBuildButton;
    public static EditorUIController Instance;
    public GameObject newIsland;
    public GameObject RessourcesSetter;



    // Use this for initialization
    void Start() {
        if (Instance != null) {
            Debug.LogError("EditorUIController has two 2controller");
        }
        Instance = this;
        TypeButton.interactable = false;
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            TogglePauseMenu();
        }
    }
    public void ChangeBuild(bool type) {
        BuildButton.interactable = type;
        TypeListExact.SetActive(type);
        TypeList.SetActive(type);

        TypeButton.interactable = !type;
        BuildList.SetActive(!type);
        BuildListExact.SetActive(!type);
        BuildDestroyButton.gameObject.SetActive(!type);
        BuildBuildButton.gameObject.SetActive(!type);
    }

    public void DestroyToggle(bool b) {
        BuildDestroyButton.interactable = !b;
        BuildBuildButton.interactable = b;
    }
    public void TogglePauseMenu() {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
    }
    public void NewIslandToggle() {
        newIsland.SetActive(!newIsland.activeSelf);
    }
    public void RessourcesSetterToggle() {
        RessourcesSetter.SetActive(!RessourcesSetter.activeSelf);
    }
}
