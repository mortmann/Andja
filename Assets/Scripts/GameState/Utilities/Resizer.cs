using UnityEngine;

namespace Andja.Utility {

    public class Resizer : MonoBehaviour {

        // Use this for initialization
        private void Start() {
            AdjustSize();
        }

        public void AdjustSize() {
            Vector2 size = this.GetComponent<RectTransform>().sizeDelta;
            this.GetComponent<RectTransform>().sizeDelta = size;
        }
    }
}