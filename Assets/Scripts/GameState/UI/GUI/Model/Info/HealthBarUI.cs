using Andja.Model;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class HealthBarUI : MonoBehaviour {
        public Slider Bar;
        public Text Text;
        public Image HpFillImage;

        private void Start() {
            Bar.interactable = false;
        }

        public void SetHealth(float hp, float maxHP) {
            Bar.value = hp;
            Bar.maxValue = maxHP;
            float Percentage = hp / maxHP;
            byte red = (byte)(255 * Mathf.Clamp01(2.0f * (1 - Percentage)));
            byte green = (byte)(255 * Mathf.Clamp01(2.0f * Percentage));
            HpFillImage.color = new Color32(red, green, 0, 255);
            if (Text != null)
                Text.text = Mathf.RoundToInt(hp) + "/" + Mathf.RoundToInt(maxHP);
        }
    }
}