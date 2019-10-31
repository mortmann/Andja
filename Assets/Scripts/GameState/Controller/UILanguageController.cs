﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using UnityEditor; 

public class UILanguageController : MonoBehaviour {
    public static UILanguageController Instance { get; protected set; }
    Action cbLanguageChange;
    public static string selectedLanguage = "English";
    Dictionary<string, string> nameToText;
    Dictionary<string, string> nameToHover;

    List<string> missingLocalizationData;
    public Dictionary<string, string> LocalizationsToFile;

    public static readonly string localizationFilePrefix = "localization-";
    public static readonly string localizationFileType = ".xml";
    public static readonly string localizationXMLDirectory = "UILocalizations";

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
        LocalizationsToFile = new Dictionary<string, string>();

        string fullpath = Path.Combine(ConstantPathHolder.StreamingAssets, "XMLs", UILanguageController.localizationXMLDirectory);
        string[] allLocalizationsFiles = Directory.GetFiles(fullpath, UILanguageController.localizationFilePrefix
                                                                    + "*" + UILanguageController.localizationFileType);
        //Check the files if they are readable
        foreach (string file in allLocalizationsFiles) {
            XmlDocument xmlDoc = new XmlDocument(); 
            xmlDoc.LoadXml(System.IO.File.ReadAllText(file));
            LocalizationsToFile.Add(xmlDoc.DocumentElement.Attributes[0].InnerXml,file);
        }
        if (LocalizationsToFile.ContainsKey(selectedLanguage) == false) {
            selectedLanguage = new List<string>(LocalizationsToFile.Keys)[0]; //just for the edge case of someone deleting english
        }
        LoadLocalization(LocalizationsToFile[selectedLanguage]);
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
    public void ChangeLanguage(string language) {
        if (LocalizationsToFile.ContainsKey(language) == false) {
            Debug.LogWarning("Old selected Language not available!");
            return;
        }
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
    public void LoadLocalization(string file) {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        //string filename = localizationFilePrefix + selectedLanguage + localizationFileType;
        //string filepath = System.IO.Path.Combine(ConstantPathHolder.StreamingAssets, "XMLs", localizationXMLDirectory, filename);
        //TextAsset ta = ((TextAsset)Resources.Load("XMLs/UILocalizations/localization-"+selectedLanguage, typeof(TextAsset)));
        xmlDoc.LoadXml(System.IO.File.ReadAllText(file)); // load the file.
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