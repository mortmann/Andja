using Andja.Model;
using Andja.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Andja.Pathfinding {

    public class RoutePathfinding : BasePathfinding {
        public Structure StartStructure;
        public Structure GoalStructure;

        public RoutePathfinding() : base() {
        }
        public RoutePathfinding(IPathfindAgent worker) : base() {
            this.agent = worker;
        }

        /// <summary>
        /// Preferrably those list ONLY contains tiles WITH roads build on it for
        /// Performance reasons. But it checks if there is a route
        /// </summary>
        /// <param name="startTiles">Start tiles.</param>
        /// <param name="endTiles">End tiles.</param>
        public void SetDestination(Structure start, Structure goal) {
            StartStructure = start;
            GoalStructure = goal;
            //this.endStrTiles = endStrTiles;
            //this.startStrTiles = startStrTiles;
            AddPathJob();
        }

        public override void SetDestination(Tile end) {
            Debug.LogError("ROUTE PATHFINDING CANT DO THIS.");
        }

        public override void SetDestination(float x, float y) {
            //get the tiles from the world to get a current reference and not an empty from the load
            DestTile = World.Current.GetTileAt(x, y);
            startTile = World.Current.GetTileAt(startTile.X, startTile.Y);
            AddPathJob();
        }

        protected override void CalculatePath() {
            if (Job != null && (Job.Status == JobStatus.InQueue || Job.Status == JobStatus.Calculating))
                PathfindingThreadHandler.RemoveJob(Job);

            if (StartStructure != null) {
                CalculateStructuresPath();
            }
            else {
                CalculateTileStructure();
            }
        }

        private void CalculateTileStructure() {
            Tile t = World.Current.GetTileAt(Position2);
            //this is to find the route it originally after loading took to find the path
            RoadStructure road = t.Structure as RoadStructure;
            if (road == null) {
                foreach (Tile tile in t.GetNeighbours()) {
                    if (tile.Structure is RoadStructure rs) {
                        if (GoalStructure.GetRoutes().Contains(rs.Route)) {
                            road = rs;
                        }
                    }
                }
            }
            Route route = road.Route;
            List<Vector2> goals = null;
            if (agent.CanEndInUnwakable) {
                goals = GoalStructure.Tiles.Select(x => x.Vector2).ToList();
            }
            else {
                goals = GoalStructure.RoadsAroundStructure().TakeWhile(x => x.Route == route).Select(y => y.Center).ToList();
            }
            Job = PathfindingThreadHandler.EnqueueJob(agent, route.Grid, road.Center, goals[0],
                                                        new List<Vector2> { road.Center }, goals, 
                                                        OnPathJobFinished);
        }

        private void CalculateStructuresPath() {
            List<Route> toCheckRoutes = new List<Route>(StartStructure.GetRoutes());
            toCheckRoutes.RemoveAll(x => GoalStructure.GetRoutes().Contains(x) == false);
            if (toCheckRoutes.Count == 0) {
                Debug.LogError("Trying to find Route between non connected Structures!");
                return;
            }
            Job = new PathJob(agent, toCheckRoutes.Count);
            for (int i = 0; i < toCheckRoutes.Count; i++) {
                Route route = toCheckRoutes[i];
                Job.Grid[i] = route.Grid;
                Job.StartTiles[i] = StartStructure.Tiles.Select(y => y.Vector2).ToList();
                if (agent.CanEndInUnwakable) {
                    Job.EndTiles[i] = GoalStructure.Tiles.Select(y => y.Vector2).ToList();
                }
                else {
                    Job.EndTiles[i] = GoalStructure.RoadsAroundStructure()
                                            .TakeWhile(x => x.Route == route).Select(y => y.Center).ToList();
                }
            }
            Job.QueueModifier += ModifyQueue;
            Job.OnPathInvalidated += PathInvalidated;
            PathfindingThreadHandler.EnqueueJob(Job, OnPathJobFinished);
        }

        private Queue<Vector2> ModifyQueue(Queue<Vector2> queue) {
            Queue<Vector2> newQueue = new Queue<Vector2>();
            Vector2 dir = new Vector2();
            Vector2 curr;
            Vector2 last = queue.Peek();
            bool addFirst = CurrTile != null; //if it is already moving add extra step to move over first
            while (queue.Count > 0) {
                curr = queue.Dequeue();
                if (queue.Count > 0) {
                    Vector2 next = queue.Peek();
                    dir = next - curr;
                }
                GetOffset(dir, out Vector2 offset);
                GetOffset(curr - last, out Vector2 offset2);
                if(offset.x == 0)
                    offset.x = offset2.x;
                if (offset.y == 0)
                    offset.y = offset2.y;

                //TODO: FIX THIS! -- it works but it is ugly
                if (addFirst) {
                    Vector2 pos = new Vector2(X, Y);
                    if (offset.x > 0)
                        pos.x = Mathf.FloorToInt(X) + offset.x;
                    if (offset.y > 0)
                        pos.y = Mathf.FloorToInt(Y) + offset.y;
                    newQueue.Enqueue(new Vector2(pos.x, pos.y));
                    addFirst = false;
                }
                if (queue.Count == 0) {
                    if (dir.x > 0 || dir.y > 0)
                        offset += dir * (Worker.WorldSize);
                }
                last = curr;
                newQueue.Enqueue(curr + offset);
            }
            return newQueue;
        }

        private void OnPathJobFinished() {
            FinishQueue(Job.Path);
        }

        private void FinishQueue(Queue<Vector2> currentQueue) {
            worldPath = currentQueue;
            if (CurrTile == null) {
                X = worldPath.Peek().x;
                Y = worldPath.Peek().y;
            }
            //if (CurrTile == null) {
            //    Vector2 start = Util.FindClosestPoints(StartStructure.Tiles.Select(x => x.Vector2), worldPath.Peek())[0];
            //    GetOffset(worldPath.Peek() - start, out Vector2 offset);
            //    X = start.x + offset.x;
            //    Y = start.y + offset.y;
            //}
            //if (agent.CanEndInUnwakable) {
            //    Vector2 dest = Util.FindClosestPoints(GoalStructure.Tiles.Select(x => x.Vector2), worldPath.Last())[0];
            //    GetOffset(worldPath.Last() - dest, out Vector2 offset);
            //    worldPath.Enqueue(dest + offset);
            //}
            Vector2 last = worldPath.Last();
            DestTile = World.Current.GetTileAt(last);
            dest_X = last.x;
            dest_Y = last.y;
            startTile = CurrTile;
        }

        private static void GetOffset(Vector2 dir, out Vector2 offset) {
            offset = new Vector2();
            if (dir.x > 0) {
                offset.y = -Worker.WorldSize;
            }
            if (dir.x < 0) {
                offset.y = Worker.WorldSize;
            }
            if (dir.y > 0) {
                offset.x = Worker.WorldSize;
            }
            if (dir.y < 0) {
                offset.x = -Worker.WorldSize;
            }
        }

        public override void HandleNoPathFound() {
            if (agent is Worker w) {
                w.GoHome(true);
            }
            else {
                Debug.LogWarning("RoutePathfinding HandleNoPathFound for agent " + agent.GetType() + " is not implemented");
            }
        }
    }
}