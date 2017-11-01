using UnityEngine;
using UnityStandardAssets.ImageEffects;
using Smaa;

public class GS_AntiAliasing : GS_SliderBase {
	public override void OnStart (){
		setting = Settings.AntiAliasing;
		if(graphicsSettings.hasSavedGraphicsOption(setting))
			SetAntiAliasing (int.Parse (graphicsSettings.getSavedGraphicsOption (setting)));
	}
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
		graphicsSettings.setSavedGraphicsOption (setting,value);
        // Set the actual slider value. For the OnSliderValueChange() callback
        // this is uneccesary, but it shouldn't cause any harm. We do however
        // need to do it when the value is set from an outside source like the
        // graphics presets.
        slider.value = value;
    }
}
