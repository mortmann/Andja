using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CityUI : MonoBehaviour {

    public InputField NameText;
    public City city;
    public ValueNameSetter Income;
    public ValueNameSetter Expanses;
    public ValueNameSetter Balance;
    public ValueNameSetter PeopleCount;
    public Toggle AutoUpgradeHomesToggle;

    void Start() {
        if(city == null) {
            return;
        }
        AutoUpgradeHomesToggle.isOn = city.AutoUpgradeHomes;
        //Make the Name editable
        NameText.onEndEdit.AddListener(OnNameEdit);
        NameText.readOnly = true;
        NameText.interactable = false;
        NameText.text = city.Name;
        EventTrigger trigger = NameText.GetComponent<EventTrigger>();
        EventTrigger.Entry click = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerClick
        };
        click.callback.AddListener((data) => {
            PointerEventData ped = ((PointerEventData)data);
            if (ped.button == PointerEventData.InputButton.Right)
                OnInputFieldClick();
        });
        trigger.triggers.Add(click);
    }

    private void OnInputFieldClick() {
        if (NameText.readOnly == false)
            return;
        NameText.readOnly = false;
        NameText.interactable = true;
        NameText.Select();
    }

    private void OnNameEdit(string name) {
        city.Name = name;
        NameText.readOnly = true;
    }
    public void OnEnableAutoUpgrade(bool change) {
        city.AutoUpgradeHomes = change;
    }
    private void Update() {
        Income.Show(city.Income);
        Expanses.Show(city.Expanses);
        Balance.Show(city.Balance);
        PeopleCount.Show(city.PopulationCount);
    }
}
