using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum Language {English,German}

public class UILanguageController : MonoBehaviour {
	public static UILanguageController Instance { get; protected set; }
	Action cbLanguageChange;
	public static Language selectedLanguage = Language.English;
	Dictionary<string,string> nameToText;
	Dictionary<string,string> nameToHover;

	// Use this for initialization
	void Start () {
		if (Instance != null) {
			Debug.LogError("There should never be two UILanguageController.");
		}
		Instance = this;
		nameToText = new Dictionary<string, string> ();
		nameToHover = new Dictionary<string, string> ();
		nameToText.Add ("Peasents", "1");
		nameToHover.Add ("Peasents", "Peasents");
		//Need a way to load it in
	}
	public string getText(string name){
		if(nameToText.ContainsKey(name)==false){
			return "Missing Translation - " + name;
		}
		return nameToText [name];
	}
	public string getHoverOverText(string name){
		if(nameToHover.ContainsKey(name)==false){
			return "Missing Translation - " + name;
		}
		return nameToHover [name];
	}
	public bool hasHoverOverText(string name){
		return nameToHover.ContainsKey (name);
	}
	public void ChangeLanguage(Language language){
		selectedLanguage = language;
		if (cbLanguageChange != null)
			cbLanguageChange ();
	}

	public void RegisterLanguageChange(Action callbackfunc) {
		cbLanguageChange += callbackfunc;
	}
	public void UnregisterLanguageChange(Action callbackfunc) {
		cbLanguageChange -= callbackfunc;
	}
}
