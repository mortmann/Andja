using Andja.Controller;

namespace Andja.UI.Menu {

    public class GS_Vsync : GS_SliderBase {

        public override void OnStart() {
            setting = GraphicsSetting.Vsync;
            tls.SetStaticLanguageVariables(StaticLanguageVariables.Off, StaticLanguageVariables.On);
            if (graphicsSettings.HasSavedGraphicsOption(setting))
                SetVsync(graphicsSettings.GetSavedGraphicsOptionInt(setting));
            else
                SetVsync(0);
        }

        protected override void OnSliderValueChange() {
            SetVsync(Value);
        }

        private void SetVsync(int value) {
            graphicsSettings.SetVsync(value);
            slider.value = value;
            tls.ShowValue(value);
        }
    }
}