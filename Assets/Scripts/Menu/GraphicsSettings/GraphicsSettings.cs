using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;
using UnityStandardAssets.ImageEffects;
using Smaa;

public enum GraphicsSetting { Preset, AnisotropicFiltering, AntiAliasing, Vsync, Fullscreen, Resolution, TextureQuality, Brightness }

public class GraphicsSettings : MonoBehaviour {

    [HideInInspector]
    public UnityEvent lowPresetEvent;
    [HideInInspector]
    public UnityEvent mediumPresetEvent;
    [HideInInspector]
    public UnityEvent highPresetEvent;
    [HideInInspector]
    public UnityEvent ultraPresetEvent;

    // A reference to the slider for setting graphics preset so we can update it.
    public Slider presetSlider;
    // The camera in use.
    protected Camera cam;
    bool hasLoaded = false;
    // All the settings register their listeners in Awake().
    void Awake() {
        //IF we succesful loaded saved data no need to set it to a preset
        graphicsOptions = new Dictionary<GraphicsSetting, string>();
        graphicsOptionsToSave = new Dictionary<GraphicsSetting, string>();
        hasLoaded = (ReadGraphicsOption());
        lowPresetEvent = new UnityEvent();
        mediumPresetEvent = new UnityEvent();
        highPresetEvent = new UnityEvent();
        ultraPresetEvent = new UnityEvent();
        presetSlider.onValueChanged.AddListener(SetGraphicsPreset);
        OptionsToggle.ChangedState += OnOptionChanged;
    }

    // So we can invoke safely invoke them in Start().
    void Start() {
        
    }

    public void SetGraphicsPreset(float value) {
        switch ((int)value) {
            // Low.
            case 0:
                lowPresetEvent.Invoke();
                break;
            // Medium.
            case 1:
                mediumPresetEvent.Invoke();
                break;
            // High.
            case 2:
                highPresetEvent.Invoke();
                break;
            // Ultra.
            case 3:
                ultraPresetEvent.Invoke();
                break;
            // Custom. Do nothing.
            default:
                break;
        }
        presetSlider.value = value;
    }

    Dictionary<GraphicsSetting, string> graphicsOptions;
    Dictionary<GraphicsSetting, string> graphicsOptionsToSave;

    public void SetToCustom() {
        presetSlider.value = 4;
    }
    string fileName = "graphics.ini";
    public void SaveGraphicsOption() {

        SetOptions(graphicsOptionsToSave);
        foreach (GraphicsSetting s in graphicsOptionsToSave.Keys) {
            graphicsOptions[s] = graphicsOptionsToSave[s];
        }
        graphicsOptionsToSave.Clear();
        graphicsOptions[GraphicsSetting.Preset] = presetSlider.value.ToString();

        string path = Application.dataPath.Replace("/Assets", "");
        if (Directory.Exists(path) == false) {
            // NOTE: This can throw an exception if we can't create the folder,
            // but why would this ever happen? We should, by definition, have the ability
            // to write to our persistent data folder unless something is REALLY broken
            // with the computer/device we're running on.
            Directory.CreateDirectory(path);
        }
        if (graphicsOptions == null) {
            return;
        }
        string filePath = System.IO.Path.Combine(path, fileName);
        File.WriteAllText(filePath, JsonConvert.SerializeObject(graphicsOptions));
    }
    public bool ReadGraphicsOption() {
        string filePath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), fileName);
        if (File.Exists(filePath) == false) {
            return false;
        }
        graphicsOptions = JsonConvert.DeserializeObject<Dictionary<GraphicsSetting, string>>(File.ReadAllText(filePath));
        SetOptions(graphicsOptions);
        return true;
    }

    private void SetOptions(Dictionary<GraphicsSetting, string> options) {
        foreach (GraphicsSetting optionName in options.Keys) {
            string val = options[optionName];
            switch (optionName) {
                case GraphicsSetting.Preset:
                    SetGraphicsPreset(int.Parse(val));
                    break;
                case GraphicsSetting.AnisotropicFiltering:
                    SetAnisotropicFiltering(bool.Parse(val));
                    break;
                case GraphicsSetting.AntiAliasing:
                    SetAntiAliasing(int.Parse(val));
                    break;
                case GraphicsSetting.Brightness:
                    SetBrightness(float.Parse(val));
                    break;
                case GraphicsSetting.Fullscreen:
                    SetFullscreen(bool.Parse(val));
                    break;
                case GraphicsSetting.Resolution:
                    SetResolution(JsonUtility.FromJson<CustomResolution>(val));
                    break;
                case GraphicsSetting.TextureQuality:
                    SetTextureQuality(int.Parse(val));
                    break;
                case GraphicsSetting.Vsync:
                    SetVsync(int.Parse(val));
                    break;
                default:
                    Debug.LogWarning("No case for " + optionName);
                    break;
            }
        }
    }

    public void SetSavedGraphicsOption(GraphicsSetting name, object val) {
        graphicsOptions[name] = val.ToString();
    }
    public bool HasSavedGraphicsOption(GraphicsSetting name) {
        return graphicsOptions.ContainsKey(name);
    }
    public string GetSavedGraphicsOption(GraphicsSetting name) {
        if (HasSavedGraphicsOption(name) == false)
            return null;
        return graphicsOptions[name];
    }
    public void SetAnisotropicFiltering(bool value) {
        if (value) {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        }
        else {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        }
    }

    public void SetAntiAliasing(int value) {
        if (value == 2) {
            cam.GetComponent<Antialiasing>().enabled = false;
            cam.GetComponent<SMAA>().enabled = true;
        }
        else if (value == 1) {
            cam.GetComponent<Antialiasing>().enabled = true;
            cam.GetComponent<SMAA>().enabled = false;
        }
        else {
            cam.GetComponent<Antialiasing>().enabled = false;
            cam.GetComponent<SMAA>().enabled = false;
        }
    }
    public void SetVsync(int value) {
        QualitySettings.vSyncCount = value;
    }

    public void SetFullscreen(bool value) {
        Screen.fullScreen = value;
    }

    public void SetResolution(CustomResolution res) {
        Screen.SetResolution(res.width, res.height, Screen.fullScreen, res.refreshRate);
    }

    public void SetTextureQuality(int value) {
        // In the quality settings 0 is full quality textures, while 3 is the lowest.
        QualitySettings.masterTextureLimit = 3 - value;
    }
    public void SetBrightness(float value) {
        // In the quality settings 0 is full quality textures, while 3 is the lowest.
        cam.GetComponent<Brightness>().brightness = value;
    }

    public void OnOpen() {
        if (hasLoaded) {
            return;
        }
        //no file found set to preset
        SetGraphicsPreset(3);
    }
    public class CustomResolution {
        public int width;
        public int height;
        public int refreshRate;

        public CustomResolution() {
        }
        public CustomResolution(Resolution res) {
            width = res.width;
            height = res.height;
            refreshRate = res.refreshRate;
        }
        public override string ToString() {
            return string.Format(width + " x " + height + " @ " + refreshRate);
        }
    }

    private void OnOptionChanged(bool open) {
        if (open) {
            if (hasLoaded) {
                return;
            }
            //no file found set to preset
            SetGraphicsPreset(3);
        }
        else {
            SaveGraphicsOption();
        }
    }
}


