using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GS_AutorotateBuilding : ToggleBase {
    public void Start() {
        setting = GameplaySetting.autorotate;
        if (GameplaySettings.Instance.HasSavedGameplayOption(setting))
            toggle.isOn = bool.Parse(GameplaySettings.Instance.GetSavedGameplayOption(setting));
    }
    protected override void OnClick() {
        //TODO have anything todo
        Debug.LogError("THE OnSliderValueChange, THEY DO NOTHING!");
        GameplaySettings.Instance.SetSavedGameplayOption(setting, IsOn);
    }
}
