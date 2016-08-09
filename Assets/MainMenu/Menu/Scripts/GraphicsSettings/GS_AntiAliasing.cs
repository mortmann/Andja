using UnityEngine;
using UnityStandardAssets.ImageEffects;
using Smaa;

public class GS_AntiAliasing : GS_SliderBase {

    protected override void GraphicsPresetLow() {
        SetAntiAliasing(0);
    }

    protected override void GraphicsPresetMedium() {
        SetAntiAliasing(1);
    }

    protected override void GraphicsPresetHigh() {
        SetAntiAliasing(2);
    }

    protected override void GraphicsPresetUltra() {
        SetAntiAliasing(2);
    }

    protected override void OnSliderValueChange() {
        SetAntiAliasing(Value);
    }

    void SetAntiAliasing(int value) {
        if (value == 2) {
            cam.GetComponent<Antialiasing>().enabled = false;
            cam.GetComponent<SMAA>().enabled = true;
        }
        else if (value == 1) {
            cam.GetComponent<Antialiasing>().enabled = true;
            cam.GetComponent<SMAA>().enabled = false;
        }
        else {
            cam.GetComponent<Antialiasing>().enabled = false;
            cam.GetComponent<SMAA>().enabled = false;
        }

        // Set the actual slider value. For the OnSliderValueChange() callback
        // this is uneccesary, but it shouldn't cause any harm. We do however
        // need to do it when the value is set from an outside source like the
        // graphics presets.
        slider.value = value;
    }
}
