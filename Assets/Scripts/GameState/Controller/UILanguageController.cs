using Andja.Model;
using Andja.UI;
using Andja.UI.Menu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Andja.Controller {

    public enum StaticLanguageVariables { 
        On, Off, 
        And, Or, 
        Yes, No,
        Empty, 
        BuildCost, Upkeep, Price,
        Locked, 
        CurrentDamage, MaximumDamage, Speed,
        Balance,
        BalanceChange,
        Pirate, FlyingTrader,
        World
    }
    /// <summary>
    /// Loads Translations (except prototypes sends it off to PrototypController) and changing language 
    /// </summary>
    public class UILanguageController : MonoBehaviour {
        public static UILanguageController Instance { get; protected set; }
        private Action _cbLanguageChange;

        public static string selectedLanguage = "English";
        private Dictionary<string, TranslationData> _idToTranslation;
        private List<TranslationData> _requiredLocalizationData;
        public Dictionary<string, string> LocalizationsToFile;

        public static readonly string LocalizationFilePrefix = "";
        public static readonly string LocalizationFileType = "-ui.loc";
        public static readonly string LocalizationXMLDirectory = "Localizations";
        private void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two UILanguageController.");
            }
            Instance = this;
            SceneManager.sceneLoaded += OnLevelLoaded;
        }

        private void OnLevelLoaded(Scene s, LoadSceneMode lsm) {
            Dictionary<string, TranslationData> localizationDataDictionary = new Dictionary<string, TranslationData>();
            TranslationBase[] texts = Resources.FindObjectsOfTypeAll<TranslationBase>();
            foreach (TranslationBase t in texts) {
                foreach (TranslationData td in t.GetTranslationDatas()) {
                    if (td.id == null || td.id.Trim().Length == 0) {
                        Debug.LogError("Text Identifier is Empty for " + t.GetRealName());
                        continue;
                    }
                    if (localizationDataDictionary.ContainsKey(td.id) == false) {
                        localizationDataDictionary.Add(td.id, td);
                    }
                    localizationDataDictionary[td.id].AddUIElement(t.GetRealName());
                }
            }
            if (_requiredLocalizationData == null)
                _requiredLocalizationData = new List<TranslationData>(localizationDataDictionary.Values);
            else
                _requiredLocalizationData.AddRange(localizationDataDictionary.Values);
        }
        public void OnEnable() {
            _idToTranslation = new Dictionary<string, TranslationData>();
            //#if Unity_Editor
            TranslationBase[] texts = Resources.FindObjectsOfTypeAll<TranslationBase>();
            Dictionary<string, TranslationData> localizationDataDictionary = new Dictionary<string, TranslationData>();
            foreach (TranslationBase t in texts) {
                foreach (TranslationData td in t.GetTranslationDatas()) {
                    if (td.id == null || td.id.Trim().Length == 0) {
                        Debug.LogError("Text Identifier is Empty for " + t.GetRealName());
                        continue;
                    }
                    if (localizationDataDictionary.ContainsKey(td.id) == false) {
                        localizationDataDictionary.Add(td.id, td);
                    }
                    localizationDataDictionary[td.id].AddUIElement(t.GetRealName());
                }
            }
            foreach (GraphicsOptions go in Enum.GetValues(typeof(GraphicsOptions))) {
                string name = nameof(GraphicsOptions) + "/" + go.ToString();
                if (_idToTranslation.ContainsKey(name) == false) {
                    localizationDataDictionary.Add(name, new TranslationData(name, false, 0));
                }
            }
            foreach (FullScreenMode go in Enum.GetValues(typeof(FullScreenMode))) {
                string name = nameof(FullScreenMode) + "/" + go.ToString();
                if (_idToTranslation.ContainsKey(name) == false) {
                    localizationDataDictionary.Add(name, new TranslationData(name, false, 0));
                }
            }
            foreach (InformationType go in Enum.GetValues(typeof(InformationType))) {
                string name = nameof(InformationType) + "/" + go.ToString();
                if (_idToTranslation.ContainsKey(name) == false) {
                    localizationDataDictionary.Add(name, new TranslationData(name, false, 0));
                }
            }
            foreach (StaticLanguageVariables go in Enum.GetValues(typeof(StaticLanguageVariables))) {
                string name = nameof(StaticLanguageVariables) + "/" + go.ToString();
                if (_idToTranslation.ContainsKey(name) == false) {
                    localizationDataDictionary.Add(name, new TranslationData(name, false, 0));
                }
            }
            foreach (DiplomacyType go in Enum.GetValues(typeof(DiplomacyType))) {
                string name = nameof(DiplomacyType) + "/" + go.ToString();
                if (_idToTranslation.ContainsKey(name) == false) {
                    localizationDataDictionary.Add(name, new TranslationData(name, false, 0));
                }
            }
            foreach (FogOfWarStyle go in Enum.GetValues(typeof(FogOfWarStyle))) {
                string name = nameof(FogOfWarStyle) + "/" + go.ToString();
                if (_idToTranslation.ContainsKey(name) == false) {
                    localizationDataDictionary.Add(name, new TranslationData(name, false, 0));
                }
            }
            foreach (MapErrorMessage go in Enum.GetValues(typeof(MapErrorMessage))) {
                string name = nameof(MapErrorMessage) + "/" + go.ToString();
                if (_idToTranslation.ContainsKey(name) == false) {
                    localizationDataDictionary.Add(name, new TranslationData(name, false, 0));
                }
            }
            if (_requiredLocalizationData == null)
                _requiredLocalizationData = new List<TranslationData>(localizationDataDictionary.Values);
            else
                _requiredLocalizationData.AddRange(localizationDataDictionary.Values);
            _requiredLocalizationData = _requiredLocalizationData.OrderBy(x => x.id).ToList();
            //#endif //Unity_Editor
            LocalizationsToFile = new Dictionary<string, string>();
            string fullpath = Path.Combine(ConstantPathHolder.StreamingAssets, GameData.DataLocation, LocalizationXMLDirectory);
            string[] allLocalizationsFiles = Directory.GetFiles(fullpath, LocalizationFilePrefix
                                                                        + "*" + LocalizationFileType);
            //Check the files if they are readable
            foreach (string file in allLocalizationsFiles) {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(File.ReadAllText(file));
                LocalizationsToFile.Add(xmlDoc.DocumentElement.Attributes[0].InnerXml, file);
            }
            if (LocalizationsToFile.ContainsKey(selectedLanguage) == false) {
                selectedLanguage = new List<string>(LocalizationsToFile.Keys)[0]; //just for the edge case of someone deleting english
            }
            LoadLocalization(LocalizationsToFile[selectedLanguage]);
        }

        internal List<string> LoadHints() {
            XmlSerializer xml = new XmlSerializer(typeof(Hints));
            string file = Path.Combine(ConstantPathHolder.StreamingAssets, GameData.DataLocation, 
                LocalizationXMLDirectory, "hints-"  + selectedLanguage + ".loc");
            if(File.Exists(file) == false) {
                return new List<string> {"No Hint list was found for selected Language.", "Do not be afraid. This does not interrupts the rest of the gameplay.", "Except of course their is more missing than that.", "Also never pet a burning dog."};
            }
            Hints hints = xml.Deserialize(new StringReader(File.ReadAllText(file))) as Hints;
            return hints?.hints == null ? new List<string>() : new List<string>(hints.hints);
        }

        public TranslationData GetTranslationData(string name) {
            return _idToTranslation.ContainsKey(name) == false ? null : _idToTranslation[name];
        }

        public TranslationData GetTranslationData(StaticLanguageVariables val) {
            return GetTranslationData(nameof(StaticLanguageVariables) +"/"+ val.ToString());
        }
        public TranslationData GetTranslationData(InformationType val) {
            return GetTranslationData(nameof(InformationType) + "/" + val.ToString());
        }
        public TranslationData GetTranslationData(DiplomacyType val) {
            return GetTranslationData(nameof(DiplomacyType) + "/" + val.ToString());
        }
        public void AddTranslationData(TranslationData data) {
            _requiredLocalizationData.Add(data);
        }

        public void ChangeLanguage(string language) {
            if (LocalizationsToFile.ContainsKey(language) == false) {
                Debug.LogWarning("Selected Language not available!");
                language = "English";
            }
            selectedLanguage = language;
            LoadLocalization(LocalizationsToFile[selectedLanguage]);
            PrototypController.Instance?.ReloadLanguage();
            _cbLanguageChange?.Invoke();
        }

        public void RegisterLanguageChange(Action callbackfunc) {
            _cbLanguageChange += callbackfunc;
        }

        public void UnregisterLanguageChange(Action callbackfunc) {
            _cbLanguageChange -= callbackfunc;
        }

        public void OnDestroy() {
            if (Application.isEditor) {
                FileStream file = File.Create(Path.Combine(Application.dataPath.Replace("/Assets", ""), "Empty" + "-ui.loc"));
                XmlSerializer xml = new XmlSerializer(typeof(UILanguageLocalizations));
                UILanguageLocalizations missing = new UILanguageLocalizations() {
                    language = "Empty",
                    localizationData = _requiredLocalizationData.ToArray()
                };
                xml.Serialize(file, missing);
            }
            Instance = null;
        }

        public void LoadLocalization(string file) {
            XmlSerializer xml = new XmlSerializer(typeof(UILanguageLocalizations));
            UILanguageLocalizations uiLoc = xml.Deserialize(new StringReader(File.ReadAllText(file))) as UILanguageLocalizations;
            //idToTranslation.Clear();
            foreach (TranslationData data in uiLoc.localizationData) {
                _idToTranslation[data.id] = data;
            }
        }

        [Serializable]
        public class UILanguageLocalizations {
            [XmlAttribute] public string language;
            [XmlArray("localizationData")] [XmlArrayItem("translationData")] public TranslationData[] localizationData;
        }
        [XmlRoot]
        public class Hints {
            [XmlArray(ElementName = "hint" )] public string[] hints;
        }
        internal string[] GetLabels(Type EnumType) {
            if (EnumType.IsEnum == false)
                return null;
            List<string> labels = new List<string>();
            foreach (Enum go in Enum.GetValues(EnumType)) {
                string name = EnumType.Name + "/" + go.ToString();
                if (_idToTranslation.ContainsKey(name)) {
                    labels.Add(_idToTranslation[name].translation);
                }
                else {
                    labels.Add("Missing Translation " + go.ToString());
                }
            }
            return labels.ToArray();
        }

        internal string[] GetStrings(string[] languageValues) {
            List<string> labels = new List<string>();
            foreach (string go in languageValues) {
                if (_idToTranslation.ContainsKey(go)) {
                    labels.Add(_idToTranslation[go].translation);
                }
                else {
                    labels.Add("Missing Translation " + go.ToString());
                }
            }
            return labels.ToArray();
        }
        internal string GetStaticVariables(StaticLanguageVariables paras) {
            string name = nameof(StaticLanguageVariables) + "/" + paras.ToString();
            if (_idToTranslation.ContainsKey(name)) {
                return (_idToTranslation[name].translation);
            }
            else {
                return ("Missing Translation " + paras);
            }
        }

        internal string[] GetStaticVariables(params StaticLanguageVariables[] paras) {
            return paras.Select(GetStaticVariables).ToArray();
        }
        /// <summary>
        /// Message should be either an enum or string.
        /// If it is a class it should have toString with a correct translation name associated.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal string GetTranslation(object message) {
            string name;
            if (message is Enum) {
                name = message.GetType().Name + "/" + message.ToString();
            } else {
                name = message.ToString();
            }
            if (_idToTranslation.ContainsKey(name)) {
                return (_idToTranslation[name].translation);
            }
            else {
                return ("Missing Translation " + message);
            }
        }
    }
}