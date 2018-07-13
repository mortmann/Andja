using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Newtonsoft.Json;

public class InputHandler {

	static Dictionary<string,KeyBind> nameToKeyBinds;
	static string fileName="keybinds.ini";
	//TODO add a between layer for mouse buttons -> so it can be switched

	// Use this for initialization
	public InputHandler () {
		nameToKeyBinds = new Dictionary<string, KeyBind> ();
		LoadInputSchema (Application.dataPath.Replace ("/Assets",""));
//		SetupKeyBinds ();

	}
	public static Dictionary<string,KeyBind> GetBinds(){
		return nameToKeyBinds;
	}

	private static void SetupKeyBinds(){
		if (nameToKeyBinds.ContainsKey ("BuildMenu")==false)
			ChangePrimaryNameToKey ("BuildMenu",KeyCode.B );
		if (nameToKeyBinds.ContainsKey ("TradeMenu")==false)
			ChangePrimaryNameToKey ("TradeMenu",KeyCode.M);
		if (nameToKeyBinds.ContainsKey ("Offworld")==false)
			ChangePrimaryNameToKey ("Offworld",KeyCode.O);
		if (nameToKeyBinds.ContainsKey ("TogglePause")==false)
			ChangePrimaryNameToKey ("TogglePause",KeyCode.Space);
		if (nameToKeyBinds.ContainsKey ("Rotate")==false)
			ChangePrimaryNameToKey ("Rotate",KeyCode.R); 
		if (nameToKeyBinds.ContainsKey ("Console")==false)
			ChangePrimaryNameToKey ("Console",KeyCode.F1); 
		if (nameToKeyBinds.ContainsKey ("Cancel")==false)
			ChangePrimaryNameToKey ("Cancel",KeyCode.Escape);
        if (nameToKeyBinds.ContainsKey("Screenshot") == false)
            ChangePrimaryNameToKey("Screenshot", KeyCode.F12);
    }	
	public static void ChangePrimaryNameToKey(string name, KeyCode key){
		if(nameToKeyBinds.ContainsKey (name)){
			nameToKeyBinds [name].SetPrimary (key);
			return;
		}
		nameToKeyBinds.Add (name,new KeyBind (key, KeyBind.notSetCode));

	}
	public static void ChangeSecondaryNameToKey(string name, KeyCode key){
		if(nameToKeyBinds.ContainsKey (name)){
			nameToKeyBinds [name].SetSecondary (key);
			return;
		}
		nameToKeyBinds.Add (name,new KeyBind (KeyBind.notSetCode , key));

	}
	public static bool GetButtonDown(string name){
		if (nameToKeyBinds.ContainsKey (name) == false) {	
			Debug.LogWarning ("No KeyBind for Name " + name);
			return false;
		}
		return nameToKeyBinds[name].GetButtonDown();
	}

	public static bool GetButton (string name){
		if (nameToKeyBinds.ContainsKey (name) == false) {	
			Debug.LogWarning ("No KeyBind for Name " + name);
			return false;
		}
		return nameToKeyBinds[name].GetButton();
	}


	public static void SaveInputSchema(){
		string path = Application.dataPath.Replace ("/Assets", "");
		if( Directory.Exists(path ) == false ) {
			// NOTE: This can throw an exception if we can't create the folder,
			// but why would this ever happen? We should, by definition, have the ability
			// to write to our persistent data folder unless something is REALLY broken
			// with the computer/device we're running on.
			Directory.CreateDirectory( path  );
		}
		string filePath = System.IO.Path.Combine(path,fileName) ;
		File.WriteAllText( filePath, JsonConvert.SerializeObject(nameToKeyBinds,
			new JsonSerializerSettings() {
				Formatting = Newtonsoft.Json.Formatting.Indented
			}));
	}
	public static void LoadInputSchema(string path){
		try {
			string filePath = System.IO.Path.Combine(path,fileName) ;
			nameToKeyBinds = new Dictionary<string, KeyBind> ();
			string lines = File.ReadAllText (filePath);
			nameToKeyBinds = JsonConvert.DeserializeObject<Dictionary<string, KeyBind>> (lines);
		} finally {
			SetupKeyBinds ();
			SaveInputSchema (); // create the file so it can be manipulated 
		}
	}

	public class KeyBind {
		public const KeyCode notSetCode = KeyCode.RightWindows;

		/// <summary>
		/// DO NOT SET DIRECTLY
		/// </summary>
		[JsonProperty]
		KeyCode primary = notSetCode;
		/// <summary>
		/// DO NOT SET DIRECTLY
		/// </summary>
		[JsonProperty]
		KeyCode secondary = notSetCode;

		public KeyBind(){
			
		}
		public KeyBind(KeyCode primary, KeyCode secondary){
			this.primary = primary;
			this.secondary = secondary;
		}
		public String GetPrimaryString(){
			if(primary == KeyCode.Exclaim){
				return "-";
			}
			return primary.ToString ();
		}
		public String GetSecondaryString(){
			if(secondary == KeyCode.Exclaim){
				return "-";
			}
			return secondary.ToString ();
		}
		public bool SetPrimary(KeyCode k){
			if(k == KeyCode.Exclaim){
				return false;
			}
			primary = k;
			return true;
		}
		public bool SetSecondary(KeyCode k){
			if(k == KeyCode.Exclaim){
				return false;
			}
			secondary = k;
			return true;
		}
		public bool GetButtonDown(){
			return Input.GetKeyDown (primary) && primary != notSetCode
				|| Input.GetKeyDown (secondary) && secondary != notSetCode ;
		}

		public bool GetButton(){
			return Input.GetKey (primary) && primary != notSetCode
				|| Input.GetKey (secondary) && secondary != notSetCode ;
			
		}
	}

}
