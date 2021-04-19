using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class CircleProgressBar : MonoBehaviour {
        public Image fillingCircle;
        public Text percentText;

        // Use this for initialization
        private void Start() {
        }

        public void SetProgress(float amount) {
            percentText.text = Mathf.RoundToInt(amount * 100) + "%";
            fillingCircle.fillAmount = amount;
        }
    }
}