using UnityEngine;
using System.Collections;


public class GS_Fullscreen : GS_SliderBase {
    public override void OnStart() {
        setting = GraphicsSetting.Fullscreen;
        if (graphicsSettings.HasSavedGraphicsOption(setting))
            SetFullscreen(int.Parse(graphicsSettings.GetSavedGraphicsOption(setting)));
    }
    protected override void OnSliderValueChange() {
        SetFullscreen(Value);
    }

    void SetFullscreen(int value) {
        graphicsSettings.SetFullscreen(value);
        displayValue.text = (FullScreenMode)value+"";
        slider.value = System.Convert.ToInt16(value);
    }
}

