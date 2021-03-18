using UnityEngine;

public class GS_AnisotropicFiltering : GS_SliderBase {
    public static bool[] PresetValues = { false, true, true, true };
    public override void OnStart() {
        setting = GraphicsSetting.AnisotropicFiltering;
        if (graphicsSettings.HasSavedGraphicsOption(setting))
            SetAnisotropicFiltering(graphicsSettings.GetSavedGraphicsOptionBool(setting));
    }

    protected override void OnSliderValueChange() {
        SetAnisotropicFiltering(System.Convert.ToBoolean(Value));
    }

    void SetAnisotropicFiltering(bool value) {
        graphicsSettings.SetAnisotropicFiltering(value);
        // Set the actual slider value. For the OnSliderValueChange() callback
        // this is uneccesary, but it shouldn't cause any harm. We do however
        // need to do it when the value is set from an outside source like the
        // graphics presets.
        slider.value = System.Convert.ToInt16(value);
    }
}
