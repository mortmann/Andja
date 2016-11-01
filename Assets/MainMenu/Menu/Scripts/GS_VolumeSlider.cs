using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public enum VolumeType { Master, SoundEffect, Ambient, Music, UI}

public class GS_VolumeSlider : GS_SliderBase {
	public VolumeType myType;
	public override void OnStart(){
		GetComponent<Slider> ().value = FindObjectOfType<MenuAudioManager> ().getVolumeFor(myType);
	}
}
