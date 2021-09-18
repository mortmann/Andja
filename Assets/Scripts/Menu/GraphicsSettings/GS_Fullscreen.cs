using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class GS_Fullscreen : GS_SliderBase {
        public static bool DisableExklusivFullscreen => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan;
        public override void OnStart() {
            if (DisableExklusivFullscreen) 
                GetComponent<Slider>().minValue = 1;
            setting = GraphicsSetting.Fullscreen;
            tls.valueEnumType = typeof(FullScreenMode);
            //tls.preValueString = "GraphicsSettings";
            if (graphicsSettings.HasSavedGraphicsOption(setting))
                SetFullscreen(graphicsSettings.GetSavedGraphicsOptionInt(setting));
            else
                SetFullscreen((int)FullScreenMode.ExclusiveFullScreen);
        }

        protected override void OnSliderValueChange() {
            SetFullscreen(Value);
        }

        private void SetFullscreen(int value) {
            if (DisableExklusivFullscreen && value == 0)
                value = 1;
            graphicsSettings.SetFullscreen(value);
            slider.value = System.Convert.ToInt16(value);
            tls.ShowValue(Value);
        }
    }
}