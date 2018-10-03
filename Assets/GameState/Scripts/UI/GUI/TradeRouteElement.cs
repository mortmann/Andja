using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class TradeRouteElement : MonoBehaviour {
    public InputField NameText;
    public Button DeleteButton;
    Action<TradeRoute> onSelect; 
    Action<TradeRoute> onDelete;
    TradeRoute tradeRoute;
    void Start() {
        DeleteButton.onClick.AddListener(OnDeleteClick);
        NameText.onEndEdit.AddListener(OnNameEdit);
        NameText.readOnly = true;
        NameText.interactable = false;

        EventTrigger trigger = NameText.GetComponent<EventTrigger>();
        EventTrigger.Entry click = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerClick
        };
        click.callback.AddListener((data) => {
            PointerEventData ped = ((PointerEventData)data);
            if (ped.button == PointerEventData.InputButton.Left)
                OnSelectTradeRoute();
            if (ped.button == PointerEventData.InputButton.Right)
                OnInputFieldClick();
        });
        trigger.triggers.Add(click);
    }

    private void OnSelectTradeRoute() {
        onSelect?.Invoke(tradeRoute);
    }

    private void OnInputFieldClick() {
        if (NameText.readOnly == false)
            return;
        NameText.readOnly = false;
        NameText.interactable = true;
        NameText.Select();
    }

    private void OnNameEdit(string name) {
        tradeRoute.Name = name;
        NameText.readOnly = true;
    }

    private void OnDeleteClick() {
        onDelete?.Invoke(tradeRoute);
    }
    public void SetTradeRoute(TradeRoute tradeRoute, Action<TradeRoute> onSelect, Action<TradeRoute> onDelete) {
        this.tradeRoute = tradeRoute;
        NameText.text = tradeRoute.Name;
        this.onDelete += onDelete;
        this.onSelect += onSelect;
    }
    public void Update() {
        //Doing this to prevent the error when setting this on edit end
        if (NameText.readOnly == true)
            NameText.interactable = false;
    }
}
