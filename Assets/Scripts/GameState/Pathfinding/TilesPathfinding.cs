using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using UnityEngine;

public class TilesPathfinding : Pathfinding {

    //for structure
    protected List<Tile> startTiles;
    protected List<Tile> endTiles;

    public TilesPathfinding() : base() {
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

        pathDest = Path_Destination.Tile;
        if (startTiles == null) {
            startTiles = new List<Tile> {
                startTile
            };
            endTiles = new List<Tile> {
                DestTile
            };
        }
        IsAtDestination = false;
        Path_AStar pa = new Path_AStar(startTiles[0].Island, startTiles, endTiles);
        if (startTile == null) {
            CurrTile = pa.path.Peek();
            startTile = CurrTile;
        }
        else {
            while (startTile == pa.path.Peek()  && worldPath.Count > 0) {
                pa.Dequeue();
            }
            CurrTile = World.Current.GetTileAt(X, Y);
        }
        worldPath = new Queue<Vector2>();
        while (pa.path.Count > 0) {
            worldPath.Enqueue(pa.path.Dequeue().Vector2);
        }

        DestTile = World.Current.GetTileAt(backPath.Peek());
        dest_X = DestTile.X;
        dest_Y = DestTile.Y;

        worldPath.Enqueue(Destination);
        CreateReversePath();
        backPath.Enqueue(Position2);

        //important
        IsDoneCalculating = true;
    }

}
