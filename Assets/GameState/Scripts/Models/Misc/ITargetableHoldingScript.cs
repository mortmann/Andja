
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ITargetableHoldingScript : MonoBehaviour {

    public ITargetable Holding;
    public bool IsUnit => Holding is Unit;
    public float x;
    public float y;
    public float rot;
    public int playerNumber;
    public bool Debug_Mode => false; //TODO make this somewhere GLOBAL
    public void Start() {
        line = gameObject.GetComponentInChildren<LineRenderer>();
    }

    public UnitDoModes currentUnitDO = UnitDoModes.Idle;
    public UnitMainModes currentUnitMain = UnitMainModes.Idle;
    private LineRenderer line;

    //FIXME TODO REMOVE DIS
    public void Update() {
        if (Holding == null) {
            Destroy(this);
        }
        x = Holding.CurrentPosition.x;
        y = Holding.CurrentPosition.y;
        playerNumber = Holding.PlayerNumber;
        if (Holding is Unit == false)
            return;
        Unit unit = (Unit)Holding;
        if (unit.pathfinding == null) {
            return;
        }
        if (Debug_Mode && unit.pathfinding.worldPath != null) {
            List<Vector3> lineVecs = new List<Vector3>();

            if (unit.CurrentDoingMode != UnitDoModes.Move) {
                line.gameObject.SetActive(false);
            } else {
                line.gameObject.SetActive(true);
            }
            line.positionCount = unit.pathfinding.worldPath.Count + 2;
            line.useWorldSpace = true;
            lineVecs.Add(unit.pathfinding.Position);
            if (unit.pathfinding.NextTile != null) {
                lineVecs.Add(unit.pathfinding.NextTile.Vector);
            }
            foreach (Tile t in unit.pathfinding.worldPath) {
                if (lineVecs.Count == unit.pathfinding.worldPath.Count - 2)
                    break;
                lineVecs.Add(t.Vector + Vector3.back);
            }
            if(unit.pathfinding.IsAtDestination==false) {
                lineVecs.Add(new Vector3(unit.pathfinding.dest_X, unit.pathfinding.dest_Y, -1));
            }

            line.positionCount = lineVecs.Count;
            for(int i = 0; i < lineVecs.Count; i++) {
                line.SetPosition(i,lineVecs[i]);
            }
        }
        
        x = unit.pathfinding.CurrTile.X;
        y = unit.pathfinding.CurrTile.Y;
        rot = unit.pathfinding.rotation;
        currentUnitDO = unit.CurrentDoingMode;
        currentUnitMain = unit.CurrentMainMode;
    }
}
