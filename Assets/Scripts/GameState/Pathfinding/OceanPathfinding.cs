using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using EpPathFinding.cs;
using UnityEngine;

public class OceanPathfinding : Pathfinding {
    Tile start;

    StaticGrid tileGrid;

    public Ship Ship;

    protected override float Speed {
        get {
            return Ship.Speed;
        }
        set { // cant set it
        }
    }

    public OceanPathfinding() : base() {
        TurnType = Turning_Type.TurnRadius;

    }

    public OceanPathfinding(Tile t, Ship s) {
        Ship = s;
        rotationSpeed = s.RotationSpeed;
        CurrTile = t;
        TurnType = Turning_Type.TurnRadius;
    }


    public override void SetDestination(Tile end) {
        SetDestination(end.X, end.Y);
    }
    public override void SetDestination(float x, float y) {
        pathDest = Path_Destination.Exact;
        dest_X = x;
        dest_Y = y;
        this.start = World.Current.GetTileAt(X, Y);
        this.DestTile = World.Current.GetTileAt(x, y);
        tileGrid = World.Current.TilesGrid;
        StartCalculatingThread();
    }
    protected override void CalculatePath() {
        TurnType = Turning_Type.TurnRadius;

        pathDest = Path_Destination.Exact;
        System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();
        StopWatch.Start();
        JumpPointParam jpParam = new JumpPointParam(tileGrid, new GridPos(start.X, start.Y), new GridPos(DestTile.X, DestTile.Y), true, DiagonalMovement.OnlyWhenNoObstacles);
        List<GridPos> pos = JumpPointFinder.FindPath(jpParam);
        worldPath = new Queue<Vector2>();
        //we probably needs to remove the first tile cause it may interfere with smooth pathing
        for (int i = 0; i < pos.Count; i++) {
            worldPath.Enqueue(World.Current.GetTileAt(pos[i].x, pos[i].y).Vector2); //make sure it is correct tile etc
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
        StopWatch.Stop();
        //Debug.Log("CalculatePath Steps:" + worldPath.Count + " - "+ StopWatch.ElapsedMilliseconds + "ms (" + StopWatch.Elapsed.TotalSeconds + "s)! ");
        //important
        IsDoneCalculating = true;
    }
}
