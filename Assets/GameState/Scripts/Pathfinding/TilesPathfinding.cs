using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using UnityEngine;

public class TilesPathfinding : Pathfinding {

	//for building 
	protected List<Tile> startTiles;
	protected List<Tile> endTiles;

	public void SetDestination(List<Tile> startTiles, List<Tile> endTiles) {
		this.startTiles = startTiles;
		this.endTiles = endTiles;
		Thread calcPath = new Thread (CalculatePath);
		calcPath.Start ();
	}

	protected override void CalculatePath(){
		Path_AStar pa = new Path_AStar (startTiles[0].myIsland,startTiles,endTiles);
		worldPath = pa.path;
		currTile = worldPath.Peek ();
		startTile = currTile;
		CreateReversePath ();
		destTile = backPath.Peek ();
	}

}
