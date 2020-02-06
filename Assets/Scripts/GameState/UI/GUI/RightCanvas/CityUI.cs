using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CityUI : MonoBehaviour {

    public NameInputField NameField;
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
        AutoUpgradeHomesToggle.isOn = city.autoUpgradeHomes;
        NameField.SetName(city.Name, OnNameEdit);
        //Make the Name editable
    }


    private void OnNameEdit(string name) {
        city.Name = name;
    }
    public void OnEnableAutoUpgrade(bool change) {
        city.autoUpgradeHomes = change;
    }
    private void Update() {
        Income.Show(city.income);
        Expanses.Show(city.expanses);
        Balance.Show(city.Balance);
        PeopleCount.Show(city.PopulationCount);
    }
}
