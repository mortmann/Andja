using Andja.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class ToggleBase : MonoBehaviour {
        protected Toggle toggle;
        TextLanguageSetter translation;
        protected bool IsOn => toggle.isOn;
        protected int DiplayValue => toggle.isOn ? 0 : 1;

        // Use this for initialization
        private void OnEnable() {
            translation = GetComponent<TextLanguageSetter>();
            translation.SetStaticLanguageVariables(StaticLanguageVariables.Off, StaticLanguageVariables.On);
            toggle = GetComponent<Toggle>();
        }

        private void Start() {
            toggle.onValueChanged.AddListener(OnToggleClick);
            OnStart();
        }

        protected virtual void OnStart() {
        }

        protected void OnToggleClick(bool change) {
            translation.ShowValue(change ? 1 : 0);
            OnClick();
        }

        protected virtual void OnClick() {
        }
    }
}