
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
    public bool Debug_Mode => true; //TODO make this somewhere GLOBAL


    public UnitDoModes currentUnitDO = UnitDoModes.Idle;
    public UnitMainModes currentUnitMain = UnitMainModes.Idle;

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
            LineRenderer line = gameObject.GetComponentInChildren<LineRenderer>();
            line.positionCount = unit.pathfinding.worldPath.Count + 3;
            line.useWorldSpace = true;
            int s = 0;
            line.SetPosition(s, unit.pathfinding.Position);
            s++;
            if (unit.pathfinding.NextTile != null) {
                line.SetPosition(s, unit.pathfinding.NextTile.Vector);
                s++;
            }
            foreach (Tile t in unit.pathfinding.worldPath) {
                line.SetPosition(s, t.Vector + Vector3.back);
                s++;
            }
            line.SetPosition(s, new Vector3(unit.pathfinding.dest_X, unit.pathfinding.dest_Y, -1));
        }
        x = unit.pathfinding.CurrTile.X;
        y = unit.pathfinding.CurrTile.Y;
        rot = unit.pathfinding.rotation;
        currentUnitDO = unit.CurrentDoingMode;
        currentUnitMain = unit.CurrentMainMode;
    }
}
