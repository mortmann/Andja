using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class GS_SliderBase : MonoBehaviour {
        protected GraphicsSetting setting;

        // The camera in use.
        protected Camera cam;

        // The text we display to the user for the slider value.
        protected Text displayValue;

        // The slider.
        protected Slider slider;

        // The slider value as an int.
        protected int Value {
            get { return (int)slider.value; }
        }

        protected TextLanguageSetter tls;

        protected GraphicsSettings graphicsSettings;

        public string[] displayLabels;

        private void OnEnable() {
            // Get the camera.
            cam = Camera.main;
            // Get the slider.
            slider = GetComponent<Slider>();

            // Register the graphics preset listeners.
            graphicsSettings = FindObjectOfType<GraphicsSettings>();
            graphicsSettings.GraphicsPreset += OnGraphicsPresetChange;
            // Attach the listener for the method we call when the slider value changes.
            slider.onValueChanged.AddListener((f) => OnSliderValueChange());
            slider.onValueChanged.AddListener((f) => OnSliderValueChangeSetDisplayText());
            tls = GetComponent<TextLanguageSetter>();
        }

        protected virtual void OnGraphicsPresetChange(int obj) {
        }

        protected void Start() {
            // Find the Text component for the display value.
            displayValue = transform.Find("Value").GetComponent<Text>();
            tls.valueText = displayValue;

            // Initialize it to the current slider value.
            displayValue.text = slider.value.ToString();

            if (displayLabels.Length > 0) {
                displayValue.text = displayLabels[Value];
            }
            OnStart();
        }

        public virtual void OnStart() {
        }

        /**
         * Each setting class overrides this and changes whatever it wants changed
         * when we modify the slider. For example turns on/off an image effect or
         * adjusts the volume in the audio mixer.
         */

        protected virtual void OnSliderValueChange() {
        }

        /**
         * Set the text value to display in the menu for this settings slider. A
         * setting class can override this to display whatever it wants in the menu.
         */

        protected virtual void OnSliderValueChangeSetDisplayText() {
            //if (displayLabels.Length > 0) {
            //    displayValue.text = displayLabels[Value];
            //}
            //else {
            //    displayValue.text = Value.ToString();
            //}
            //tls.ShowValue(Value);
        }
    }
}