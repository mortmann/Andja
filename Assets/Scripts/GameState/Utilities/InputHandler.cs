using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Andja.Utility {

    public enum InputName { BuildMenu, TradeMenu, Offworld, TogglePause, Rotate, Console, Cancel, Screenshot, Stop, DiplomacyMenu }

    public class InputHandler {
        public static bool ShiftKey => Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift);

        private static Dictionary<InputName, KeyBind> nameToKeyBinds;
        private static string fileName = "input.ini";
        public static float MouseSensitivity { private set; get; }

        internal static void SetSensitivity(float value) {
            //TODO: make it even matter
        }

        //TODO add a between layer for mouse buttons -> so it can be switched

        // Use this for initialization
        public InputHandler() {
            nameToKeyBinds = new Dictionary<InputName, KeyBind>();
            LoadInputSchema(Application.dataPath.Replace("/Assets", ""));
            //		SetupKeyBinds ();
        }

        public static Dictionary<InputName, KeyBind> GetBinds() {
            return nameToKeyBinds;
        }

        private static void SetupKeyBinds() {
            foreach (InputName name in Enum.GetValues(typeof(InputName))) {
                KeyCode keyCode = KeyCode.RightWindows;
                if (nameToKeyBinds.ContainsKey(name)) //skip already declared
                    continue;
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
                }
                ChangePrimaryNameToKey(name, keyCode);
            }
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

        public static bool GetButton(InputName name) {
            if (nameToKeyBinds.ContainsKey(name) == false) {
                Debug.LogWarning("No KeyBind for Name " + name);
                return false;
            }
            return nameToKeyBinds[name].GetButton();
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
                MouseSensitivity = MouseSensitivity
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
        }
    }
}