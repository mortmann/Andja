using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using UnityEngine;

public class TilesPathfinding : Pathfinding {

	//for building 
	protected List<Tile> startTiles;
	protected List<Tile> endTiles;

    public TilesPathfinding() : base() {
    }
    public override void SetDestination(Tile end) {
        DestTile = end;
        CalculatePath();
    }

    public override void SetDestination(float x, float y) {
        //get the tiles from the world to get a current reference and not an empty from the load
        DestTile = World.Current.GetTileAt(x, y);
        startTile = World.Current.GetTileAt(startTile.X, startTile.Y);
        CalculatePath();
    }
    public void SetDestination(List<Tile> startTiles, List<Tile> endTiles) {
		this.startTiles = startTiles;
        this.endTiles = endTiles;
		Thread calcPath = new Thread (CalculatePath);
		calcPath.Start ();
	}

	protected override void CalculatePath(){
        pathDest = Path_dest.tile;

        if (startTiles==null){
            startTiles = new List<Tile> {
                startTile
            };
            endTiles = new List<Tile> {
                DestTile
            };
        }
        IsAtDest = false;
        Path_AStar pa = new Path_AStar (startTiles[0].MyIsland,startTiles,endTiles);
		worldPath = pa.path;
        CreateReversePath();

        if (startTile == null) {
            CurrTile = worldPath.Peek();
            startTile = CurrTile;
        } else {
            while (CurrTile != worldPath.Peek() && worldPath.Count > 0) {
                worldPath.Dequeue();
            }
            CurrTile = World.Current.GetTileAt(X, Y);
            //worldPath.Dequeue();
        }
        DestTile = backPath.Peek ();
        dest_X = DestTile.X;
        dest_Y = DestTile.Y;
	}

}
