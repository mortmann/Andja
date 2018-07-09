using UnityEngine;

public class GS_TextureQuality : GS_SliderBase {

	public override void OnStart (){
		setting = GraphicsSetting.TextureQuality;
		if(graphicsSettings.HasSavedGraphicsOption(setting))
			SetTextureQuality (int.Parse (graphicsSettings.GetSavedGraphicsOption (setting)));
	}
	protected override void GraphicsPresetLow() { 
        SetTextureQuality(0);
    }

    protected override void GraphicsPresetMedium() {
        SetTextureQuality(1);
    }

    protected override void GraphicsPresetHigh() {
        SetTextureQuality(2);
    }

    protected override void GraphicsPresetUltra() {
        SetTextureQuality(3);
    }

    protected override void OnSliderValueChange() {
        SetTextureQuality(Value);
    }

    void SetTextureQuality(int value) {
		graphicsSettings.SetSavedGraphicsOption (setting,value);
        // Set the actual slider value. For the OnSliderValueChange() callback
        // this is uneccesary, but it shouldn't cause any harm. We do however
        // need to do it when the value is set from an outside source like the
        // graphics presets.
        slider.value = value;
    }
}
