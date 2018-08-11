using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class ShipPrototypeData : UnitPrototypeData {
    public int maximumAmountOfCannons = 0;
    public int damagePerCannon = 1;
    //TODO: think about a way it doesnt require each ship to have this 
    //      OR are there sometyp like HEAVY and prototype returns the associated cannon
    public Item cannonType = null;
}
//TODO: think about how if ships could be capturable if they are low, at war and the capturing ship can do it?
[JsonObject(MemberSerialization.OptIn)]
public class Ship : Unit {
    [JsonPropertyAttribute] public TradeRoute tradeRoute;
	[JsonPropertyAttribute] bool goingToOffworldMarket;
	[JsonPropertyAttribute] public bool isOffWorld;
	[JsonPropertyAttribute] Item[] toBuy;
	[JsonPropertyAttribute] float offWorldTime;
    [JsonPropertyAttribute] Item _cannonItem;
    public Item CannonItem {
        get { if(_cannonItem == null) { _cannonItem = ShipData.cannonType.CloneWithCount(); } return _cannonItem; }
    }
    protected ShipPrototypeData _shipPrototypData;
    public int MaximumAmountOfCannons => ShipData.maximumAmountOfCannons;
    public override bool IsShip => true;
    public override float CurrentDamage => CannonItem.count * ShipData.damagePerCannon;
    public override float MaximumDamage => ShipData.maximumAmountOfCannons * ShipData.damagePerCannon;

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
		inventory = new Inventory (6,50);
		offWorldTime = 5f;
		pathfinding = new OceanPathfinding (t,this);
	}
    public Ship(Unit unit, int playerNumber, Tile t) {
        this.ID = unit.ID;
        this._prototypData = unit.Data;
        this.CurrHealth = MaxHealth;
        this.playerNumber = playerNumber;
        inventory = new Inventory(InventoryPlaces, InventorySize);
        UserSetName = "Ship " + UnityEngine.Random.Range(0, 1000000000);
        pathfinding = new OceanPathfinding(t, this);
    }
    public override Unit Clone(int playerNumber, Tile t) {
        return new Ship(this, playerNumber, t);
    }
    public Ship(int id, ShipPrototypeData spd) {
        this.ID = id;
        this._shipPrototypData = spd;
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

    internal bool HasCannonsToAddInInventory() {
        return inventory.ContainsItemWithID(CannonItem.ID);
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

    internal void RemoveCannonsToInventory() {
        CannonItem.count -= inventory.AddItem(CannonItem);
    }

    internal void AddCannonsFromInventory() {
        Item temp = CannonItem.Clone();
        temp.count = MaximumAmountOfCannons - CannonItem.count;
        CannonItem.count += inventory.GetItemWithMaxItemCount(temp).count;
    }

    internal bool CanRemoveCannons() {
        if(CannonItem.count <= 0) {
            return false;
        }
        if (inventory.HasSpaceForItem(CannonItem) == false) {
            return false;
        }
        return true;
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
    /// <summary>
    /// Returns true only if it can reach the exact tile but
    /// will try still to get close as possible to the given coordinates
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
	public override bool AddMovementCommand (float x, float y){
		Tile tile = World.Current.GetTileAt(x, y);
		if(tile == null){
			return false;
		}
		onPatrol = false;
        ((OceanPathfinding)pathfinding).SetDestination(x, y);
        return tile.Type == TileType.Ocean; 
	}
    /// <summary>
    /// Returns the added amount of cannons
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public void AddCannons(Item toAdd) {
        if (toAdd.ID != CannonItem.ID) {
            Debug.LogWarning("Tried to add incombatible cannons to this ship!");
            return;
        }
        int restneeded = ShipData.maximumAmountOfCannons - CannonItem.count;
        int added = Mathf.Clamp(toAdd.count, 0, restneeded);
        CannonItem.count += added;
        toAdd.count -= added;
    }

}
