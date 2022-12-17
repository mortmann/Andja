using Andja.Model;
using Andja.Utility;

namespace Andja.Controller {
    public class CopyMouseState : BaseMouseState {
        public override CursorType CursorType => CursorType.Copy;

        public override void Update() {
            if (InputHandler.GetMouseButtonDown(InputMouse.Secondary)) {
                MouseController.Instance.SetMouseState(MouseState.Idle);
            }
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) == false) return;
            Tile t = MouseController.Instance.GetTileUnderneathMouse();
            if (t.Structure == null)
                return;
            if (t.Structure.CanBeBuild == false)
                return;
            BuildController.Instance.StartStructureBuild(t.Structure.ID);
        }
    }
}