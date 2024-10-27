using Andja.Controller;
using System;
using UnityEngine;

namespace Andja.UI.Menu {

    public class GS_Framelimiter : GS_SliderBase {
        public static int MinimumValue => 30;

        public override void OnStart() {
            slider.minValue = MinimumValue - 1;
            setting = GraphicsSetting.Framelimit;
            tls.SetStaticLanguageVariables(StaticLanguageVariables.Off);
            if (graphicsSettings.HasSavedGraphicsOption(setting))
                SetFramelimiter(graphicsSettings.GetSavedGraphicsOptionInt(setting));
            else
                SetFramelimiter(Convert.ToInt32(Screen.currentResolution.refreshRateRatio.value));
        }

        protected override void OnSliderValueChange() {
            SetFramelimiter(Value);
        }

        private void SetFramelimiter(int value) {
            if (value < MinimumValue)
                value = -1;
            graphicsSettings.SetFramelimit(value);
            slider.value = value;
            tls.ShowNumberWithCutoff(value, MinimumValue);
        }
    }
}