using Andja.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace Andja.Pathfinding {

    public class TilesPathfinding : BasePathfinding {

        //for structure
        protected List<Tile> startTiles;

        protected List<Tile> endTiles;

        public TilesPathfinding() {

        }
        public TilesPathfinding(IPathfindAgent worker) : base() {
            agent = worker;
        }

        public override void SetDestination(Tile end) {
            DestTile = end;
            CalculatePath();
        }

        public override void SetDestination(float x, float y) {
            //get the tiles from the world to get a current reference and not an empty from the load
            DestTile = World.Current.GetTileAt(x, y);
            if (startTile == null) {
                startTile = World.Current.GetTileAt(X, Y);
                if (startTile.Island == null) {
                    Debug.Log(startTile);
                    return;
                }
            } 
            AddPathJob();
        }

        public void SetDestination(List<Tile> startTiles, List<Tile> endTiles) {
            this.startTiles = startTiles;
            this.endTiles = endTiles;
            AddPathJob();
        }

        protected override void CalculatePath() {
            if (startTiles == null) {
                startTiles = new List<Tile> {
                    World.Current.GetTileAt(startTile.X, startTile.Y)
                };
                endTiles = new List<Tile> {
                    World.Current.GetTileAt(DestTile.X, DestTile.Y)
                };
            }
            IsAtDestination = false;
            if (Job != null && (Job.Status == JobStatus.InQueue || Job.Status == JobStatus.Calculating))
                PathfindingThreadHandler.RemoveJob(Job);
            Job = PathfindingThreadHandler.EnqueueJob(agent, startTiles[0].Island.Grid,
                                                                startTiles[0].Vector2, endTiles[0].Vector2,
                                                                startTiles.Select(x => x.Vector2).ToList(),
                                                                endTiles.Select(x => x.Vector2).ToList(),
                                                                OnPathJobFinished);
        }

        private void OnPathJobFinished() {
            worldPath = Job.Path;
            if (CurrTile == null) {
                CurrTile = World.Current.GetTileAt(worldPath.Peek());
                X = worldPath.Peek().x;
                Y = worldPath.Peek().y;
            }
            CreateReversePath();
            worldPath.Dequeue();
            dest_X = backPath.Peek().x;
            dest_Y = backPath.Peek().y;
            DestTile = World.Current.GetTileAt(backPath.Peek());
            Job.OnPathInvalidated += PathInvalidated;
        }

        public override void HandleNoPathFound() {
            if(agent is Worker w) {
                w.Destroy();
            } else {
                Debug.LogWarning("TilesPathfinding HandleNoPathFound for agent " + agent.GetType() + " is not implemented");
            }
        }
    }
}