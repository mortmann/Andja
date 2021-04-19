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
        public Action<Item, bool> onButtonClick;
        public Item Item { get; protected set; }
        public Text priceText;
        public EventTrigger trigger;
        public Image Highlight;

        public void Show(Item item, int maxStacksize, Action<Item, bool> cbButton) {
            itemUI = GetComponentInChildren<ItemUI>();
            itemUI.SetItem(item, maxStacksize);
            onButtonClick += cbButton;
            if (item == null) {
                return;
            }
            this.Item = item.CloneWithCount();
            ChangeItemCount(maxStacksize / 2);
            UpdateSellBuy(true);
        }

        public void UpdatePriceText(int price) {
            priceText.text = "" + price;
        }

        public void Show(Item item, int maxStacksize, Trade trade) {
            itemUI = GetComponentInChildren<ItemUI>();
            itemUI.SetItem(item, maxStacksize);
            sellButton.interactable = false;
            this.Item = item;
            UpdateSellBuy(trade == Trade.Sell);
        }

        public void ChangeItemCount(int amount) {
            itemUI.ChangeItemCount(amount);
            Item.count = amount;
        }

        public void SetItem(Item i, int maxValue, bool changeColor = false) {
            Item = i;
            itemUI.SetItem(i, maxValue, changeColor);
            UpdateSellBuy(true);
        }

        public void RefreshItem(Item i) {
            Item = i;
            itemUI.RefreshItem(i);
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
            onButtonClick?.Invoke(Item, sell);
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