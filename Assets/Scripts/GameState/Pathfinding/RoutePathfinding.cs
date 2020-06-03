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

        pathDestination = Path_Destination.Tile;
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
        worldPath = new Queue<Vector2>();
        //Queue<Vector2> tempPath = new Queue<Vector2>(worldPath.Reverse());
        Vector2 offset = new Vector2();
        Vector2 dir = new Vector2();
        Vector2 curr;
        bool addFirst = CurrTile != null; //if it is already moving add extra step to move over first
        while (currentQueue.Count>0) {
            curr = currentQueue.Dequeue().Vector2;
            if (currentQueue.Count > 0) {
                Vector2 next = currentQueue.Peek().Vector2;
                dir = next - curr;
                if (dir.x > 0) {
                    offset.y = Worker.WorldSize;
                }
                if (dir.x < 0) {
                    offset.y = 1 - Worker.WorldSize;
                }
                if (dir.y > 0) {
                    offset.x = 1 - Worker.WorldSize;
                }
                if (dir.y < 0) {
                    offset.x = Worker.WorldSize;
                }
            } else {
                if(dir.x>0||dir.y>0)
                    offset += dir * (1 - Worker.WorldSize);
            }
            //TODO: FIX THIS! -- it works but it is ugly
            if(addFirst) {
                Vector2 pos = new Vector2(X, Y);
                if (offset.x > 0)
                    pos.x = Mathf.FloorToInt(X) + offset.x;
                if (offset.y > 0)
                    pos.y = Mathf.FloorToInt(Y) + offset.y;
                worldPath.Enqueue(new Vector2(pos.x, pos.y));
                addFirst = false;
            }
            worldPath.Enqueue(curr + offset);
        }

        if (worldPath == null || worldPath.Count == 0) {
            Debug.LogError("FAILED ROUTE PATHFINDING");
            return;
        }
        CreateReversePath();
        dest_X = backPath.Peek().x;
        dest_Y = backPath.Peek().y;
        DestTile = World.Current.GetTileAt(backPath.Peek());

        if (CurrTile==null) {
            CurrTile = World.Current.GetTileAt(worldPath.Peek());
            X = worldPath.Peek().x;
            Y = worldPath.Peek().y;
            worldPath.Dequeue();
        }
        startTile = CurrTile;
        
        //important
        IsDoneCalculating = true;
    }

}
