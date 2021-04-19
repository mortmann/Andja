using Andja.Controller;
using Andja.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

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

        public void Set(Sprite sprite, StaticLanguageVariables variable, string showText) {
            name = variable.ToString();
            image.sprite = sprite;
            text.text = showText;
            showHoverOver.SetVariable(variable, true);
        }

        internal void SetText(string showText) {
            text.text = showText;
        }

        internal void SetBrightColorText() {
            SetColorText(Color.white);
        }

        internal void SetColorText(Color color) {
            text.color = color;
        }
    }
}