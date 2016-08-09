using UnityEngine;

public class GS_TextureQuality : GS_SliderBase {

    protected override void GraphicsPresetLow() {
        SetTextureQuality(0);
    }

    protected override void GraphicsPresetMedium() {
        SetTextureQuality(1);
    }

    protected override void GraphicsPresetHigh() {
        SetTextureQuality(2);
    }

    protected override void GraphicsPresetUltra() {
        SetTextureQuality(3);
    }

    protected override void OnSliderValueChange() {
        SetTextureQuality(Value);
    }

    void SetTextureQuality(int value) {
        // In the quality settings 0 is full quality textures, while 3 is the lowest.
        QualitySettings.masterTextureLimit = 3 - value;

        // Set the actual slider value. For the OnSliderValueChange() callback
        // this is uneccesary, but it shouldn't cause any harm. We do however
        // need to do it when the value is set from an outside source like the
        // graphics presets.
        slider.value = value;
    }
}
