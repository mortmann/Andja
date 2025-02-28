using Andja.Pathfinding;
using System.Collections.Generic;
using UnityEngine;
using Andja.FogOfWar;
using Andja.Controller;
using UnityEngine.Serialization;

namespace Andja.Model.Components {
    public class TargetHoldingScript : MonoBehaviour {
        private static bool DebugMode => ConsoleController.DEBUG_MODE; //TODO make this somewhere GLOBAL
        public Target Holding;

        private Rigidbody2D _rigid;
        public bool IsUnit => Unit != null;
        public float x;
        public float y;
        public float rot;
        public int PlayerNumber => Holding.PlayerNumber;
        public Unit Unit;

        public UnitDoModes currentUnitDo = UnitDoModes.Idle;
        public UnitMainModes currentUnitMain = UnitMainModes.Idle;
        public TurningType turnType;
        private LineRenderer _line;
        private bool _isCurrentlyVisible;

        public bool IsCurrentlyVisible => _isCurrentlyVisible || Unit.IsOwnedByCurrentPlayer();

        public void Start() {
            _line = gameObject.GetComponentInChildren<LineRenderer>();
            _rigid = gameObject.GetComponent<Rigidbody2D>();

            transform.position = Unit.PositionVector;
        }


        //FIXME TODO REMOVE DIS
        public void Update() {
            if (Holding == null) {
                Destroy(this);
            }

            x = Holding.CurrentPosition.x;
            y = Holding.CurrentPosition.y;
            if (IsUnit == false)
                return;
            if (Unit.Pathfinding == null) {
                return;
            }

            if (DebugMode && Unit.Pathfinding.worldPath != null) {
                if (Unit.IsOwnedByCurrentPlayer()) {
                    _line.startColor = Color.yellow;
                    _line.endColor = Color.yellow;
                }

                List<Vector3> lineVecs = new List<Vector3>();

                if (Unit.CurrentDoingMode != UnitDoModes.Move) {
                    _line.gameObject.SetActive(false);
                }
                else {
                    _line.gameObject.SetActive(true);
                }

                _line.positionCount = Unit.Pathfinding.worldPath.Count + 2;
                _line.useWorldSpace = true;
                lineVecs.Add(Unit.Pathfinding.Position);
                if (Unit.Pathfinding.NextDestination != null) {
                    lineVecs.Add((Vector3)Unit.Pathfinding.NextDestination.Value);
                }

                foreach (Vector2 t in Unit.Pathfinding.worldPath) {
                    if (lineVecs.Count == Unit.Pathfinding.worldPath.Count - 2)
                        break;
                    Vector3 temp = t;
                    lineVecs.Add(temp + Vector3.back);
                }

                if (Unit.Pathfinding.IsAtDestination == false) {
                    lineVecs.Add(new Vector3(Unit.Pathfinding.dest_X, Unit.Pathfinding.dest_Y, -1));
                }

                _line.positionCount = lineVecs.Count;
                for (int i = 0; i < lineVecs.Count; i++) {
                    _line.SetPosition(i, lineVecs[i]);
                }
            }

            x = Unit.Pathfinding.CurrTile.X;
            y = Unit.Pathfinding.CurrTile.Y;
            rot = Unit.Pathfinding.rotation;
            currentUnitDo = Unit.CurrentDoingMode;
            currentUnitMain = Unit.CurrentMainMode;
        }

        public void FixedUpdate() {
            turnType = Unit.Pathfinding.TurnType;
            //rigid.AddForce(unit.Pathfinding.LastMove);
            _rigid.MoveRotation(Unit.Rotation);
            //transform.rotation = new Quaternion(0, 0, unit.Rotation, 0);
            _rigid.MovePosition(Unit.PositionVector);
        }

        private void OnTriggerEnter2D(Collider2D collision) {
            if (collision.gameObject.GetComponent<FogOfWarTrigger>() != null) {
                _isCurrentlyVisible = true;
            }
        }

        private void OnTriggerExit2D(Collider2D collision) {
            if (collision.gameObject.GetComponent<FogOfWarTrigger>() != null) {
                _isCurrentlyVisible = false;
            }
        }


        public void SetUnit(Unit unit) {
            Holding = unit.GetElement<Target>();
            this.Unit = unit;
        }
    }
}