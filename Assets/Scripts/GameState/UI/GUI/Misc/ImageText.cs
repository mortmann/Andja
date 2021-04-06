using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
public class ImageText : MonoBehaviour {
    public Image image;
    public Text text;
    public ShowHoverOver showHoverOver;
    public void Set(Sprite sprite, LanguageVariables variables, string showText) {
        name = variables.Name;
        image.sprite = sprite;
        text.text = showText;
        showHoverOver.SetVariable(variables, true);
    }
}
