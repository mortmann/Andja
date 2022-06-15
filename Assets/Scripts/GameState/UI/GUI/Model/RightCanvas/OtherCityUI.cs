using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.UI.Model {

    public class OtherCityUI : MonoBehaviour {
        public City city { protected set; get; }
        public GameObject ItemsCanvas;
        public GameObject TradeItemPrefab;
        public GameObject ItemCanvas;
        Dictionary<TradeItem, TradeItemUI> tradeItemToUI = new Dictionary<TradeItem, TradeItemUI>();
        // Use this for initialization
        public void Show(City c) {
            city = c;
            city.RegisterCityDestroy(OnCityDestroy);

            city.Inventory.RegisterOnChangedCallback(OnInventoryChange);
            tradeItemToUI.Clear();
            foreach (Transform item in ItemsCanvas.transform) {
                Destroy(item.gameObject);
            }
            foreach (string itemID in city.itemIDtoTradeItem.Keys) {
                TradeItem ti = city.itemIDtoTradeItem[itemID];
                GameObject g = Instantiate(TradeItemPrefab);
                g.transform.SetParent(ItemCanvas.transform, false);
                TradeItemUI tiui = g.GetComponent<TradeItemUI>();
                tiui.Show(city.Inventory.MaxStackSize, ti);
                tiui.UpdatePriceText(ti.price);
                string id = itemID;
                tiui.AddListener((data) => { OnClickItemToTrade(id); });
                tradeItemToUI.Add(ti, tiui);
            }
            OnInventoryChange(city.Inventory);
        }

        public void OnInventoryChange(Inventory inventory) {
            foreach (var item in tradeItemToUI) {
                item.Value.UpdateAmount(inventory.GetAmountFor(item.Key.ItemId));
            }
        }
        public void OnClickItemToTrade(string itemID, int amount = 50) {
            Unit u = city.warehouse.inRangeUnits.Find(x => x.playerNumber == PlayerController.currentPlayerNumber);
            if (u == null || u.IsShip == false) {
                Debug.Log("No Ship in Range");
                return;
            }
            city.SellingTradeItem(itemID, PlayerController.CurrentPlayer, ((Ship)u), amount);
        }

        public void OnCityDestroy(City c) {
            if (city != c) {
                return;
            }
            UIController.Instance.HideCityUI(c);
        }

        private void OnDisable() {
            if (city != null) {
                city.UnregisterCityDestroy(OnCityDestroy);
            }
        }
    }
}