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
        private ICity city;
        private void Start() {
            amountSlider.onValueChanged.AddListener(OnAmountSliderChange);
            priceSlider.onValueChanged.AddListener(OnPriceSliderChange);
            amountSlider.value = 25f;
        }
        public void Show(ICity c) {
            city = c;
            amountSlider.maxValue = city.Inventory.MaxStackSize;
            
            foreach (Transform t in TradeCanvas.transform) {
                Destroy(t.gameObject);
            }

            List<string> items = new List<string>(c.ItemIDtoTradeItem.Keys);
            for (int i = 0; i < city.TradeItemCount; i++) {
                TradeItemUI tradeItemUI = Instantiate(TradeItemPrefab);
                tradeItemUI.transform.SetParent(TradeCanvas.transform, false);
                if (c.ItemIDtoTradeItem.Count <= i) {
                    tradeItemUI.Show(null, c.Inventory.MaxStackSize, OnSellBuyClick);
                }
                else {
                    tradeItemUI.Show(c.ItemIDtoTradeItem[items[i]], c.Inventory.MaxStackSize, OnSellBuyClick);
                }
                tradeItemUI.AddListener((data) => { OnTradeItemClick(tradeItemUI); });
            }
        }

        public void OnItemSelected(Item item) {
            if (tradeItemUICurrentlySelected == null)
                return;
            if (city.ItemIDtoTradeItem.ContainsKey(item.ID)) {
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
            amountSlider.SetValueWithoutNotify(ti.count);
            item.count = Mathf.RoundToInt(amountSlider.value);
            OnPriceSliderChange(ti.price);
        }

        public void OnSellBuyClick(string itemID, bool sell) {
            if (itemID == null) {
                return;
            }
            if (city.ItemIDtoTradeItem.ContainsKey(itemID) == false) {
                return;
            }
            city.ItemIDtoTradeItem[itemID].IsSelling = sell;
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
            city.ChangeTradeItemAmount(tradeItemUICurrentlySelected.tradeItem);
        }

        public void OnPriceSliderChange(float f) {
            if (tradeItemUICurrentlySelected == null)
                return;
            if (tradeItemUICurrentlySelected.tradeItem == null)
                return;
            if (city.ItemIDtoTradeItem.ContainsKey(tradeItemUICurrentlySelected.tradeItem.ItemId) == false) {
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
            city.RemoveTradeItem(tradeItemUICurrentlySelected.tradeItem.ItemId);
            tradeItemUICurrentlySelected.RefreshItem(null);
        }

        private void OnDisable() {
            tradeItemUICurrentlySelected?.OnUnselect();
            tradeItemUICurrentlySelected = null;
        }
    }
}