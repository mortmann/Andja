using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class GS_Resolution : MonoBehaviour {
    Dropdown dp;
    Dictionary<string, Resolution> resolutions;
    protected GraphicsSettings graphicsSettings;
    protected GraphicsSetting setting;

    // Use this for initialization
    void Start() {
        setting = GraphicsSetting.Resolution;
        graphicsSettings = FindObjectOfType<GraphicsSettings>();
        dp = GetComponent<Dropdown>();
        List<string> resses = new List<string>();
        resolutions = new Dictionary<string, Resolution>();
        
        foreach (Resolution res in Screen.resolutions) {
            if (resolutions.ContainsKey(res.ToString())) {
                continue;
            }
            resolutions[res.ToString()] = res;
            resses.Add(res.ToString());
        }
        dp.AddOptions(resses);
        if (graphicsSettings.HasSavedGraphicsOption(setting)) {
            dp.value = resses.FindIndex(x => {
                return x.Equals(graphicsSettings.GetSavedGraphicsOption(setting).ToString());
            });
        } else {
            dp.value = resses.FindIndex(x => {
                return x.Equals(new GraphicsSettings.CustomResolution {
                    height = Screen.height, width = Screen.width, refreshRate =Screen.currentResolution.refreshRate
                }.ToString());
            });
        }
    }

    public void OnChange() {
        string res = dp.options[dp.value].text;
        if (resolutions.ContainsKey(res))
            graphicsSettings.SetResolution(new GraphicsSettings.CustomResolution(resolutions[res]));
    }

}
