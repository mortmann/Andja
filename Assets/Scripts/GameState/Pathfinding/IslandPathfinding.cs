using Andja.Model;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Pathfinding {

    public class IslandPathfinding : BasePathfinding {
        public IslandPathfinding() : base() {
        }

        public IslandPathfinding(Unit u, Tile start) {
            this.agent = u;
            CurrTile = start;
            X = start.X;
            Y = start.Y;
            NextDestination = CurrTile.Vector2;
            dest_X = start.X;
            dest_Y = start.Y;
        }

        public override void HandleNoPathFound() {
            dest_X = Position2.x;
            dest_Y = Position2.y;
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
            this.DestTile = World.Current.GetTileAt(x, y);
            dest_X = x;
            dest_Y = y;
            AddPathJob();
        }

        protected override void CalculatePath() {
            if (Job != null && (Job.Status == JobStatus.InQueue || Job.Status == JobStatus.Calculating))
                PathfindingThreadHandler.RemoveJob(Job);
            PathGrid grid = CurrTile.Island.Grid;
            Job = PathfindingThreadHandler.EnqueueJob(agent, grid, Position2, new Vector2(dest_X, dest_Y), OnPathJobFinished);
        }

        private void OnPathJobFinished() {
            worldPath = Job.Path;
            CreateReversePath();
            backPath.Enqueue(Position2);
            if (worldPath.Count > 0) {
                worldPath.Dequeue();
            }
            if (worldPath.Count > 0) {
                NextDestination = worldPath.Dequeue();
            }
            Job.OnPathInvalidated += PathInvalidated;
        }
    }
}