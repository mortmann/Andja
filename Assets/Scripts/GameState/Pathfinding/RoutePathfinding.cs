using Andja.Model;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Andja.Pathfinding {

    public class RoutePathfinding : BasePathfinding {
        public Structure StartStructure;
        public Structure GoalStructure;
        public bool hasToEnterWorkStructure;

        public RoutePathfinding() : base() {
        }
        public RoutePathfinding(float rotationSpeed) : base() {
            this.rotationSpeed = rotationSpeed;
        }

        /// <summary>
        /// Preferrably those list ONLY contains tiles WITH roads build on it for
        /// Performance reasons. But it checks if there is a route
        /// </summary>
        /// <param name="startTiles">Start tiles.</param>
        /// <param name="endTiles">End tiles.</param>
        public void SetDestination(Structure start, Structure goal, bool hasToWorkEnterStructure = true) {
            StartStructure = start;
            GoalStructure = goal;
            //this.endStrTiles = endStrTiles;
            //this.startStrTiles = startStrTiles;
            TurnType = Turning_Type.OnPoint;
            this.hasToEnterWorkStructure = hasToWorkEnterStructure;
            StartCalculatingThread();
        }

        public override void SetDestination(Tile end) {
            Debug.LogError("ROUTE PATHFINDING CANT DO THIS.");
            //DestTile = end;
            //StartCalculatingThread();
        }

        public override void SetDestination(float x, float y) {
            //get the tiles from the world to get a current reference and not an empty from the load
            DestTile = World.Current.GetTileAt(x, y);
            startTile = World.Current.GetTileAt(startTile.X, startTile.Y);
            StartCalculatingThread();
        }

        protected override void CalculatePath() {
            pathDestination = Path_Destination.Tile;
            //if (startTiles == null) {
            //    startTiles = new List<Tile> {
            //        startTile
            //    };
            //    endTiles = new List<Tile> {
            //        DestTile
            //    };
            //}
            if (StartStructure != null) {
                CalculateStructuresPath();
            }
            else {
                CalculateTileStructure();
            }

            //important
            IsDoneCalculating = true;
        }

        private void CalculateTileStructure() {
            Tile t = World.Current.GetTileAt(Position2);
            RoadStructure road = t.Structure as RoadStructure;
            if (road == null) {
                foreach (Tile tile in t.GetNeighbours()) {
                    if (tile.Structure is RoadStructure rs) {
                        if (GoalStructure.GetRoutes().Contains(rs.Route)) {
                            road = rs;
                        }
                    }
                }
                road.Route.TileGraph.AddNodeToRouteTileGraph(t);
            }
            Route route = road.Route;
            List<Tile> endTiles = null;
            endTiles = GoalStructure.RoadsAroundStructure().Select(x => x.BuildTile).ToList();
            endTiles = endTiles.Where(x => route.TileGraph.Tiles.Contains(x)).ToList();

            Path_AStar pa = new Path_AStar(route, route.TileGraph, t, endTiles[0], new List<Tile> { t }, endTiles);
            if (pa.path == null) {
                Debug.LogError("Route Pathfinding failed to find Path. -- Debug is needed.");
                return;
            }
            MakeRoadOffset(pa.path);
        }

        private void CalculateStructuresPath() {
            Queue<Tile> currentQueue = null;
            List<Route> toCheckRoutes = new List<Route>(StartStructure.GetRoutes());
            toCheckRoutes.RemoveAll(x => GoalStructure.GetRoutes().Contains(x) == false);
            if (toCheckRoutes.Count == 0) {
                Debug.LogError("Trying to find Route between non connected Structures!");
                return;
            }
            foreach (Route route in toCheckRoutes) {
                StartStructure.Tiles.ForEach(x => route.TileGraph.AddNodeToRouteTileGraph(x));
                List<Tile> endTiles = null;
                if (hasToEnterWorkStructure) {
                    endTiles = GoalStructure.Tiles;
                    GoalStructure.Tiles.ForEach(x => route.TileGraph.AddNodeToRouteTileGraph(x));
                }
                else {
                    endTiles = GoalStructure.RoadsAroundStructure().Select(x => x.BuildTile).ToList();
                }
                endTiles = endTiles.Where(x => route.TileGraph.Tiles.Contains(x)).ToList();
                List<Tile> startTiles = StartStructure.Tiles.Where(x => route.TileGraph.Tiles.Contains(x)).ToList();

                Path_AStar pa = new Path_AStar(route, route.TileGraph, startTiles[0], endTiles[0], startTiles, endTiles);
                if (pa.path == null || currentQueue != null && currentQueue.Count < pa.path.Count) {
                    continue;
                }
                currentQueue = pa.path;
            }

            if (currentQueue == null) {
                Debug.LogError("Route Pathfinding failed to find Path. -- Debug is needed.");
                return;
            }
            MakeRoadOffset(currentQueue);
        }

        private void MakeRoadOffset(Queue<Tile> currentQueue) {
            worldPath = new Queue<Vector2>();
            Vector2 offset = new Vector2();
            Vector2 dir = new Vector2();
            Vector2 curr;
            bool addFirst = CurrTile != null; //if it is already moving add extra step to move over first
            while (currentQueue.Count > 0) {
                curr = currentQueue.Dequeue().Vector2;
                if (currentQueue.Count > 0) {
                    Vector2 next = currentQueue.Peek().Vector2;
                    dir = next - curr;
                }
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
                //TODO: FIX THIS! -- it works but it is ugly
                if (addFirst) {
                    Vector2 pos = new Vector2(X, Y);
                    if (offset.x > 0)
                        pos.x = Mathf.FloorToInt(X) + offset.x;
                    if (offset.y > 0)
                        pos.y = Mathf.FloorToInt(Y) + offset.y;
                    worldPath.Enqueue(new Vector2(pos.x, pos.y));
                    addFirst = false;
                }
                if (currentQueue.Count == 0) {
                    if (dir.x > 0 || dir.y > 0)
                        offset += dir * (1 - Worker.WorldSize);
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

            if (CurrTile == null) {
                CurrTile = World.Current.GetTileAt(worldPath.Peek());
                X = worldPath.Peek().x;
                Y = worldPath.Peek().y;
                worldPath.Dequeue();
            }
            startTile = CurrTile;
        }
    }
}