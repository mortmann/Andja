using Andja.Controller;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
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
        public string hoverOverTranslation;

        [XmlArray("Values")]
        public string[] values;

        [XmlArray("uiElements", IsNullable = true)]
        public List<string> UIElements = new List<string>();

        public bool onlyHoverOver;
        public int valueCount = 0;

        public bool ShouldSerializeUIElements() {
            return UIElements.Count > 0;
        }

        public bool ShouldSerializeonlyHoverOver() {
            return onlyHoverOver;
        }

        public bool ShouldSerializevalueCount() {
            return valueCount > 0;
        }

        public TranslationData(string id, string name, string hoverOver, string[] values) {
            this.id = id;
            this.translation = name;
            this.hoverOverTranslation = hoverOver;
            this.values = values;
        }

        public TranslationData(string id, bool OnlyHoverOver, int ValueCount) : this(id, OnlyHoverOver) {
            this.valueCount = ValueCount;
        }

        public TranslationData(string id, bool OnlyHoverOver) : this(id) {
            this.onlyHoverOver = OnlyHoverOver;
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

        public bool OnlyHoverOver;
        public int Values;

        public Text nameText;
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
                if (nameText == null) {
                    Debug.LogError("TextLanguageSetter has no text object! " + name);
                    return;
                }
                //if (string.IsNullOrEmpty(translationData.translation) == false)
                //    nameText.text = translationData.translation + nameSuffix;
            }
            OnChangeLanguage();
        }

        public override void OnChangeLanguage() {
            translationData = UILanguageController.Instance.GetTranslationData(Identifier);
            if(currentValue != -1)
                ShowValue(currentValue);
            if (OnlyHoverOver)
                return;
            if (translationData == null) {
                Debug.LogError("Missing Translations Data for " + Identifier);
                return;
            }
            if (string.IsNullOrEmpty(translationData.translation) == false)
                nameText.text = translationData.translation + nameSuffix;
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
            if (valueText == null) {
                Debug.LogError("Label Text is null for " + Identifier);
                return;
            }
            if (translationData == null)
                translationData = UILanguageController.Instance.GetTranslationData(name);
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
            valueText.text = translationData.values[i];
        }

        public void OnPointerEnter(PointerEventData eventData) {
            if (Input.GetMouseButtonDown(0)) {
                GameObject.FindObjectOfType<HoverOverScript>().Unshow();
                return;
            }
            if (translationData?.hoverOverTranslation != null)
                GameObject.FindObjectOfType<HoverOverScript>().Show(translationData.hoverOverTranslation);
        }

        public void OnPointerExit(PointerEventData eventData) {
            GameObject.FindObjectOfType<HoverOverScript>().Unshow();
        }

        public void OnPointerDown(PointerEventData eventData) {
            GameObject.FindObjectOfType<HoverOverScript>().Unshow();
        }

        internal void SetNameSuffix(string suffix) {
            nameSuffix = " " + suffix;
        }

        public override TranslationData[] GetTranslationDatas() {
            return new TranslationData[] { new TranslationData(Identifier, OnlyHoverOver, Values) };
        }
    }
}