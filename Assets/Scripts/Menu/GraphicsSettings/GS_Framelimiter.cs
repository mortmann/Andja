using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GS_Framelimiter : GS_SliderBase {
    public static int MinimumValue => 30;

    public override void OnStart() {
        slider.minValue = MinimumValue - 1;
        setting = GraphicsSetting.Framelimit;
        tls.SetStaticLanguageVariables(StaticLanguageVariables.Off);
        if (graphicsSettings.HasSavedGraphicsOption(setting))
            SetFramelimiter(graphicsSettings.GetSavedGraphicsOptionInt(setting));
        else
            SetFramelimiter(Screen.currentResolution.refreshRate);
    }
    protected override void OnSliderValueChange() {
        SetFramelimiter(Value);
    }

    void SetFramelimiter(int value) {
        if (value < MinimumValue)
            value = -1;
        graphicsSettings.SetFramelimit(value);
        slider.value = value;
        tls.ShowNumberWithCutoff(value, MinimumValue);
    }
}

