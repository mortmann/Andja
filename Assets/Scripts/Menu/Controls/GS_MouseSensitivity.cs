using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class GS_MouseSensitivity : MonoBehaviour {
        public InputField input;
        private Slider slider;

        // Use this for initialization
        private void Start() {
            slider = GetComponent<Slider>();

            input.onValueChanged.AddListener(OnInputValueChange);
            input.text = "1";
            slider.onValueChanged.AddListener(OnSliderValueChange);
        }

        private void OnSliderValueChange(float value) {
            OnValueChange((float)System.Math.Round(value, 2), false);
        }

        private void OnValueChange(float value, bool text) {
            if (text)
                KeyInputSettings.SetMouseSensitivity(value);
            else
                input.text = "" + value;
        }

        private void OnInputValueChange(string value) {
            if (value == "") {
                return;
            }
            OnValueChange((float)System.Math.Round(float.Parse(value), 2), true);
        }
    }
}