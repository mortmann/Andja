using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UnitBuildUI : MonoBehaviour {
    public Image unitImage;
    public Text nameOfUnit;
    public Unit unit;
    public Button button;
    public void Show(Unit u) {
        if (u == null) {
            nameOfUnit.text = "";
            return;
        }
        unit = u;
        nameOfUnit.text = u.Name;
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry enter = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerEnter
        };
        enter.callback.AddListener((data) => {
            OnMouseEnter();
        });
        trigger.triggers.Add(enter);
        EventTrigger.Entry exit = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerExit
        };
        exit.callback.AddListener((data) => {
            OnMouseExit();
        });
        trigger.triggers.Add(exit);
    }
    public void OnMouseEnter() {
        string descriptiontemp = "This Unit costs: ";
        foreach (Item i in unit.BuildingItems) {
            descriptiontemp += i.ToSmallString();
        }
        GameObject.FindObjectOfType<HoverOverScript>().Show(unit.Name, descriptiontemp);
    }
    public void OnMouseExit() {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
    public void AddClickListener(UnityAction ueb, bool clearAll = false) {
        //EventTrigger trigger = GetComponent<EventTrigger>();
        //EventTrigger.Entry entry = new EventTrigger.Entry {
        //    eventID = EventTriggerType.PointerClick
        //};
        if (clearAll) {
            ClearAllTriggers();
        }
        //entry.callback.AddListener(ueb);
        //trigger.triggers.Add(entry);
        button.onClick.AddListener(ueb);
    }
    public void ClearAllTriggers() {
        button.onClick.RemoveAllListeners();
    }
    public void SetIsBuildable(bool canbuild) {
        button.interactable = canbuild;
    }
}
