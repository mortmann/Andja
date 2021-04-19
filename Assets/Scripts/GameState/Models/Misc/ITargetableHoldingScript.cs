using Andja.Pathfinding;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public class ITargetableHoldingScript : MonoBehaviour {
        public ITargetable Holding;
        private Rigidbody2D rigid;
        public bool IsUnit => Holding is Unit;
        public float x;
        public float y;
        public float rot;
        public int PlayerNumber => Holding.PlayerNumber;
        public bool Debug_Mode => false; //TODO make this somewhere GLOBAL

        public void Start() {
            line = gameObject.GetComponentInChildren<LineRenderer>();
            rigid = gameObject.GetComponent<Rigidbody2D>();

            transform.position = unit.PositionVector;
        }

        Unit unit => (Unit)Holding;

        public UnitDoModes currentUnitDO = UnitDoModes.Idle;
        public UnitMainModes currentUnitMain = UnitMainModes.Idle;
        public Turning_Type turnType;
        private LineRenderer line;

        //FIXME TODO REMOVE DIS
        public void Update() {
            if (Holding == null) {
                Destroy(this);
            }
            x = Holding.CurrentPosition.x;
            y = Holding.CurrentPosition.y;
            if (Holding is Unit == false)
                return;
            if (unit.pathfinding == null) {
                return;
            }
            if (Debug_Mode && unit.pathfinding.worldPath != null) {
                List<Vector3> lineVecs = new List<Vector3>();

                if (unit.CurrentDoingMode != UnitDoModes.Move) {
                    line.gameObject.SetActive(false);
                }
                else {
                    line.gameObject.SetActive(true);
                }
                line.positionCount = unit.pathfinding.worldPath.Count + 2;
                line.useWorldSpace = true;
                lineVecs.Add(unit.pathfinding.Position);
                if (unit.pathfinding.NextDestination != null) {
                    lineVecs.Add((Vector3)unit.pathfinding.NextDestination.Value);
                }
                foreach (Vector2 t in unit.pathfinding.worldPath) {
                    if (lineVecs.Count == unit.pathfinding.worldPath.Count - 2)
                        break;
                    Vector3 temp = t;
                    lineVecs.Add(temp + Vector3.back);
                }
                if (unit.pathfinding.IsAtDestination == false) {
                    lineVecs.Add(new Vector3(unit.pathfinding.dest_X, unit.pathfinding.dest_Y, -1));
                }

                line.positionCount = lineVecs.Count;
                for (int i = 0; i < lineVecs.Count; i++) {
                    line.SetPosition(i, lineVecs[i]);
                }
            }

            x = unit.pathfinding.CurrTile.X;
            y = unit.pathfinding.CurrTile.Y;
            rot = unit.pathfinding.rotation;
            currentUnitDO = unit.CurrentDoingMode;
            currentUnitMain = unit.CurrentMainMode;
        }

        public void FixedUpdate() {
            turnType = unit.pathfinding.TurnType;
            //rigid.AddForce(unit.pathfinding.LastMove);
            rigid.MoveRotation(unit.Rotation);
            //transform.rotation = new Quaternion(0, 0, unit.Rotation, 0);
            rigid.MovePosition(unit.PositionVector);
        }
    }
}