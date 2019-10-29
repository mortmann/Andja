using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GS_AutorotateStructure : ToggleBase {
    protected override void OnStart() {
        setting = GameplaySetting.Autorotate;
        if (GameplaySettings.Instance.HasSavedGameplayOption(setting))
            toggle.isOn = bool.Parse(GameplaySettings.Instance.GetSavedGameplayOption(setting));
    }
    protected override void OnClick() {
        GameplaySettings.Instance.SetSavedGameplayOption(setting, IsOn);
    }
}
