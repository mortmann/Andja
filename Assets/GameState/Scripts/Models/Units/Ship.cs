using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Ship : Unit {
	public TradeRoute tradeRoute;

	bool goingToOffworldMarket;
	public bool isOffWorld;
	Item[] toBuy;
	float offWorldTime;

	public Ship(Tile t,int playernumber){
		this.playerNumber = playernumber;
		inventory = new Inventory (6, "SHIP");
		isShip = true;
		pathfinding = new Pathfinding (speed, t);
		speed = 2f;
		offWorldTime = 5f;
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
		if (cbUnitChanged != null)
			cbUnitChanged(this);
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
			inventory.addItem (om.BuyItemToOffWorldMarket (item,item.count,myPlayer));
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
			goal = World.current.GetTileAt (0, Y);
		}
		if(X<Y){
			goal = World.current.GetTileAt (X,0);
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
		Tile tile = World.current.GetTileAt(x, y);
		if(tile == null){
			return;
		}
		if (tile.Type != TileType.Ocean) {
			return;
		}
		onPatrol = false;
		pathfinding.AddMovementCommand( x, y);
	}
}
