using UnityEngine;

public class GS_Vsync : GS_SliderBase {
    public override void OnStart() {
        setting = GraphicsSetting.Vsync;
        if (graphicsSettings.HasSavedGraphicsOption(setting))
            SetVsync(int.Parse(graphicsSettings.GetSavedGraphicsOption(setting)));
    }
    protected override void GraphicsPresetLow() {
        SetVsync(0);
    }

    protected override void GraphicsPresetMedium() {
        SetVsync(0);
    }

    protected override void GraphicsPresetHigh() {
        SetVsync(0);
    }

    protected override void GraphicsPresetUltra() {
        SetVsync(0);
    }

    protected override void OnSliderValueChange() {
        SetVsync(Value);
    }

    void SetVsync(int value) {
        graphicsSettings.SetVsync(value);
        slider.value = value;
    }
}
