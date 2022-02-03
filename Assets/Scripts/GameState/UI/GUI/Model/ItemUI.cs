using Andja.Controller;
using Andja.Model;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        public bool IsSelected => selectedMarker.enabled;

        public Image image;
        public Text text;
        public Slider slider;
        public Image selectedMarker;
        public bool changeColor = false;
        private UnityAction<PointerEventData> OnClick;
        public Item item;

        public void SetItem(Item i, int maxValue, bool changeColor = false) {
            this.changeColor = changeColor;
            RefreshItem(i);
            ChangeMaxValue(maxValue);
        }

        public void SetSelected(bool select) {
            selectedMarker.enabled = select;
        }

        public void RefreshItem(Item i) {
            if (i == null) {
                image.sprite = null;
                ChangeItemCount(0);
            }
            else {
                ChangeItemCount(i);
                image.sprite = UISpriteController.GetItemImageForID(i.ID);
            }
            item = i;
        }

        public void ChangeItemCount(Item i) {
            ChangeItemCount(i.count);
        }

        public void ChangeItemCount(int amount) {
            text.text = amount + "t";
            slider.value = amount;
            AdjustSliderColor();
        }

        public void ChangeItemCount(float amount) {
            text.text = amount + "t";
            slider.value = amount;
            AdjustSliderColor();
        }

        public void ChangeMaxValue(int maxValue) {
            slider.maxValue = maxValue;
            AdjustSliderColor();
        }

        private void AdjustSliderColor() {
            if (changeColor == false) {
                return;
            }
            if (slider.value / slider.maxValue < 0.2f) {
                slider.GetComponentInChildren<Image>().color = Color.red;
            }
            else {
                slider.GetComponentInChildren<Image>().color = Color.green;
            }
        }

        public void SetInactive(bool inactive) {
            Color c = image.color;
            if (inactive) {
                c.a = 0.5f;
            }
            else {
                c.a = 1;
            }
            image.color = c;
        }
        public void SetMissing(bool missing) {
            if (missing) {
                image.color = new Color(1f,0,0,0.5f);
            }
            else {
                image.color = Color.white;
            }
        }
        public void AddClickListener(UnityAction<PointerEventData> ueb, bool clearAll = false) {
            if (clearAll)
                ClearAllTriggers();
            OnClick += ueb;
        }

        public void ClearAllTriggers() {
            OnClick = null;
        }

        public void OnPointerEnter(PointerEventData eventData) {
            GameObject.FindObjectOfType<ToolTip>().Show(
                item != null ? item.Name : UILanguageController.Instance.GetStaticVariables(StaticLanguageVariables.Empty));
        }

        public void OnPointerExit(PointerEventData eventData) {
            GameObject.FindObjectOfType<ToolTip>().Unshow();
        }

        public void OnPointerClick(PointerEventData eventData) {
            OnClick?.Invoke(eventData);
        }
    }
}