using Andja.Controller;
using Andja.Model;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class ImageText : MonoBehaviour {
        public Image image;
        public Text text;
        public ShowHoverOver showHoverOver;
        public Func<string> updateText;
        public void Set(Sprite sprite, LanguageVariables variables, string showText) {
            gameObject.SetActive(true);
            name = variables.Name;
            image.sprite = sprite;
            text.text = showText;
            showHoverOver.SetVariable(variables, true);
        }
        public void Set(Sprite sprite, LanguageVariables variable, Func<string> update) {
            Set(sprite, variable, update.Invoke());
        }
        public void Set(Sprite sprite, StaticLanguageVariables variable, string showText) {
            gameObject.SetActive(true);
            name = variable.ToString();
            image.sprite = sprite;
            text.text = showText;
            showHoverOver.SetVariable(variable, true);
        }
        public void Set(Sprite sprite, StaticLanguageVariables variable, Func<string> update) {
            Set(sprite, variable, update.Invoke());
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

        internal void Set(Sprite sprite, LanguageVariables variables) {
            Set(sprite, variables, variables.Name);
            UILanguageController.Instance.RegisterLanguageChange(()=> { text.text = variables.Name; });
        }
        private void Update() {
            if(updateText != null) {
                SetText(updateText.Invoke());
            }
        }
    }
}