using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Andja.UI.Menu {

    public enum GameplaySetting { Autorotate, Language }

    public class GameplaySettings : MonoBehaviour {
        public static GameplaySettings Instance;

        public List<string> Localizations;

        // Use this for initialization
        private void Start() {
            Instance = this;
            gameplayOptions = new Dictionary<GameplaySetting, string>();
            gameplayOptionsToSave = new Dictionary<GameplaySetting, string>();
            Localizations = new List<string>(UILanguageController.Instance.LocalizationsToFile.Keys);
            ReadGameplayOption();
            SaveGameplayOption();

            OptionsToggle.ChangedState += OnOptionClosed;
        }

        private Dictionary<GameplaySetting, string> gameplayOptions;
        private Dictionary<GameplaySetting, string> gameplayOptionsToSave;

        private string fileName = "gameplay.ini";

        public void SaveGameplayOption() {
            if (gameplayOptionsToSave == null)
                return; //only happens if pausemenu is active wenn gamestate is loaded

            SetOptions(gameplayOptionsToSave);
            foreach (GameplaySetting s in gameplayOptionsToSave.Keys) {
                gameplayOptions[s] = gameplayOptionsToSave[s];
            }
            gameplayOptionsToSave.Clear();

            string path = Application.dataPath.Replace("/Assets", "");
            if (Directory.Exists(path) == false) {
                // NOTE: This can throw an exception if we can't create the folder,
                // but why would this ever happen? We should, by definition, have the ability
                // to write to our persistent data folder unless something is REALLY broken
                // with the computer/device we're running on.
                Directory.CreateDirectory(path);
            }
            if (gameplayOptions == null) {
                return;
            }
            string filePath = System.IO.Path.Combine(path, fileName);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(gameplayOptions));
        }

        public bool ReadGameplayOption() {
            string filePath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), fileName);
            if (File.Exists(filePath) == false) {
                return false;
            }
            gameplayOptions = JsonConvert.DeserializeObject<Dictionary<GameplaySetting, string>>(File.ReadAllText(filePath));
            SetOptions(gameplayOptions);
            foreach (GameplaySetting s in Enum.GetValues(typeof(GameplaySetting))) {
                if (HasSavedGameplayOption(s) == false) {
                    switch (s) {
                        case GameplaySetting.Autorotate:
                            SetSavedGameplayOption(s, true);
                            break;

                        case GameplaySetting.Language:
                            SetSavedGameplayOption(s, UILanguageController.selectedLanguage);
                            break;

                        default:
                            Debug.Log("ADD DEFAULT SETTING TO GAMEPLAYSETTING " + s);
                            break;
                    }
                }
            }
            return true;
        }

        private void SetOptions(Dictionary<GameplaySetting, string> options) {
            foreach (GameplaySetting optionName in options.Keys) {
                string val = options[optionName];
                switch (optionName) {
                    case GameplaySetting.Autorotate:
                        MouseController.autorotate = bool.Parse(val);
                        break;

                    case GameplaySetting.Language:
                        UILanguageController.Instance.ChangeLanguage(val);
                        break;

                    default:
                        Debug.LogWarning("No case for " + optionName);
                        break;
                }
            }
        }

        public void SetSavedGameplayOption(GameplaySetting name, object val) {
            gameplayOptionsToSave[name] = val.ToString();
        }

        public bool HasSavedGameplayOption(GameplaySetting name) {
            return gameplayOptions.ContainsKey(name);
        }

        public string GetSavedGameplayOption(GameplaySetting name) {
            if (HasSavedGameplayOption(name) == false)
                return null;
            return gameplayOptions[name];
        }

        private void OnOptionClosed(bool open) {
            if (open)
                return;
            SaveGameplayOption();
        }
    }
}