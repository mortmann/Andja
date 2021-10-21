namespace Andja.UI.Menu {

    public class GS_AutorotateStructure : ToggleBase {
        protected override void OnStart() {
            if (GameplaySettings.Instance.HasSavedGameplayOption(GameplaySetting.Autorotate))
                toggle.isOn = bool.Parse(GameplaySettings.Instance.GetSavedGameplayOption(GameplaySetting.Autorotate));
        }

        protected override void OnClick() {
            GameplaySettings.Instance.SetSavedGameplayOption(GameplaySetting.Autorotate, IsOn);
        }
    }
}