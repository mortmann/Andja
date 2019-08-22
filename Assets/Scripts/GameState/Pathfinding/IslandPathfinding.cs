using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class IslandPathfinding : Pathfinding {

    Tile start;
    public IslandPathfinding() : base() {
    }
    public IslandPathfinding(Unit u, Tile start) {
        this._speed = u.Speed;
        this.rotationSpeed = u.RotationSpeed;
        CurrTile = start;
        NextTile = CurrTile;
        dest_X = start.X;
        dest_Y = start.Y;
    }

    public override void SetDestination(Tile end) {
        SetDestination(end.Vector);
    }
    public void SetDestination(Vector3 end) {
        SetDestination(end.x, end.y);
    }
    public override void SetDestination(float x, float y) {
        if (x == dest_X || dest_Y == y)
            return;
        this.start = this.CurrTile;
        this.DestTile = World.Current.GetTileAt(x, y);
        dest_X = x;
        dest_Y = y;
        pathDest = Path_dest.exact;
        StartCalculatingThread();
    }
    protected override void CalculatePath() {
        IsDoneCalculating = false;
        if (start == null)
            start = World.Current.GetTileAt(X, Y);
        Path_AStar pa = new Path_AStar(start.MyIsland, start, DestTile);
        worldPath = pa.path;
        CreateReversePath();
        if (worldPath.Count > 0) {
            worldPath.Dequeue();
        }
        if (worldPath.Count > 0) {
            NextTile = worldPath.Dequeue();
        }
        //important 
        IsDoneCalculating = true;
    }


}
