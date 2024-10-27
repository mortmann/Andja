using Andja.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class CityUI : MonoBehaviour {
        public HiddenInputField NameField;
        public ICity city;
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
            NameField.Set(city.Name, city.SetName);
            //Make the Name editable
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
}