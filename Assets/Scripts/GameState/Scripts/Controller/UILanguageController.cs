using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using UnityEditor; 
public enum Language { English, German }

public class UILanguageController : MonoBehaviour {
    public static UILanguageController Instance { get; protected set; }
    Action cbLanguageChange;
    public static Language selectedLanguage = Language.English;
    Dictionary<string, string> nameToText;
    Dictionary<string, string> nameToHover;

    List<string> missingLocalizationData;

    // Use this for initialization
    void Awake() {
        if (Instance != null) {
            Debug.LogError("There should never be two UILanguageController.");
        }
        Instance = this;
        missingLocalizationData = new List<string>();
        nameToText = new Dictionary<string, string>();
        nameToHover = new Dictionary<string, string>();
        TextLanguageSetter[] texts = Resources.FindObjectsOfTypeAll<TextLanguageSetter>();
        foreach(TextLanguageSetter t in texts){
            if(t.OnlyHoverOver==false)
                missingLocalizationData.Add(t.GetRealName() + "text");
            missingLocalizationData.Add(t.GetRealName() + "hover");
        }
        LoadLocalization();
    }
    public string GetText(string name) {
        if (nameToText.ContainsKey(name) == false) {
            missingLocalizationData.Add(name + "text");
            return null;
        }
        return nameToText[name];
    }
    public string GetHoverOverText(string name) {
        if (nameToHover.ContainsKey(name) == false) {
            missingLocalizationData.Add(name + "hover");
            return null;
        }
        return nameToHover[name];
    }
    public bool HasHoverOverText(string name) {
        return nameToHover.ContainsKey(name);
    }
    public void ChangeLanguage(Language language) {
        selectedLanguage = language;
        cbLanguageChange?.Invoke();
    }

    public void RegisterLanguageChange(Action callbackfunc) {
        cbLanguageChange += callbackfunc;
    }
    public void UnregisterLanguageChange(Action callbackfunc) {
        cbLanguageChange -= callbackfunc;
    }
    void OnDestroy() {
        FileStream file = File.Create(Path.Combine(Application.dataPath.Replace("/Assets", ""), "Missing-UI-Localization-"+selectedLanguage));
        XmlSerializer xml = new XmlSerializer(typeof(UILanguageLocalizations));
        UILanguageLocalizations missing = new UILanguageLocalizations() {
            missingLocalization = missingLocalizationData.ToArray()
        };
        xml.Serialize(file,missing);
        Instance = null;
    }
    public void LoadLocalization() {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        TextAsset ta = ((TextAsset)Resources.Load("XMLs/localization-"+selectedLanguage, typeof(TextAsset)));
        xmlDoc.LoadXml(ta.text); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("UI/element")) {
            string path = node.GetAttribute("name") +"/";
            if (node.Name == "textelements") {
                XmlNode text = node.SelectSingleNode("text");
                if (text != null)
                    nameToText.Add(path, text.InnerXml);
                XmlNode hoverOver = node.SelectSingleNode("hoverOver");
                if (text != null)
                    nameToText.Add(path, hoverOver.InnerXml);
            }
            ForChilds(path,node.ChildNodes);
            //do {
            //    path = node.GetAttribute("name");
            //    
               

            //} while (currentNode != node);
        }
        //foreach(String s in nameToText.Keys) {
        //    Debug.Log(s);
        //}
    }
    public void ForChilds(string path, XmlNodeList list) {
        foreach(XmlNode node in list) {
            string tempPath = path;
            if (node is XmlElement)
                tempPath += ((XmlElement)node).GetAttribute("name") + "/";
            
            if (node.Name == "textelement") {
                XmlNode text = node.SelectSingleNode("text");
                if (text != null)
                    nameToText.Add(tempPath, text.InnerXml);
                XmlNode hoverOver = node.SelectSingleNode("hoverOver");
                if (text != null)
                    nameToHover.Add(tempPath, hoverOver.InnerXml);
            }
            ForChilds(tempPath,node.ChildNodes);
        }
    }

    [Serializable]
    public class UILanguageLocalizations {
        [XmlArray] public string[] missingLocalization;
    }

}
