using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        float percantage = hp / maxHP;
        byte red = (byte) (255 * Mathf.Clamp01( 2.0f * (1 - percantage) ));
        byte green = (byte) (255 * Mathf.Clamp01( 2.0f * percantage) );
        HpFillImage.color = new Color32(red, green, 0, 255);
        Text.text = Mathf.RoundToInt (hp) + "/" + Mathf.RoundToInt(maxHP);
    }
}
