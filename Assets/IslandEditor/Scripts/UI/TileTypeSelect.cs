using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TileTypeSelect : MonoBehaviour {
	public GameObject prefabListItem;
	public GameObject content;
	TypeGraphicsSelect tgs;


	// Use this for initialization
	void Start () {
		tgs = GameObject.FindObjectOfType<TypeGraphicsSelect> ();
		foreach (TileType item in Enum.GetValues(typeof(TileType))) {
			GameObject g = GameObject.Instantiate (prefabListItem);
			g.transform.SetParent (content.transform);
			g.GetComponentInChildren<Text >().text = item.ToString ();
			TileType temp = item;
			EventTrigger eventTrigger = g.GetComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Select;
			entry.callback = new EventTrigger.TriggerEvent();
			entry.callback.AddListener((data)=>{OnSelect (temp);});
			eventTrigger.triggers.Add (entry);

		}
	}

	public void OnSelect(TileType item){
		EditorController.Instance.selectedTileType = item;
		tgs.ChangeType (item);
	}
	// Update is called once per frame
	void Update () {
	
	}

}
