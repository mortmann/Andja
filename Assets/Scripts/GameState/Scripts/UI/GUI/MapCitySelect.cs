using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;

public class MapCitySelect : MonoBehaviour {
    public Text CityName;
    public Text Number;
    public Toggle toggle;

    public void SelectAs(int number) {
        Number.text = "" + number;
        toggle.isOn = true;
    }

    internal void Unselect() {
        Number.text = "";
        toggle.isOn = false;
    }
}
