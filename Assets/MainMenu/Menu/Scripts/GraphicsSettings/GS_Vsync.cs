using UnityEngine;

public class GS_Vsync : GS_SliderBase {

    protected override void GraphicsPresetLow() {
        SetVsync(0);
    }

    protected override void GraphicsPresetMedium() {
        SetVsync(0);
    }

    protected override void GraphicsPresetHigh() {
        SetVsync(0);
    }

    protected override void GraphicsPresetUltra() {
        SetVsync(0);
    }

    protected override void OnSliderValueChange() {
        SetVsync(Value);
    }

    void SetVsync(int value) {
        QualitySettings.vSyncCount = value;

        // Set the actual slider value. For the OnSliderValueChange() callback
        // this is uneccesary, but it shouldn't cause any harm. We do however
        // need to do it when the value is set from an outside source like the
        // graphics presets.
        slider.value = value;
    }
}
