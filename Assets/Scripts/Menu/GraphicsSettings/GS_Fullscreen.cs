using UnityEngine;
using System.Collections;


public class GS_Fullscreen : GS_SliderBase {
    public override void OnStart() {
        setting = GraphicsSetting.Fullscreen;
        tls.valueEnumType = typeof(FullScreenMode);
        //tls.preValueString = "GraphicsSettings";
        if (graphicsSettings.HasSavedGraphicsOption(setting))
            SetFullscreen(graphicsSettings.GetSavedGraphicsOptionInt(setting));
        else
            SetFullscreen((int)FullScreenMode.ExclusiveFullScreen);
    }
    protected override void OnSliderValueChange() {
        SetFullscreen(Value);
    }

    void SetFullscreen(int value) {
        graphicsSettings.SetFullscreen(value);
        slider.value = System.Convert.ToInt16(value);
        tls.ShowValue(Value);
    }
}

