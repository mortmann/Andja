using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    [RequireComponent(typeof(Slider), typeof(EventTrigger))]
    public class SliderHoverOver : MonoBehaviour {
        private Slider slider;
        public int decimals = 1;
        private int round;
        public string suffix;
        public string prefix;
        private Vector3 position;

        public void Start() {
            EventTrigger trigger = GetComponent<EventTrigger>();
            EventTrigger.Entry enter = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerEnter
            };
            enter.callback.AddListener((data) => {
                OnPointerEnter();
            });
            trigger.triggers.Add(enter);
            EventTrigger.Entry exit = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerExit
            };
            exit.callback.AddListener((data) => {
                OnPointerExit();
            });
            trigger.triggers.Add(exit);
            slider = GetComponent<Slider>();
            ;
        }

        public void OnPointerEnter() {
            RectTransform rect = GetComponent<RectTransform>();
            position.x = rect.rect.center.x + rect.position.x;
            position.y = rect.rect.center.y + rect.position.y;
            string s = suffix + " " + Math.Round(slider.value, decimals) + "/" + slider.maxValue + " " + prefix;
            FindObjectOfType<HoverOverScript>().Show(s, position, false, true, null);
        }

        public void OnPointerExit() {
            FindObjectOfType<HoverOverScript>().Unshow();
        }
    }
}