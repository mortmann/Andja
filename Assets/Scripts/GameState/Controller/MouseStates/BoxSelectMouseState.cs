using Andja.Editor;
using Andja.Model.Components;
using Andja.Utility;
using UnityEngine.EventSystems;
using UnityEngine;
using System.Collections.Generic;
using Andja.Model;

namespace Andja.Controller {
    public class BoxSelectMouseState : BaseMouseState {
        public override CursorType CursorType => CursorType.Upgrade;
        private bool _displayDragRectangle;
        private Rect _drawRect;
        private Vector3 _dragStartPosition;
        private Vector3 _lastFramePosition => MouseController.Instance.LastFramePosition;
        private Vector3 _lastFrameGuiPosition => MouseController.Instance.LastFrameGuiPosition;
        public List<Unit> selectedUnitGroup => MouseController.Instance.selectedUnitGroup;
        public override void Activate() {
            base.Activate();
            _dragStartPosition = MouseController.Instance.CurrentFramePosition;
        }
        public override void Update() {
            if (InputHandler.GetMouseButton(InputMouse.Primary) == false) {
                Vector3 v1 = _dragStartPosition;
                Vector3 v2 = _lastFramePosition;
                v1.z = 0;
                v2.z = 0;
                Vector3 min = Vector3.Min(v1, v2);
                Vector3 max = Vector3.Max(v1, v2);
                Vector3 dimensions = max - min;
                Collider2D[] c2d = Physics2D.OverlapBoxAll(min + dimensions / 2, dimensions, 0);
                if (MouseController.OverrideCurrentSetting)
                    selectedUnitGroup.Clear();
                foreach (Collider2D c in c2d) {
                    ITargetableHoldingScript target = c.GetComponent<ITargetableHoldingScript>();
                    if (target == null)
                        continue;
                    if (target.IsUnit == false)
                        continue;
                    if (target.Holding.PlayerNumber != PlayerController.currentPlayerNumber) continue;
                    Unit u = ((Unit)target.Holding);
                    if (selectedUnitGroup.Contains(u) == false)
                        selectedUnitGroup.Add(u);
                }
                if (selectedUnitGroup.Count > 1)
                    MouseController.Instance.SelectUnitGroup(selectedUnitGroup);
                else if (selectedUnitGroup.Count == 1)
                    MouseController.Instance.SelectUnit(selectedUnitGroup[0]);
                else {
                    MouseController.Instance.SetMouseState(MouseState.Idle);// nothing selected
                    MouseController.Instance.UnselectStuff();
                }
                _drawRect = Rect.zero;
                _displayDragRectangle = false;
            }

            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            // Drag already started
            Vector3 screenPosition1 = Camera.main.WorldToScreenPoint(_dragStartPosition);
            Vector3 screenPosition2 = _lastFrameGuiPosition;
            screenPosition1.y = Screen.height - screenPosition1.y;
            screenPosition2.y = Screen.height - screenPosition2.y;

            // Calculate corners
            var topLeft = Vector3.Min(screenPosition1, screenPosition2);
            var bottomRight = Vector3.Max(screenPosition1, screenPosition2);
            // Create Rect
            Debug.Log(topLeft.ToString() + bottomRight.ToString());
            _drawRect = Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
            _displayDragRectangle = true;
        }
        public override void OnGui() {
            base.OnGui();
            if(_displayDragRectangle)
                Util.DrawScreenRectBorder(_drawRect, 2, new Color(0.9f, 0.9f, 0.9f, 0.9f));
        }
        public override void Reset() {
            base.Reset();
            _dragStartPosition = MouseController.Instance.CurrentFramePosition;
            _drawRect = Rect.zero;
            _displayDragRectangle = false;
        }
    }
}