using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class RoutePathfinding : Pathfinding {

    List<Tile> startTiles;
    List<Tile> endTiles;
    public RoutePathfinding() : base() { }
    /// <summary>
    /// Preferrably those list ONLY contains tiles WITH roads build on it for
    /// Performance reasons. But it checks if there is a route
    /// </summary>
    /// <param name="startTiles">Start tiles.</param>
    /// <param name="endTiles">End tiles.</param>
    public void SetDestination(List<Tile> startTiles, List<Tile> endTiles) {
        this.startTiles = startTiles;
        this.endTiles = endTiles;
        TurnType = Turning_Type.OnPoint;
        StartCalculatingThread();
    }

    public override void SetDestination(Tile end) {
        DestTile = end;
        StartCalculatingThread();
    }

    public override void SetDestination(float x, float y) {
        //get the tiles from the world to get a current reference and not an empty from the load
        DestTile = World.Current.GetTileAt(x, y);
        startTile = World.Current.GetTileAt(startTile.X, startTile.Y);
        StartCalculatingThread();
    }

    protected override void CalculatePath() {

        pathDest = Path_Destination.Tile;
        if (startTiles == null) {
            startTiles = new List<Tile> {
                startTile
            };
            endTiles = new List<Tile> {
                DestTile
            };
        }

        Queue<Tile> currentQueue = null;
        List<Route> checkedRoutes = new List<Route>();
        foreach (Tile st in startTiles) {
            if (st.Structure == null || st.Structure.GetType() != typeof(RoadStructure)) {
                continue;
            }
            RoadStructure r1 = st.Structure as RoadStructure;
            foreach (Tile et in endTiles) {
                if (et.Structure.GetType() != typeof(RoadStructure)) {
                    continue;
                }
                RoadStructure r2 = et.Structure as RoadStructure;
                if (r1.Route != r2.Route) {
                    continue;
                }
                if (checkedRoutes.Contains(r1.Route) == false) {
                    Path_AStar pa = new Path_AStar(r1.Route, st, et, startTiles, endTiles);
                    checkedRoutes.Add(r1.Route);
                    if (pa.path == null || currentQueue != null && currentQueue.Count < pa.path.Count) {
                        continue;
                    }
                    currentQueue = pa.path;
                }
            }
        }
        CurrTile = currentQueue.Peek();
        worldPath = new Queue<Vector2>();
        while (currentQueue.Count>0) {
            worldPath.Enqueue(currentQueue.Dequeue().Vector2);
        }
        if (worldPath == null || worldPath.Count == 0) {
            return;
        }
        startTile = CurrTile;
        CreateReversePath();
        DestTile = World.Current.GetTileAt(backPath.Peek());
        worldPath.Dequeue();
        X = startTile.X;
        Y = startTile.Y;
        dest_X = DestTile.X;
        dest_Y = DestTile.Y;
        //important
        IsDoneCalculating = true;
    }

}
