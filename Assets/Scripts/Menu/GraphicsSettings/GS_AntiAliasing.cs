namespace Andja.UI.Menu {

    public class GS_AntiAliasing : GS_SliderBase {
        public static int[] PresetValues = { 0, 1, 2, 2 };

        public override void OnStart() {
            TextLanguageSetter tls = GetComponent<TextLanguageSetter>();
            setting = GraphicsSetting.AntiAliasing;
            if (graphicsSettings.HasSavedGraphicsOption(setting))
                SetAntiAliasing(graphicsSettings.GetSavedGraphicsOptionInt(setting));
        }

        protected override void OnSliderValueChange() {
            SetAntiAliasing(Value);
        }

        private void SetAntiAliasing(int value) {
            graphicsSettings.SetSavedGraphicsOption(setting, value);
            // Set the actual slider value. For the OnSliderValueChange() callback
            // this is uneccesary, but it shouldn't cause any harm. We do however
            // need to do it when the value is set from an outside source like the
            // graphics presets.
            slider.value = value;
        }
    }
}