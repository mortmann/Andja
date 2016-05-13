using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class City {
    //TODO: set this to the player that creates this
    public int playerNumber = 0;
    public Island island { get; protected set; }
    public Inventory myInv;
    public List<Structure> myStructures;
	public List<Tile> myTiles;
	public List<Route> myRoutes;
	public int cityBalance;

    public City(Island island) {
        this.island = island;
        myInv = new Inventory();
        myStructures = new List<Structure>();
		myTiles = new List<Tile> ();
		myRoutes = new List<Route> ();
    }

    internal void update(float deltaTime) {
        foreach(Structure s in myStructures) {
            s.update(deltaTime);
        }
    }

	public void addStructure(Structure str){
		cityBalance += str.maintenancecost;
		myStructures.Add (str);

	}

	public void addTile(Tile t){
		if (t.Type == TileType.Water) {
			return;
		}
		t.myCity = this;
		myTiles.Add (t);
	}

	public void removeRessources(Item[] remove){
		foreach (Item item in remove) {
			myInv.removeItemAmount (item);
		}
	}

	public void AddRoute(Route route){
		this.myRoutes.Add (route);
	}

	public void RemoveRoute(Route route){
		if(myRoutes.Contains (route)){
			myRoutes.Remove (route);
		} 
	}
	public void removeStructure(Structure structure){
		if (myStructures.Contains (structure)) {
			myStructures.Remove (structure);
			cityBalance -= structure.maintenancecost;
		}
	}

}
