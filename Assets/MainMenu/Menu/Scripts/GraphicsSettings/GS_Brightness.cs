using UnityEngine;

public class GS_Brightness : GS_SliderBase {

    protected override void Start() {
        base.Start();
        displayValue.text = Value.ToString() + "%";
    }

    protected override void OnSliderValueChange() {

	}

    protected override void OnSliderValueChangeSetDisplayText() {
        displayValue.text = Value.ToString() + "%";
    }
}
