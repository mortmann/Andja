using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class EventMessage : MonoBehaviour {
	
	string eventName;
	public Vector2 position;

	// Use this for initialization
	public void Setup (string name, Vector2 position) {
		this.position = position;
		this.name = name;
		if(name.Length>30){
			name = name.Substring (0,30) + "...";
		}
		GetComponentInChildren<Text> ().text = name;
		//TODO change Image here also
		//Probably load the sprites in EventUIManager and get it from there 
		EventTrigger trigger = GetComponent<EventTrigger> ();
        EventTrigger.Entry enter = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerEnter
        };
        enter.callback.AddListener( ( data) => {
			OnMouseEnter ();
		} );
		trigger.triggers.Add( enter );
        EventTrigger.Entry exit = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerExit
        };
        exit.callback.AddListener( ( data) => {
			OnMouseExit ();
		} );
		trigger.triggers.Add( exit );
	}	
	// Update is called once per frame
	void Update () {
	
	}

	public void OnPointerClick(){
		if(position.x < 0 || position.y < 0){
			return;
		}
		CameraController.Instance.MoveCameraToPosition (position);
	}
	public void OnMouseEnter(){
		GameObject.FindObjectOfType<HoverOverScript> ().Show (name);
	}
	public void OnMouseExit(){
		GameObject.FindObjectOfType<HoverOverScript> ().Unshow ();
	}
}
