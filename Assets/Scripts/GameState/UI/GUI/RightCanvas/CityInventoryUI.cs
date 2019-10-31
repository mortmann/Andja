﻿using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class CityInventoryUI : MonoBehaviour {
    public GameObject cityname;
    public GameObject contentCanvas;
    public GameObject itemPrefab;
    public GameObject tradePanel;

    Dictionary<string, ItemUI> itemToGO;
    public City city;

    Action<Item> onItemPressed;

    public CityUI CityInfo;

    public void ShowInventory(City city, Action<Item> onItemPressed = null) {
        if (city == null && this.city == city) {
            return;
        }
        city.RegisterCityDestroy(OnCityDestroy);
        cityname.GetComponent<Text>().text = city.Name;
        this.city = city;
        this.onItemPressed = onItemPressed;

        city.inventory.RegisterOnChangedCallback(OnInventoryChange);

        foreach (Transform child in contentCanvas.transform) {
            Destroy(child.gameObject);
        }
        itemToGO = new Dictionary<string, ItemUI>();
        foreach (Item item in city.inventory.Items.Values) {
            GameObject go_i = GameObject.Instantiate(itemPrefab);
            go_i.name = item.Name + " Item";
            ItemUI iui = go_i.GetComponent<ItemUI>();
            itemToGO.Add(item.ID, iui);
            iui.SetItem(item, city.inventory.MaxStackSize, true);
            Item i = item.Clone();
            iui.AddClickListener((data) => {
                OnItemClick(i);
            });
            go_i.transform.SetParent(contentCanvas.transform);
        }
    }
    public void OnCityUIToggle() {
        CityInfo.city = city;
        CityInfo.gameObject.SetActive(!CityInfo.gameObject.activeSelf);
    }
    public void OnCityDestroy(City c) {
        if (city != c) {
            return;
        }
        UIController.Instance.HideCityUI(c);
    }

    void OnItemClick(Item item) {
        onItemPressed?.Invoke(item);
        //if (trade) {
        //	//trade to ship
        //	city.TradeWithShip (city.inventory.GetItemInInventoryClone (item));
        //	return;
        //} 
        //if(GameObject.FindObjectOfType<TradeRoutePanel> ()!=null){
        //	//select item for trademenu
        //	TradeRoutePanel tp = GameObject.FindObjectOfType<TradeRoutePanel> ();
        //	tp.GetClickedItemCity(item);
        //	return;
        //}
        //if(tradePanel.activeSelf){
        //	
        //}
    }


    public void OnTradeMenuClick() {
        if (!tradePanel.activeSelf)
            tradePanel.GetComponent<TradePanel>().Show(city);
        tradePanel.SetActive(!tradePanel.activeSelf);
        onItemPressed += (item) => tradePanel.GetComponent<TradePanel>().OnItemSelected(city.inventory.GetItemInInventoryClone(item));
    }
    public void OnInventoryChange(Inventory changedInv) {
        foreach (string i in changedInv.Items.Keys) {
            itemToGO[i].ChangeItemCount(city.inventory.Items[i].count);
            itemToGO[i].ChangeMaxValue(city.inventory.MaxStackSize);
        }
    }

    void OnDisable() {
        tradePanel.SetActive(false);
        if (city != null) {
            city.UnregisterCityDestroy(OnCityDestroy);
        }
    }

}