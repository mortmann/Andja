using System;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class NewGameSettingSetter : MonoBehaviour {
        public NewGameSetting Setting;
        public bool TextIsNumber;
        private InputField inputField;
        private Toggle toggle;
        Dropdown dropdown;

        private void Start() {
            inputField = GetComponentInChildren<InputField>();
            toggle = GetComponentInChildren<Toggle>();
            dropdown = GetComponentInChildren<Dropdown>();
            if (Setting == NewGameSetting.Seed) {
                inputField.text = UnityEngine.Random.Range(0, int.MaxValue) + "";
            }
            if (inputField != null) {
                inputField.onEndEdit.AddListener(InputFieldEndEdit);
                InputFieldEndEdit(inputField.text); // mainly for testing -- will set it to what the default text is
            }
            if (toggle != null) {
                toggle.onValueChanged.AddListener(ToggleChange);
                ToggleChange(toggle.isOn); // mainly for testing -- will set it to what the default text is
            }
            if (dropdown != null) {
                dropdown.onValueChanged.AddListener(DropdownChange);
                DropdownChange(dropdown.value);
            }
        }

        private void DropdownChange(int value) {
            switch (Setting) {
                case NewGameSetting.FogOfWar:
                    NewGameSettings.SetFogOfWar((Controller.FogOfWarStyle)value);
                    break;
            }
        }

        private void ToggleChange(bool value) {
            switch (Setting) {
                case NewGameSetting.Seed:
                    break;

                case NewGameSetting.Width:
                    break;

                case NewGameSetting.Height:
                    break;

                case NewGameSetting.Pirate:
                    NewGameSettings.SetPirate(value);
                    break;

                case NewGameSetting.Fire:
                    //NewGameSettings.SetFire(value);
                    Debug.LogWarning("NOT WORKING");
                    break;
            }
        }

        private void InputFieldEndEdit(string text) {
            int value = 0;
            if (TextIsNumber && int.TryParse(text, out value) == false && text.Length > 0) {
                Debug.LogError(transform.name + " Inputfield is not number only ");
            }
            switch (Setting) {
                case NewGameSetting.Seed:
                    NewGameSettings.SetSeed(text);
                    break;

                case NewGameSetting.Width:
                    NewGameSettings.SetWidth(value);
                    break;

                case NewGameSetting.Height:
                    NewGameSettings.SetHeight(value);
                    break;

                case NewGameSetting.Pirate:
                    break;

                case NewGameSetting.Fire:
                    break;
            }
        }
    }
}