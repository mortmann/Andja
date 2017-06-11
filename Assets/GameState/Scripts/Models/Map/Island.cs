﻿using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using System.Linq;

public enum Climate {Cold,Middle,Warm};

[JsonObject(MemberSerialization.OptIn)]
public class Island : IGEventable{
	public const int TargetType = 11;
	#region Serialize

	[JsonPropertyAttribute] public List<City> myCities;
	[JsonPropertyAttribute] public List<Fertility> myFertilities;
	[JsonPropertyAttribute] public Climate myClimate;
	[JsonPropertyAttribute] public Dictionary<string,int> myRessources;
	[JsonPropertyAttribute] public Tile StartTile;

	#endregion
	#region RuntimeOrOther

	public Path_TileGraph tileGraphIslandTiles { get; protected set; }
	public List<Tile> myTiles;
	public Vector2 min;
	public Vector2 max;
	public City wilderniss;
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
		myFertilities = new List<Fertility> ();
		myRessources = new Dictionary<string, int> ();
		myCities = new List<City>();
		Setup ();
		//TODO REMOVE THIS
		//LOAD this from map file?
		myRessources ["stone"] = int.MaxValue;

    }
	public Island(){
	}
	private void Setup(){
		myTiles = new List<Tile>();
		foreach (Tile t in StartTile.GetNeighbours()) {
			IslandFloodFill(t);
		}
		StartTile.myIsland = this;
		allReadyHighlighted = false;
		World.current.RegisterOnEvent (OnEventCreated,OnEventEnded);
		tileGraphIslandTiles = new Path_TileGraph(this);
	}
	public IEnumerable<Structure> Load(){
		Setup ();
		List<Structure> structs = new List<Structure>();
		foreach(City c in myCities){
			if(c.playerNumber == -1){
				wilderniss = c;
			}
			structs.AddRange(c.Load ());
		}
		return structs;
	}

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
        if(tile.myIsland == this) {
            // already in there
            return;
        }
		min = new Vector2 (tile.X, tile.Y);
		max = new Vector2 (tile.X, tile.Y);
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


            if (t.Type != TileType.Ocean && t.myIsland != this) {
                myTiles.Add(t);
                t.myIsland = this;
                Tile[] ns = t.GetNeighbours();
                foreach (Tile t2 in ns) {
                    tilesToCheck.Enqueue(t2);
                }
            }
        }
		//city that contains all the structures like trees that doesnt belong to any player
		//so it has the playernumber -1 -> needs to be checked for when buildings are placed
		//have a function like is notplayer city
		//it does not need NEEDs
		if(myCities.Count>0){
			return; // this means it got loaded in so there is already a wilderniss
		}
		myCities.Add (new City(myTiles,this)); 
		wilderniss = myCities [0];
//		BuildController.Instance.BuildOnTile (myTiles,true,BuildController.Instance.structurePrototypes[3],true);
    }

    public void update(float deltaTime) {
		for (int i = 0; i < myCities.Count; i++) {
			myCities[i].update(deltaTime);
        }
    }
	public void AddStructure(Structure str){
		allReadyHighlighted = false;
		if(str.City == wilderniss){
//			Debug.LogWarning ("adding to wilderniss wanted?");
		}
		str.City.addStructure (str);
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
				if(ac!=null){
					ac (ge);
				}
			}
			return;
		} else {
			if(ac!=null){
				ac (ge);
			}
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

}
