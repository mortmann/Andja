using UnityEngine;
using System.Collections;

public class GS_Fullscreen : GS_SliderBase {

	protected override void OnSliderValueChange() {
		SetFullscreen(System.Convert.ToBoolean(Value));
	}

	void SetFullscreen(bool value) {
		Screen.fullScreen = value;
		// Set the actual slider value. For the OnSliderValueChange() callback
		// this is uneccesary, but it shouldn't cause any harm. We do however
		// need to do it when the value is set from an outside source like the
		// graphics presets.
		slider.value = System.Convert.ToInt16(value);
	}
}
