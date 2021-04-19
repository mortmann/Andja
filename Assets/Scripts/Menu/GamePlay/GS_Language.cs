using Andja.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class GS_Language : MonoBehaviour {
        public GameplaySetting setting;
        private Dropdown Dropdown;

        protected void Start() {
            Dropdown = GetComponentInChildren<Dropdown>();
            setting = GameplaySetting.Language;
            Dropdown.options.Clear();
            Dropdown.AddOptions(GameplaySettings.Instance.Localizations);
            if (GameplaySettings.Instance.HasSavedGameplayOption(setting))
                Dropdown.value = GameplaySettings.Instance.Localizations.FindIndex(x => {
                    return x == GameplaySettings.Instance.GetSavedGameplayOption(setting);
                });
            Dropdown.onValueChanged.AddListener(OnSelect);
        }

        protected void OnSelect(int id) {
            GameplaySettings.Instance.SetSavedGameplayOption(setting, Dropdown.options[id].text);
            UILanguageController.Instance.ChangeLanguage(Dropdown.options[id].text);
        }
    }
}