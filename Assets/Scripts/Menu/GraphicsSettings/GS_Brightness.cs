using UnityEngine;

public class GS_Brightness : GS_SliderBase {

    public override void OnStart() {
        setting = GraphicsSetting.Brightness;
        if (graphicsSettings.HasSavedGraphicsOption(setting))
            slider.value = (float.Parse(graphicsSettings.GetSavedGraphicsOption(setting)));
        displayValue.text = Value.ToString() + "%";
    }

    protected override void OnSliderValueChange() {
        graphicsSettings.SetBrightness(slider.value);
    }

    protected override void OnSliderValueChangeSetDisplayText() {
        displayValue.text = Value.ToString() + "%";
    }
}
