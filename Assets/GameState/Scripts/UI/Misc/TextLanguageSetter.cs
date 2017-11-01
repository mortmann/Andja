using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
public class TextLanguageSetter : MonoBehaviour {
	// Use this for initialization
	Text myText;
	void Start () {
		myText = GetComponent<Text> ();
		if (myText == null)
			myText = GetComponentInChildren<Text> ();
		if(myText==null){
			Debug.LogError("TextLanguageSetter has no text object! " + name);
		}
		myText.text = UILanguageController.Instance.getText (name);
		UILanguageController.Instance.RegisterLanguageChange (OnChangeLanguage);
		if(GetComponent<EventTrigger> ()==null){
			this.gameObject.AddComponent<EventTrigger> ();
		}
		if(UILanguageController.Instance.hasHoverOverText (name)==false){
			return;
		}
		EventTrigger trigger = GetComponent<EventTrigger> ();
		EventTrigger.Entry enter = new EventTrigger.Entry( );
		enter.eventID = EventTriggerType.PointerEnter;
		enter.callback.AddListener( ( data) => {
			OnMouseEnter ();
		} );
		trigger.triggers.Add( enter );
		EventTrigger.Entry exit = new EventTrigger.Entry( );
		exit.eventID = EventTriggerType.PointerExit;
		exit.callback.AddListener( ( data) => {
			OnMouseExit ();
		} );
		trigger.triggers.Add( exit );
	}
	void OnChangeLanguage () {
		myText.text = UILanguageController.Instance.getText (name);
	}
	void OnDestroy(){
		UILanguageController.Instance.UnregisterLanguageChange (OnChangeLanguage);
	}
	void OnDisable(){
		UILanguageController.Instance.UnregisterLanguageChange (OnChangeLanguage);
	}
	public void OnMouseEnter(){
		GameObject.FindObjectOfType<HoverOverScript> ().Show (UILanguageController.Instance.getHoverOverText (name));
	}
	public void OnMouseExit(){
		GameObject.FindObjectOfType<HoverOverScript> ().Unshow ();
	}
}
