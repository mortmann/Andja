using Andja.Model;
using Andja.Model.Components;
using Andja.Utility;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.Controller {
    public class SingleUnitMouseState : BaseMouseState {
        Unit SelectedUnit => MouseController.Instance.SelectedUnit;
        protected MouseUnitState MouseUnitState => MouseController.Instance.MouseUnitState;
        protected Vector3 MapClampedMousePosition => MouseController.Instance.MapClampedMousePosition;
        public static bool OverrideCurrentSetting => InputHandler.ShiftKey == false; // TODO: better name
        public override void Activate() {
            base.Activate();
            SelectedUnit.RegisterOnDestroyCallback(OnUnitDestroy);
            UIController.Instance.OpenUnitUI(SelectedUnit);
            MouseController.Instance.UIDebug(SelectedUnit);
        }
        public override void Update() {
            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            //TEMPORARY FOR TESTING
            if (Input.GetKeyDown(KeyCode.U)) {
                if (MouseController.Instance.SelectedUnit.IsShip) {
                    ((Ship)SelectedUnit).ShotAtPosition(MouseController.Instance.GetLastMousePosition());
                }
            }
            Transform hit = MouseController.Instance.MouseRayCast();
            CheckUnitCursor();
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary)) {
                switch (MouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;

                    case MouseUnitState.Normal:
                        //TODO: Better way?
                        if (hit) {
                            ITargetableHoldingScript iths = hit.GetComponent<ITargetableHoldingScript>();
                            if (iths != null) {
                                if (iths.Holding == SelectedUnit) {
                                    return;
                                }
                            }
                        }
                        MouseController.Instance.UnselectUnit();
                        break;

                    case MouseUnitState.Patrol:
                        SelectedUnit.AddPatrolCommand(MapClampedMousePosition.x, MapClampedMousePosition.y);
                        break;

                    case MouseUnitState.Build:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (InputHandler.GetMouseButtonDown(InputMouse.Secondary) == false) return;
            if (SelectedUnit.PlayerNumber != PlayerController.currentPlayerNumber) {
                MouseController.Instance.SetMouseState(MouseState.Idle);
                return;
            }

            if (hit == null) {
                switch (MouseUnitState) {
                    case MouseUnitState.None:
                        Debug.LogWarning("MouseController is in the wrong state!");
                        break;

                    case MouseUnitState.Normal:
                        SelectedUnit.GiveMovementCommand(MapClampedMousePosition.x, MapClampedMousePosition.y,
                            OverrideCurrentSetting);
                        break;

                    case MouseUnitState.Patrol:
                        MouseController.Instance.SetMouseUnitState(MouseUnitState.Normal);
                        break;

                    case MouseUnitState.Build:
                        MouseController.Instance.ResetBuild();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else {
                ITargetableHoldingScript targetableHoldingScript = hit.GetComponent<ITargetableHoldingScript>();
                if (targetableHoldingScript != null) {
                    SelectedUnit.GiveAttackCommand(targetableHoldingScript.Holding, OverrideCurrentSetting);
                }
                else if (hit.GetComponent<CrateHoldingScript>() != null) {
                    SelectedUnit.GivePickUpCrateCommand(hit.GetComponent<CrateHoldingScript>().thisCrate,
                        OverrideCurrentSetting);
                }
                else if (targetableHoldingScript == null) {
                    Tile t = MouseController.Instance.GetTileUnderneathMouse();
                    switch (t.Structure) {
                        case null:
                            return;
                        case ICapturable structure:
                            SelectedUnit.GiveCaptureCommand(structure, OverrideCurrentSetting);
                            break;
                        case TargetStructure tStructure:
                            SelectedUnit.GiveAttackCommand(tStructure, OverrideCurrentSetting);
                            break;
                    }
                }
            }
        }
        protected void CheckUnitCursor() {
            if (SelectedUnit.IsOwnedByCurrentPlayer() == false)
                return;
            if (MouseUnitState == MouseUnitState.Build) return;
            Transform hit = MouseController.Instance.MouseRayCast();
            bool attackAble = false;
            if (hit) {
                ITargetableHoldingScript iths = hit.GetComponent<ITargetableHoldingScript>();
                if (iths != null) {
                    attackAble = PlayerController.Instance.ArePlayersAtWar(PlayerController.currentPlayerNumber, iths.PlayerNumber);
                    if (SelectedUnit != iths.Holding
                        && PlayerController.currentPlayerNumber == iths.PlayerNumber
                        && SelectedUnit.IsUnit == iths.IsUnit) {
                        MouseController.ChangeCursorType(CursorType.Escort);
                        return;
                    }
                }
            }
            Structure str = MouseController.Instance.GetTileUnderneathMouse()?.Structure;
            if (str is TargetStructure) {
                attackAble = PlayerController.Instance.ArePlayersAtWar(PlayerController.currentPlayerNumber, str.PlayerNumber);
            }
            MouseController.ChangeCursorType(attackAble ? CursorType.Attack : CursorType.Pointer);
        }
        private void OnUnitDestroy(Unit unit, IWarfare warfare) {
            if (SelectedUnit == unit) {
                MouseController.Instance.SetMouseState(MouseState.Idle);
            }
        }
        public override void Deactivate() {
            base.Deactivate();
            SelectedUnit.UnregisterOnDestroyCallback(OnUnitDestroy);
        }
    }
}