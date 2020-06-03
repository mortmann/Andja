using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using UnityEngine;

public class TilesPathfinding : Pathfinding {

    //for structure
    protected List<Tile> startTiles;
    protected List<Tile> endTiles;

    public TilesPathfinding(float Speed, float RotationSpeed) : base() {
        this.Speed = Speed;
        this.rotationSpeed = RotationSpeed;
        TurnType = Turning_Type.TurnRadius;
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
        startTile = World.Current.GetTileAt(startTile.X, startTile.Y);
        //CalculatePath();
        StartCalculatingThread();
    }
    public void SetDestination(List<Tile> startTiles, List<Tile> endTiles) {
        this.startTiles = startTiles;
        this.endTiles = endTiles;
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
        IsAtDestination = false;
        Queue<Tile> currentQueue = null;
        foreach (Tile st in startTiles) {
            foreach(Tile et in endTiles) {
                Path_AStar pa = new Path_AStar(startTiles[0].Island, st, et, startTiles, endTiles);
                if (pa.path == null || currentQueue != null && currentQueue.Count < pa.path.Count) {
                    continue;
                }
                currentQueue = pa.path;
            }
        }

        worldPath = new Queue<Vector2>();
        while (currentQueue.Count > 0) {
            worldPath.Enqueue(currentQueue.Dequeue().Vector2+new Vector2(0.5f,0.5f));
        }
        
        if (CurrTile == null) {
            CurrTile = World.Current.GetTileAt(worldPath.Peek());
            X = worldPath.Peek().x;
            Y = worldPath.Peek().y;
            worldPath.Dequeue();
        }
        
        CreateReversePath();
        DestTile = World.Current.GetTileAt(backPath.Peek());

        dest_X = backPath.Peek().x;
        dest_Y = backPath.Peek().y;

        //worldPath.Enqueue(Destination);
        //backPath.Enqueue(Position2);

        //important
        IsDoneCalculating = true;
    }

}
