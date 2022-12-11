using Andja.Model;
using Andja.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.Controller {
    public class SingleBuildMouseState : BuildMouseState {
        public override void Update() {
            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            if (ToBuildStructure == null) {
                return;
            }
            UpdateSinglePreview();
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary) == false) return;
            List<Tile> structureTiles = ToBuildStructure.GetBuildingTiles(MouseController.Instance.GetTileUnderneathMouse());
            MouseController.Instance.Build(structureTiles);
        }
    }
}
