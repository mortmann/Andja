using Andja.Model;
using EpPathFinding.cs;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Pathfinding {

    public class OceanPathfinding : BasePathfinding {
        private Tile start;

        private StaticGrid tileGrid;

        public OceanPathfinding() : base() {
            if (_x < 0)
                _x = 0;
            if (_y < 0)
                _y = 0;
        }

        public OceanPathfinding(Tile t, Ship s) {
            agent = s;
            CurrTile = t;
            X = t.X;
            Y = t.Y;
        }

        public override void SetDestination(Tile end) {
            SetDestination(end.X, end.Y);
        }

        public override void SetDestination(float x, float y) {
            dest_X = x;
            dest_Y = y;
            this.start = World.Current.GetTileAt(X, Y);
            this.DestTile = World.Current.GetTileAt(x, y);
            tileGrid = (StaticGrid)World.Current.TilesGrid.Clone();
            StartCalculatingThread();
        }

        protected override void CalculatePath() {
            tileGrid.Reset();
            System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();
            StopWatch.Start();
            //TODO change this algo
            JumpPointParam jpParam = new JumpPointParam(tileGrid, new GridPos(start.X, start.Y), new GridPos(DestTile.X, DestTile.Y), true, DiagonalMovement.OnlyWhenNoObstacles);
            List<GridPos> pos = JumpPointFinder.FindPath(jpParam);

            worldPath = new Queue<Vector2>();
            //we probably need to remove the first tile cause it may interfere with smooth pathing
            for (int i = 0; i < pos.Count; i++) {
                if (i == pos.Count - 1 && pos.Count > 2) {
                    Vector2 curr = new Vector2(pos[i].x, pos[i].y);
                    Vector2 dir = curr - new Vector2(pos[i - 2].x, pos[i - 2].y);
                    dir = dir.normalized;
                    if (World.Current.GetTileAt(new Vector2(Mathf.Clamp(pos[i].x + dir.x, 0, World.Current.Width - 1), pos[i].y)).Type == TileType.Ocean &&
                        World.Current.GetTileAt(new Vector2(pos[i].x, Mathf.Clamp(pos[i].y + dir.y, 0, World.Current.Height - 1))).Type == TileType.Ocean) {
                        continue;
                    }
                }
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
            //important
            IsDoneCalculating = true;
        }
    }
}