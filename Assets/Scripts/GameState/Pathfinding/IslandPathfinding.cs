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
        NextDestination = CurrTile.Vector2;
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
        pathDest = Path_Destination.Exact;
        StartCalculatingThread();
    }
    protected override void CalculatePath() {
        IsDoneCalculating = false;
        if (start == null)
            start = World.Current.GetTileAt(X, Y);
        Path_AStar pa = new Path_AStar(start.Island, start, DestTile);

        worldPath = new Queue<Vector2>();
        while (pa.path.Count > 0) {
            worldPath.Enqueue(pa.path.Dequeue().Vector2);
        }
        worldPath.Enqueue(Destination);
        CreateReversePath();
        backPath.Enqueue(Position2);
        if (worldPath.Count > 0) {
            worldPath.Dequeue();
        }
        if (worldPath.Count > 0) {
            NextDestination = worldPath.Dequeue();
        }
        //important 
        IsDoneCalculating = true;
    }


}
