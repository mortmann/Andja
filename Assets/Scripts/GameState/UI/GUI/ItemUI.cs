using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public Image image;
    public Text text;
    public Slider slider;
    public Image selectedMarker;
    public bool changeColor = false;
    Action OnClick;
    public bool IsSelected => selectedMarker.enabled;
    Item item;
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
            ChangeItemCount(0);
        }
        else {
            ChangeItemCount(i);
            image.sprite = UIController.GetItemImageForID(i.ID);
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
    void AdjustSliderColor() {
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
    public void AddClickListener(UnityAction<BaseEventData> ueb, bool clearAll = false) {
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerClick
        };
        if (clearAll) {
            ClearAllTriggers();
        }
        entry.callback.AddListener(ueb);
        trigger.triggers.Add(entry);
    }
    public void ClearAllTriggers() {
        EventTrigger trigger = GetComponent<EventTrigger>();
        trigger.triggers.Clear();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        GameObject.FindObjectOfType<HoverOverScript>().Show(
            item != null ? item.Name : UILanguageController.Instance.GetStaticVariables(StaticLanguageVariables.Empty)[0]);
    }

    public void OnPointerExit(PointerEventData eventData) {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
}
