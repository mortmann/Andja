using Andja.Model;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Pathfinding {

    public class TilesPathfinding : BasePathfinding {

        //for structure
        protected List<Tile> startTiles;

        protected List<Tile> endTiles;
        public bool canEndInUnwakable;

        public TilesPathfinding() {

        }
        public TilesPathfinding(float Speed, float RotationSpeed, bool canEndInUnwakable) : base() {
            this.Speed = Speed;
            this.rotationSpeed = RotationSpeed;
            TurnType = Turning_Type.TurnRadius;
            this.canEndInUnwakable = canEndInUnwakable;
        }

        public override void SetDestination(Tile end) {
            DestTile = end;
            CalculatePath();
        }

        public override void SetDestination(float x, float y) {
            if (startTile == null) {
                Debug.LogWarning("This cannot be called when starttile is null! Why did it get called? -- Please fix!");
                return;
            }
            //get the tiles from the world to get a current reference and not an empty from the load
            DestTile = World.Current.GetTileAt(x, y);
            startTile = World.Current.GetTileAt(X, Y);
            StartCalculatingThread();
        }
        bool debug;

        public void SetDestination(List<Tile> startTiles, List<Tile> endTiles, bool debug = false) {
            this.debug = debug;
            this.startTiles = startTiles;
            this.endTiles = endTiles;
            StartCalculatingThread();
        }

        protected override void CalculatePath() {
            pathDestination = Path_Destination.Tile;
            TurnType = Turning_Type.TurnRadius;
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
            try {
                foreach (Tile st in startTiles) {
                    foreach (Tile et in endTiles) {
                        Path_AStar pa = new Path_AStar(debug, startTiles[0].Island, st, et, startTiles, endTiles, canEndInUnwakable);
                        if (pa.path == null || currentQueue != null && currentQueue.Count < pa.path.Count) {
                            continue;
                        }
                        currentQueue = pa.path;
                    }
                }
            } catch(System.Exception e) {
                Debug.Log(e.Message);
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