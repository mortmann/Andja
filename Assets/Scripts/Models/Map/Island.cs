using System.Collections.Generic;
using UnityEngine;
using System;

public class Island {
    public Path_TileGraph tileGraph { get; protected set; }
    public List<Tile> myTiles;
    public List<City> myCities;
	public Action<City> cbCityCreated;

    //TODO: get a tile to start with!
    public Island(Tile startTile) {
        myTiles = new List<Tile>();
        myTiles.Add(startTile);
        myCities = new List<City>();
        startTile.myIsland = this;
        foreach (Tile t in startTile.GetNeighbours()) {
            IslandFloodFill(t);
        }
        tileGraph = new Path_TileGraph(this);
    }
    protected void IslandFloodFill(Tile tile) {
        if (tile == null) {
            // We are trying to flood fill off the map, so just return
            // without doing anything.
            return;
        }
        if (tile.Type == TileType.Water) {
            // Water is the border of every island :>
            return;
        }
        if(tile.myIsland == this) {
            // already in there
            return;
        }
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);
        while (tilesToCheck.Count > 0) {
            Tile t = tilesToCheck.Dequeue();
            if (t.Type != TileType.Water && t.myIsland != this) {
                myTiles.Add(t);
                t.myIsland = this;
                Tile[] ns = t.GetNeighbours();
                foreach (Tile t2 in ns) {
                    tilesToCheck.Enqueue(t2);
                }
            }
        }
    }

    public void update(float deltaTime) {
        foreach(City c in myCities) {
            c.update(deltaTime);
        }
    }

    public City CreateCity() {
        City c = new City(this);
		myCities.Add (c);
		if (cbCityCreated != null)
			cbCityCreated(c);
        return c;
    }


}
