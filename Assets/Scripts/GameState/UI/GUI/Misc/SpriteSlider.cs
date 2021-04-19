using UnityEngine;

namespace Andja.UI {

    public class SpriteSlider : MonoBehaviour {
        public GameObject background;
        public GameObject percent;
        private bool setted;

        public void Show(GameObject go, float f) {
            ChangePercent(f);
            if (setted) {
                return;
            }
            setted = true;
            background.transform.SetParent(go.transform, false);
            background.transform.localPosition = (go.transform.localScale / 2);
        }

        public void ChangePercent(float f) {
            //if its smaller than 1 it should be a percentage
            //if its not than you will notice this
            if (f < 1 && f != 0) {
                f *= 100;
                Debug.LogWarning("The Percentage is smaller than 1 - but this needs percentage of 1-100");
            }
            if (f <= 30) {
                percent.GetComponent<SpriteRenderer>().color = Color.red;
            }
            if (f > 30) {
                percent.GetComponent<SpriteRenderer>().color = Color.yellow;
            }
            if (f > 60) {
                percent.GetComponent<SpriteRenderer>().color = Color.green;
            }
            percent.transform.localScale = new Vector3(1 * f, 1, 0);
        }
    }
}