using UnityEngine;

namespace Andja.UI.Menu {

    public class GS_VolumeSlider : GS_SliderBase {
        public VolumeType myType;

        public override void OnStart() {
            slider.value = MenuAudioManager.Instance.GetVolumeFor(myType);
        }

        protected override void OnSliderValueChange() {
            MenuAudioManager.Instance.SetVolumeFor(myType, slider.value);
        }

        protected override void OnSliderValueChangeSetDisplayText() {
            tls.ShowNumber(Mathf.RoundToInt(slider.value));
        }
    }
}