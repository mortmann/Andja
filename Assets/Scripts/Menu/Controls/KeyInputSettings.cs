using Andja.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class KeyInputSettings : MonoBehaviour {
        public Transform contentTransform;
        public GameObject keyPrefab;
        private Dictionary<InputName, KeyInputSingle> buttonNameToGO;
        private InputName selectedButton;
        private bool hasSelected = false;
        private bool primaryChange;

        // Use this for initialization
        private void Start() {
            new InputHandler();
            buttonNameToGO = new Dictionary<InputName, KeyInputSingle>();
            foreach (Transform item in contentTransform) {
                Destroy(item.gameObject);
            }
            foreach (InputName name in InputHandler.GetBinds().Keys) {
                InputHandler.KeyBind item = InputHandler.GetBinds()[name];
                KeyInputSingle key = Instantiate(keyPrefab, contentTransform, false).GetComponent<KeyInputSingle>();
                key.SetUp(name, item, OnClickButton);
                //key.transform.SetParent(contentTransform);
                buttonNameToGO.Add(name, key);
            }
        }

        public void OnClickButton(InputName name, bool primary) {
            hasSelected = true;
            selectedButton = name;
            primaryChange = primary;
            Cursor.visible = false;
        }

        public static void SetMouseSensitivity(float value) {
            InputHandler.SetSensitivity(value);
        }
        public void OnGUI() {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                hasSelected = false;
                Cursor.visible = true;
            }
            else
                if (Event.current != null && (Event.current.isKey)) {
                if (hasSelected == false) {
                    return;
                }
                KeyCode s = Event.current.keyCode;
                if (s == InputHandler.KeyBind.notSetCode) {
                    return;
                }
                buttonNameToGO[selectedButton].ChangeButtonText(primaryChange, "" + s);
                if (primaryChange) {
                    InputHandler.ChangePrimaryNameToKey(selectedButton, s);
                }
                else {
                    InputHandler.ChangeSecondaryNameToKey(selectedButton, s);
                }
                Cursor.visible = true;
                InputHandler.SaveInputSchema();
                hasSelected = false;
            }
        }
    }
}