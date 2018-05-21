using UnityEngine;
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
	void OnEnable() {
		typeToGameObjects = new Dictionary<string, List<GameObject>> ();
		LoadSprites ();
		foreach (string type in typeTotileSpriteNames.Keys) {
			List<GameObject> gos = new List<GameObject> ();
			int number = 0;
            if(typeTotileSpriteNames[type] == null) {
                Debug.Log(type);
                continue;
            }
			foreach (string sprite in typeTotileSpriteNames[type]) {
				//need to find all sprites for that type
				GameObject g = GameObject.Instantiate (prefabListItem);
				g.transform.SetParent (content.transform);

				g.GetComponentInChildren<Text > ().text = sprite;
				//set the trigger up
				EventTrigger eventTrigger = g.GetComponent<EventTrigger> ();
                EventTrigger.Entry entry = new EventTrigger.Entry {
                    eventID = EventTriggerType.Select,
                    callback = new EventTrigger.TriggerEvent()
                };
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

	}
	public void ChangeType(TileType item){
		foreach(Transform t in content.transform){
			t.gameObject.SetActive (false);
		}
		if(item == TileType.Ocean){
			return;
		}
		if(typeToGameObjects.ContainsKey (item.ToString ())==false){
			return;
		}
		foreach (GameObject go in typeToGameObjects[item.ToString ()]) {
			go.SetActive (true);
		}
		currentSelected = item;
		OnSelect (0);
	}
	public void OnSelect(int number){
		EditorController.Instance.spriteName = typeTotileSpriteNames [currentSelected.ToString ()] [number];
	}

	void LoadSprites() {
		foreach(TileType tt in Enum.GetValues(typeof(TileType))) {
            typeTotileSpriteNames.Add(tt.ToString(), TileSpriteController.GetSpriteNamesForType(tt, EditorController.climate ));
            if(typeTotileSpriteNames[tt.ToString()] != null)
              Debug.Log(tt +"  "+typeTotileSpriteNames[tt.ToString()].Count);
        }
	}
}
