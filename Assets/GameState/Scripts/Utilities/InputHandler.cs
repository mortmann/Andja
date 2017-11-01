using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
public class InputHandler {

	static Dictionary<string,KeyBind> nameToKeyBinds;
	static string fileName="keybinds.ini";


	// Use this for initialization
	public InputHandler () {
		nameToKeyBinds = new Dictionary<string, KeyBind> ();
		LoadInputSchema (Application.dataPath.Replace ("/Assets",""));
		SetupKeyBinds ();

	}
	public static Dictionary<string,KeyBind> GetBinds(){
		return nameToKeyBinds;
	}

	private void SetupKeyBinds(){
		if (nameToKeyBinds.ContainsKey ("BuildMenu")==false)
			nameToKeyBinds.Add ("BuildMenu",new KeyBind("BuildMenu", KeyCode.B, KeyBind.notSetCode) );
		if (nameToKeyBinds.ContainsKey ("TradeMenu")==false)
			nameToKeyBinds.Add ("TradeMenu",new KeyBind("TradeMenu", KeyCode.M, KeyBind.notSetCode));
		if (nameToKeyBinds.ContainsKey ("Offworld")==false)
			nameToKeyBinds.Add ("Offworld",new KeyBind("Offworld", KeyCode.O, KeyBind.notSetCode));
		if (nameToKeyBinds.ContainsKey ("TogglePause")==false)
			nameToKeyBinds.Add ("TogglePause",new KeyBind("TogglePause", KeyCode.Space, KeyBind.notSetCode));
		if (nameToKeyBinds.ContainsKey ("Rotate")==false)
			nameToKeyBinds.Add ("Rotate",new KeyBind("Rotate", KeyCode.R, KeyBind.notSetCode)); 
		if (nameToKeyBinds.ContainsKey ("Console")==false)
			nameToKeyBinds.Add ("Console",new KeyBind("Console", KeyCode.F1, KeyBind.notSetCode)); 

	}	
	public static void ChangePrimaryNameToKey(string name, KeyCode key){
		if(nameToKeyBinds.ContainsKey (name)){
			nameToKeyBinds [name].SetPrimary (key);
			return;
		}
		nameToKeyBinds.Add (name,new KeyBind (name, key, KeyBind.notSetCode));

	}
	public static void ChangeSecondaryNameToKey(string name, KeyCode key){
		if(nameToKeyBinds.ContainsKey (name)){
			nameToKeyBinds [name].SetSecondary (key);
			return;
		}
		nameToKeyBinds.Add (name,new KeyBind (name, KeyBind.notSetCode , key));

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
//		StringWriter writer = new StringWriter ();
//		List<string> keys = new List<string> (primaryNameToKey.Keys);
//		keys.AddRange (secondaryNameToKey.Keys);
//		foreach (string key in keys.Distinct()) {
//			if(primaryNameToKey.ContainsKey (key)){
//				writer.Write (key);
//				writer.WriteLine (":"+primaryNameToKey[key]);
//			}
//			if(secondaryNameToKey.ContainsKey (key)){
//				writer.Write (key);
//				writer.WriteLine (":"+secondaryNameToKey[key]);
//			}
//		}	
		KeyBind[] binds = new KeyBind[nameToKeyBinds.Count];
		nameToKeyBinds.Values.CopyTo (binds, 0);
		File.WriteAllText( filePath, JsonUtil.arrayToJson<KeyBind>(binds));
	}
	public static void LoadInputSchema(string path){
		string filePath = System.IO.Path.Combine(path,fileName) ;
		if(File.Exists (filePath)==false){
			return;
		}
		nameToKeyBinds = new Dictionary<string, KeyBind> ();
		string lines = File.ReadAllText (filePath);
		KeyBind[] binds = JsonUtil.getJsonArray<KeyBind> (lines);
		foreach (KeyBind item in binds) {
			nameToKeyBinds.Add (item.name, item);
		}

	}
	[Serializable]  
	public class KeyBind {
		public const KeyCode notSetCode = KeyCode.Exclaim;

		public string name;
		/// <summary>
		/// DO NOT SET DIRECTLY
		/// </summary>
		[SerializeField]
		KeyCode primary = KeyCode.Exclaim;
		/// <summary>
		/// DO NOT SET DIRECTLY
		/// </summary>
		[SerializeField]
		KeyCode secondary = KeyCode.Exclaim;

		public KeyBind(){
			
		}
		public KeyBind(string name, KeyCode primary, KeyCode secondary){
			this.name = name;
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
			if(primary != notSetCode){
				return Input.GetKeyDown (primary);
			}
			if(secondary != notSetCode){
				return Input.GetKeyDown (secondary);
			}
			return false;
		}

		public bool GetButton(){
			if(primary != notSetCode){
				return Input.GetKey (primary);
			}
			if(secondary != notSetCode){
				return Input.GetKey (secondary);
			}
			return false;
		}
	}

}
