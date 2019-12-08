using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;

public enum VolumeType { MasterVolume, SoundEffectsVolume, AmbientVolume, MusicVolume, UIVolume }

public class MenuAudioManager : MonoBehaviour {
    public static string fileName = "volume.ini";
    public AudioMixer mixer;
    public static MenuAudioManager Instance;

    public AudioClip hoverSound;
    public AudioClip sliderSound;
    public AudioClip clickSound;

    Dictionary<VolumeType, int> volumes;

    void Start() {
        Instance = this;
        volumes = new Dictionary<VolumeType, int>();
        OptionsToggle.ChangedState += OnOptionClosed;
        ReadVolumes();
    }

    private void OnOptionClosed(bool open) {
        if (open)
            return;
        SaveVolumes();
    }

    public void PlayClickSound() {
        GetComponentInChildren<AudioSource>().clip = clickSound;
        GetComponentInChildren<AudioSource>().Play();
    }
    public void PlayHoverSound() {
        GetComponentInChildren<AudioSource>().clip = hoverSound;
        GetComponentInChildren<AudioSource>().Play();
    }
    public void PlaySliderSound() {
        GetComponentInChildren<AudioSource>().clip = sliderSound;
        GetComponentInChildren<AudioSource>().Play();
    }

    public void SetVolume(float value, VolumeType type) {
        mixer.SetFloat(type.ToString(), ConvertToDecibel(value));
        volumes[type] = Mathf.RoundToInt(value);
    }
    public float GetVolumeFor(VolumeType volType) {
        if (volumes.ContainsKey(volType) == false) {
            Debug.LogError("VolumeType not in Dictionary");
            return -1;
        }
        return volumes[volType];
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
    public void SaveVolumes() {
        string path = Application.dataPath.Replace("/Assets", "");
        if (Directory.Exists(path) == false) {
            // NOTE: This can throw an exception if we can't create the folder,
            // but why would this ever happen? We should, by definition, have the ability
            // to write to our persistent data folder unless something is REALLY broken
            // with the computer/device we're running on.
            Directory.CreateDirectory(path);
        }
        if (volumes == null) {
            return;
        }
        string filePath = System.IO.Path.Combine(path, fileName);
        File.WriteAllText(filePath, JsonConvert.SerializeObject(volumes));
    }


    public void ReadVolumes() {
        string filePath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), fileName);
        if (File.Exists(filePath) == false) {
            SetVolume(100,VolumeType.MasterVolume);
            SetVolume(50, VolumeType.MusicVolume);
            SetVolume(80, VolumeType.AmbientVolume);
            SetVolume(50, VolumeType.UIVolume);
            SetVolume(70, VolumeType.SoundEffectsVolume);
            return;
        }
        volumes = JsonConvert.DeserializeObject<Dictionary<VolumeType, int>>(File.ReadAllText(filePath));
        SetVolume(GetVolumeFor(VolumeType.AmbientVolume), VolumeType.AmbientVolume);
        SetVolume(GetVolumeFor(VolumeType.MasterVolume), VolumeType.MasterVolume);
        SetVolume(GetVolumeFor(VolumeType.MusicVolume), VolumeType.MusicVolume);
        SetVolume(GetVolumeFor(VolumeType.UIVolume), VolumeType.UIVolume);
        SetVolume(GetVolumeFor(VolumeType.SoundEffectsVolume), VolumeType.SoundEffectsVolume);
    }
    public static Dictionary<string, int> StaticReadSoundVolumes() {
        string filePath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), fileName);
        if (File.Exists(filePath) == false) {
            return null;
        }
        return JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(filePath));
    }

}
