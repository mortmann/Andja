using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using System.Linq;

public enum Climate { Cold, Middle, Warm };

[JsonObject(MemberSerialization.OptIn)]
public class Island : IGEventable {
    #region Serialize

    [JsonPropertyAttribute] public List<City> myCities;
    [JsonPropertyAttribute] public Climate myClimate;
    [JsonPropertyAttribute] public Dictionary<string, int> Ressources;
    [JsonPropertyAttribute] public Tile StartTile;

    #endregion
    #region RuntimeOrOther
    public List<Fertility> myFertilities;
    public Path_TileGraph TileGraphIslandTiles { get; protected set; }
    public int Width {
        get {
            return Mathf.CeilToInt(Maximum.x - Minimum.x);
        }
    }
    public int Height {
        get {
            return Mathf.CeilToInt(Maximum.y - Minimum.y);
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
    public Vector2 Minimum;
    public Vector2 Maximum;
    public Vector2 Center;
    private City _wilderness;
    public bool allReadyHighlighted;
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
        Ressources = new Dictionary<string, int>();
        myCities = new List<City>();

        this.myClimate = climate;
        
        myTiles = new List<Tile>();
        StartTile.MyIsland = this;
        foreach (Tile t in StartTile.GetNeighbours()) {
            IslandFloodFill(t);
        }
        Setup();
    }

    internal void RemoveRessources(string ressourceID, int count) {
        if (Ressources.ContainsKey(ressourceID) == false)
            return;
        Ressources[ressourceID] -= count;
    }

    internal bool HasRessource(string ressourceID) {
        if (Ressources.ContainsKey(ressourceID) == false)
            return false;
        return Ressources[ressourceID] > 0;
    }

    public Island(Tile[] tiles, Climate climate = Climate.Middle) {
        Ressources = new Dictionary<string, int>();
        myCities = new List<City>();
        this.myClimate = climate;
        SetTiles(tiles);
        Setup();
    }
    public Island() {
    }
    private void Setup() {
        allReadyHighlighted = false;
        World.Current.RegisterOnEvent(OnEventCreate, OnEventEnded);
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

    public IEnumerable<Structure> Load() {
        Setup();
        List<Structure> structs = new List<Structure>();
        foreach (City c in myCities) {
            if (c.playerNumber == -1) {
                Wilderness = c;
            }
            c.island = this;
            structs.AddRange(c.Load(this));
        }
        return structs;
    }

    internal void SetTiles(Tile[] tiles) {
        this.myTiles = new List<Tile>(tiles);
        StartTile = tiles[0];
        Minimum = new Vector2(tiles[0].X, tiles[0].Y);
        Maximum = new Vector2(tiles[0].X, tiles[0].Y);
        foreach (Tile t in tiles) {
            t.MyIsland = this;
            if (Minimum.x > t.X) {
                Minimum.x = t.X;
            }
            if (Minimum.y > t.Y) {
                Minimum.y = t.Y;
            }
            if (Maximum.x < t.X) {
                Maximum.x = t.X;
            }
            if (Maximum.y < t.Y) {
                Maximum.y = t.Y;
            }
        }
        Center = Minimum + ((Maximum - Minimum) / 2);
        if (Wilderness != null)
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
        if (tile.MyIsland == this) {
            // already in there
            return;
        }
        Minimum = new Vector2(tile.X, tile.Y);
        Maximum = new Vector2(tile.X, tile.Y);
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);
        while (tilesToCheck.Count > 0) {

            Tile t = tilesToCheck.Dequeue();
            if (Minimum.x > t.X) {
                Minimum.x = t.X;
            }
            if (Minimum.y > t.Y) {
                Minimum.y = t.Y;
            }
            if (Maximum.x < t.X) {
                Maximum.x = t.X;
            }
            if (Maximum.y < t.Y) {
                Maximum.y = t.Y;
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
        return myCities.Find(x => x.playerNumber == playerNumber);
    }
    public City CreateCity(int playerNumber) {
        if (myCities.Exists(x => x.playerNumber == playerNumber)) {
            Debug.LogError("TRIED TO CREATE A SECOND CITY -- IS NEVER ALLOWED TO HAPPEN!");
            return myCities.Find(x => x.playerNumber == playerNumber);
        }
        allReadyHighlighted = false;
        City c = new City(playerNumber, this);
        myCities.Add(c);
        return c;
    }
    public void RemoveCity(City c) {
        myCities.Remove(c);
    }

    #region igeventable
    public override void OnEventCreate(GameEvent ge) {
        OnEvent(ge, cbEventCreated, true);
    }
    void OnEvent(GameEvent ge, Action<GameEvent> ac, bool start) {
        if (ge.target is Island) {
            if (ge.target == this) {
                ge.EffectTarget(this, start);
                ac?.Invoke(ge);
            }
            return;
        }
        else {
            ac?.Invoke(ge);
            return;
        }
    }
    public override void OnEventEnded(GameEvent ge) {
        OnEvent(ge, cbEventEnded, false);
    }

    public override int GetPlayerNumber() {
        return -1;
    }
    #endregion
    public static MapGenerator.Range GetRangeForSize(Size sizeType) {
        switch (sizeType) {
            case Size.VerySmall:
                return new MapGenerator.Range(40, 80);
            case Size.Small:
                return new MapGenerator.Range(80, 120);
            case Size.Medium:
                return new MapGenerator.Range(120, 160);
            case Size.Large:
                return new MapGenerator.Range(160, 200);
            case Size.VeryLarge:
                return new MapGenerator.Range(200, int.MaxValue);
            default:
                //Debug.LogError("NOT RECOGNISED ISLAND SIZE! Nothing has no size!");
                return new MapGenerator.Range(0, 0);
        }
    }
    public static Size GetSizeTyp(int widht, int height) {
        foreach (Size size in Enum.GetValues(typeof(Size))) {
            int middle = widht + height;
            middle /= 2;
            if (GetRangeForSize(size).IsBetween(middle)) {
                return size;
            }
        }
        Debug.LogError("The Island does not fit any Range! Widht = " + widht + " : Height " + height);
        return Size.Other;
    }

}
