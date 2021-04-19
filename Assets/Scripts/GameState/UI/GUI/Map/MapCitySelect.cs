using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

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
}