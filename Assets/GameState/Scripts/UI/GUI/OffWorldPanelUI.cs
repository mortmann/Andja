﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class OffWorldPanelUI : MonoBehaviour {

	public Button delete;
	public Button send;
	public GameObject onShip;
	public GameObject toBuy;
	public GameObject itemPrefab;
	public Dropdown shipDP;

	int _pressedItem;
	int pressedItem{
		get{return _pressedItem;}
		set{
			_pressedItem = value;
		}
	}
	Dictionary<int,ItemUI> intToGameObject;
	Dictionary<int,Item> intToItem;
	public List<Ship> ships;
	public Slider amountSlider;
	Ship ship;
	Dictionary<Unit,string> unitNames;


	void Start(){
		intToGameObject = new Dictionary<int, ItemUI> ();
		intToItem = new Dictionary<int, Item> ();
		amountSlider.onValueChanged.AddListener (OnAmountSliderMoved);
		delete.onClick.AddListener (OnDeleteClick);
		send.onClick.AddListener (OnSendClick);
		unitNames = new Dictionary<Unit,string> ();
		ships = new List<Ship> ();
		foreach (Unit item in World.current.units) {
			if(item.isShip==false||item.playerNumber!=PlayerController.Instance.currentPlayerNumber){
				continue;
			}
			ships.Add ((Ship) item); 
			unitNames.Add ((Ship) item,item.Name); 
			item.RegisterOnDestroyCallback (OnShipDestroy);
			item.RegisterOnChangedCallback (OnShipChanged);
		}
		RefreshDropDownValues ();
		shipDP.onValueChanged.AddListener (OnDropDownChange);
		pressedItem = 0;
		if(ships.Count>0){
			Show(ships [0]);
		}
		ResetItemIcons ();
	}
	public void OnDropDownChange(int i){Debug.Log (i);
		Show (ships[i]);
	}
	public void OnShipDestroy(Unit u){
		unitNames.Remove (u);
		shipDP.RefreshShownValue ();
	}
	public void OnShipChanged(Unit u){
		unitNames [u] = u.Name;
		shipDP.RefreshShownValue ();
	}
	public void RefreshDropDownValues(){
		shipDP.ClearOptions ();
		shipDP.AddOptions (new List<string>(unitNames.Values));
		shipDP.RefreshShownValue ();
	}
	public void OnDeleteClick(){
		intToGameObject [pressedItem].SetItem (null, ship.inventory.maxStackSize);
		intToItem.Remove (pressedItem);
	}
	public void OnSendClick(){
		List<Item> list = new List<Item> (intToItem.Values);
		foreach (var item in list) {
			Debug.Log (item.ToString ()); 
		}
		ship.SendToOffworldMarket (list.ToArray ());
		unitNames.Remove (ship);
		ship = null;
		RefreshDropDownValues ();
	}
		
	public void OnAmountSliderMoved(float f){
		if(intToGameObject.ContainsKey (pressedItem)==false){
			return;
		}
		intToGameObject [this.pressedItem].ChangeItemCount (f);
		intToItem [pressedItem].count = (int)f;
	}
	public void Show(Ship unit){
		amountSlider.maxValue = unit.inventory.maxStackSize;
		this.ship = unit;
		ResetItemIcons ();
	}
	private void AddItemPrefabTo(Transform t){
		GameObject g = GameObject.Instantiate (itemPrefab);
		g.transform.SetParent (t);
		g.GetComponentInChildren<Slider> ().maxValue = ship.inventory.maxStackSize;
		g.GetComponentInChildren<Text> ().text= ship.inventory.maxStackSize+"t";
		//TODO add listener stuff
		int i = intToGameObject.Count;
		intToGameObject.Add (i,g.GetComponent<ItemUI> ()); 
		g.GetComponent<ItemUI> ().AddListener (( data ) => { OnItemClick( i ); } );

	}
	public void OnItemClick(int i){
		pressedItem = i;
	}
	public void OnOffWorldItemClick(Item i){
		intToGameObject [pressedItem].RefreshItem (i);
		intToItem.Add (pressedItem,i); 
	}
	public void ResetItemIcons(){
		Debug.Log ("Reset"); 
		intToGameObject = new Dictionary<int, ItemUI> ();
		foreach(Transform t in onShip.transform){
			GameObject.Destroy (t.gameObject);
		}
		foreach(Transform t in toBuy.transform){
			GameObject.Destroy (t.gameObject);
		}
		if(ship==null){
			return;
		}
		for (int i = 0; i < ship.inventory.numberOfSpaces; i++) {
			AddItemPrefabTo (toBuy.transform); 
			Item item = ship.inventory.GetItemWithNRinItems(i);
			GameObject g = GameObject.Instantiate (itemPrefab);
			g.GetComponent<ItemUI> ().SetItem (item,ship.inventory.maxStackSize);
			g.transform.SetParent (onShip.transform);
		}
	}

	void OnDisable(){
		foreach (Ship item in ships) {
			item.UnregisterOnChangedCallback (OnShipChanged);
			item.UnregisterOnChangedCallback (OnShipDestroy);
		}
	}
}