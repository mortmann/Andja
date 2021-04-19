using System;
using UnityEngine;

namespace Andja.UI.Menu {

    public class GS_Monitor : GS_SliderBase {

        public override void OnStart() {
            setting = GraphicsSetting.Monitor;
            slider.maxValue = Display.displays.Length - 1;
            GetComponent<GS_SteppedSlider>().CalculateHandleSize();
            if (graphicsSettings.HasSavedGraphicsOption(setting))
                SetMonitor(graphicsSettings.GetSavedGraphicsOptionInt(setting));
            else
                SetMonitor(Array.FindIndex(Display.displays, x => x == Display.main));
        }

        protected override void OnSliderValueChange() {
            SetMonitor(Value);
        }

        private void SetMonitor(int value) {
            graphicsSettings.SetMonitor(value);
            slider.value = Convert.ToInt16(value);
            tls.ShowNumber(Value);
        }
    }
}