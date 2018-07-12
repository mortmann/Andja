using UnityEngine;
using System.Collections;

public class GS_Fullscreen : GS_SliderBase {
	public override void OnStart (){
		setting = GraphicsSetting.Fullscreen;
		if(graphicsSettings.HasSavedGraphicsOption(setting))
			SetFullscreen (bool.Parse (graphicsSettings.GetSavedGraphicsOption (setting)));
	}
	protected override void OnSliderValueChange() {
		SetFullscreen(System.Convert.ToBoolean(Value));
	}

	void SetFullscreen(bool value) {
		graphicsSettings.SetSavedGraphicsOption (setting,value);
        MouseController.autorotate = value;
        slider.value = System.Convert.ToInt16(value);
	}
}
