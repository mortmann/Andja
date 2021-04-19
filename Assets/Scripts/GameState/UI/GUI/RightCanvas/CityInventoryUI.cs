using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
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

        city.Inventory.RegisterOnChangedCallback(OnInventoryChange);

        foreach (Transform child in contentCanvas.transform) {
            Destroy(child.gameObject);
        }
        itemToGO = new Dictionary<string, ItemUI>();
        List<Item> items = new List<Item>(city.Inventory.Items.Values);
        items = items.OrderBy(x => x.Data.UnlockLevel).ThenBy(y=>y.Data.UnlockPopulationCount).ToList();
        foreach (Item item in items) {
            GameObject go_i = GameObject.Instantiate(itemPrefab);
            go_i.name = item.Name + " Item";
            ItemUI iui = go_i.GetComponent<ItemUI>();
            itemToGO.Add(item.ID, iui);
            iui.SetItem(item, city.Inventory.MaxStackSize, true);
            Item i = item.Clone();
            iui.AddClickListener((s) => {
                OnItemClick(i);
            });
            go_i.transform.SetParent(contentCanvas.transform, false);
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
        onItemPressed += (item) => tradePanel.GetComponent<TradePanel>().OnItemSelected(city.Inventory.GetItemInInventoryClone(item));
    }
    public void OnInventoryChange(Inventory changedInv) {
        foreach (string i in changedInv.Items.Keys) {
            itemToGO[i].ChangeItemCount(city.Inventory.Items[i].count);
            itemToGO[i].ChangeMaxValue(city.Inventory.MaxStackSize);
        }
    }

    void OnDisable() {
        tradePanel.SetActive(false);
        if (city != null) {
            city.UnregisterCityDestroy(OnCityDestroy);
        }
    }

}
