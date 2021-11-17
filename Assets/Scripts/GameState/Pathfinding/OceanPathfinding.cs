using Andja.Model;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Pathfinding {

    public class OceanPathfinding : BasePathfinding {
        private Tile start;

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
            IsAtDestination = true;
        }

        public override void HandleNoPathFound() {
            //Ocean Paths are always viable and never gets blocked for now so it if it is 
            //than it is 100% faulty pathfinding
            Debug.LogError("Ocean Pathfinding did not find Path to destination " + DestTile + " (" + Destination + ")");
        }

        public override void SetDestination(Tile end) {
            SetDestination(end.X, end.Y);
        }

        public override void SetDestination(float x, float y) {
            dest_X = x;
            dest_Y = y;
            this.start = World.Current.GetTileAt(X, Y);
            this.DestTile = World.Current.GetTileAt(x, y);
            AddPathJob();
        }

        protected override void CalculatePath() {
            if (World.Current == null)
                return;
            if (Job != null && (Job.Status == JobStatus.InQueue || Job.Status == JobStatus.Calculating))
                PathfindingThreadHandler.RemoveJob(Job);
            Job = PathfindingThreadHandler.EnqueueJob(agent, null, Position2, new Vector2(dest_X, dest_Y), OnPathJobFinished, null);
        }

        private void OnPathJobFinished() {
            worldPath = Job.Path;
            CreateReversePath();
            backPath.Enqueue(Position2);
            if(World.Current.Tilesmap[Mathf.FloorToInt(dest_X)][Mathf.FloorToInt(dest_Y)] == false){
                Vector2 v = worldPath.Last();
                dest_X = v.x;
                dest_Y = v.y;
                DestTile = World.Current.GetTileAt(dest_X, dest_Y);
            }
            if (worldPath.Count > 0) {
                NextDestination = worldPath.Dequeue();
            }
        }
    }
}