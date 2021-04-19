using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class GS_Resolution : MonoBehaviour {
        private Dropdown dp;
        private Dictionary<string, Resolution> resolutions;
        protected GraphicsSettings graphicsSettings;
        protected GraphicsSetting setting;

        // Use this for initialization
        private void Start() {
            setting = GraphicsSetting.Resolution;
            graphicsSettings = FindObjectOfType<GraphicsSettings>();
            Setup();
        }

        public void OnChange() {
            string res = dp.options[dp.value].text;
            if (resolutions.ContainsKey(res))
                graphicsSettings.SetResolution(new GraphicsSettings.CustomResolution(resolutions[res]));
        }

        internal void Setup() {
            dp = GetComponent<Dropdown>();
            if (dp == null)
                return;
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
            int index = -1;
            if (graphicsSettings.HasSavedGraphicsOption(setting)) {
                index = resses.FindIndex(x => {
                    return x.Equals(graphicsSettings.GetSavedGraphicsOption(setting).ToString());
                });
            }
            if (index < 0) {
                index = resses.FindIndex(x => {
                    return x.Equals(new GraphicsSettings.CustomResolution {
                        height = Screen.height,
                        width = Screen.width,
                        refreshRate = Screen.currentResolution.refreshRate
                    }.ToString());
                });
            }
            dp.value = index;
        }
    }
}