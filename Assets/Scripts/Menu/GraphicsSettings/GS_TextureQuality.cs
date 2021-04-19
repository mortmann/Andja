namespace Andja.UI.Menu {

    public class GS_TextureQuality : GS_SliderBase {
        public static int[] PresetValues = { 0, 1, 2, 3 };

        public override void OnStart() {
            tls.valueEnumType = typeof(GraphicsOptions);
            //tls.preValueString = "GraphicsSettings";
            setting = GraphicsSetting.TextureQuality;
            slider.value = graphicsSettings.GetSavedGraphicsOptionInt(setting);
        }

        protected override void OnSliderValueChange() {
            SetTextureQuality(Value);
        }

        protected override void OnGraphicsPresetChange(int value) {
            SetTextureQuality(PresetValues[value]);
        }

        private void SetTextureQuality(int value) {
            graphicsSettings.SetTextureQuality(value);
            // Set the actual slider value. For the OnSliderValueChange() callback
            // this is uneccesary, but it shouldn't cause any harm. We do however
            // need to do it when the value is set from an outside source like the
            // graphics presets.
            slider.value = value;
            tls.ShowValue(value);
        }
    }
}