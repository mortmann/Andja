using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class TradePanel : MonoBehaviour {
        public Slider amountSlider;
        public Slider priceSlider;
        public GameObject TradeCanvas;
        public TradeItemUI TradeItemPrefab;

        private TradeItemUI tradeItemUICurrentlySelected;
        private City city;
        private void Start() {
            amountSlider.onValueChanged.AddListener(OnAmountSliderChange);
            priceSlider.onValueChanged.AddListener(OnPriceSliderChange);
        }
        // Use this for initialization
        public void Show(City c) {
            city = c;
            amountSlider.maxValue = city.Inventory.MaxStackSize;
            foreach (Transform t in TradeCanvas.transform) {
                Destroy(t.gameObject);
            }

            List<string> items = new List<string>(c.itemIDtoTradeItem.Keys);
            for (int i = 0; i < city.TradeItemCount; i++) {
                TradeItemUI tradeItemUI = GameObject.Instantiate(TradeItemPrefab);
                tradeItemUI.transform.SetParent(TradeCanvas.transform, false);
                if (c.itemIDtoTradeItem.Count <= i) {
                    tradeItemUI.Show(null, c.Inventory.MaxStackSize, OnSellBuyClick);
                }
                else {
                    Item item = c.Inventory.GetItemClone(items[i]);
                    tradeItemUI.Show(c.itemIDtoTradeItem[items[i]], c.Inventory.MaxStackSize, OnSellBuyClick);
                    tradeItemUI.ChangeItemCount(c.itemIDtoTradeItem[items[i]].count);
                }
                tradeItemUI.AddListener((data) => { OnTradeItemClick(tradeItemUI); });
            }
        }

        public void OnItemSelected(Item item) {
            if (tradeItemUICurrentlySelected == null)
                return;
            if (city.itemIDtoTradeItem.ContainsKey(item.ID)) {
                Debug.Log("already in it");
                return;
            }
            if (tradeItemUICurrentlySelected.tradeItem != null) {
                RemoveCurrentTradeItem();
            }
            TradeItem ti = new TradeItem(item.ID, ((int)amountSlider.value),
                            ((int)priceSlider.value), tradeItemUICurrentlySelected.Trade);
            city.AddTradeItem(ti);
            tradeItemUICurrentlySelected.Show(city.Inventory.MaxStackSize, ti);
            amountSlider.value = city.Inventory.MaxStackSize / 2;
            item.count = Mathf.RoundToInt(amountSlider.value);
            OnPriceSliderChange(50);
        }

        public void OnSellBuyClick(string itemID, bool sell) {
            if (itemID == null) {
                return;
            }
            if (city.itemIDtoTradeItem.ContainsKey(itemID) == false) {
                return;
            }
            city.itemIDtoTradeItem[itemID].IsSelling = sell;
        }

        public void OnTradeItemClick(TradeItemUI ui) {
            tradeItemUICurrentlySelected?.OnUnselect();
            tradeItemUICurrentlySelected = ui;
            tradeItemUICurrentlySelected.OnSelect();
        }

        public void OnAmountSliderChange(float f) {
            if (tradeItemUICurrentlySelected == null)
                return;
            if (tradeItemUICurrentlySelected.tradeItem == null)
                return;
            tradeItemUICurrentlySelected.ChangeItemCount(Mathf.RoundToInt(f));
            city.ChangeTradeItemAmount(tradeItemUICurrentlySelected.Item);
        }

        public void OnPriceSliderChange(float f) {
            if (tradeItemUICurrentlySelected == null)
                return;
            if (tradeItemUICurrentlySelected.tradeItem == null)
                return;
            if (city.itemIDtoTradeItem.ContainsKey(tradeItemUICurrentlySelected.tradeItem.ItemId) == false) {
                Debug.Log("OnPriceChange - item not found in tradeitems");
                return;
            }
            //GAME SIDE
            city.ChangeTradeItemPrice(tradeItemUICurrentlySelected.tradeItem.ItemId, Mathf.RoundToInt(f));
            //UI SIDE
            //TODO: when ui-rework this needs to be a seperate text
            TranslationData td = Controller.UILanguageController.Instance.GetTranslationData(Controller.StaticLanguageVariables.Price);
            priceSlider.GetComponentInChildren<Text>().text = td?.translation + f;
        }

        public void OnDeleteClick() {
            RemoveCurrentTradeItem();
        }

        private void RemoveCurrentTradeItem() {
            if (tradeItemUICurrentlySelected.tradeItem == null)
                return;
            city.RemoveTradeItem(tradeItemUICurrentlySelected.Item);
            tradeItemUICurrentlySelected.RefreshItem(null);
        }

        private void OnDestroy() {
            tradeItemUICurrentlySelected?.OnUnselect();
            tradeItemUICurrentlySelected = null;
        }
    }
}