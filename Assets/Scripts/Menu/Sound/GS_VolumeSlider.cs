﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class GS_VolumeSlider : GS_SliderBase {
    public VolumeType myType;
    public override void OnStart() {
        GetComponent<Slider>().value = FindObjectOfType<MenuAudioManager>().GetVolumeFor(myType);
    }
}