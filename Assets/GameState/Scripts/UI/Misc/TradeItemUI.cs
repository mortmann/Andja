using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
public class TradeItemUI : MonoBehaviour {

    ItemUI itemUI;
    private bool _sell = true;
    public bool Sell {
        get { return _sell; }
    }
    public Button sellButton;
    public Button buyButton;
    public Action<Item, bool> onButtonClick;
    public Item item { get; protected set; }
    public Text priceText;
    public EventTrigger trigger;

    public void Show(Item item, int maxStacksize, Action<Item, bool> cbButton) {
        itemUI = GetComponentInChildren<ItemUI>();
        itemUI.SetItem(item, maxStacksize);
        sellButton.interactable = false;
        onButtonClick += cbButton;
        if (item == null) {
            return;
        }
        this.item = item.CloneWithCount();
        ChangeItemCount(maxStacksize / 2);
    }
    public void UpdatePriceText(int price) {
        priceText.text = "" + price;
    }
    public void Show(Item item, int maxStacksize, bool sell) {
        itemUI = GetComponentInChildren<ItemUI>();
        itemUI.SetItem(item, maxStacksize);
        sellButton.interactable = false;
        this.item = item;
        UpdateSellBuy(sell);
    }
    public void ChangeItemCount(int amount) {
        itemUI.ChangeItemCount(amount);
        item.count = amount;
    }
    public void SetItem(Item i, int maxValue, bool changeColor = false) {
        itemUI.SetItem(item, maxValue, changeColor);
        item = i;
    }
    public void RefreshItem(Item i) {
        item = i;
        itemUI.RefreshItem(null);
    }
    public void UpdateSellBuy(bool sell) {
        if (sell) {
            sellButton.interactable = false;
            buyButton.interactable = true;
        }
        else {
            sellButton.interactable = true;
            buyButton.interactable = false;
        }
        _sell = sell;
        onButtonClick?.Invoke(item, sell);
    }
    public void AddListener(UnityAction<BaseEventData> ueb) {
        //FIRST EVENTTRIGGER is the item itself
        EventTrigger.Entry entry = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerClick
        };
        entry.callback.AddListener(ueb);
        trigger.triggers.Add(entry);
    }

}
