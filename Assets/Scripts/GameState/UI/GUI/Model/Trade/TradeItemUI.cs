using Andja.Model;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class TradeItemUI : MonoBehaviour {
        private ItemUI itemUI;
        public Trade Trade;
        public Button sellButton;
        public Button buyButton;
        public Action<string, bool> onButtonClick;
        public Text priceText;
        public EventTrigger trigger;
        public Image Highlight;
        public TradeItem tradeItem;

        public void Show(TradeItem tradeItem, int maxStacksize, Action<string, bool> cbButton) {
            itemUI = GetComponentInChildren<ItemUI>();
            onButtonClick += cbButton;
            if (tradeItem != null) {
                itemUI.SetItem(new Item(tradeItem.ItemId, tradeItem.count), maxStacksize);
                this.tradeItem = tradeItem;
                UpdateSellBuy(tradeItem.IsSelling);
            } 
        }

        public void UpdatePriceText(int price) {
            priceText.text = "" + price;
        }

        public void Show(int maxStacksize, TradeItem tradeItem) {
            itemUI = GetComponentInChildren<ItemUI>();
            itemUI.SetItem(new Item(tradeItem.ItemId, tradeItem.count), maxStacksize);
            sellButton.interactable = false;
            this.tradeItem = tradeItem;
            UpdateSellBuy(tradeItem.trade == Trade.Sell);
        }


        public void ChangeItemCount(int amount) {
            itemUI.ChangeItemCount(amount);
            tradeItem.count = amount;
        }

        public void RefreshItem(Item i) {
            tradeItem = null;
            itemUI.RefreshItem(i);
        }
        private void Update() {
            if(tradeItem != null)
                UpdatePriceText(tradeItem.price);
        }
        public void UpdateSellBuy(bool sell) {
            if (sell) {
                sellButton.interactable = false;
                buyButton.interactable = true;
                Trade = Trade.Sell;
            }
            else {
                sellButton.interactable = true;
                buyButton.interactable = false;
                Trade = Trade.Buy;
            }
            if(tradeItem != null) {
                UpdatePriceText(tradeItem.price);
                onButtonClick?.Invoke(tradeItem.ItemId, sell);
            }
        }

        internal void UpdateAmount(int itemAmount) {
            int amount = 0;
            if (Trade == Trade.Sell) { 
                amount = Mathf.Clamp(itemAmount - tradeItem.count, 0, int.MaxValue);
                itemUI.ChangeItemCount(amount);
            }
            if (Trade == Trade.Buy) {
                amount = Mathf.Clamp(tradeItem.count - itemAmount, 0, int.MaxValue);
                itemUI.ChangeItemCount(amount);
            }
            trigger.enabled = amount > 0;
        }

        public void AddListener(UnityAction<BaseEventData> ueb) {
            //FIRST EVENTTRIGGER is the item itself
            EventTrigger.Entry entry = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerClick
            };
            entry.callback.AddListener(ueb);
            trigger.triggers.Add(entry);
        }

        internal void OnSelect() {
            Highlight.gameObject.SetActive(true);
            trigger.gameObject.SetActive(false);
        }

        internal void OnUnselect() {
            Highlight.gameObject.SetActive(false);
            trigger.gameObject.SetActive(true);
        }
    }
}