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
    [JsonPropertyAttribute] public bool isOffWorld;
    [JsonPropertyAttribute] Item[] toBuy;
    [JsonPropertyAttribute] float offWorldTime;
    [JsonPropertyAttribute] Item _cannonItem;
    public Item CannonItem {
        get { if (_cannonItem == null) { _cannonItem = ShipData.cannonType.CloneWithCount(); } return _cannonItem; }
    }
    protected ShipPrototypeData _shipPrototypData;

    public int DamagePerCannon => CalculateRealValue("damagePerCannon", ShipData.damagePerCannon);
    public int MaximumAmountOfCannons => CalculateRealValue("maximumAmountOfCannons", ShipData.maximumAmountOfCannons);
    public override float CurrentDamage => CalculateRealValue("CurrentDamage", DamagePerCannon);
    public override float MaximumDamage => CalculateRealValue("MaximumDamage", MaximumAmountOfCannons * DamagePerCannon);

    public override bool IsShip => true;

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
    public Ship(Tile t, int playernumber) {
        this.playerNumber = playernumber;
        inventory = new Inventory(6, 50);
        offWorldTime = 5f;
        pathfinding = new OceanPathfinding(t, this);
    }
    public Ship(Unit unit, int playerNumber, Tile t) {
        this.ID = unit.ID;
        this._prototypData = unit.Data;
        this.CurrentHealth = MaxHealth;
        this.playerNumber = playerNumber;
        inventory = new Inventory(InventoryPlaces, InventorySize);
        PlayerSetName = "Ship " + UnityEngine.Random.Range(0, 1000000000);
        pathfinding = new OceanPathfinding(t, this);
    }
    public override Unit Clone(int playerNumber, Tile t) {
        return new Ship(this, playerNumber, t);
    }
    public Ship(int id, ShipPrototypeData spd) {
        this.ID = id;
        this._shipPrototypData = spd;
    }

    public override void DoAttack(float deltaTime) {
        if (CurrentTarget != null) {
            if (attackCooldownTimer > 0) {
                return;
            }
            attackCooldownTimer = AttackRate;
            for (int i = 0; i < CannonItem.count; i++) {
                cbCreateProjectile?.Invoke(new Projectile(this, CurrentTarget));
            }
        }
    }

    protected override void UpdateTradeRoute(float deltaTime) {
        if (tradeRoute == null || tradeRoute.Valid == false) {
            CurrentMainMode = UnitMainModes.Idle;
            return;
        }
        if (pathfinding.IsAtDestination) {
            //do trading here
            //take some time todo that
            if (tradeTime > 0) {
                tradeTime -= deltaTime;
                return;
            }
            tradeRoute.DoCurrentTrade(this);
            tradeTime = 1.5f;
            SetDestinationIfPossible(tradeRoute.GetNextDestination(this));
        }
    }
    public void SetTradeRoute(TradeRoute tr) {
        tradeRoute = tr;
        StartTradeRoute();
    }

    public void StartTradeRoute() {
        if (tradeRoute == null)
            return;
        CurrentMainMode = UnitMainModes.TradeRoute;
        SetDestinationIfPossible(tradeRoute.GetNextDestination(this));
    }

    private void SetDestinationIfPossible(Tile tile) {
        SetDestinationIfPossible(tile.X, tile.Y);
    }

    internal bool HasCannonsToAddInInventory() {
        return inventory.ContainsItemWithID(CannonItem.ID);
    }

    protected override void UpdateWorldMarket(float deltaTime) {
        if (pathfinding.IsAtDestination && isOffWorld == false) {
            isOffWorld = true;
            CallChangedCallback();
        }
        if (offWorldTime > 0) {
            offWorldTime -= deltaTime;
            return;
        }
        offWorldTime = 3;
        OffworldMarket om = WorldController.Instance.offworldMarket;
        //FIRST SELL everything in inventory to make space for all the things
        Player myPlayer = PlayerController.GetPlayer(playerNumber);
        Item[] i = inventory.GetAllItemsAndRemoveThem();
        foreach (Item item in i) {
            om.SellItemToOffWorldMarket(item, myPlayer);
        }
        foreach (Item item in toBuy) {
            inventory.AddItem(om.BuyItemToOffWorldMarket(item, item.count, myPlayer));
        }
        isOffWorld = false;
        CurrentMainMode = UnitMainModes.Idle;
        CallChangedCallback();
    }
    /// <summary>
    /// Does not remove itself from TradeRoute 
    /// Instead call it from the TradeRoute -> RemoveShip()!
    /// </summary>
    internal void StopTradeRoute() {
        tradeRoute = null;
        CurrentMainMode = UnitMainModes.Idle;
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
        if (CannonItem.count <= 0) {
            return false;
        }
        if (inventory.HasSpaceForItem(CannonItem) == false) {
            return false;
        }
        return true;
    }

    public void SendToOffworldMarket(Item[] toBuy) {
        //TODO OPTIMISE THIS SO IT CHECKS THE ROUTE FOR ANY
        //ISLANDS SO IT CAN TAKE A OTHER ROUTE
        if (X >= Y) {
            SetDestinationIfPossible(0, Y);
        }
        if (X < Y) {
            SetDestinationIfPossible(X, 0);
        }
        this.toBuy = toBuy;
        CurrentMainMode = UnitMainModes.OffWorldMarket;
    }
    /// <summary>
    /// Returns true only if it can reach the exact tile but
    /// will try still to get close as possible to the given coordinates
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
	protected override bool SetDestinationIfPossible(float x, float y) {
        Tile tile = World.Current.GetTileAt(x, y);
        if (tile == null) {
            return false;
        }
        ((OceanPathfinding)pathfinding).SetDestination(x, y);
        CurrentDoingMode = UnitDoModes.Move;
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
    public override float GetCurrentDamage(Combat.ArmorType armorType) {
        return MyDamageType.GetDamageMultiplier(armorType) * ShipData.damagePerCannon;
    }
}
