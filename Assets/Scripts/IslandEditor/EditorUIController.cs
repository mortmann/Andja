using Andja.UI.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.Editor {

    public class EditorUIController : MonoBehaviour {
        public Button BuildButton;
        public Button TypeButton;

        public GameObject TypeList;
        public GameObject BuildList;
        public GameObject TypeListExact;
        public PauseMenu pauseMenu;
        public GameObject BuildListExact;
        public Button BuildDestroyButton;
        public Button BuildBuildButton;
        public static EditorUIController Instance;
        public GameObject newIsland;
        public GameObject ResourcesSetter;

        // Use this for initialization
        private void Start() {
            if (Instance != null) {
                Debug.LogError("EditorUIController has two 2controller");
            }
            Instance = this;
            TypeButton.interactable = false;
        }

        // Update is called once per frame
        private void Update() {
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
            pauseMenu.Toggle();
        }

        public void NewIslandToggle() {
            newIsland.SetActive(!newIsland.activeSelf);
        }

        public void ResourcesSetterToggle() {
            ResourcesSetter.SetActive(!ResourcesSetter.activeSelf);
        }

        private void OnDestroy() {
            Instance = null;
        }
    }
}