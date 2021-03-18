﻿using UnityEngine;
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
public enum GraphicsOptions { VeryLow, Low, Medium, High, Ultra }
public class GraphicsSettings : MonoBehaviour {

    // A reference to the slider for setting graphics preset so we can update it.
    public Slider presetSlider;
    // The camera in use.
    protected Camera MainCamera => Camera.main;
    bool hasLoaded = false;

    Dictionary<GraphicsSetting, object> graphicsOptions;
    Dictionary<GraphicsSetting, object> graphicsOptionsToSave;
    public Action<int> GraphicsPreset;
    void Awake() {
        graphicsOptionsToSave = new Dictionary<GraphicsSetting, object>();
        graphicsOptions = new Dictionary<GraphicsSetting, object>();

        OptionsToggle.ChangedState += OnOptionChanged;
        if (presetSlider.gameObject.activeSelf && presetSlider != null) {
            presetSlider.onValueChanged.AddListener(SetGraphicsPreset);
            SetGraphicsPreset(3);
        }

        hasLoaded = (ReadGraphicsOption());
        if (hasLoaded)
            SaveGraphicsOption();
    }

    void Start() {
        
    }

    public void SetGraphicsPreset(float value) {
        int IntValue = Mathf.FloorToInt(Mathf.Clamp(value, 0, 4));
        GraphicsPreset?.Invoke(IntValue);
        presetSlider.value = IntValue;
    }

    public void SetToCustom() {
        presetSlider.value = 4;
    }
    string fileName = "graphics.ini";

    public static int PresetValues = 4;

    public void SaveGraphicsOption() {
        SetOptions(graphicsOptionsToSave);
        foreach (GraphicsSetting s in graphicsOptionsToSave.Keys) {
            graphicsOptions[s] = graphicsOptionsToSave[s];
        }
        graphicsOptionsToSave.Clear();
        if(presetSlider!=null&&presetSlider.gameObject.activeSelf)
            graphicsOptions[GraphicsSetting.Preset] = Mathf.RoundToInt(presetSlider.value);

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
        File.WriteAllText(filePath, JsonConvert.SerializeObject(graphicsOptions, Formatting.Indented));
    }
    public bool ReadGraphicsOption() {
        string filePath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), fileName);
        if (File.Exists(filePath) == false) {
            return false;
        }
        Dictionary<GraphicsSetting, object> Loaded = JsonConvert.DeserializeObject<Dictionary<GraphicsSetting, object>>(File.ReadAllText(filePath));
        if (Loaded != null)
            SetOptions(Loaded);
        return Loaded != null;
    }

    private void SetOptions(Dictionary<GraphicsSetting, object> options) {
        foreach (GraphicsSetting optionName in typeof(GraphicsSetting).GetEnumValues()) {
            if (options.ContainsKey(optionName) == false)
                continue;
            object val = options[optionName];
            switch (optionName) {
                case GraphicsSetting.Preset:
                    SetGraphicsPreset(Convert.ToInt32(val));
                    break;
                case GraphicsSetting.AnisotropicFiltering:
                    SetAnisotropicFiltering(Convert.ToBoolean(val));
                    break;
                case GraphicsSetting.AntiAliasing:
                    SetAntiAliasing(Convert.ToInt32(val));
                    break;
                case GraphicsSetting.Brightness:
                    SetBrightness(Convert.ToInt32(val));
                    break;
                case GraphicsSetting.Fullscreen:
                    SetFullscreen(Convert.ToInt32(val));
                    break;
                case GraphicsSetting.Resolution:
                    if(val is CustomResolution) {
                        SetResolution((CustomResolution)val);
                    } else {
                        SetResolution(JsonUtility.FromJson<CustomResolution>(Convert.ToString(val)));
                    }
                    break;
                case GraphicsSetting.TextureQuality:
                    SetTextureQuality(Convert.ToInt32(val));
                    break;
                case GraphicsSetting.Vsync:
                    SetVsync(Convert.ToInt32(val));
                    break;
                default:
                    Debug.LogWarning("No case for " + optionName);
                    break;
            }
        }
    }


    public void SetSavedGraphicsOption(GraphicsSetting name, object val) {
        if(presetSlider != null && presetSlider.gameObject.activeSelf) {
            CheckPreset();
        }
        graphicsOptionsToSave[name] = val;
    }

    private void CheckPreset() {
        int currentPreset;
        for (int i = PresetValues-1; i >= 0; i++) {
            currentPreset = i;
            foreach (GraphicsSetting optionName in typeof(GraphicsSetting).GetEnumValues()) {
                if (graphicsOptions.ContainsKey(optionName) == false)
                    continue;
                object val = graphicsOptionsToSave.ContainsKey(optionName)? graphicsOptionsToSave[optionName] : graphicsOptions[optionName];
                switch (optionName) {
                    case GraphicsSetting.Preset:
                        break;
                    case GraphicsSetting.AnisotropicFiltering:
                        if (GS_AnisotropicFiltering.PresetValues[i].Equals(val) == false) {
                            currentPreset = -1;
                            break;
                        }
                        break;
                    case GraphicsSetting.AntiAliasing:
                        if (GS_AntiAliasing.PresetValues[i].Equals(val) == false) {
                            currentPreset = -1;
                            break;
                        }
                        break;
                    case GraphicsSetting.Brightness:
                        break;
                    case GraphicsSetting.Fullscreen:
                        break;
                    case GraphicsSetting.Resolution:
                        break;
                    case GraphicsSetting.TextureQuality:
                        if (GS_TextureQuality.PresetValues[i].Equals(val) == false) {
                            currentPreset = -1;
                            break;
                        }
                        break;
                    case GraphicsSetting.Vsync:
                        break;
                    default:
                        Debug.LogWarning("No case for " + optionName);
                        break;
                }
            }
            if(currentPreset == i) {
                presetSlider.value = i;
                return;
            } 
        }
        //if we are here no preset match was found
        SetToCustom();
    }

    public bool HasSavedGraphicsOption(GraphicsSetting name) {
        return graphicsOptions.ContainsKey(name);
    }
    public object GetSavedGraphicsOption(GraphicsSetting name) {
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
        SetSavedGraphicsOption(GraphicsSetting.AnisotropicFiltering, value);
    }

    public void SetAntiAliasing(int value) {
        value = Mathf.Clamp(value, 0, GS_AntiAliasing.PresetValues.Length-1);
        if (value == 2) {
            MainCamera.GetComponent<Antialiasing>().enabled = false;
            MainCamera.GetComponent<SMAA>().enabled = true;
        }
        else if (value == 1) {
            MainCamera.GetComponent<Antialiasing>().enabled = true;
            MainCamera.GetComponent<SMAA>().enabled = false;
        }
        else {
            MainCamera.GetComponent<Antialiasing>().enabled = false;
            MainCamera.GetComponent<SMAA>().enabled = false;
        }
        SetSavedGraphicsOption(GraphicsSetting.AntiAliasing, value);
    }
    public void SetVsync(int value) {
        value = Mathf.Clamp(value, 0, 1);
        QualitySettings.vSyncCount = value;
        SetSavedGraphicsOption(GraphicsSetting.Vsync, value);
    }

    public void SetFullscreen(int value) {
        value = Mathf.Clamp(value, 0, 3);
        Screen.SetResolution(Screen.width, Screen.height,
                                (FullScreenMode)value, Screen.currentResolution.refreshRate);
        SetSavedGraphicsOption(GraphicsSetting.Fullscreen, value);
    }

    public void SetResolution(CustomResolution res) {
        Screen.SetResolution(res.width, res.height, Screen.fullScreen, res.refreshRate);
        SetSavedGraphicsOption(GraphicsSetting.Resolution, res);
    }

    public void SetTextureQuality(int value) {
        value = Mathf.Clamp(value, 0, 3);
        // In the quality settings 0 is full quality textures, while 3 is the lowest.
        QualitySettings.masterTextureLimit = 3 - value;
        SetSavedGraphicsOption(GraphicsSetting.TextureQuality, value);
    }
    public void SetBrightness(int value) {
        value = Mathf.Clamp(value, 20, 300);
        // In the quality settings 0 is full quality textures, while 3 is the lowest.
        if(MainCamera != null)
            MainCamera.GetComponent<Brightness>().brightness = value / 100f;
        SetSavedGraphicsOption(GraphicsSetting.Brightness, value);
    }

    [JsonObject]
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
            return string.Format(width + " x " + height + " @ " + refreshRate + "Hz");
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

    internal int GetSavedGraphicsOptionInt(GraphicsSetting setting) {
        return Convert.ToInt32(GetSavedGraphicsOption(setting));
    }
    internal float GetSavedGraphicsOptionFloat(GraphicsSetting setting) {
        return Convert.ToSingle(GetSavedGraphicsOption(setting));
    }
    internal bool GetSavedGraphicsOptionBool(GraphicsSetting setting) {
        return Convert.ToBoolean(GetSavedGraphicsOption(setting));
    }
    internal string GetSavedGraphicsOptionString(GraphicsSetting setting) {
        return Convert.ToString(GetSavedGraphicsOption(setting));
    }

}


