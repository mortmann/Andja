using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Xml;
using System.Xml.Serialization;

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
    public TranslationData(string id, bool OnlyHoverOver, int ValueCount) {
        this.id = id;
        this.valueCount = ValueCount;
        this.onlyHoverOver = OnlyHoverOver;
    }
    public TranslationData() {
    }

    internal void AddUIElement(string v) {
        UIElements.Add(v);
    }
}
public class TextLanguageSetter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler { 
    public string Identifier;

    // Use this for initialization
    public bool OnlyHoverOver;
    public int Values;

    public Text nameText;
    public Text valueText;


    public string[] languageValues; // Names To Values
    //Values from a enum
    internal Type valueEnumType; //Enum are the Values
    //Selection of some common Words
    private StaticLanguageVariables[] staticLanguageVariables;
    private TranslationData translationData;

    public string GetRealName() {
        string realname = "";
        Transform current = transform;
        while (current != null) {
            realname = current.name + "/" + realname;
            current = current.parent;
        }
        return realname;
    }

    void Start() {
        //realname = GetRealName();
        translationData = UILanguageController.Instance?.GetTranslationData(Identifier);
        if (translationData == null) {
            Debug.LogError("Missing Translations Data for " + Identifier);
            return;
        }
        if (OnlyHoverOver == false) {
            if (nameText == null)
                nameText = GetComponent<Text>();
            if (nameText == null)
                nameText = GetComponentInChildren<Text>(true);
            if (nameText == null) {
                Debug.LogError("TextLanguageSetter has no text object! " + name);
                return;
            }
            
            if (string.IsNullOrEmpty(translationData.translation) ==false)
                nameText.text = translationData.translation;
            UILanguageController.Instance.RegisterLanguageChange(OnChangeLanguage);
            if (GetComponent<EventTrigger>() == null) {
                this.gameObject.AddComponent<EventTrigger>();
            }
        }
    }
    void OnChangeLanguage() { 
        translationData = UILanguageController.Instance.GetTranslationData(Identifier);
        if (OnlyHoverOver)
            return;
        if (translationData == null) {
            Debug.LogError("Missing Translations Data for " + Identifier);
            return;
        }
        if (string.IsNullOrEmpty(translationData.translation) == false)
            nameText.text = translationData.translation;
    }
    internal void SetStaticLanguageVariables(params StaticLanguageVariables[] vals) {
        staticLanguageVariables = vals;
    }
    void OnDestroy() {
        if (UILanguageController.Instance == null)
            return;
        UILanguageController.Instance.UnregisterLanguageChange(OnChangeLanguage);
    }
    void OnDisable() {
        if (UILanguageController.Instance == null)
            return;
        //UILanguageController.Instance.UnregisterLanguageChange(OnChangeLanguage);
    }
    public void ShowValue(int i) {
        if (languageValues == null && staticLanguageVariables == null && valueEnumType == null)
            return;
        if(i<0) {
            Debug.LogError("Negative Label Value trying to be set. -" + Identifier);
            return;
        }
        if(valueText == null) {
            Debug.LogError("Label Text is null for " + Identifier);
            return;
        }
        if(translationData == null)
            translationData = UILanguageController.Instance.GetTranslationData(name);
        if (translationData.values == null) {
            if (valueEnumType != null) {
                translationData.values = UILanguageController.Instance.GetLabels(valueEnumType);
            } else 
            if(staticLanguageVariables != null) {
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
        valueText.text = translationData.values[i];
    }

    public TranslationData GetData() {
        return new TranslationData(Identifier, OnlyHoverOver, Values);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (translationData.hoverOverTranslation != null)
            GameObject.FindObjectOfType<HoverOverScript>().Show(translationData.hoverOverTranslation);
    }

    public void OnPointerExit(PointerEventData eventData) {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
}
