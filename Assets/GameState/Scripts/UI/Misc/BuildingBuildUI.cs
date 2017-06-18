using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildingBuildUI : MonoBehaviour {

	public GameObject mouseOverPrefab;
	GameObject mouseOver;
	public Structure structure;

	// Use this for initialization
	public void Show (Structure str,bool hoverOver=true) {
		this.structure = str;
		GetComponentInChildren<Text> ().text = str.spriteName;
		EventTrigger trigger = GetComponent<EventTrigger> ();
		if (hoverOver) {
			EventTrigger.Entry enter = new EventTrigger.Entry ();
			enter.eventID = EventTriggerType.PointerEnter;
			enter.callback.AddListener (( data) => {
				OnMouseEnter ();
			});
			trigger.triggers.Add (enter);

			trigger.triggers.Add (enter);
			EventTrigger.Entry exit = new EventTrigger.Entry ();
			exit.eventID = EventTriggerType.PointerExit;
			exit.callback.AddListener (( data) => {
				OnMouseExit ();
			});
			trigger.triggers.Add (exit);
		}


		EventTrigger.Entry dragStart = new EventTrigger.Entry( );
		dragStart.eventID = EventTriggerType.BeginDrag;
		dragStart.callback.AddListener( ( data) => {
			OnDragStart ();
		} );
		trigger.triggers.Add( dragStart );


		EventTrigger.Entry dragStop = new EventTrigger.Entry( );
		dragStop.eventID = EventTriggerType.EndDrag;
		dragStop.callback.AddListener( ( data) => {
			OnDragEnd ();
		} );
		trigger.triggers.Add( dragStop );
	}
	public void OnMouseEnter(){
//		hoverover = true;
		GameObject.FindObjectOfType<HoverOverScript> ().Show (structure.spriteName);
	}
	public void OnMouseExit(){
		//TODO: reset hovertime better
		GameObject.FindObjectOfType<HoverOverScript> ().Unshow ();

	}
	public void OnDragStart(){
		UIController.Instance.SetDragAndDropBuild (this.gameObject);
	}
	public void OnDragEnd(){
		UIController.Instance.StopDragAndDropBuild ();
	}

}
