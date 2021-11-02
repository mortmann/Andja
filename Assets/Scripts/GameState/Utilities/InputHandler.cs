using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Andja.Utility {

    public enum InputName { 
        BuildMenu, TradeMenu, Offworld, DiplomacyMenu,
        TogglePause, Stop, Cancel, 
        Rotate, CopyStructure, UpgradeTool,
        Screenshot, UnitGrouping,
        Console, BugReport 
    }
    public enum InputMouse {
        Primary, Middle, Secondary
    }
    public class InputHandler {
        public static bool ShiftKey => Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift);

        private static Dictionary<InputName, KeyBind> nameToKeyBinds;
        private static string fileName = "input.ini";
        public static float MouseSensitivity { private set; get; }
        public static bool IsSetup;
        public static bool InvertedMouseButtons;
        static int PrimaryMouse => InvertedMouseButtons ? 1 : 0;
        static int SecondaryMouse => InvertedMouseButtons ? 0 : 1;

        internal static void SetSensitivity(float value) {
            //TODO: make it even matter
        }
        static KeyCode[] alphaNumbers = new KeyCode[] { KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
            KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9};
        static KeyCode[] keyNumbers = new KeyCode[] { KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3,
            KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9 };

        public InputHandler() {
            if (IsSetup)
                return;
            nameToKeyBinds = new Dictionary<InputName, KeyBind>();
            LoadInputSchema(Application.dataPath.Replace("/Assets", ""));
            IsSetup = true;
            //		SetupKeyBinds ();
        }

        public static Dictionary<InputName, KeyBind> GetBinds() {
            return nameToKeyBinds;
        }

        private static void SetupKeyBinds() {
            foreach (InputName name in Enum.GetValues(typeof(InputName))) {
                if (nameToKeyBinds.ContainsKey(name)) //skip already declared
                    continue;
                KeyCode keyCode;
                //add here the base input layout
                switch (name) {
                    case InputName.BuildMenu:
                        keyCode = KeyCode.B;
                        break;

                    case InputName.TradeMenu:
                        keyCode = KeyCode.M;
                        break;

                    case InputName.Offworld:
                        keyCode = KeyCode.O;
                        break;

                    case InputName.TogglePause:
                        keyCode = KeyCode.Space;
                        break;

                    case InputName.Rotate:
                        keyCode = KeyCode.R;
                        break;

                    case InputName.Console:
                        keyCode = KeyCode.F1;
                        break;

                    case InputName.Cancel:
                        keyCode = KeyCode.Escape;
                        break;

                    case InputName.Screenshot:
                        keyCode = KeyCode.F12;
                        break;

                    case InputName.Stop:
                        keyCode = KeyCode.P;
                        break;

                    case InputName.DiplomacyMenu:
                        keyCode = KeyCode.N;
                        break;

                    case InputName.BugReport:
                        keyCode = KeyCode.F2;
                        break;

                    case InputName.CopyStructure:
                        keyCode = KeyCode.LeftControl;
                        break;

                    case InputName.UpgradeTool:
                        keyCode = KeyCode.LeftAlt;
                        break;

                    case InputName.UnitGrouping:
                        keyCode = KeyCode.LeftShift;
                        break;

                    default:
                        Debug.LogError("InputName " + name + " does not have a default value!");
                        continue;
                }
                ChangePrimaryNameToKey(name, keyCode);
            }
        }

        public static int HotkeyDown() {
            for (int i = 0; i < 10; i++) {
                if(Input.GetKeyDown(alphaNumbers[i]) || Input.GetKeyDown(keyNumbers[i])) {
                    return i;
                }
            }
            return -1;
        }

        public static void ChangePrimaryNameToKey(InputName name, KeyCode key) {
            RemoveKeyCodeFromBinds(key);
            if (nameToKeyBinds.ContainsKey(name)) {
                nameToKeyBinds[name].SetPrimary(key);
                return;
            }
            nameToKeyBinds.Add(name, new KeyBind(key, KeyBind.notSetCode));
        }

        public static void ChangeSecondaryNameToKey(InputName name, KeyCode key) {
            RemoveKeyCodeFromBinds(key);
            if (nameToKeyBinds.ContainsKey(name)) {
                nameToKeyBinds[name].SetSecondary(key);
                return;
            }
            nameToKeyBinds.Add(name, new KeyBind(KeyBind.notSetCode, key));
        }

        public static void RemoveKeyCodeFromBinds(KeyCode key) {
            KeyBind kb = GetKeyBindUses(key);
            if (kb == null)
                return;
            kb.RemoveKey(key);
        }

        public static KeyBind GetKeyBindUses(KeyCode key) {
            return new List<KeyBind>(nameToKeyBinds.Values).Find(x => x.HasKeyCode(key));
        }

        public static bool KeyAlreadyAssigned(KeyCode key) {
            return GetKeyBindUses(key) != null;
        }

        public static bool GetButtonDown(InputName name) {
            if (nameToKeyBinds.ContainsKey(name) == false) {
                Debug.LogWarning("No KeyBind for Name " + name);
                return false;
            }
            return nameToKeyBinds[name].GetButtonDown();
        }
        public static bool GetButtonUp(InputName name) {
            if (nameToKeyBinds.ContainsKey(name) == false) {
                Debug.LogWarning("No KeyBind for Name " + name);
                return false;
            }
            return nameToKeyBinds[name].GetButtonUp();
        }
        public static bool GetButton(InputName name) {
            if (nameToKeyBinds.ContainsKey(name) == false) {
                Debug.LogWarning("No KeyBind for Name " + name);
                return false;
            }
            return nameToKeyBinds[name].GetButton();
        }

        public static bool GetMouseButton(InputMouse name) {
            switch (name) {
                case InputMouse.Primary:
                    return Input.GetMouseButton(PrimaryMouse);
                case InputMouse.Middle:
                    return Input.GetMouseButton(2);
                case InputMouse.Secondary:
                    return Input.GetMouseButton(SecondaryMouse);
                default:
                    return false;
            }
        }
        public static bool GetMouseButtonDown(InputMouse name) {
            switch (name) {
                case InputMouse.Primary:
                    return Input.GetMouseButtonDown(PrimaryMouse);
                case InputMouse.Middle:
                    return Input.GetMouseButtonDown(2);
                case InputMouse.Secondary:
                    return Input.GetMouseButtonDown(SecondaryMouse);
                default:
                    return false;
            }
        }
        public static bool GetMouseButtonUp(InputMouse name) {
            switch (name) {
                case InputMouse.Primary:
                    return Input.GetMouseButtonUp(PrimaryMouse);
                case InputMouse.Middle:
                    return Input.GetMouseButtonUp(2);
                case InputMouse.Secondary:
                    return Input.GetMouseButtonUp(SecondaryMouse);
                default:
                    return false;
            }
        }
        public static void SaveInputSchema() {
            string path = Application.dataPath.Replace("/Assets", "");
            if (Directory.Exists(path) == false) {
                // NOTE: This can throw an exception if we can't create the folder,
                // but why would this ever happen? We should, by definition, have the ability
                // to write to our persistent data folder unless something is REALLY broken
                // with the computer/device we're running on.
                Directory.CreateDirectory(path);
            }
            string filePath = System.IO.Path.Combine(path, fileName);
            InputSave save = new InputSave {
                nameToKeyBinds = nameToKeyBinds,
                MouseSensitivity = MouseSensitivity,
                InvertedMouseButtons = InvertedMouseButtons
            };
            File.WriteAllText(filePath, JsonConvert.SerializeObject(save,
                new JsonSerializerSettings() {
                    Formatting = Newtonsoft.Json.Formatting.Indented
                }));
        }

        public static void LoadInputSchema(string path) {
            try {
                string filePath = System.IO.Path.Combine(path, fileName);
                if (File.Exists(filePath)) {
                    nameToKeyBinds = new Dictionary<InputName, KeyBind>();
                    string lines = File.ReadAllText(filePath);
                    InputSave save = JsonConvert.DeserializeObject<InputSave>(lines);
                    nameToKeyBinds = save.nameToKeyBinds;
                    MouseSensitivity = save.MouseSensitivity;
                    InvertedMouseButtons = save.InvertedMouseButtons;
                }
            }
            finally {
                SetupKeyBinds();
                SaveInputSchema(); // create the file so it can be manipulated
            }
        }

        public class KeyBind {
            public const KeyCode notSetCode = KeyCode.None;

            /// <summary>
            /// DO NOT SET DIRECTLY
            /// </summary>
            [JsonProperty]
            [JsonConverter(typeof(StringEnumConverter))]
            private KeyCode Primary = notSetCode;

            /// <summary>
            /// DO NOT SET DIRECTLY
            /// </summary>
            [JsonProperty]
            [JsonConverter(typeof(StringEnumConverter))]
            private KeyCode Secondary = notSetCode;

            public KeyBind() {
            }

            public KeyBind(KeyCode primary, KeyCode secondary) {
                this.Primary = primary;
                this.Secondary = secondary;
            }

            public String GetPrimaryString() {
                if (Primary == notSetCode) {
                    return "-";
                }
                return Primary.ToString();
            }

            public String GetSecondaryString() {
                if (Secondary == notSetCode) {
                    return "-";
                }
                return Secondary.ToString();
            }

            public bool SetPrimary(KeyCode k) {
                if (k == notSetCode) {
                    return false;
                }
                Primary = k;
                return true;
            }

            public bool SetSecondary(KeyCode k) {
                if (k == notSetCode) {
                    return false;
                }
                Secondary = k;
                return true;
            }

            public bool GetButtonDown() {
                return Input.GetKeyDown(Primary) && Primary != notSetCode
                    || Input.GetKeyDown(Secondary) && Secondary != notSetCode;
            }
            public bool GetButtonUp() {
                return Input.GetKeyUp(Primary) && Primary != notSetCode
                    || Input.GetKeyUp(Secondary) && Secondary != notSetCode;
            }
            public bool GetButton() {
                return Input.GetKey(Primary) && Primary != notSetCode
                    || Input.GetKey(Secondary) && Secondary != notSetCode;
            }

            public bool HasKeyCode(KeyCode key) {
                return Primary == key || Secondary == key;
            }

            internal void RemoveKey(KeyCode key) {
                if (Primary == key)
                    Primary = notSetCode;
                if (Secondary == key)
                    Secondary = notSetCode;
            }
        }

        private class InputSave {
            public Dictionary<InputName, KeyBind> nameToKeyBinds;
            public float MouseSensitivity;
            public bool InvertedMouseButtons;
        }
    }
}