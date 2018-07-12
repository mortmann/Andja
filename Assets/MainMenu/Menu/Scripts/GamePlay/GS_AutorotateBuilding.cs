using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GS_AutorotateBuilding : ToggleBase {
    protected override void OnStart() {
        setting = GameplaySetting.autorotate;
        if (GameplaySettings.Instance.HasSavedGameplayOption(setting))
            toggle.isOn = bool.Parse(GameplaySettings.Instance.GetSavedGameplayOption(setting));
    }
    protected override void OnClick() {
        GameplaySettings.Instance.SetSavedGameplayOption(setting, IsOn);
    }
}
