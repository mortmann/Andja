using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class InputHandler {

	static Dictionary<string,KeyCode> primaryNameToKey;
	static Dictionary<string,KeyCode> secondaryNameToKey;
	static string fileName="keybinds.ini";


	// Use this for initialization
	public InputHandler () {
		primaryNameToKey = new Dictionary<string, KeyCode> ();
		secondaryNameToKey = new Dictionary<string, KeyCode> ();
//		SaveInputSchema (Application.dataPath.Replace ("/Assets",""));
		LoadInputSchema (Application.dataPath.Replace ("/Assets",""));
		SetupKeyBinds ();

	}
	public static Dictionary<string,KeyCode> GetPrimaryBinds(){
		return primaryNameToKey;
	}
	public static Dictionary<string,KeyCode> GetSecondaryBinds(){
		return secondaryNameToKey;
	}
	private void SetupKeyBinds(){
		if(primaryNameToKey.Count>0){
			return;
		}
		primaryNameToKey.Add ("BuildMenu",KeyCode.B);
		primaryNameToKey.Add ("TradeMenu",KeyCode.M);
		primaryNameToKey.Add ("Offworld",KeyCode.O);
		primaryNameToKey.Add ("TogglePause",KeyCode.Space);
		primaryNameToKey.Add ("Rotate",KeyCode.R); 
	}	
	public static void ChangePrimaryNameToKey(string name, KeyCode key){
		if(primaryNameToKey.ContainsKey (name)){
			primaryNameToKey [name] = key;
			return;
		}
		primaryNameToKey.Add (name,key);
	}
	public static void ChangeSecondaryNameToKey(string name, KeyCode key){
		if(secondaryNameToKey.ContainsKey (name)){
			secondaryNameToKey [name] = key;
			return;
		}
		secondaryNameToKey.Add (name,key);
	}
	public static bool GetButtonDown(string name){
		if(primaryNameToKey.ContainsKey (name)==false){
			if(secondaryNameToKey.ContainsKey (name)==false){
				Debug.LogWarning ("No Key found with name " + name);
				return false;
			}
			return Input.GetKeyDown (secondaryNameToKey[name]);
		}
		return Input.GetKeyDown (primaryNameToKey[name]);
	}

	public static bool GetButton (string name){
		if(primaryNameToKey.ContainsKey (name)==false){
			if(secondaryNameToKey.ContainsKey (name)==false){
				Debug.LogWarning ("No Key found with name " + name);
				return false;
			}
			return Input.GetKey (secondaryNameToKey[name]);
		}
		return Input.GetKey (primaryNameToKey[name]);
	}


	public static void SaveInputSchema(string path){
		if( Directory.Exists(path ) == false ) {
			// NOTE: This can throw an exception if we can't create the folder,
			// but why would this ever happen? We should, by definition, have the ability
			// to write to our persistent data folder unless something is REALLY broken
			// with the computer/device we're running on.
			Directory.CreateDirectory( path  );
		}
		string filePath = System.IO.Path.Combine(path,fileName) ;
		StringWriter writer = new StringWriter ();
		List<string> keys = new List<string> (primaryNameToKey.Keys);
		keys.AddRange (secondaryNameToKey.Keys);
		foreach (string key in keys.Distinct()) {
			if(primaryNameToKey.ContainsKey (key)){
				writer.Write (key);
				writer.WriteLine (":"+primaryNameToKey[key]);
			}
			if(secondaryNameToKey.ContainsKey (key)){
				writer.Write (key);
				writer.WriteLine (":"+secondaryNameToKey[key]);
			}
		}	
		File.WriteAllText( filePath, writer.ToString() );
	}
	public static void LoadInputSchema(string path){
		string filePath = System.IO.Path.Combine(path,fileName) ;
		if(File.Exists (filePath)==false){
			return;
		}
		string[] lines = File.ReadAllLines (filePath);
		foreach(string line in lines){
			string[] split = line.Split (':');
			if(split.Length!=2){
				continue;
			}
			split[0]=split[0].Trim();
			split[1]=split[1].Trim();
			if(primaryNameToKey.ContainsKey (split[0])){
				if(secondaryNameToKey.ContainsKey (split[0])){
					continue;
				}
				secondaryNameToKey.Add (split[0],(KeyCode)System.Enum.Parse (typeof(KeyCode), split[1],true));
				continue;
			}
			primaryNameToKey.Add (split[0],(KeyCode)System.Enum.Parse (typeof(KeyCode), split[1],true)); 
		}

	}

}
