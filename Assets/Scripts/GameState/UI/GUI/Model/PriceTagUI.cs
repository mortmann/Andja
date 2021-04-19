using Andja.Model;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class PriceTagUI : MonoBehaviour {
        public Text sellText;
        public Text buyText;
        public ItemUI itemUI;
        private OffworldMarket.Price Price;

        public void Show(Item item, OffworldMarket.Price price) {
            item.count = 1;
            itemUI.SetItem(item, 1);
            this.Price = price;
            UpdatePrice();
        }

        public void UpdatePrice() {
            sellText.text = "+" + Price.Sell;
            buyText.text = "-" + Price.Buy;
        }

        public void AddListener(UnityAction<BaseEventData> ueb) {
            EventTrigger trigger = GetComponentInChildren<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerClick
            };

            entry.callback.AddListener(ueb);
            trigger.triggers.Add(entry);
        }

        public void Update() {
            UpdatePrice();
        }
    }
}