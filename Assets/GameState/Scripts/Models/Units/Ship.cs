﻿using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ShipPrototypeData : UnitPrototypeData {
    public int maximumAmountOfCannons = 0;
    public int damagePerCannon = 1;
}

[JsonObject(MemberSerialization.OptIn)]
public class Ship : Unit {
	[JsonPropertyAttribute] public TradeRoute tradeRoute;
	[JsonPropertyAttribute] bool goingToOffworldMarket;
	[JsonPropertyAttribute] public bool isOffWorld;
	[JsonPropertyAttribute] Item[] toBuy;
	[JsonPropertyAttribute] float offWorldTime;
    protected ShipPrototypeData _shipPrototypData;

    public ShipPrototypeData ShipData {
        get {
            if (_shipPrototypData == null) {
                _shipPrototypData = (ShipPrototypeData)PrototypController.Instance.GetUnitPrototypDataForID(ID);
            }
            return _shipPrototypData;
        }
    }
    public Ship() {

    }
    public Ship(Tile t,int playernumber){
		this.playerNumber = playernumber;
		inventory = new Inventory (6,50, "SHIP");
		isShip = true;
		offWorldTime = 5f;
		pathfinding = new OceanPathfinding (t,this);
	}
    public Ship(Unit unit, int playerNumber, Tile t) {
        this.ID = unit.ID;
        this._prototypData = unit.Data;
        this.CurrHealth = MaxHealth;
        this.playerNumber = playerNumber;
        inventory = new Inventory(InventoryPlaces, InventorySize, "SHIP");
        isShip = true;
        UserSetName = "Ship " + UnityEngine.Random.Range(0, 1000000000);
        pathfinding = new OceanPathfinding(t, this);
    }
    public override Unit Clone(int playerNumber, Tile t) {
        return new Ship(this, playerNumber, t);
    }
    public Ship(int id, ShipPrototypeData spd) {
        this.ID = id;
        this._shipPrototypData = spd;
        isShip = true;
    }

    public override void Update (float deltaTime){
		//TRADEROUTE
		UpdateTradeRoute (deltaTime);
		//PAROL
		UpdateParol ();
		//WORLDMARKET
		UpdateWorldMarket (deltaTime);
		//MOVE THE SHIP
		pathfinding.Update_DoMovement (deltaTime);
        //		pathfinding.UpdateRotationOnPoint ();
        //		r2d.MovePosition (transform.position + pathfinding.Update_DoMovement(deltaTime));
        //		r2d.MoveRotation (transform.rotation.z + pathfinding.UpdateRotation ());
        cbUnitChanged?.Invoke(this);
    }
	private void UpdateTradeRoute(float deltaTime){
		if(tradeRoute!=null&&tradeRoute.Valid){
			if(pathfinding.IsAtDest&&tradeRoute.isStarted){
				//do trading here
				//take some time todo that
				if(tradeTime<0){
					tradeRoute.doCurrentTrade (this);
					tradeTime = 1.5f;
					//then get a next destination
					AddMovementCommand (tradeRoute.getNextDestination ());	
				} else {
					tradeTime -= deltaTime;
				}
			} 
			if(tradeRoute.isStarted==false){
				//start the route
				AddMovementCommand (tradeRoute.getNextDestination ());
			}
		}
	}
	private void UpdateWorldMarket (float deltaTime){
		if(goingToOffworldMarket==false){
			return;
		}
		if(pathfinding.IsAtDest&& isOffWorld==false){
			isOffWorld = true;
			CallChangedCallback ();
		}
		if(isOffWorld==false){
			return;
		}
		if(offWorldTime>0){
			offWorldTime -= deltaTime;
			return;
		}
		offWorldTime = 3;
		OffworldMarket om = WorldController.Instance.offworldMarket;
		//FIRST SELL everything in inventory to make space for all the things
		Player myPlayer = PlayerController.Instance.GetPlayer (playerNumber);
		Item[] i = inventory.GetAllItemsAndRemoveThem ();
		foreach (Item item in i) {
			om.SellItemToOffWorldMarket (item,myPlayer);
		}
		foreach (Item item in toBuy) {
			inventory.AddItem (om.BuyItemToOffWorldMarket (item,item.count,myPlayer));
		}
		isOffWorld = false;
		this.goingToOffworldMarket = false;
		CallChangedCallback ();
	}

	public void SendToOffworldMarket(Item[] toBuy){
		Tile goal=null;
		//TODO OPTIMISE THIS SO IT CHECKS THE ROUTE FOR ANY
		//ISLANDS SO IT CAN TAKE A OTHER ROUTE
		if(X >= Y){
			goal = World.Current.GetTileAt (0, Y);
		}
		if(X<Y){
			goal = World.Current.GetTileAt (X,0);
		}
		goingToOffworldMarket = true;
		this.toBuy = toBuy;
		OverrideCurrentMission ();
		AddMovementCommand (goal);
	}
	protected override void OverrideCurrentMission (){
		onWayToPatrolTarget = false;
		onPatrol = false;
		if (tradeRoute != null)
			tradeRoute.isStarted = false;
	}
	public override void AddMovementCommand (float x, float y){
		Tile tile = World.Current.GetTileAt(x, y);
		if(tile == null){
			return;
		}
		if (tile.Type != TileType.Ocean) {
			return;
		}
		onPatrol = false;
		((OceanPathfinding)pathfinding).SetDestination(x,y);
	}
}
