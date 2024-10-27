using UnityEngine;
using UnityEngine.EventSystems;
using Andja.Model;
using System;
using UnityEngine.UI;

namespace Andja.UI.Model {
    public class MapTradeStop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler {
        public TradeRoute.Stop stop;
        public bool cityStop;

        public Vector2 Position => transform.localPosition;

        public void Setup(TradeRoute.Stop stop, bool cityStop) {
            this.stop = stop;
            this.cityStop = cityStop;
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (cityStop)
                return;
            switch (eventData.button) {
                case PointerEventData.InputButton.Left:
                    TradeRoutePanel.Instance.tradeRouteLine.OnTradeStopDown(stop);
                    break;
                case PointerEventData.InputButton.Right:
                    break;
                case PointerEventData.InputButton.Middle:
                    break;
            }
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (cityStop)
                return;
            switch (eventData.button) {
                case PointerEventData.InputButton.Left:
                    TradeRoutePanel.Instance.tradeRouteLine.OnTradeStopUp(stop);
                    break;
                case PointerEventData.InputButton.Right:
                    TradeRoutePanel.Instance.tradeRouteLine.RemoveStop(this);
                    break;
                case PointerEventData.InputButton.Middle:
                    break;
            }
        }

        public void OnDrag(PointerEventData eventData) {
            if (cityStop)
                return;
            TradeRoutePanel.Instance.tradeRouteLine.OnStopPointDrag(eventData);
            transform.localPosition = TradeRoutePanel.Instance.tradeRouteLine.currentLocalPosition;
        }

        public void OnEndDrag(PointerEventData eventData) {
            if (cityStop)
                return;
            TradeRoutePanel.Instance.tradeRouteLine.OnTradeStopUp(stop);
        }

        internal void SetRotation(float angle) {
            transform.localEulerAngles = new Vector3(0, 0, angle);
        }

        public void SetFirst() {
            GetComponent<Image>().color = Color.green;
        }
    }

}