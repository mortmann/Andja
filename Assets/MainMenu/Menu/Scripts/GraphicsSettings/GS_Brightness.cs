using UnityEngine;

public class GS_Brightness : GS_SliderBase {

    protected override void Start() {
        base.Start();
        displayValue.text = Value.ToString() + "%";
    }

    protected override void OnSliderValueChange() {
        cam.GetComponent<Brightness>().brightness = slider.value / 100f;
    }

    protected override void OnSliderValueChangeSetDisplayText() {
        displayValue.text = Value.ToString() + "%";
    }
}
