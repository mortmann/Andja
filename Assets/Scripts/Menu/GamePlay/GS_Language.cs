using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GS_Language : MonoBehaviour {
    public GameplaySetting setting;
    Dropdown Dropdown;
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
