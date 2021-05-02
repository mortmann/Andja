using Andja.Model;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    public class TradeRouteElement : MonoBehaviour { 
        public static TradeRouteElement Selected;
        public HiddenInputField NameText;
        public Button DeleteButton;
        public GameObject Outline;
        private Action<TradeRoute> onSelect;
        private Action<TradeRoute> onDelete;
        private TradeRoute tradeRoute;
        public TradeRouteElement() {
            Unselect();
        }

        private void OnSelectTradeRoute() {
            Select();
            onSelect?.Invoke(tradeRoute);
        }

        private void Select() {
            if (Selected != null)
                Selected.Unselect();
            Selected = this;
            Outline.SetActive(true);
        }

        private void Unselect() {
            if(Outline != null)
                Outline.SetActive(false);
        }

        private void OnDeleteClick() {
            //TODO: add yes/no dialog
            onDelete?.Invoke(tradeRoute);
        }

        public void SetTradeRoute(TradeRoute tradeRoute, Action<TradeRoute> onSelect, Action<TradeRoute> onDelete) {
            this.tradeRoute = tradeRoute;
            DeleteButton.onClick.AddListener(OnDeleteClick);
            NameText.Set(tradeRoute.Name, tradeRoute.SetName, true, true, OnSelectTradeRoute);
            this.onDelete += onDelete;
            this.onSelect += onSelect;
            Select();
        }

    }
}