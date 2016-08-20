﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
public class ItemUI : MonoBehaviour {
	public Image image;//TODO load it fromanywhere?
	public Text text;
	public Slider slider;
	public bool changeColor=false;

	public string itemName;
	public void SetItem(Item i, int maxValue,bool changeColor = false){
		this.changeColor = changeColor;
		ChangeMaxValue (maxValue);
		if (i == null) { 
			ChangeItemCount (0);			
			itemName="Empty";//FIXME not hardcoded
		} else {
			itemName=i.name;
			ChangeItemCount (i);
		}
		//set item pic andso
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
	public void ChangeItemCount(Item i){
		ChangeItemCount (i.count);
	}
	public void ChangeItemCount(int amount){
		text.text =amount+"t";
		slider.value =amount;
		AdjustSliderColor ();
	}
	public void ChangeItemCount(float amount){
		text.text =amount+"t";
		slider.value =amount;
		AdjustSliderColor ();
	}
	public void ChangeMaxValue(int maxValue){
		slider.maxValue = maxValue;
		AdjustSliderColor ();
	}
	void AdjustSliderColor(){
		if(changeColor==false){
			return;
		}
		if (slider.value / slider.maxValue < 0.2f) {
			slider.GetComponentInChildren<Image> ().color = Color.red;
		} else {
			slider.GetComponentInChildren<Image> ().color = Color.green;
		}
	}

	public void AddListener(UnityAction<BaseEventData> ueb){
		EventTrigger trigger = GetComponent<EventTrigger> ();
		EventTrigger.Entry entry = new EventTrigger.Entry( );
		entry.eventID = EventTriggerType.PointerClick;

		entry.callback.AddListener( ueb );
		trigger.triggers.Add( entry );
	}

	public void OnMouseEnter(){
		GameObject.FindObjectOfType<HoverOverScript> ().Show (itemName);
	}
	public void OnMouseExit(){
		GameObject.FindObjectOfType<HoverOverScript> ().Unshow ();
	}

}