using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilePathfinding : Pathfinding {

	//for building 
	protected List<Tile> roadTilesAroundStartStructure;
	protected List<Tile> roadTilesAroundEndStructure;

	public TilePathfinding(List<Tile> startTiles, List<Tile> endTiles, float speed) {
		roadTilesAroundStartStructure = startTiles;
		roadTilesAroundEndStructure = endTiles;

	
	}

}
