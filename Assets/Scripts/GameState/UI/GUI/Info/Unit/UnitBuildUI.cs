﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UnitBuildUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
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
    }
    public void OnPointerEnter(PointerEventData eventData) {
        string descriptiontemp = "This Unit costs: ";
        foreach (Item i in unit.BuildingItems) {
            descriptiontemp += i.ToSmallString();
        }
        GameObject.FindObjectOfType<HoverOverScript>().Show(unit.Name, descriptiontemp);
    }
    public void OnPointerExit(PointerEventData eventData) {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
    public void AddClickListener(UnityAction ueb, bool clearAll = false) {
        if (clearAll) {
            ClearAllTriggers();
        }
        button.onClick.AddListener(ueb);
    }
    public void ClearAllTriggers() {
        button.onClick.RemoveAllListeners();
    }
    public void SetIsBuildable(bool canbuild) {
        button.interactable = canbuild;
    }
}
