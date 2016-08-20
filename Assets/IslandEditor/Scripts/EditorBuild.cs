using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EditorBuild : MonoBehaviour {
	public GameObject prefabListItem;
	public GameObject content;
	public GameObject AgeStage;

	// Use this for initialization
	void Start () {
		bool first=true;
		foreach (int item in EditorStructureSpriteController.Instance.structurePrototypes.Keys) {
			GameObject g = GameObject.Instantiate (prefabListItem);
			g.transform.SetParent (content.transform);
			g.GetComponentInChildren<Text >().text = EditorStructureSpriteController.Instance.structurePrototypes[item].name;
			int temp = item;
			EventTrigger eventTrigger = g.GetComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Select;
			entry.callback = new EventTrigger.TriggerEvent();
			entry.callback.AddListener((data)=>{OnSelect (temp);});
			eventTrigger.triggers.Add (entry);
			if(first)
				OnSelect (temp);
		}

	}
		
	public void OnSelect(int id){
		EditorController.Instance.structureID = id;
		int ages = EditorStructureSpriteController.Instance.GetGrowableStages (id);
		foreach (Transform item in AgeStage.transform) {
			GameObject.Destroy (item.gameObject);
		}
		for (int i = 0; i <= ages; i++) {
			GameObject g = GameObject.Instantiate (prefabListItem);
			g.transform.SetParent (AgeStage.transform);
			g.GetComponentInChildren<Text >().text = i.ToString ();
			int temp = i;
			EventTrigger eventTrigger = g.GetComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Select;
			entry.callback = new EventTrigger.TriggerEvent();
			entry.callback.AddListener((data)=>{OnAgeSelect (temp);});
			eventTrigger.triggers.Add (entry);
		}
	}
	public void OnAgeSelect(int age){
		EditorController.Instance.structureStage = age;
		EditorStructureSpriteController.Instance.growableLevel = age;
	}
}
