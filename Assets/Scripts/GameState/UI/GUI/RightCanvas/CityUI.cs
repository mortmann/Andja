using Andja.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class CityUI : MonoBehaviour {
        public NameInputField NameField;
        public City city;
        public ValueNameSetter Income;
        public ValueNameSetter Expanses;
        public ValueNameSetter Balance;
        public ValueNameSetter PeopleCount;
        public Toggle AutoUpgradeHomesToggle;

        private void Start() {
            if (city == null) {
                return;
            }
            AutoUpgradeHomesToggle.isOn = city.AutoUpgradeHomes;
            NameField.SetName(city.Name, OnNameEdit);
            //Make the Name editable
        }

        private void OnNameEdit(string name) {
            city.Name = name;
        }

        public void OnEnableAutoUpgrade(bool change) {
            city.AutoUpgradeHomes = change;
        }

        private void Update() {
            Income.Show(city.income);
            Expanses.Show(city.expanses);
            Balance.Show(city.Balance);
            PeopleCount.Show(city.PopulationCount);
        }
    }
}