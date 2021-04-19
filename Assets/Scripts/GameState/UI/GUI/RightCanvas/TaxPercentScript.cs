using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class TaxPercentScript : MonoBehaviour {
        private Slider mySlider;
        public Text knobText;

        // Use this for initialization
        private void Start() {
            mySlider = gameObject.GetComponent<Slider>();
        }

        // Update is called once per frame
        private void Update() {
            knobText.text = mySlider.value + "%";
        }
    }
}