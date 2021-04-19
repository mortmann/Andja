using UnityEngine;

namespace Andja.UI.Menu {

    public class GS_Brightness : GS_SliderBase {

        public override void OnStart() {
            setting = GraphicsSetting.Brightness;
            if (graphicsSettings.HasSavedGraphicsOption(setting))
                slider.value = (graphicsSettings.GetSavedGraphicsOptionFloat(setting));
            else
                slider.value = 100;
            OnSliderValueChange();
            displayValue.text = Value.ToString() + "%";
        }

        protected override void OnSliderValueChange() {
            graphicsSettings.SetBrightness(Mathf.RoundToInt(slider.value));
        }

        protected override void OnSliderValueChangeSetDisplayText() {
            displayValue.text = Value.ToString() + "%";
        }
    }
}