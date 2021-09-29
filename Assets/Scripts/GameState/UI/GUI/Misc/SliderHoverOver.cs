using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    [RequireComponent(typeof(Slider))]
    public class SliderHoverOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        private Slider slider;
        public int decimals = 1;
        public string suffix;
        public string prefix;
        private Vector3 position;

        public void Start() {
            slider = GetComponent<Slider>();
        }

        public void OnPointerEnter(PointerEventData eventData) {
            RectTransform rect = GetComponent<RectTransform>();
            position.x = rect.rect.center.x + rect.position.x;
            position.y = rect.rect.center.y + rect.position.y;
            string s = suffix + " " + Math.Round(slider.value, decimals) + "/" + slider.maxValue + " " + prefix;
            FindObjectOfType<ToolTip>().Show(s, position, false, true, null);
        }

        public void OnPointerExit(PointerEventData eventData) {
            FindObjectOfType<ToolTip>().Unshow();
        }
    }
}