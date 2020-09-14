using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
public class ImageText : MonoBehaviour {
    public Image image;
    public Text text;
    public ShowHoverOver showHoverOver;
    public void Set(Sprite sprite, string showText, string hoverOver) {
        name = showText;
        image.sprite = sprite;
        text.text = showText;
        showHoverOver.Text = hoverOver;
    }
}
