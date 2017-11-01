using UnityEngine;

public class GS_Brightness : GS_SliderBase {

	public override void OnStart() {
		setting = Settings.Brightness;
		if(graphicsSettings.hasSavedGraphicsOption(setting))
			slider.value = (float.Parse (graphicsSettings.getSavedGraphicsOption (setting)));
        displayValue.text = Value.ToString() + "%";
    }

    protected override void OnSliderValueChange() {
		graphicsSettings.setSavedGraphicsOption (setting,slider.value);
	}

    protected override void OnSliderValueChangeSetDisplayText() {
        displayValue.text = Value.ToString() + "%";
    }
}
