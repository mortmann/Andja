using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.LoadScreen {
    public class ImageColorChanger : MonoBehaviour {
        Image image;
        Color nextColor;
        Color currentColor;
        float time;
        void Start() {
            image = GetComponent<Image>();
            image.color = new Color(Random.value, Random.value, Random.value);
            currentColor = image.color;
            nextColor = new Color(Random.value, Random.value, Random.value);
        }

        void Update() {
            if(time > 1) {
                time = 0;
                currentColor = nextColor;
                nextColor = new Color(Random.value, Random.value, Random.value);
            }
            image.color = new Color(Mathf.Lerp(currentColor.r, nextColor.r, time),
                                    Mathf.Lerp(currentColor.g, nextColor.g, time),
                                    Mathf.Lerp(currentColor.b, nextColor.b, time));
            time += Time.deltaTime * 0.1f;
        }
    }

}
