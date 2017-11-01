using UnityEngine;
using System.Collections;

public class GS_Fullscreen : GS_SliderBase {
	public override void OnStart (){
		setting = Settings.Fullscreen;
		if(graphicsSettings.hasSavedGraphicsOption(setting))
			SetFullscreen (bool.Parse (graphicsSettings.getSavedGraphicsOption (setting)));
	}
	protected override void OnSliderValueChange() {
		SetFullscreen(System.Convert.ToBoolean(Value));
	}

	void SetFullscreen(bool value) {
		graphicsSettings.setSavedGraphicsOption (setting,value);
		slider.value = System.Convert.ToInt16(value);
	}
}
