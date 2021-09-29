using Andja.Controller;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    [Serializable]
    [XmlRoot("translationData")]
    public class TranslationData {

        [XmlAttribute]
        public string id;

        public string translation = "[**Missing**]";
        public string toolTipTranslation;
        public bool valueIsMainTranslation;
        [XmlArray]
        public string[] values;

        [XmlArray("uiElements", IsNullable = true)]
        public List<string> UIElements = new List<string>();

        public bool onlyToolTip;
        public int valueCount = 0;

        public bool ShouldSerializeUIElements() {
            return UIElements.Count > 0;
        }

        public bool ShouldSerializeonlyToolTip() {
            return onlyToolTip;
        }

        public bool ShouldSerializevalueCount() {
            return valueCount > 0;
        }

        public TranslationData(string id, string name, string toolTip,  string[] values) {
            this.id = id;
            this.translation = name;
            this.toolTipTranslation = toolTip;
            this.values = values;
        }

        public TranslationData(string id, bool onlyToolTip, bool valueIsMainTranslation, int ValueCount) 
                                    : this(id, onlyToolTip, ValueCount) {
            this.valueIsMainTranslation = valueIsMainTranslation;
        }
        public TranslationData(string id, bool onlyToolTip, int ValueCount) : this(id, onlyToolTip) {
            this.valueCount = ValueCount;
        }
        public TranslationData(string id, bool onlyToolTip) : this(id) {
            this.onlyToolTip = onlyToolTip;
            if(this.onlyToolTip)
                toolTipTranslation = "[**Missing**]";
        }

        public TranslationData(string id) {
            this.id = id;
        }

        public TranslationData() {
        }

        internal void AddUIElement(string v) {
            UIElements.Add(v);
        }
    }

    public class TextLanguageSetter : TranslationBase, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler {
        public string Identifier;
        public TranslationData translationData;
        public bool ValueIsMainTranslation;
        public bool OnlyHoverOver; //should be now OnlyToolTip but renaming removes inspector data 
        public int Values;
        //Is the main translated text
        public Text nameText; //TODO: when moving to tmp only rename to a better name
        public TMP_Text tmp_nameText;

        public Text valueText;
        private string nameSuffix;

        public string[] languageValues; // Names To Values

                                        //Values from a enum
        internal Type valueEnumType; //Enum are the Values

                                     //Selection of some common Words
        private StaticLanguageVariables[] staticLanguageVariables;
        private int currentValue = -1;
        public override void OnStart() {
            if (OnlyHoverOver == false) {
                if (nameText == null)
                    nameText = GetComponent<Text>();
                if (nameText == null)
                    nameText = GetComponentInChildren<Text>(true);
                if (nameText == null)
                    tmp_nameText = GetComponentInChildren<TMP_Text>(true);
                if (nameText == null && tmp_nameText == null) {
                    Debug.LogError("TextLanguageSetter has no text object! " + name);
                    return;
                }
            }
            OnChangeLanguage();
        }

        public override void OnChangeLanguage() {
            translationData = UILanguageController.Instance.GetTranslationData(Identifier);
            if (currentValue != -1)
                ShowValue(currentValue);
            if (OnlyHoverOver)
                return;
            if (translationData == null) {
                Debug.LogWarning("Missing Translations Data for " + Identifier);
                return;
            }
            ShowTranslation();
        }

        private void ShowTranslation() {
            if (string.IsNullOrEmpty(translationData.translation) == false) {
                if (nameText != null) {
                    nameText.text = translationData.translation + nameSuffix;
                }
                else {
                    tmp_nameText.text = translationData.translation + nameSuffix;
                }
            }
        }

        internal void SetStaticLanguageVariables(params StaticLanguageVariables[] vals) {
            staticLanguageVariables = vals;
        }

        private void OnDestroy() {
            if (UILanguageController.Instance == null)
                return;
            UILanguageController.Instance.UnregisterLanguageChange(OnChangeLanguage);
        }

        private void OnDisable() {
            if (UILanguageController.Instance == null)
                return;
            //UILanguageController.Instance.UnregisterLanguageChange(OnChangeLanguage);
        }

        public void ShowNumber(int i) {
            valueText.text = i + "";
        }

        /// <summary>
        /// i = the number
        /// cutLowOff: i must be lower than
        /// cutHighOff: i must be equal or bigger than (does not work with negativ numbers)
        /// </summary>
        /// <param name="i"></param>
        /// <param name="cutLowOff"></param>
        /// <param name="cutHighOff"></param>
        public void ShowNumberWithCutoff(int i, int cutLowOff, int cutHighOff = -1) {
            if (cutHighOff > 0 && i >= cutHighOff) {
                ShowValue(1);
                return;
            }
            if (i >= cutLowOff) {
                valueText.text = i + "";
                return;
            }
            ShowValue(0);
        }

        public void ShowValue(int i) {
            if (languageValues == null && staticLanguageVariables == null && valueEnumType == null) {
                valueText.text = i + "";
                return;
            }
            if (i < 0) {
                Debug.LogError("Negative Label Value trying to be set. -" + Identifier);
                return;
            }
            if (ValueIsMainTranslation == false && valueText == null) {
                Debug.LogError("Label Text is null for " + Identifier);
                return;
            }
            if (translationData == null)
                translationData = UILanguageController.Instance.GetTranslationData(name);
            if (translationData == null)
                return;
            if (translationData.values == null || translationData.values.Length == 0) {
                if (valueEnumType != null) {
                    translationData.values = UILanguageController.Instance.GetLabels(valueEnumType);
                }
                else
                if (staticLanguageVariables != null) {
                    translationData.values = UILanguageController.Instance.GetStaticVariables(staticLanguageVariables);
                }
                else {
                    translationData.values = UILanguageController.Instance.GetStrings(languageValues);
                }
            }
            if (i >= translationData.values.Length) {
                Debug.LogWarning("Missing Value for " + Identifier);
                return;
            }
            currentValue = i;
            if(ValueIsMainTranslation) {
                translationData.translation = translationData.values[i];
                ShowTranslation();
            }
            else {
                valueText.text = translationData.values[i];
            }
        }
        public void OnPointerEnter(PointerEventData eventData) {
            if (translationData?.toolTipTranslation != null)
                GameObject.FindObjectOfType<ToolTip>().Show(translationData.toolTipTranslation);
        }

        public void OnPointerExit(PointerEventData eventData) {
            GameObject.FindObjectOfType<ToolTip>().Unshow();
        }

        public void OnPointerDown(PointerEventData eventData) {
            GameObject.FindObjectOfType<ToolTip>().Unshow();
        }

        internal void SetNameSuffix(string suffix) {
            nameSuffix = " " + suffix;
        }

        public override TranslationData[] GetTranslationDatas() {
            return new TranslationData[] { new TranslationData(Identifier, OnlyHoverOver, ValueIsMainTranslation, Values) };
        }

        internal void SetColor(Color c) {
            if(nameText != null) {
                nameText.color = c;
            }
            if (tmp_nameText != null) {
                tmp_nameText.color = c;
            }
        }
    }
}