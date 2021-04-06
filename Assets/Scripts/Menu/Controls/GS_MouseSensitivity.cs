using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GS_MouseSensitivity : MonoBehaviour {

    public InputField input;
    Slider slider;
    // Use this for initialization
    void Start() {
        slider = GetComponent<Slider>();

        input.onValueChanged.AddListener(OnInputValueChange);
        input.text = "1";
        slider.onValueChanged.AddListener(OnSliderValueChange);
    }

    void OnSliderValueChange(float value) {
        OnValueChange((float)System.Math.Round(value, 2), false);
    }
    void OnValueChange(float value, bool text) {
        if(text)
            KeyInputSettings.SetMouseSensitivity(value);
        else
            input.text = "" + value;
    }
    void OnInputValueChange(string value) {
        if (value == "") {
            return;
        }
        OnValueChange((float)System.Math.Round(float.Parse(value), 2),true);
    }

}
