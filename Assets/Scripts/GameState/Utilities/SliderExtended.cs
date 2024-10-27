using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderExtended : UnityEngine.UI.Slider {
    public void SetValue(float val, bool sendEvent) {
        Set(val, sendEvent);
    }
}
