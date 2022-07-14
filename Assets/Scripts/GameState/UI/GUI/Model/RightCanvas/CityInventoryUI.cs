using Andja.Controller;
using Andja.Model;
using Andja.UI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class CityInventoryUI : MonoBehaviour {
        public HiddenInputField cityname;
        public HiddenInputField tradeAmount;

        public GameObject contentCanvas;
        public GameObject itemPrefab;
        public GameObject tradePanel;

        private Dictionary<string, ItemUI> itemToGO;
        public ICity city;

        private Action<Item> onItemPressed;

        public CityUI CityInfo;

        public void ShowInventory(ICity city, Action<Item> onItemPressed = null) {
            if (city == null && this.city == city) {
                return;
            }
            city.RegisterCityDestroy(OnCityDestroy);
            this.city = city;
            this.onItemPressed = onItemPressed;
            cityname.Set(city.Name, city.SetName);

            tradeAmount.Set(city.PlayerTradeAmount, city.SetPlayerTradeAmount, true,
                ()=>{ return 0; }, () => { return city.Inventory.MaxStackSize; }
                );
            city.Inventory.RegisterOnChangedCallback(OnInventoryChange);

            foreach (Transform child in contentCanvas.transform) {
                Destroy(child.gameObject);
            }
            itemToGO = new Dictionary<string, ItemUI>();
            List<Item> items = new List<Item>(city.Inventory.BaseItems);
            items = items.OrderBy(x => x.Data.UnlockLevel).ThenBy(y => y.Data.UnlockPopulationCount).ToList();
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

        public void OnCityDestroy(ICity c) {
            if (city != c) {
                return;
            }
            UIController.Instance.HideCityUI(c);
        }

        private void OnItemClick(Item item) {
            onItemPressed?.Invoke(item);
        }

        public void OnTradeMenuClick() {
            if (!tradePanel.activeSelf)
                tradePanel.GetComponent<TradePanel>().Show(city);
            tradePanel.SetActive(!tradePanel.activeSelf);
            onItemPressed += (item) => tradePanel.GetComponent<TradePanel>().OnItemSelected(city.Inventory.GetItemClone(item));
        }

        public void OnInventoryChange(Inventory changedInv) {
            CityInventory cityInventory = (CityInventory)changedInv;
            foreach (string i in cityInventory.Items.Keys) {
                itemToGO[i].ChangeItemCount(cityInventory.Items[i].count);
                itemToGO[i].ChangeMaxValue(cityInventory.MaxStackSize);
            }
        }
        public void ChangeCityPlayerTradeAmount(int change) {
            city.SetPlayerTradeAmount(Mathf.Clamp(city.PlayerTradeAmount + change, 0, city.Inventory.MaxStackSize));
            tradeAmount.Set(city.PlayerTradeAmount + "");
        }
        public void OnDisable() {
            tradePanel.SetActive(false);
            if (city != null) {
                city.UnregisterCityDestroy(OnCityDestroy);
            }
        }
    }
}