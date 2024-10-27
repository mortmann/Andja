using Andja.Controller;
using Andja.Model;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class ImageText : MonoBehaviour {
        public Image image;
        public Text text;
        string addon;
        public ShowToolTip showHoverOver;
        public Func<string> updateText;
        LayoutElement element;
        RectTransform imageRect;
        Func<bool> UpdateWarningColor;
        private void Start() {
            imageRect = image.GetComponent<RectTransform>();
            element = GetComponent<LayoutElement>();
        }
        public void Set(Sprite sprite, LanguageVariables variables, string showText, Func<bool> updateWarningColor = null) {
            gameObject.SetActive(true);
            name = variables.Name;
            image.sprite = sprite;
            SetText(showText);
            showHoverOver.SetVariable(variables, true);
            UpdateWarningColor = updateWarningColor;
        }
        public void Set(Sprite sprite, LanguageVariables variable, Func<string> update) {
            updateText = update;
            Set(sprite, variable, update.Invoke());
        }
        public void Set(Sprite sprite, StaticLanguageVariables variable, string showText) {
            gameObject.SetActive(true);
            name = variable.ToString();
            image.sprite = sprite;
            SetText(showText);
            showHoverOver.SetVariable(variable, true);
        }
        public void Set(Sprite sprite, StaticLanguageVariables variable, Func<string> update) {
            updateText = update;
            Set(sprite, variable, update.Invoke());
        }
        internal void SetText(string showText, Func<bool> updateWarningColor = null) {
            text.text = showText + addon;
            UpdateWarningColor = updateWarningColor;
        }

        internal void SetBrightColorText() {
            SetColorText(Color.white);
        }

        internal void RemoveAddon() {
            addon = null;
        }
        internal void ShowAddon(string addon, TextColor textColor) {
            ShowAddon(addon, UIController.GetTextColor(textColor));
        }
        internal void ShowAddon(string addon, Color32 color) {
            ShowAddon(addon, "#"+ColorUtility.ToHtmlStringRGB(color));
        }
        internal void ShowAddon(string addon, string hexColor) {
            this.addon = " (<color=" + hexColor + ">" + addon + "</color>)";
        }
        internal void SetColorText(Color color) {
            text.color = color;
        }

        internal void Set(Sprite sprite, LanguageVariables variables) {
            Set(sprite, variables, variables.Name, null);
            UILanguageController.Instance.RegisterLanguageChange(()=> { SetText(variables.Name); });
        }
        private void Update() {
            if(updateText != null) {
                SetText(updateText.Invoke());
            }
            if(element != null)
                element.preferredWidth = imageRect.sizeDelta.x + text.preferredWidth;
            if(UpdateWarningColor?.Invoke() == true) {
                SetColorText(Color.red);
            } else {
                SetBrightColorText();
            }
        }

    }
}