using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Combat;

public class UnitPrototypeData : LanguageVariables {
    public int PopulationLevel = 0;
    public int PopulationCount = 0;
    public int inventoryPlaces;
    public int inventorySize;
    public int maintenancecost;
    public int buildcost;

    public DamageType myDamageType;
    public ArmorType myArmorType;

    public Item[] buildingItems;
    public string spriteBaseName;
    public float buildTime = 1f;
    public float maximumHealth;
    public float attackRange = 1f;
    public float damage = 10;
    public float attackRate = 1;
    public float speed;
    public float rotationSpeed = 2f;
    public float aggroTime = 2f;
    public float captureSpeed=0.01f;

    public float width = 0;
    public float height = 0;
}

public enum UnitDoModes { Idle, Move, Fight, Capture, Trade, OffWorld }
public enum UnitMainModes { Idle, Moving, Aggroing, Attack, Patrol, Capture, TradeRoute, OffWorldMarket, Escort, PickUpCrate }

[JsonObject(MemberSerialization.OptIn)]
public class Unit : IGEventable,IWarfare {
    public readonly float EscortDistance = 2f;

    //save these Variables
    #region Serialize
    [JsonPropertyAttribute] public string ID;
    [JsonPropertyAttribute] public int playerNumber;
    [JsonPropertyAttribute] protected string _UserSetName;
    [JsonPropertyAttribute] protected float _currHealth;

    [JsonPropertyAttribute] float aggroCooldownTimer = 1f;

    [JsonPropertyAttribute] Queue<Command> queuedCommands;
    [JsonPropertyAttribute] public PatrolCommand patrolCommand;

    [JsonPropertyAttribute] public float tradeTime = 1.5f;
    [JsonPropertyAttribute] public float attackCooldownTimer = 1;
    [JsonPropertyAttribute] public Pathfinding pathfinding;
    [JsonPropertyAttribute] public Inventory inventory;
    [JsonPropertyAttribute] protected UnitDoModes _CurrentDoingMode = UnitDoModes.Idle;
    [JsonPropertyAttribute] protected UnitMainModes _CurrentMainMode = UnitMainModes.Idle;
    public UnitDoModes CurrentDoingMode {
        get {
            return _CurrentDoingMode;
        }
        set {
            if (_CurrentDoingMode != value)
                Debug.Log(value);
            _CurrentDoingMode = value;
        }
    }
    public UnitMainModes CurrentMainMode {
        get {
            return _CurrentMainMode;
        }
        set {
            if(_CurrentMainMode != value)
                Debug.Log(value);
            _CurrentMainMode = value;
        }
    }
    #endregion
    //being calculated at runtime
    #region calculated 
    //TODO decide on this:
    public float BuildRange {
        get {
            return AttackRange;
        }
    }

    public Command CurrentCommand => queuedCommands.Count == 0 ? null : queuedCommands.Peek();
    public ITargetable CurrentTarget {
        get {
            if (CurrentCommand is AttackCommand == false)
                return null;
            return ((AttackCommand)CurrentCommand).target;
        }
    }

    public string PlayerSetName {
        get {
            return _UserSetName;
        }
        protected set {
            _UserSetName = value;
        }
    }
    public float CurrentHealth {
        get { return _currHealth; }
        protected set {
            if (value <= 0) {
                Destroy();
            }
            _currHealth = value;
        }
    }
    internal void ReduceHealth(float damage) {
        if (damage < 0) {
            damage = -damage;
            Debug.LogWarning("Damage should be never smaller than 0 - Fixed it!");
        }
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }
    public void RepairHealth(float heal) {
        if (heal < 0) {
            heal = -heal;
            Debug.LogWarning("Healing should be never smaller than 0 - Fixed it!");
        }
        CurrentHealth += heal;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }
    internal void ChangeHealth(float change) {
        if (change < 0)
            ReduceHealth(-change); //damage should not be negativ
        if (change > 0)
            RepairHealth(change);
    }
    public OutputStructure rangeUStructure;
    protected Action<Unit> cbUnitChanged;
    protected Action<Unit> cbUnitDestroyed;
    protected Action<Projectile> cbCreateProjectile;
    protected Action<Unit, string> cbUnitSound;
    public float X {
        get {
            return pathfinding.X;
        }
    }
    public float Y {
        get {
            return pathfinding.Y;
        }
    }
    public float Rotation {
        get {
            return pathfinding.rotation;
        }
    }

    internal void Load() {
        Setup();
        if (pathfinding.IsAtDestination == false)
            pathfinding.cbIsAtDestination += OnArriveDestination;
    }

    private void Setup() {
        World.Current.RegisterOnEvent(OnEventCreate, OnEventEnded);
        inventory.OnMeChanged(inventory);
        if (IsShip)
            ((OceanPathfinding)pathfinding).Ship = (Ship)this;
    }

    public Vector3 VectorPosition {
        get { return new Vector3(X, Y); }
    }
    public Vector2 Vector2Position {
        get { return new Vector2(X, Y); }
    }
    public bool IsDead {
        get { return _currHealth <= 0; }
    }
    #endregion
    //gets from prototyp / being loaded in from masterfile
    #region prototype
    public float CaptureSpeed => Data.captureSpeed;

    public float AttackRange => CalculateRealValue("attackRange", Data.attackRange);
    public float Damage => CalculateRealValue("damage", Data.damage);
    public float MaxHealth => CalculateRealValue("maximumHealth", Data.maximumHealth);
    public float AttackRate => CalculateRealValue("attackRange", Data.attackRange);
    public float Speed => CalculateRealValue("speed", Data.speed) * SpeedModifier;

    public virtual float SpeedModifier => 1f;

    public float RotationSpeed => CalculateRealValue("rotationSpeed", Data.rotationSpeed);
    public int InventoryPlaces => CalculateRealValue("inventoryPlaces", Data.inventoryPlaces); //UNTESTED HOW THIS WILL WORK
    public int InventorySize => CalculateRealValue("inventorySize", Data.inventorySize); //UNTESTED HOW THIS WILL WORK
    public float AggroTime => CalculateRealValue("aggroTime", Data.aggroTime); //UNTESTED HOW THIS WILL WORK
    public int MaintenanceCost => CalculateRealValue("maintenancecost", Data.maintenancecost); //UNTESTED HOW THIS WILL WORK


    public virtual bool IsShip => false;

    public float BuildTime => Data.buildTime;
    public int BuildCost => Data.buildcost;

    public virtual Unit Clone(int playerNumber, Tile t) {
        return new Unit(this, playerNumber, t);
    }

    public float Width => Data.width;
    public float Height => Data.height;
    public Item[] BuildingItems => Data.buildingItems;
    public string Name => Data.Name;

    #endregion

    protected UnitPrototypeData _prototypData;
    public UnitPrototypeData Data {
        get {
            if (_prototypData == null) {
                _prototypData = PrototypController.Instance.GetUnitPrototypDataForID(ID);
            }
            return _prototypData;
        }
    }

    public Vector2 CurrentPosition => VectorPosition;
    public Vector2 NextDestinationPosition => pathfinding.NextDestination;
    public Vector2 LastMovement => pathfinding.LastMove;

    public int PlayerNumber => playerNumber;

    public float MaximumHealth { get { return CalculateRealValue("maximumHealth", Data.maximumHealth); } }
    public virtual float CurrentDamage => CalculateRealValue("CurrentDamage", Data.damage);
    public virtual float MaximumDamage => CalculateRealValue("MaximumDamage", Data.damage);    
    public DamageType MyDamageType => Data.myDamageType;
    public ArmorType MyArmorType => Data.myArmorType;
    public bool IsDestroyed => IsDead;

    public List<Command> QueuedCommands => queuedCommands == null ? null : new List<Command>(queuedCommands);


    public override string GetID() { return ID; } // only needs to get changed WHEN there is diffrent ids

    [JsonConstructor]
    public Unit() {
        if (queuedCommands == null)
            queuedCommands = new Queue<Command>();
        if (patrolCommand == null)
            patrolCommand = new PatrolCommand();
    }

    public Unit(string id, UnitPrototypeData upd) {
        this.ID = id;
        this._prototypData = upd;
        patrolCommand = new PatrolCommand();
    }

    public Unit(Unit unit, int playerNumber, Tile t) {
        this.ID = unit.ID;
        patrolCommand = new PatrolCommand();
        this._prototypData = unit.Data;
        this.CurrentHealth = MaxHealth;
        this.playerNumber = playerNumber;
        PlayerSetName = "Unit " + UnityEngine.Random.Range(0, 1000000000);
        pathfinding = new IslandPathfinding(this, t);
        queuedCommands = new Queue<Command>();
        Setup();
    }
    public virtual void Update(float deltaTime) {
        if (CurrentCommand != null && CurrentCommand.IsFinished) {
            queuedCommands.Dequeue();
            if (CurrentCommand == null)
                CurrentMainMode = UnitMainModes.Idle; // no commands so be lazy
        }
        switch (CurrentMainMode) {
            case UnitMainModes.Idle:
                if(CurrentDoingMode!=UnitDoModes.Idle)
                    CurrentDoingMode = UnitDoModes.Idle;
                if (CurrentCommand != null) {
                    CurrentMainMode = CurrentCommand.MainMode;
                }
                break;
            case UnitMainModes.Moving:
                if (CurrentDoingMode != UnitDoModes.Move) {
                    pathfinding.cbIsAtDestination += OnArriveDestination;
                    Vector2 dest = CurrentCommand.Position;
                    SetDestinationIfPossible(dest.x, dest.y);
                    CurrentDoingMode = UnitDoModes.Move;
                }
                break;
            case UnitMainModes.Aggroing:
                if (CurrentTarget != null)
                    CurrentDoingMode = UnitDoModes.Fight;
                if (IsInRange() == false) // TODO: make it possible to have small range where it can "walk" to the enemy!
                    CurrentMainMode = UnitMainModes.Idle;
                break;
            case UnitMainModes.Attack:
                if (IsInRange() == false) {
                    if (CurrentDoingMode != UnitDoModes.Move) {
                        //pathfinding.cbIsAtDestination += OnArriveDestination;
                        Vector2 dest = CurrentTarget.CurrentPosition;
                        SetDestinationIfPossible(dest.x, dest.y);
                    }
                }
                else 
                if(CurrentDoingMode != UnitDoModes.Fight) {
                    //is in range start fighting
                    //if (CurrentCommand is MoveCommand)
                    //    queuedCommands.Dequeue();
                    CurrentDoingMode = UnitDoModes.Fight;
                }
                break;
            case UnitMainModes.Patrol:
                //UpdateAggroRange(deltaTime);
                if (CurrentDoingMode != UnitDoModes.Move) {
                    CurrentDoingMode = UnitDoModes.Move;
                    SetDestinationIfPossible(CurrentCommand.Position);
                    //pathfinding.cbIsAtDestination = null;
                    //pathfinding.cbIsAtDestination += OnArriveDestination;
                }
                break;
            case UnitMainModes.Capture:
                if (IsInRange() == false)
                    CurrentDoingMode = UnitDoModes.Move;
                else
                    CurrentDoingMode = UnitDoModes.Capture;
                break;
            case UnitMainModes.TradeRoute:
                UpdateTradeRoute(deltaTime);
                break;
            case UnitMainModes.OffWorldMarket:
                if (pathfinding.IsAtDestination)
                    UpdateWorldMarket(deltaTime);
                break;
            case UnitMainModes.PickUpCrate:
                TryToAddCrate(((PickUpCrateCommand)CurrentCommand).crate);
                break;
            case UnitMainModes.Escort:
                Debug.LogError("Not implemented yet!");
                break;
        }
        switch (CurrentDoingMode) {
            case UnitDoModes.Idle:
                UpdateAggroRange(deltaTime);
                break;
            case UnitDoModes.Move:
                UpdateMovement(deltaTime);
                break;
            case UnitDoModes.Fight:
                UpdateCombat(deltaTime);
                break;
            case UnitDoModes.Capture:
                UpdateCapture(deltaTime);
                break;
        }
        pathfinding.Update_DoRotate(deltaTime);
    }

    private void SetDestinationIfPossible(Vector2 position) {
        SetDestinationIfPossible(position.x, position.y);
    }

    protected virtual void UpdateWorldMarket(float deltaTime) {
        CurrentMainMode = UnitMainModes.Idle;
    }
    protected virtual void UpdateTradeRoute(float deltaTime) {
        CurrentMainMode = UnitMainModes.Idle;
    }
    private void FollowTarget() {
        GiveAttackCommand(CurrentTarget);
    }
    protected void UpdateMovement(float deltaTime) {
        pathfinding.Update_DoMovement(deltaTime);
        cbUnitChanged?.Invoke(this);
    }
    protected void UpdateAggroRange(float deltaTime) {
        if (CurrentTarget != null) {
            return;
        }
        aggroCooldownTimer -= deltaTime;
        if (aggroCooldownTimer > 0) {
            return;
        }
        aggroCooldownTimer = AggroTime;

        Collider2D[] c2d = Physics2D.OverlapCircleAll(new Vector2(X, Y), Data.attackRange);
        foreach (var item in c2d) {
            //check for not null = only to be sure its not null
            if (item == null) {
                continue;
            }
            ITargetableHoldingScript targetableHoldingScript = item.transform.GetComponent<ITargetableHoldingScript>();
            if (targetableHoldingScript == null || targetableHoldingScript.IsUnit == false) {
                continue;
            }
            Unit u = targetableHoldingScript.Holding as Unit;
            if (u == null || u.playerNumber == playerNumber) {
                continue;
            }
            //see if players are at war
            if (PlayerController.Instance.ArePlayersAtWar(playerNumber, u.playerNumber)) {
                GiveAttackCommand(u);
                CurrentMainMode = UnitMainModes.Aggroing;
                return;
            }
        }
        CurrentMainMode = UnitMainModes.Idle;
    }


    public bool GiveCaptureCommand(ICapturable warfare, bool overrideCurrent = false) {
        if (PlayerController.Instance.ArePlayersAtWar(PlayerNumber, warfare.PlayerNumber) == false) {
            return false;
        }
        if (IsInRange() == false && GiveMovementCommand(ClosestTargetPosition(warfare)) == false)
            return false;
        AddCommand(new CaptureCommand(warfare), overrideCurrent);
        return true;
    }
    public bool GiveAttackCommand(ITargetable target, bool overrideCurrent = false) {
        if (target.IsAttackableFrom(this) == false) {
            return false;
        }
        if (PlayerController.Instance.ArePlayersAtWar(PlayerNumber, target.PlayerNumber) == false) {
            return false;
        }
        //can it reach it?

        if (IsInRange() == false) {
            if (CanReach(ClosestTargetPosition(target)) == false) {
                return false;
            }
            //if (GiveMovementCommand(ClosestTargetPosition(target), overrideCurrent) == false)
            //    return false;
            //else
            //    overrideCurrent = false; // dont override movement command that got added for attacking
        }
        AddCommand(new AttackCommand(target), overrideCurrent);
        return true;
    }

   

    public void AddCommand(Command command, bool overrideCurrent) {
        if (overrideCurrent)
            GoIdle();
        queuedCommands.Enqueue(command);
    }

    public void GoIdle() {
        CurrentMainMode = UnitMainModes.Idle;
        CurrentDoingMode = UnitDoModes.Idle;
        queuedCommands.Clear();
    }

    public virtual bool IsInRange() {
        if (CurrentTarget == null)
            return false;
        return (CurrentTarget.CurrentPosition - CurrentPosition).magnitude <= AttackRange;
    }
    public Vector2 ClosestTargetPosition(ITargetable target) {
        Tile nearstTile = World.Current.GetTileAt(target.CurrentPosition);
        if (nearstTile.Structure == null)
            return target.CurrentPosition;
        if (nearstTile.Structure.IsWalkable)
            return target.CurrentPosition;
        float nearDist = float.MaxValue;
        foreach (Tile item in nearstTile.Structure.neighbourTiles) {
            if (IsShip) {
                if (item.Type != TileType.Ocean) {
                    continue;
                }
            }
            else {
                if (item.Type == TileType.Ocean || item.MovementCost <= 0) {
                    continue;
                }
            }
            float currDist = (item.Vector - pathfinding.CurrTile.Vector).magnitude;
            if (currDist < nearDist) {
                currDist = nearDist;
                nearstTile = item;
            }
        }
        return nearstTile.Vector;
    }
    public bool UpdateCombat(float deltaTime) {
        if (CurrentTarget == null) {
            GoIdle();
            return false;
        }
        if (CurrentTarget.IsDestroyed) {
            GoIdle();
            return false;
        }
        if (PlayerController.Instance.ArePlayersAtWar(CurrentTarget.PlayerNumber, playerNumber) == false) {
            GoIdle();
            return false;
        }
        if (IsInRange()==false) {
            return false;
        }
        DoAttack(deltaTime);
        return true;
    }
    public bool UpdateCapture(float deltaTime) {
        if (CurrentTarget == null) {
            GoIdle();
            return false;
        }
        if (IsShip && CurrentTarget is Ship == false) {
            GoIdle();
            return false; // ships cant capture anything else than ships
        }
        if (CurrentTarget.IsDestroyed) {
            GoIdle();
            return false;
        }
        if (PlayerController.Instance.ArePlayersAtWar(CurrentTarget.PlayerNumber, playerNumber) == false) {
            GoIdle();
            return false;
        }
        if (IsInRange()) {
            return false;
        }
        ((ICapturable)CurrentTarget).Capture(this, CaptureSpeed);
        return true;
    }
    public virtual void DoAttack(float deltaTime) {
        if (CurrentTarget != null) {
            if (attackCooldownTimer > 0) {
                attackCooldownTimer -= deltaTime;
                return;
            }
            attackCooldownTimer = AttackRate;
            CurrentTarget.TakeDamageFrom(this);
        }
    }

    protected void UpdateOnArriveDestinationPatrol() {
        //PATROL
        patrolCommand.ChangeToNextPosition();
        SetDestinationIfPossible(patrolCommand.Position);

    }
    public void IsInRangeOfWarehouse(OutputStructure ware) {
        if (ware == null) {
            Debug.LogError("WARNING Range warehouse null");
            return;
        }
        rangeUStructure = ware;
    }
    public void ToTradeItemToNearbyWarehouse(Item clicked) {
        Debug.Log(clicked.ToString());
        if (rangeUStructure != null && rangeUStructure is WarehouseStructure) {
            if (rangeUStructure.PlayerNumber == playerNumber) {
                rangeUStructure.City.TradeFromShip(this, clicked);
            }
            else {
                Player p = PlayerController.GetPlayer(playerNumber);
                rangeUStructure.City.SellToCity(clicked.ID, p, (Ship)this, clicked.count);
            }
        }
    }

    public void AddPatrolCommand(float targetX, float targetY) {
        Tile tile = World.Current.GetTileAt(targetX, targetY);
        if (tile == null) {
            return;
        }
        if (tile.Type == TileType.Ocean && IsShip == false) {
            return;
        }
        if (tile.Type != TileType.Ocean && IsShip) {
            return;
        }
        if (tile.Type == TileType.Mountain) {
            return;
        }
        if (patrolCommand == null)
            patrolCommand = new PatrolCommand();
        patrolCommand.AddPosition(new Vector2(targetX, targetY));
        if (patrolCommand.PositionCount > 1) {
            AddCommand(patrolCommand, true);
            CurrentMainMode = UnitMainModes.Patrol;
        }
    }
    public void ResumePatrolCommand() {
        if (CurrentMainMode == UnitMainModes.Patrol) {
            return;
        }
        AddCommand(patrolCommand, true);
    }
    public void StopPatrolCommand() {
        if (CurrentMainMode == UnitMainModes.Patrol) {
            queuedCommands.Dequeue();
        }
    }
    public void ClearPatrolCommands() {
        StopPatrolCommand();
        patrolCommand.ClearPositions();
    }
    public bool GiveMovementCommand(Vector2 vec2, bool overrideCurrent = false) {
        return GiveMovementCommand(vec2.x, vec2.y, overrideCurrent);
    }
    public bool GiveMovementCommand(Tile t, bool overrideCurrent = false) {
        if (t == null) {
            //not really an error it can happen
            return false;
        }
        else {
            return GiveMovementCommand(t.X, t.Y);
        }
    }
    public bool GiveMovementCommand(float x, float y, bool overrideCurrent = false) {
        if (x == X && y == Y)
            return true;
        if (CanReach(x, y) == false)
            return false;
        AddCommand(new MoveCommand(new Vector2(x, y)), overrideCurrent);
        return true;
    }

    private void OnArriveDestination(bool atDest) {
        if (atDest == false) {
            return;
        }
        switch (CurrentMainMode) {
            case UnitMainModes.Idle:
                CurrentDoingMode = UnitDoModes.Idle;
                break;
            case UnitMainModes.Moving:
                CurrentMainMode = UnitMainModes.Idle;
                CurrentDoingMode = UnitDoModes.Idle;
                ((MoveCommand)CurrentCommand).SetFinished();
                break;
            case UnitMainModes.Aggroing:
                if (CurrentTarget != null)
                    CurrentDoingMode = UnitDoModes.Fight;
                break;
            case UnitMainModes.Attack:
                if (CurrentTarget != null)
                    CurrentDoingMode = UnitDoModes.Fight;
                else
                    CurrentMainMode = UnitMainModes.Idle;
                break;
            case UnitMainModes.Patrol:
                UpdateOnArriveDestinationPatrol();
                return;//dont unregister from arrivedestination
            case UnitMainModes.Capture:
                CurrentDoingMode = UnitDoModes.Capture;
                break;
            case UnitMainModes.TradeRoute:
                CurrentDoingMode = UnitDoModes.Trade;
                break;
            case UnitMainModes.OffWorldMarket:
                CurrentDoingMode = UnitDoModes.OffWorld;
                break;
        }
        pathfinding.cbIsAtDestination -= OnArriveDestination;
    }

    /// <summary>
    /// Set the destination of this unit!
    /// Returns True if it can reach the goal!
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    protected virtual bool SetDestinationIfPossible(float x, float y) {
        if (CanReach(x, y) == false) {
            return false;
        }
        pathfinding.SetDestination(x, y);
        return true;
    }
    private bool CanReach(Vector2 vec) {
        return CanReach(vec.x, vec.y);
    }

    internal void GivePickUpCrateCommand(Crate crate, bool overrideCurrent) {
        if(crate.IsInRange(CurrentPosition)&&overrideCurrent) {
            TryToAddCrate(crate);
        }
        else {
            CurrentMainMode = UnitMainModes.PickUpCrate;
            CurrentDoingMode = UnitDoModes.Move;
            SetDestinationIfPossible(crate.position.x,crate.position.y);
            //GiveMovementCommand(crate.position,overrideCurrent);
            AddCommand(new PickUpCrateCommand(crate), false);
            pathfinding.cbIsAtDestination += OnArriveDestination;
        }
    }

    public bool CanReach(float x, float y) {
        Tile tile = World.Current.GetTileAt(x, y);
        if (tile == null) {
            return false;
        }
        if (tile.Type == TileType.Ocean && IsShip == false) {
            return false;
        }
        if (tile.Type == TileType.Mountain) {
            return false;
        }
        if (pathfinding.CurrTile.MyIsland != tile.MyIsland) {
            return false;
        }
        return true;
    }

    public int TryToAddItem(Item item) {
        return inventory.AddItem(item);
    }
    public int TryToAddItemMaxAmount(Item item, int amount) {
        Item t = item.Clone();
        t.count = amount;
        return inventory.AddItem(t);
    }
    public void CallChangedCallback() {
        cbUnitChanged?.Invoke(this);
    }
    internal bool TryToAddCrate(Crate thisCrate) {
        if (inventory == null)
            return false;
        if (thisCrate.IsInRange(CurrentPosition)==false)
            return false;
        int pickedup = TryToAddItem(thisCrate.item);
        thisCrate.RemoveItemAmount(pickedup);
        return true;
    }

    public virtual void Destroy() {
        //Do stuff here when on destroyed
        cbUnitDestroyed?.Invoke(this);
        _currHealth = 0;
    }
    #region RegisterCallback
    public void RegisterOnChangedCallback(Action<Unit> cb) {
        cbUnitChanged += cb;
    }
    public void UnregisterOnChangedCallback(Action<Unit> cb) {
        cbUnitChanged -= cb;
    }
    public void RegisterOnDestroyCallback(Action<Unit> cb) {
        cbUnitDestroyed += cb;
    }
    public void UnregisterOnDestroyCallback(Action<Unit> cb) {
        cbUnitDestroyed -= cb;
    }
    public void RegisterOnSoundCallback(Action<Unit, string> cb) {
        cbUnitSound += cb;
    }
    public void UnregisterOnSoundCallback(Action<Unit, string> cb) {
        cbUnitSound -= cb;
    }
    public void RegisterOnCreateProjectileCallback(Action<Projectile> cb) {
        cbCreateProjectile += cb;
    }
    public void UnregisterOnCreateProjectileCallback(Action<Projectile> cb) {
        cbCreateProjectile -= cb;
    }
    #endregion


    public bool IsAttackableFrom(IWarfare warfare) {
        return warfare.MyDamageType.GetDamageMultiplier(MyArmorType) > 0;
    }
    public void TakeDamageFrom(IWarfare warfare) {
        ReduceHealth( warfare.GetCurrentDamage(MyArmorType) );
    }

    internal bool IsPlayerUnit() {
        return PlayerController.currentPlayerNumber == playerNumber;
    }

    public virtual float GetCurrentDamage(ArmorType armorType) {
        return MyDamageType.GetDamageMultiplier(armorType) * CurrentDamage;
    }

    public override void OnEventCreate(GameEvent ge) {
        if (ge.IsTarget(this)) {
            ge.EffectTarget(this, true);
        }
    }

    public override void OnEventEnded(GameEvent ge) {
        if (ge.IsTarget(this)) {
            ge.EffectTarget(this, false);
        }
    }
}
