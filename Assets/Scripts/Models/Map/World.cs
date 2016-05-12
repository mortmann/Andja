using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class World {
    public Tile[,] tiles { get; protected set; }
    public int Width { get; protected set; }
    public int Height { get; protected set; }
    public Path_TileGraph tileGraph { get; set; }
    public List<Island> islandList { get; protected set; }
    public List<Unit> units { get; protected set; }


    Action<Unit> cbUnitCreated;
	Action<Worker> cbWorkerCreated;
    Action<Tile> cbTileChanged;

    //get { return height; } protected set { height = value;}

    public World(int width = 100, int height = 100){
        this.Width = width;
        this.Height = height;
        tiles = new Tile[width, height];
        for (int x = 0; x < width; x++) {
            for (int y= 0; y < height ; y++) {
                tiles[x, y] = new Tile(this, x, y);
                if (x > 40 && x < 60) {
                    if (y > 40 && y < 60) {
                        tiles[x, y].Type = TileType.Dirt;
                    }
                }
            }
        }
        
        tileGraph = new Path_TileGraph(this);
        islandList = new List<Island>();
        units = new List<Unit>();
//        CreateUnit(tiles[30, 30]);    
        islandList.Add(new Island(tiles[41, 41]));
		islandList [0].CreateCity ();
		foreach(Tile t in islandList [0].myTiles){
			islandList [0].myCities [0].addTile (t);
		}
    }
		
    internal void update(float deltaTime) {
        foreach(Island i in islandList) {
            i.update(deltaTime);
        }
    }
    public Tile GetTileAt(int x,int y){
        if (x >= Width ||y >= Height ) {
            return null;
        }
        if (x < 0 || y < 0) {
            return null;
        }
        return tiles[x, y];
    }
	public bool IsInTileAt(Tile t,float x,float y){
		if (x >= Width ||y >= Height ) {
			return false;
		}
		if (x < 0 || y < 0) {
			return false;
		}
		if (x + 0.5f <= t.X + 0.4f && x + 0.5f >= t.X - 0.4f) {
			if (y + 0.5f <= t.Y + 0.4f && y + 0.5f >= t.Y - 0.4f) {
				return true;
			}
		}
		return false;
	}
    public Tile GetTileAt(float fx, float fy) {
        int x = Mathf.FloorToInt(fx);
        int y = Mathf.FloorToInt(fy);
        if (x >= Width || y >= Height) {
            return null;
        }
        if (x < 0 || y < 0) {
            return null;
        }
        return tiles[x, y];
    }
    public Unit CreateUnit(Tile t) {
		GameObject go = GameObject.Instantiate((GameObject)Resources.Load ("Prefabs/ship",typeof(GameObject)));
//        Unit c = go.AddComponent<Unit>();
		Unit c = go.GetComponent<Unit> ();
        units.Add(c);
        if (cbUnitCreated != null)
            cbUnitCreated(c);
        return c;
    }
	public void CreateWorkerGameObject(Worker worker) {
		if (cbWorkerCreated != null)
			cbWorkerCreated(worker);
	}
  
    public void RegisterTileChanged(Action<Tile> callbackfunc) {
        cbTileChanged += callbackfunc;
    }

    public void UnregisterTileChanged(Action<Tile> callbackfunc) {
        cbTileChanged -= callbackfunc;
    }

    public void RegisterUnitCreated(Action<Unit> callbackfunc) {
        cbUnitCreated += callbackfunc;
    }

    public void UnregisterUnitCreated(Action<Unit> callbackfunc) {
        cbUnitCreated -= callbackfunc;
    }
	public void RegisterWorkerCreated(Action<Worker> callbackfunc) {
		cbWorkerCreated += callbackfunc;
	}

	public void UnregisterWorkerCreated(Action<Worker> callbackfunc) {
		cbWorkerCreated -= callbackfunc;
	}
    // Gets called whenever ANY tile changes
    void OnTileChanged(Tile t) {
        if (cbTileChanged == null)
            return;

        cbTileChanged(t);
    }
}
