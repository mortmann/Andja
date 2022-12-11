using Andja.Editor;
using Andja.Utility;
using UnityEngine.EventSystems;

namespace Andja.Controller {
    public class IdleMouseState : BaseMouseState {
        private bool _mouseStateIdleLeftMouseDown;
        public override CursorType CursorType => CursorType.Pointer;

        public override void Update() {
            if (EventSystem.current.IsPointerOverGameObject() == false && InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                //If some clicks down onto a ui and then goes off it and releases the mouse 
                //we do not want to open or close where the mouse ends up 
                _mouseStateIdleLeftMouseDown = true;
            }
            if (_mouseStateIdleLeftMouseDown && InputHandler.GetMouseButtonUp(InputMouse.Primary)
                    && EditorController.IsEditor == false) {
                //mouse press decide what it hit
                MouseController.Instance.MakeRaycastToCheckWhatTodo();
            }
        }
    }
}