﻿using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TypeGraphicsSelect : MonoBehaviour {

	public GameObject prefabListItem;
	public GameObject content;
	public Dictionary<string, List<string>> typeTotileSpriteNames = new Dictionary<string,List<string>>();
	public Dictionary<string,List<GameObject>> typeToGameObjects;
	TileType currentSelected;
	// Use this for initialization
	void Start () {
		typeToGameObjects = new Dictionary<string, List<GameObject>> ();
		LoadSprites ();
		foreach (string type in typeTotileSpriteNames.Keys) {
			List<GameObject> gos = new List<GameObject> ();
			int number = 0;
			foreach (string sprite in typeTotileSpriteNames[type]) {
				//need to find all sprites for that type
				GameObject g = GameObject.Instantiate (prefabListItem);
				g.transform.SetParent (content.transform);

				g.GetComponentInChildren<Text > ().text = sprite;
				//set the trigger up
				EventTrigger eventTrigger = g.GetComponent<EventTrigger> ();
				EventTrigger.Entry entry = new EventTrigger.Entry ();
				entry.eventID = EventTriggerType.Select;
				entry.callback = new EventTrigger.TriggerEvent ();
				int temp = number;
				number++;
				entry.callback.AddListener ((data) => {
					OnSelect (temp);
				});
				eventTrigger.triggers.Add (entry);
				g.SetActive (false);
				gos.Add (g); 
			}
//			Debug.Log ("typeToGameObjects |" + type + "|"); 
			typeToGameObjects.Add (type, gos); 
		}
		ChangeType (TileType.Dirt);
	}
	public void ChangeType(TileType item){
		foreach(Transform t in content.transform){
			t.gameObject.SetActive (false);
		}
		if(item == TileType.Ocean){
			return;
		}
		if(typeToGameObjects.ContainsKey (item.ToString ().ToLower ())==false){
			return;
		}
		foreach (GameObject go in typeToGameObjects[item.ToString ().ToLower ()]) {
			go.SetActive (true);
		}
		currentSelected = item;
		OnSelect (0);
	}
	public void OnSelect(int number){
		EditorController.Instance.spriteName = typeTotileSpriteNames [currentSelected.ToString ().ToLower ()] [number];
	}

	void LoadSprites() {

		typeTotileSpriteNames = new Dictionary<string, List<string>>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/TileSprites/");
		foreach (Sprite s in sprites) {
			string type = s.name.Split ('_') [0].ToLower ();
//			Debug.Log (type + " / " + s.name); 
			if(typeTotileSpriteNames.ContainsKey (type)){
				typeTotileSpriteNames [type].Add (s.name);
			} else {
				List<string> sts = new List<string> ();
				sts.Add (s.name); 
				typeTotileSpriteNames.Add (type,sts);
			}
		}
	}
}