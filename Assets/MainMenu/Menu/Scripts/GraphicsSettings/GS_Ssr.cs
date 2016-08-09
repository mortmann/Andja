using UnityEngine;

public class GS_Ssr : GS_SliderBase {

    protected override void GraphicsPresetLow() {
        SetSsr(false);
    }

    protected override void GraphicsPresetMedium() {
        SetSsr(false);
    }

    protected override void GraphicsPresetHigh() {
        SetSsr(false);
    }

    protected override void GraphicsPresetUltra() {
        SetSsr(true);
    }

    protected override void OnSliderValueChange() {
        SetSsr(System.Convert.ToBoolean(Value));
    }

    void SetSsr(bool value) {
		cam.GetComponent<SSR>().enabled = value;
        // Set the actual slider value. For the OnSliderValueChange() callback
        // this is uneccesary, but it shouldn't cause any harm. We do however
        // need to do it when the value is set from an outside source like the
        // graphics presets.
        slider.value = System.Convert.ToInt16(value);
    }
}
