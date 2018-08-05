using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;

public enum VolumeType { MasterVolume, SoundEffectsVolume, AmbientVolume, MusicVolume, UIVolume}

public class MenuAudioManager : MonoBehaviour {
	public static string fileName="volume.ini";
    public AudioMixer mixer;	
    public static MenuAudioManager instance;

    public AudioClip hoverSound;
    public AudioClip sliderSound;
    public AudioClip clickSound;

	Dictionary<string,int> volumes;

	void Start() {
        instance = this;
		volumes = new Dictionary<string, int> ();
		ReadSoundVolumes ();
    }

    public void PlayClickSound() {
        GetComponent<AudioSource>().clip = clickSound;
        GetComponent<AudioSource>().Play();
    }
    public void PlayHoverSound() {
        GetComponent<AudioSource>().clip = hoverSound;
        GetComponent<AudioSource>().Play();
    }
    public void PlaySliderSound() {
        GetComponent<AudioSource>().clip = sliderSound;
        GetComponent<AudioSource>().Play();
    }

    public void SetMusicVolume(float value) {
		mixer.SetFloat(VolumeType.MusicVolume.ToString(), ConvertToDecibel(value));
		volumes[VolumeType.MusicVolume.ToString()] = Mathf.RoundToInt (value);
    }
    public void SetSoundEffectsVolume(float value) {
		mixer.SetFloat(VolumeType.SoundEffectsVolume.ToString(), ConvertToDecibel(value));
		volumes[VolumeType.SoundEffectsVolume.ToString()] = Mathf.RoundToInt (value);
    }
	public void SetMasterVolume(float value) {
		mixer.SetFloat(VolumeType.MasterVolume.ToString(), ConvertToDecibel(value));
		volumes[VolumeType.MasterVolume.ToString()] = Mathf.RoundToInt (value);
    }
    public void SetAmbientVolume(float value) {
		mixer.SetFloat(VolumeType.AmbientVolume.ToString(), ConvertToDecibel(value));
		volumes[VolumeType.AmbientVolume.ToString()] = Mathf.RoundToInt (value);
    }
	public void SetUIVolume(float value) { 
		mixer.SetFloat (VolumeType.UIVolume.ToString(), ConvertToDecibel (value));
		volumes[VolumeType.UIVolume.ToString()] = Mathf.RoundToInt (value);
    }

	public float GetVolumeFor(VolumeType volType){
		if(volumes.ContainsKey(volType.ToString())==false){
			Debug.LogError ("VolumeType not in Dictionary");
			return -1;
		}
		return volumes[volType.ToString()];
	}

    /**
     * Convert the value coming from our sliders to a decibel value we can
     * feed into the audio mixer.
     */
    public static float ConvertToDecibel(float value) {
        // Log(0) is undefined so we just set it by default to -80 decibels
        // which is 0 volume in the audio mixer.
        float decibel = -80f;

        // I think the correct formula is Mathf.Log10(value / 100f) * 20f.
        // Using that yields -6dB at 50% on the slider which is I think is half
        // volume, but I don't feel like it sounds like half volume. :p And I also
        // felt this homemade formula sounds more natural/linear when you go towards 0.
        // Note: We use 0-100 for our volume sliders in the menu, hence the
        // divide by 100 in the equation. If you use 0-1 instead you would remove that.
        if (value > 0) {
            decibel = Mathf.Log(value / 100f) * 17f;
        }

        return decibel;
    }
	public void SaveVolumetSchema(){
		string path = Application.dataPath.Replace ("/Assets", "");
		if( Directory.Exists(path ) == false ) {
			// NOTE: This can throw an exception if we can't create the folder,
			// but why would this ever happen? We should, by definition, have the ability
			// to write to our persistent data folder unless something is REALLY broken
			// with the computer/device we're running on.
			Directory.CreateDirectory( path  );
		}
		if(volumes==null){
			return;
		}
		string filePath = System.IO.Path.Combine(path,fileName) ;
		File.WriteAllText( filePath, JsonConvert.SerializeObject(volumes) );
	}


	public void ReadSoundVolumes(){
		string filePath = System.IO.Path.Combine(Application.dataPath.Replace ("/Assets",""),fileName) ;
		if(File.Exists (filePath)==false){
			SetMasterVolume (100);
			SetMusicVolume (100);
			SetUIVolume (100);
			SetSoundEffectsVolume (100);
			SetAmbientVolume (100);
			return;
		}
		volumes = JsonConvert.DeserializeObject<Dictionary<string,int>> (File.ReadAllText (filePath));
		SetAmbientVolume (GetVolumeFor (VolumeType.AmbientVolume));
		SetMasterVolume (GetVolumeFor (VolumeType.MasterVolume));
		SetMusicVolume (GetVolumeFor (VolumeType.MusicVolume));
		SetUIVolume (GetVolumeFor (VolumeType.UIVolume));
		SetSoundEffectsVolume (GetVolumeFor (VolumeType.SoundEffectsVolume));
	}
	public static Dictionary<string,int> StaticReadSoundVolumes(){
		string filePath = System.IO.Path.Combine(Application.dataPath.Replace ("/Assets",""),fileName) ;
		if(File.Exists (filePath)==false){
			return null;
		}
		return JsonConvert.DeserializeObject<Dictionary<string,int>> (File.ReadAllText (filePath));
	}
	void OnDisable(){
		SaveVolumetSchema ();
	}


}
