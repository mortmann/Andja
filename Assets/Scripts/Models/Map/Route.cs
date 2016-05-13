using UnityEngine;
using System.Collections.Generic;

public class Route  {
	public Path_TileGraph tileGraph { get; protected set; }
	public List<Tile> myTiles;
	public Route(Tile startTile){
		myTiles = new List<Tile>();
		myTiles.Add (startTile);
		tileGraph = new Path_TileGraph(this);
	}
//	protected void IslandFloodFill(Tile tile) {
//		if (tile == null) {
//			// We are trying to flood fill off the map, so just return
//			// without doing anything.
//			return;
//		}
//		if (tile.structures == null || tile.structures.BuildTyp != BuildTypes.Path) {
//			// There is no road or structure at all
//			return;
//		}
//		Queue<Tile> tilesToCheck = new Queue<Tile>();
//		tilesToCheck.Enqueue(tile);
//		while (tilesToCheck.Count > 0) {
//			Tile t = tilesToCheck.Dequeue();
//			if (t.Type != TileType.Water) {
//				myTiles.Add(t);
//				Tile[] ns = t.GetNeighbours();
//				foreach (Tile t2 in ns) {
//					tilesToCheck.Enqueue(t2);
//				}
//			}
//		}
//	}
//
	public void addRoadTile(Tile tile){
		myTiles.Add (tile);
		tileGraph.AddNodeToRouteTileGraph (tile);
	}

	public void addRoute(Route route){
		foreach (Tile item in route.myTiles) {
			((Road)item.structures).Route = this;
		}
		tileGraph.addNodes (route.tileGraph);
		myTiles[0].myCity.RemoveRoute (route);

	}

	///for debug purpose only if no longer needed delete
	public string toString(){
		return myTiles[0].toString () + "_Route";
	}
}
