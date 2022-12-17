using Andja.Editor;
using Andja.Model;
using Andja.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.Controller {
    public class DragBuildMouseState : MultiBuildMouseState {
        public override void Update() {
            if (InputHandler.GetMouseButtonDown(InputMouse.Secondary)) {
                MouseController.Instance.ResetBuild();
                MouseController.Instance.SetMouseState(MouseState.Idle);
                return;
            }            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            // Start Drag
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                DragStartPosition = CurrentFramePositionOffset;
                ResetSingleStructurePreview();
                if (_singleStructurePreview != null) {
                    SimplePool.Despawn(_singleStructurePreview);
                    _singleStructurePreview = null;
                }
            }
            int startX = Mathf.FloorToInt(DragStartPosition.x);
            int endX = Mathf.FloorToInt(CurrentFramePositionOffset.x);
            int startY = Mathf.FloorToInt(DragStartPosition.y);
            int endY = Mathf.FloorToInt(CurrentFramePositionOffset.y);
            List<Tile> ts = GetTilesStructures(startX, endX, startY, endY);
            if (InputHandler.GetMouseButton(InputMouse.Primary)) {
                // Display a preview of the drag area
                UpdateMultipleStructurePreviews(ts);
            }
            else {
                UpdateSinglePreview();
            }
            // End Drag
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) == false) return;
            foreach (StructurePreview sp in _tileToStructurePreview.Values.OrderBy(x => x.number)) {
                MouseController.Instance.Build(sp.tiles, true);
            }
            Reset();
        }
    }
}
