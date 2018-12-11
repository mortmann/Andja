using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using System.Linq;

public enum Climate {Cold,Middle,Warm};

[JsonObject(MemberSerialization.OptIn)]
public class Island : IGEventable {
    public const int TargetType = 11;
    #region Serialize

    [JsonPropertyAttribute] public List<City> myCities;
    [JsonPropertyAttribute] public Climate myClimate;
    [JsonPropertyAttribute] public Dictionary<string, int> myRessources;
    [JsonPropertyAttribute] public Tile StartTile;

    #endregion
    #region RuntimeOrOther
    public List<Fertility> myFertilities;
    public Path_TileGraph TileGraphIslandTiles { get; protected set; }
    public int Width {
        get {
            return Mathf.CeilToInt (max.x - min.x);
        }
    }
    public int Height {
        get {
            return Mathf.CeilToInt (max.y - min.y);
        }
    }
    public City Wilderness {
        get {
            if (_wilderness == null)
                _wilderness = myCities.Find(x => x.playerNumber == -1);
            return _wilderness;
        }

        set {
            _wilderness = value;
        }
    }

    public List<Tile> myTiles;
    public Vector2 Placement;
    public Vector2 min;
	public Vector2 max;
    private City _wilderness;
    public bool allReadyHighlighted;
	Action<GameEvent> cbEventCreated;
	Action<GameEvent> cbEventEnded;

	#endregion
    /// <summary>
    /// Initializes a new instance of the <see cref="Island"/> class.
	/// DO not change anything in here unless(!!) it should not happen on load also
	/// IF both times should happen then put it into Setup!
    /// </summary>
    /// <param name="startTile">Start tile.</param>
    /// <param name="climate">Climate.</param>
	public Island(Tile startTile, Climate climate = Climate.Middle) {
		StartTile = startTile; // if it gets loaded the StartTile will already be set
		myRessources = new Dictionary<string, int> ();
		myCities = new List<City>();
		
        this.myClimate = climate;
        //TODO REMOVE THIS
        //LOAD this from map file?
        myRessources["stone"] = int.MaxValue;
        myTiles = new List<Tile>();
        StartTile.MyIsland = this;
        foreach (Tile t in StartTile.GetNeighbours()) {
            IslandFloodFill(t);
        }
        Setup();
    }
    public Island(Tile[] tiles, Climate climate = Climate.Middle) {
        myRessources = new Dictionary<string, int>();
        myCities = new List<City>();
        this.myClimate = climate;
        SetTiles(tiles);
        Setup();
        //TODO REMOVE THIS
        //LOAD this from map file?
        myRessources["stone"] = int.MaxValue;
    }
    public Island(){
	}
	private void Setup(){
        allReadyHighlighted = false;
        World.Current.RegisterOnEvent(OnEventCreated, OnEventEnded);
        //city that contains all the structures like trees that doesnt belong to any player
        //so it has the playernumber -1 -> needs to be checked for when buildings are placed
        //have a function like is notplayer city
        //it does not need NEEDs
        if (myCities.Count > 0) {
            return; // this means it got loaded in so there is already a wilderness
        }
        myCities.Add(new City(myTiles, this));
        Wilderness = myCities[0];
	}

	public IEnumerable<Structure> Load(){
		Setup ();
		List<Structure> structs = new List<Structure>();
		foreach(City c in myCities){
			if(c.playerNumber == -1){
				Wilderness = c;
			}
			c.island = this;
			structs.AddRange(c.Load ());
		}
		return structs;
	}

    internal void SetTiles(Tile[] tiles) {
        this.myTiles = new List<Tile>(tiles);
        StartTile = tiles[0];
        min = new Vector2(tiles[0].X, tiles[0].Y);
        max = new Vector2(tiles[0].X, tiles[0].Y);
        foreach (Tile t in tiles) {
            t.MyIsland = this;
            if (min.x > t.X) {
                min.x = t.X;
            }
            if (min.y > t.Y) {
                min.y = t.Y;
            }
            if (max.x < t.X) {
                max.x = t.X;
            }
            if (max.y < t.Y) {
                max.y = t.Y;
            }
        }
        if(Wilderness!=null)
            Wilderness.AddTiles(myTiles);
        TileGraphIslandTiles = new Path_TileGraph(this);
    }

    /// <summary>
    /// DEPRACATED -- Not needed anymore! Tiles are now determined by the Mapgenerator, which gives the world them for each island!
    /// </summary>
    /// <param name="tile"></param>
    protected void IslandFloodFill(Tile tile) {
        if (tile == null) {
            // We are trying to flood fill off the map, so just return
            // without doing anything.
            return;
        }
        if (tile.Type == TileType.Ocean) {
            // Water is the border of every island :>
            return;
        }
        if(tile.MyIsland == this) {
            // already in there
            return;
        }
        min = new Vector2(tile.X, tile.Y);
        max = new Vector2(tile.X, tile.Y);
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);
        while (tilesToCheck.Count > 0) {
			
            Tile t = tilesToCheck.Dequeue();
			if (min.x > t.X) {
				min.x = t.X;
			}
			if (min.y > t.Y) {
				min.y= t.Y;
			}
			if (max.x < t.X) {
				max.x = t.X;
			}
			if (max.y < t.Y) {
				max.y = t.Y;
			}


            if (t.Type != TileType.Ocean && t.MyIsland != this) {
                myTiles.Add(t);
                t.MyIsland = this;
                Tile[] ns = t.GetNeighbours();
                foreach (Tile t2 in ns) {
                    tilesToCheck.Enqueue(t2);
                }
            }
        }
        TileGraphIslandTiles = new Path_TileGraph(this);
    }

    public void Update(float deltaTime) {
		for (int i = 0; i < myCities.Count; i++) {
			myCities[i].Update(deltaTime);
        }
    }
	public City FindCityByPlayer(int playerNumber) {
		return myCities.Find(x=> x.playerNumber == playerNumber);
	}
	public City CreateCity(int playerNumber) {
		allReadyHighlighted = false;
		City c = new City(playerNumber,this);
		myCities.Add (c);
        return c;
    }
	public void RemoveCity(City c) {
		myCities.Remove (c);
	}

	public void RegisterOnEvent(Action<GameEvent> create,Action<GameEvent> ending){
		cbEventCreated += create;
		cbEventEnded += ending;
	}
	public void OnEventCreated(GameEvent ge){
		OnEvent (ge,cbEventCreated,true);
	}
	void OnEvent(GameEvent ge, Action<GameEvent> ac,bool start){
		if(ge.target is Island){
			if(ge.target == this){
				ge.InfluenceTarget (this, start);
                ac?.Invoke(ge);
            }
			return;
		} else {
            ac?.Invoke(ge);
            return;	
		}
	}
	public void OnEventEnded(GameEvent ge){
		OnEvent (ge,cbEventEnded,false);
	}
	public int GetPlayerNumber(){
		return -1;
	}
	public int GetTargetType(){
		return TargetType;
	}

    public static MapGenerator.Range GetRangeForSize(Size sizeType) {
        switch (sizeType) {
            case Size.VerySmall:
            return new MapGenerator.Range(40, 60);
            case Size.Small:
            return new MapGenerator.Range(60, 80);
            case Size.Medium:
            return new MapGenerator.Range(80, 120);
            case Size.Large:
            return new MapGenerator.Range(120, 140);
            case Size.VeryLarge:
            return new MapGenerator.Range(140, 160);
            default:
            //Debug.LogError("NOT RECOGNISED ISLAND SIZE! Nothing has no size!");
            return new MapGenerator.Range(0, 0);
        }
    }
    public static Size GetSizeTyp(int widht, int height) {
        foreach(Size size in Enum.GetValues(typeof(Size))) {
            int middle = widht + height;
            middle /= 2;
            if (GetRangeForSize(size).IsBetween(middle)) {
                return size;
            }
        }
        Debug.LogError("The Island does not fit any Range! Widht = " + widht + " : Height " + height );
        return Size.Other;
    }

}
