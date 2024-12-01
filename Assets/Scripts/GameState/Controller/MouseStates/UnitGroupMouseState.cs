using Andja.Model.Components;
using Andja.Model;
using Andja.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.Controller {
    public class UnitGroupMouseState : SingleUnitMouseState {
        public List<Unit> selectedUnitGroup => MouseController.Instance.selectedUnitGroup;
        public override void Activate() {
            base.Activate();
            selectedUnitGroup.ForEach(x => x.RegisterOnDestroyCallback(OnUnitDestroy));
            UIController.Instance.OpenUnitGroupUI(selectedUnitGroup);
        }
        public override void Update() {
            base.Update();
            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                switch (MouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;

                    case MouseUnitState.Normal:
                        MouseController.Instance.UnselectUnitGroup();
                        break;

                    case MouseUnitState.Patrol:
                        selectedUnitGroup.ForEach(x => x.AddPatrolCommand(MapClampedMousePosition.x, MapClampedMousePosition.y));
                        break;

                    case MouseUnitState.Build:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            CheckUnitCursor();
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary) == false) return;
            Transform hit = MouseController.Instance.MouseRayCast();
            if (hit == null) {
                switch (MouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;

                    case MouseUnitState.Normal:
                        selectedUnitGroup.ForEach(x => x.GiveMovementCommand(MapClampedMousePosition.x,
                            MapClampedMousePosition.y, OverrideCurrentSetting));
                        break;

                    case MouseUnitState.Patrol:
                        MouseController.Instance.SetMouseUnitState(MouseUnitState.Normal);
                        break;
                    case MouseUnitState.Build:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else {
                ITargetableHoldingScript targetableHoldingScript = hit.GetComponent<ITargetableHoldingScript>();
                if (targetableHoldingScript != null) {
                    selectedUnitGroup.ForEach(x =>
                        x.GiveAttackCommand(hit.gameObject.GetComponent<ITargetableHoldingScript>().Holding,
                            OverrideCurrentSetting));
                }
                else if (hit.GetComponent<CrateHoldingScript>() != null) {
                    //TODO: maybe nearest? other logic? air distance??
                    selectedUnitGroup[0].TryToAddCrate(hit.GetComponent<CrateHoldingScript>().thisCrate);
                }
                else if (targetableHoldingScript == null) {
                    Tile t = MouseController.Instance.GetTileUnderneathMouse();
                    if(t.Structure?.HasElement<Capturable>() == true) {
                        selectedUnitGroup.ForEach(x => x.GiveCaptureCommand(t.Structure, OverrideCurrentSetting));
                    }
                    switch (t.Structure) {
                        case null:
                            return;
                        case TargetStructure ts:
                            selectedUnitGroup.ForEach(x => x.GiveAttackCommand(ts, OverrideCurrentSetting));
                            break;
                    }
                }
            }
        }
        private void OnUnitDestroy(Unit unit, IWarfare warfare) {
            if (selectedUnitGroup.Contains(unit))
                selectedUnitGroup.Remove(unit);
        }
    }
}
