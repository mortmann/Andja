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
            } 
            StartCalculatingThread();
        }
        bool debug;

        public void SetDestination(List<Tile> startTiles, List<Tile> endTiles, bool debug = false) {
            this.debug = debug;
            this.startTiles = startTiles;
            this.endTiles = endTiles;
            StartCalculatingThread();
        }

        public void Test() {
            Pathfinder.Find(agent,
                            new PathGrid(startTiles[0].Island),
                            st.Vector2,
                            et.Vector2,
                            startTiles.Select(x => x.Vector2).ToList(),
                            endTiles.Select(x => x.Vector2).ToList()
                            );
        }


        protected override void CalculatePath() {
            if (startTiles == null) {
                startTiles = new List<Tile> {
                startTile
            };
                endTiles = new List<Tile> {
                DestTile
            };
            }
            IsAtDestination = false;
            Queue<Tile> currentQueue = null;
            foreach (Tile st in startTiles) {
                foreach (Tile et in endTiles) {
                    Path_AStar pa = new Path_AStar(debug, startTiles[0].Island, st, et, startTiles, endTiles, CanEndInUnwakable);
                    if (pa.path == null || currentQueue != null && currentQueue.Count < pa.path.Count) {
                        continue;
                    }
                    currentQueue = pa.path;
                }
            }
        
            worldPath = new Queue<Vector2>();
            while (currentQueue.Count > 0) {
                Vector2 pos = currentQueue.Dequeue().Vector2 + new Vector2(0.5f, 0.5f);
                worldPath.Enqueue(pos);
                if (currentQueue.Count == 0) {
                    DestTile = World.Current.GetTileAt(pos);
                    dest_X = pos.x;
                    dest_Y = pos.y;
                }
            }
            if (CurrTile == null) {
                CurrTile = World.Current.GetTileAt(worldPath.Peek());
                X = worldPath.Peek().x;
                Y = worldPath.Peek().y;
                worldPath.Dequeue();
            }
            CreateReversePath();
            //important
            IsDoneCalculating = true;
        }

    }
}