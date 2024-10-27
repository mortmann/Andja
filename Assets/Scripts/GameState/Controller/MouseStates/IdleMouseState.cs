using Andja.Editor;
using Andja.Model.Components;
using Andja.Model;
using Andja.Utility;
using UnityEngine;
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
                MakeRaycastToCheckWhatTodo();
            }
        }
        /// <summary>
        /// OnClick on Map. If it hits unit or structure. UIControllers decides then which UI.
        /// If nothing close UI(s).
        /// </summary>
        /// <param name="hit"></param>
        public void MakeRaycastToCheckWhatTodo() {
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            Transform hit = MouseController.Instance.MouseRayCast();
            ITargetableHoldingScript targetableHoldingScript = hit?.GetComponent<ITargetableHoldingScript>();
            if (targetableHoldingScript != null) {
                if (targetableHoldingScript.IsUnit == false) return;
                if (GameData.FogOfWarStyle == FogOfWarStyle.Always && targetableHoldingScript.IsCurrentlyVisible == false) {
                    return;
                }
                MouseController.Instance.SelectUnit((Unit)targetableHoldingScript.Holding);
            }
            else
            if (MouseController.Instance.SelectedUnit == null) {
                if (GameData.FogOfWarStyle == FogOfWarStyle.Always) {
                    if (FogOfWar.FogOfWarStructure.IsStructureVisible(hit.gameObject) == false) {
                        return;
                    }
                }
                Tile t = MouseController.Instance.GetTileUnderneathMouse();
                if (t.Structure != null && (t.Structure.HasHitbox || t.Structure is RoadStructure == false && t.Structure is GrowableStructure == false)) {
                    MouseController.Instance.UIDebug(t.Structure);
                    UIController.Instance.OpenStructureUI(t.Structure);
                    MouseController.Instance.SelectedStructure = t.Structure;
                }
                else {
                    MouseController.Instance.UIDebug(t);
                    if (MouseController.Instance.MouseState != (MouseState.Unit | MouseState.UnitGroup)) {
                        MouseController.Instance.UnselectStuff();
                    }
                }
            }
        }
    }
}