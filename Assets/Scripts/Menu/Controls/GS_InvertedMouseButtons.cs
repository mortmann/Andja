using Andja.Utility;

namespace Andja.UI.Menu {

    public class GS_InvertedMouseButtons : ToggleBase {
        protected override void OnStart() {
            toggle.isOn = InputHandler.InvertedMouseButtons;
        }

        protected override void OnClick() {
            InputHandler.InvertedMouseButtons = toggle.isOn;
        }
    }
}