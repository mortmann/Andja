﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class CityInventoryUI : MonoBehaviour {
	public GameObject cityname;
	public GameObject contentCanvas;
	public GameObject itemPrefab;
	public GameObject tradePanel;

	Dictionary<int, ItemUI> itemToGO;
	public bool trade;
	public City city;


	public void ShowInventory(City city, bool trade){
		if(city == null && this.city == city){
			return;
		}
		city.RegisterCityDestroy (OnCityDestroy);
		cityname.GetComponent<Text> ().text = city.name;
		this.city = city;
		this.trade = trade;
		city.inventory.RegisterOnChangedCallback (OnInventoryChange);

		foreach (Transform child in contentCanvas.transform) {
			Destroy (child.gameObject);
		}
		itemToGO = new Dictionary<int, ItemUI> ();
		foreach (Item item in city.inventory.items.Values) {
			GameObject go_i = GameObject.Instantiate (itemPrefab);
			go_i.name = item.name + " Item";
			ItemUI iui = go_i.GetComponent<ItemUI> ();
			itemToGO.Add (item.ID,iui);
			iui.SetItem (item,city.inventory.maxStackSize,true);
			// does this need to be here?
			// or can it be move to itemui?
			// changes in th future maybe
			Item i = item.Clone ();
			iui.AddListener (( data) => {
				OnItemClick (i);
			});

			go_i.transform.SetParent (contentCanvas.transform);
		}
	}
	public void OnCityDestroy(City c){
		if(city != c){
			return;
		}
		UIController.Instance.HideCityUI (c);
	}

	void OnItemClick(Item item){		
		if (trade) {
			//trade to ship
			city.tradeWithShip (city.inventory.getItemInInventoryClone (item));
			return;
		} 
		if(GameObject.FindObjectOfType<TradeRoutePanel> ()!=null){
			//select item for trademenu
			TradeRoutePanel tp = GameObject.FindObjectOfType<TradeRoutePanel> ();
			tp.GetClickedItemCity(item);
			return;
		}
		if(tradePanel.activeSelf){
			tradePanel.GetComponent<TradePanel> ().OnItemSelected (city.inventory.getItemInInventoryClone (item));
		}
	}


	public void OnTradeMenuClick(){
		if(!tradePanel.activeSelf)
			tradePanel.GetComponent<TradePanel> ().Show (city);
		tradePanel.SetActive (!tradePanel.activeSelf);
	}
	public void OnInventoryChange(Inventory changedInv){
		foreach(int i in changedInv.items.Keys){
			itemToGO [i].ChangeItemCount (city.inventory.items [i].count);
			itemToGO [i].ChangeMaxValue (city.inventory.maxStackSize);
		}
	}

	void OnDisable(){
		tradePanel.SetActive (false);
		if(city!=null){
			city.UnregisterCityDestroy (OnCityDestroy);
		}
	}

}
