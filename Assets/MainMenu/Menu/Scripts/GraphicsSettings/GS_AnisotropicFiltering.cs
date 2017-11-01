﻿using UnityEngine;

public class GS_AnisotropicFiltering : GS_SliderBase {
	public override void OnStart (){
		setting = Settings.AnisotropicFiltering;
		if(graphicsSettings.hasSavedGraphicsOption(setting))
			SetAnisotropicFiltering (bool.Parse (graphicsSettings.getSavedGraphicsOption (setting)));
	}
    protected override void GraphicsPresetLow() {
        SetAnisotropicFiltering(false);
    }

    protected override void GraphicsPresetMedium() {
        SetAnisotropicFiltering(true);
    }

    protected override void GraphicsPresetHigh() {
        SetAnisotropicFiltering(true);
    }

    protected override void GraphicsPresetUltra() {
        SetAnisotropicFiltering(true);
    }

    protected override void OnSliderValueChange() {
        SetAnisotropicFiltering(System.Convert.ToBoolean(Value));
    }

    void SetAnisotropicFiltering(bool value) {
		graphicsSettings.setSavedGraphicsOption (setting,value);
		// Set the actual slider value. For the OnSliderValueChange() callback
        // this is uneccesary, but it shouldn't cause any harm. We do however
        // need to do it when the value is set from an outside source like the
        // graphics presets.
        slider.value = System.Convert.ToInt16(value);
    }
}
