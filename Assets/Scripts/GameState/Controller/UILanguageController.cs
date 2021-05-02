using Andja.UI;
using Andja.UI.Menu;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Andja.Controller {

    public enum StaticLanguageVariables { On, Off, And, Or, Empty, BuildCost, Upkeep, Locked, CurrentDamage, MaximumDamage, Speed }
    /// <summary>
    /// Loads Translations (except prototypes sends it off to PrototypController) and changing language 
    /// </summary>
    public class UILanguageController : MonoBehaviour {
        public static UILanguageController Instance { get; protected set; }
        private Action cbLanguageChange;

        public static string selectedLanguage = "English";
        private Dictionary<string, TranslationData> idToTranslation;
        private List<TranslationData> requiredLocalizationData;
        public Dictionary<string, string> LocalizationsToFile;

        public static readonly string localizationFilePrefix = "";
        public static readonly string localizationFileType = "-ui.loc";
        public static readonly string localizationXMLDirectory = "Localizations";

        private void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two UILanguageController.");
            }
            Instance = this;
            idToTranslation = new Dictionary<string, TranslationData>();
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
                string name = typeof(GraphicsOptions).Name + "/" + go.ToString();
                if (idToTranslation.ContainsKey(name) == false) {
                    localizationDataDictionary.Add(name, new TranslationData(name, false, 0));
                }
            }
            foreach (FullScreenMode go in Enum.GetValues(typeof(FullScreenMode))) {
                string name = typeof(FullScreenMode).Name + "/" + go.ToString();
                if (idToTranslation.ContainsKey(name) == false) {
                    localizationDataDictionary.Add(name, new TranslationData(name, false, 0));
                }
            }
            foreach (StaticLanguageVariables go in Enum.GetValues(typeof(StaticLanguageVariables))) {
                string name = typeof(StaticLanguageVariables).Name + "/" + go.ToString();
                if (idToTranslation.ContainsKey(name) == false) {
                    localizationDataDictionary.Add(name, new TranslationData(name, false, 0));
                }
            }
            requiredLocalizationData = new List<TranslationData>(localizationDataDictionary.Values);
            requiredLocalizationData.OrderBy(x => x.id);
            //#endif //Unity_Editor
            LocalizationsToFile = new Dictionary<string, string>();
            string fullpath = Path.Combine(ConstantPathHolder.StreamingAssets, "XMLs", UILanguageController.localizationXMLDirectory);
            string[] allLocalizationsFiles = Directory.GetFiles(fullpath, UILanguageController.localizationFilePrefix
                                                                        + "*" + UILanguageController.localizationFileType);
            //Check the files if they are readable
            foreach (string file in allLocalizationsFiles) {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(System.IO.File.ReadAllText(file));
                LocalizationsToFile.Add(xmlDoc.DocumentElement.Attributes[0].InnerXml, file);
            }
            if (LocalizationsToFile.ContainsKey(selectedLanguage) == false) {
                selectedLanguage = new List<string>(LocalizationsToFile.Keys)[0]; //just for the edge case of someone deleting english
            }
            LoadLocalization(LocalizationsToFile[selectedLanguage]);
        }

        public TranslationData GetTranslationData(string name) {
            if (idToTranslation.ContainsKey(name) == false) {
                Debug.LogWarning("Translation missing for " + name);
                return null;
            }
            return idToTranslation[name];
        }

        public TranslationData GetTranslationData(StaticLanguageVariables val) {
            return GetTranslationData(typeof(StaticLanguageVariables).Name +"/"+ val.ToString());
        }

        public void AddTranslationData(TranslationData data) {
            requiredLocalizationData.Add(data);
        }

        public void ChangeLanguage(string language) {
            if (LocalizationsToFile.ContainsKey(language) == false) {
                Debug.LogWarning("Selected Language not available!");
                language = "English";
            }
            selectedLanguage = language;
            LoadLocalization(LocalizationsToFile[selectedLanguage]);
            PrototypController.Instance?.ReloadLanguage();
            cbLanguageChange?.Invoke();
        }

        public void RegisterLanguageChange(Action callbackfunc) {
            cbLanguageChange += callbackfunc;
        }

        public void UnregisterLanguageChange(Action callbackfunc) {
            cbLanguageChange -= callbackfunc;
        }

        private void OnDestroy() {
            if (Application.isEditor) {
                FileStream file = File.Create(Path.Combine(Application.dataPath.Replace("/Assets", ""), "Empty" + "-ui.loc"));
                XmlSerializer xml = new XmlSerializer(typeof(UILanguageLocalizations));
                UILanguageLocalizations missing = new UILanguageLocalizations() {
                    language = "Empty",
                    localizationData = requiredLocalizationData.ToArray()
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
                idToTranslation[data.id] = data;
            }
        }

        [Serializable]
        public class UILanguageLocalizations {
            [XmlAttribute] public string language;
            [XmlArray("localizationData")] [XmlArrayItem("translationData")] public TranslationData[] localizationData;
        }

        internal string[] GetLabels(Type EnumType) {
            if (EnumType.IsEnum == false)
                return null;
            List<string> labels = new List<string>();
            foreach (Enum go in Enum.GetValues(EnumType)) {
                string name = EnumType.Name + "/" + go.ToString();
                if (idToTranslation.ContainsKey(name)) {
                    labels.Add(idToTranslation[name].translation);
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
                if (idToTranslation.ContainsKey(go)) {
                    labels.Add(idToTranslation[go].translation);
                }
                else {
                    labels.Add("Missing Translation " + go.ToString());
                }
            }
            return labels.ToArray();
        }
        internal string GetStaticVariables(StaticLanguageVariables paras) {
            string name = typeof(StaticLanguageVariables).Name + "/" + paras.ToString();
            if (idToTranslation.ContainsKey(name)) {
                return (idToTranslation[name].translation);
            }
            else {
                return ("Missing Translation " + paras);
            }
        }

        internal string[] GetStaticVariables(params StaticLanguageVariables[] paras) {
            List<string> labels = new List<string>();
            foreach (StaticLanguageVariables p in paras) {
                labels.Add(GetStaticVariables(p));
            }
            return labels.ToArray();
        }
    }
}