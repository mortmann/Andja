using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class GS_SteppedSlider : MonoBehaviour {
        private Slider slider;

        // The slider value as an int.
        private int Value {
            get { return (int)slider.value; }
        }

        // The rect for the slider.
        private Rect sliderRect;

        // The width of the handle.
        private float handleWidth;

        // The number of steps for the slider.
        private int sliderSteps;

        private void Start() {
            slider = GetComponent<Slider>();
            // Get the slider.
            if (!slider.wholeNumbers) {
                Debug.LogError("The stepped slider only works with whole number sliders.");
                return;
            }
            // Attach the listener for the method we call when the slider value changes.
            slider.onValueChanged.AddListener((f) => OnSliderValueChangeSetPosition());
            slider.onValueChanged.AddListener((f) => MenuAudioManager.Instance.PlaySliderSound());

            CalculateHandleSize();
        }

        public void CalculateHandleSize() {
            slider = GetComponent<Slider>();
            // Get the width of the slider.
            sliderRect = slider.GetComponent<RectTransform>().rect;
            // Calculate the total number of steps for the slider.
            sliderSteps = (int)slider.maxValue - (int)slider.minValue + 1;

            float xScale = 1;// Screen.width / GetComponentInParent<UnityEngine.UI.CanvasScaler>().referenceResolution.x;
                             // Set the width of the handle.
            handleWidth = (sliderRect.width * xScale) / sliderSteps;
            slider.handleRect.sizeDelta = new Vector2(handleWidth, slider.handleRect.sizeDelta.y);

            // Set the initial handle position based on the slider value.
            SetHandlePosition(Value);
        }

        /**
         * OnChangeValue callback for the slider.
         */

        private void OnSliderValueChangeSetPosition() {
            SetHandlePosition(Value);
        }

        /**
         * Set the handle to the correct position based on the slider value.
         */

        private void SetHandlePosition(int value) {
            float xPosition = sliderRect.x + (handleWidth / 2) + handleWidth * (value - slider.minValue);
            slider.handleRect.localPosition = new Vector3(xPosition, slider.handleRect.localPosition.y, slider.handleRect.localPosition.z);
        }
    }
}