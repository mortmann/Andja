using Andja.Pathfinding;
using System.Collections.Generic;
using UnityEngine;
using Andja.FogOfWar;

namespace Andja.Model.Components {

    public class ITargetableHoldingScript : MonoBehaviour {
        public ITargetable Holding;
        private Rigidbody2D rigid;
        public bool IsUnit => Holding is Unit;
        public float x;
        public float y;
        public float rot;
        public int PlayerNumber => Holding.PlayerNumber;
        public bool Debug_Mode => false; //TODO make this somewhere GLOBAL
        Unit unit => (Unit)Holding;

        public UnitDoModes currentUnitDO = UnitDoModes.Idle;
        public UnitMainModes currentUnitMain = UnitMainModes.Idle;
        public TurningType turnType;
        private LineRenderer line;
        bool isCurrentlyVisible;
        public bool IsCurrentlyVisible {
            get {
                return isCurrentlyVisible || unit.IsOwnedByCurrentPlayer();
            }
        }

        public void Start() {
            line = gameObject.GetComponentInChildren<LineRenderer>();
            rigid = gameObject.GetComponent<Rigidbody2D>();

            transform.position = unit.PositionVector;
        }


        //FIXME TODO REMOVE DIS
        public void Update() {
            if (Holding == null) {
                Destroy(this);
            }
            x = Holding.CurrentPosition.x;
            y = Holding.CurrentPosition.y;
            if (Holding is Unit == false)
                return;
            if (unit.Pathfinding == null) {
                return;
            }
            if (Debug_Mode && unit.Pathfinding.worldPath != null) {
                List<Vector3> lineVecs = new List<Vector3>();

                if (unit.CurrentDoingMode != UnitDoModes.Move) {
                    line.gameObject.SetActive(false);
                }
                else {
                    line.gameObject.SetActive(true);
                }
                line.positionCount = unit.Pathfinding.worldPath.Count + 2;
                line.useWorldSpace = true;
                lineVecs.Add(unit.Pathfinding.Position);
                if (unit.Pathfinding.NextDestination != null) {
                    lineVecs.Add((Vector3)unit.Pathfinding.NextDestination.Value);
                }
                foreach (Vector2 t in unit.Pathfinding.worldPath) {
                    if (lineVecs.Count == unit.Pathfinding.worldPath.Count - 2)
                        break;
                    Vector3 temp = t;
                    lineVecs.Add(temp + Vector3.back);
                }
                if (unit.Pathfinding.IsAtDestination == false) {
                    lineVecs.Add(new Vector3(unit.Pathfinding.dest_X, unit.Pathfinding.dest_Y, -1));
                }

                line.positionCount = lineVecs.Count;
                for (int i = 0; i < lineVecs.Count; i++) {
                    line.SetPosition(i, lineVecs[i]);
                }
            }

            x = unit.Pathfinding.CurrTile.X;
            y = unit.Pathfinding.CurrTile.Y;
            rot = unit.Pathfinding.rotation;
            currentUnitDO = unit.CurrentDoingMode;
            currentUnitMain = unit.CurrentMainMode;
        }

        public void FixedUpdate() {
            turnType = unit.Pathfinding.TurnType;
            //rigid.AddForce(unit.Pathfinding.LastMove);
            rigid.MoveRotation(unit.Rotation);
            //transform.rotation = new Quaternion(0, 0, unit.Rotation, 0);
            rigid.MovePosition(unit.PositionVector);
        }
        private void OnTriggerEnter2D(Collider2D collision) {
            if (collision.gameObject.GetComponent<FogOfWarTrigger>() != null) {
                isCurrentlyVisible = true;
            }
        }
        private void OnTriggerExit2D(Collider2D collision) {
            if (collision.gameObject.GetComponent<FogOfWarTrigger>() != null) {
                isCurrentlyVisible = false;
            }
        }


    }
}