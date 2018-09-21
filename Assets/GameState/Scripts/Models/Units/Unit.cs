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
    public float MaxHealth;
    public float attackRange = 1f;
    public float damage = 10;
    public float attackCooldown = 1;
    public float attackRate = 1;
    public float speed;
    public float rotationSpeed = 2f;

    public float width;
    public float height;
}

public enum UnitDoModes { Idle, Move, Fight, Capture, Trade, OffWorld }
public enum UnitMainModes { Idle, Moving, Aggroing, Attack, Patrol, Capture, TradeRoute, OffWorldMarket, Escort }

[JsonObject(MemberSerialization.OptIn)]
public class Unit : IWarfare {
    //save these Variables
    #region Serialize
    [JsonPropertyAttribute] public int ID;
    [JsonPropertyAttribute] public int playerNumber;
	[JsonPropertyAttribute] protected string _UserSetName;
	[JsonPropertyAttribute] protected float _currHealth;

    [JsonPropertyAttribute] float aggroCooldown=1f;

    [JsonPropertyAttribute] public RotatingList<Vector2> PatrolPositions;

    [JsonPropertyAttribute] Queue<Command> queuedCommands;

    [JsonPropertyAttribute] public float tradeTime=1.5f;

    [JsonPropertyAttribute] public Pathfinding pathfinding;
    [JsonPropertyAttribute] public Inventory inventory;
    [JsonPropertyAttribute] public UnitDoModes CurrentDoingMode = UnitDoModes.Idle;
    [JsonPropertyAttribute] public UnitMainModes CurrentMainMode = UnitMainModes.Idle;
    #endregion
    //being calculated at runtime
    #region calculated 
    //TODO decide on this:
    public float BuildRange {
		get {
			return AttackRange;
		}
	}

    public Command CurrentCommand => queuedCommands.Count==0? null : queuedCommands.Peek();
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
	public float CurrHealth {
		get { return _currHealth;}
		protected set {
			if(value<=0){
				Destroy ();
			}
			_currHealth = value;
		}
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
	public Vector3 VectorPosition {
		get {return new Vector3 (X, Y);}
	}
    public Vector2 Vector2Position {
        get { return new Vector2(X, Y); }
    }
    public bool IsDead { 
		get { return _currHealth <= 0;}
	}
    #endregion
    //gets from prototyp / being loaded in from masterfile
    #region prototype
	float aggroTimer = 1f;
    public float attackTimer = 1;

    public float AttackRange => Data.attackRange;
	public float Damage => Data.damage;
    public float MaxHealth => Data.MaxHealth;
    public float AttackRate => Data.attackRange;
	public float Speed => Data.speed; 
	public float RotationSpeed => Data.rotationSpeed;
    public float BuildTime => Data.buildTime;
    public int BuildCost => Data.buildcost;
    public int InventoryPlaces => Data.inventoryPlaces;
    public int InventorySize => Data.inventorySize;
    public virtual bool IsShip => false;

    public virtual Unit Clone(int playerNumber, Tile t) {
        return new Unit(this, playerNumber,t);
    }

    protected internal float Width => Data.width;
	protected internal float Height => Data.height;
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
    public int PlayerNumber => playerNumber;
    public float MaximumHealth => Data.MaxHealth;
    public float CurrentHealth => _currHealth;
    public virtual float CurrentDamage => Damage;
    public virtual float MaximumDamage => Damage;
    public DamageType MyDamageType => Data.myDamageType;
    public ArmorType MyArmorType => Data.myArmorType;
    public bool IsDestroyed => IsDead;

    public List<Command> QueuedCommands => queuedCommands==null? null : new List<Command>(queuedCommands);

    [JsonConstructor]
	public Unit(){
        if(queuedCommands == null)
            queuedCommands = new Queue<Command>();
    }

    public Unit(int id,UnitPrototypeData upd) {
        this.ID = id;
        this._prototypData = upd;
    }

    public Unit(Unit unit, int playerNumber, Tile t) {
        this.ID = unit.ID;
        this._prototypData = unit.Data;
        this.CurrHealth = MaxHealth;
        this.playerNumber = playerNumber;
        PlayerSetName = "Unit " + UnityEngine.Random.Range(0, 1000000000);
        pathfinding = new IslandPathfinding(this, t);
        queuedCommands = new Queue<Command>();
    }
    public virtual void Update(float deltaTime) {
        if (CurrentCommand != null && CurrentCommand.IsFinished) {
            if (queuedCommands.Count > 0)
                queuedCommands.Dequeue();
        }
        switch (CurrentMainMode) {
            case UnitMainModes.Idle:
                CurrentDoingMode = UnitDoModes.Idle;
                if (CurrentCommand != null) {
                    CurrentMainMode = CurrentCommand.MainMode;
                }
                break;
            case UnitMainModes.Moving:
                if (CurrentDoingMode != UnitDoModes.Move) {
                    pathfinding.cbIsAtDestination += OnArriveDestination;
                    Vector2 dest = ((MoveCommand)CurrentCommand).position;
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
                if (IsInRange() == false && CurrentDoingMode != UnitDoModes.Move)
                    CurrentDoingMode = UnitDoModes.Move;
                if (CurrentDoingMode == UnitDoModes.Fight && IsInRange() == false)
                    FollowTarget();
                break;
            case UnitMainModes.Patrol:
                if (IsInRange() == false)
                    CurrentDoingMode = UnitDoModes.Move;
                else
                    CurrentDoingMode = UnitDoModes.Fight;
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
            case UnitMainModes.Escort:
                Debug.LogError("Not Implemented yet!");
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
    }
    protected virtual void UpdateWorldMarket(float deltaTime) {
        CurrentMainMode = UnitMainModes.Idle;
    }
    protected virtual void UpdateTradeRoute(float deltaTime) {
        CurrentMainMode = UnitMainModes.Idle;
    }
    private void FollowTarget() {
        GiveMovementCommand(CurrentTarget.CurrentPosition);
    }
    protected void UpdateMovement(float deltaTime) {
        pathfinding?.Update_DoMovement(deltaTime);
        cbUnitChanged?.Invoke(this);
    }
    protected void UpdateAggroRange(float deltaTime){
		if(CurrentTarget != null){
			return;
		}
		aggroTimer -= deltaTime;
		if(aggroTimer>0){
			return;
		}
		aggroTimer = aggroCooldown;

		Collider2D[] c2d = Physics2D.OverlapCircleAll (new Vector2(X,Y),Data.attackRange * 2);
		foreach (var item in c2d) {
			//check for not null = only to be sure its not null
			
			if (item == null){
				continue;
			}
            ITargetableHoldingScript targetableHoldingScript = item.transform.GetComponent<ITargetableHoldingScript>();
            if (targetableHoldingScript == null || targetableHoldingScript.IsUnit == false) {
				continue;
			}
			Unit u = (Unit) targetableHoldingScript.Holding;
			if(u.playerNumber==playerNumber){
				continue;
			}
			//see if players are at war
			if(PlayerController.Instance.ArePlayersAtWar (playerNumber,u.playerNumber)){
				GiveAttackCommand (u);
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
    public bool GiveAttackCommand(ITargetable warfare, bool overrideCurrent = false) {
        if (warfare.IsAttackableFrom(this) == false) {
            return false;
        }
        if (PlayerController.Instance.ArePlayersAtWar(PlayerNumber, warfare.PlayerNumber) == false) {
            return false;
        }
        //can it reach it?
        if (IsInRange() == false && GiveMovementCommand(ClosestTargetPosition(warfare)) == false)
            return false;
        AddCommand(new AttackCommand(warfare), overrideCurrent);
        return true;
    }
    public void AddCommand(Command command, bool add) {
        if (add == false)
            GoIdle();
        queuedCommands.Enqueue(command);
    }

    public void GoIdle() {
        CurrentMainMode = UnitMainModes.Idle;
        CurrentDoingMode = UnitDoModes.Idle;
        queuedCommands.Clear();
    }
    
    public bool IsInRange() {
        if (CurrentTarget == null)
            return false;
        return (CurrentTarget.CurrentPosition - CurrentPosition).magnitude > AttackRange;
    }
    public Vector2 ClosestTargetPosition(ITargetable target) {
        Tile nearstTile = World.Current.GetTileAt(target.CurrentPosition);
        if (nearstTile.Structure == null)
            return target.CurrentPosition;
        if(nearstTile.Structure.IsWalkable)
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
        if (IsInRange()) {
            return false;
        }
        DoAttack(deltaTime);
        attackTimer -= deltaTime;
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
        ((ICapturable)CurrentTarget).Capture(this, 0.01f);
        return true;
    }
	public virtual void DoAttack(float deltaTime){
		if(CurrentTarget != null){
			if(attackTimer>0){
				return;
			}
			attackTimer = AttackRate;
            CurrentTarget.TakeDamageFrom (this);
		}
	}

	protected void UpdateOnArriveDestinationPatrol(){
        //PATROL
        Vector2 vec = PatrolPositions.First;
        pathfinding.SetDestination (vec.x , vec.y);
	}
	public void IsInRangeOfWarehouse(OutputStructure ware){
		if(ware==null){
			Debug.LogError ("WARNING Range warehouse null"); 
			return;
		}
		rangeUStructure = ware; 
	}
	public void ToTradeItemToNearbyWarehouse(Item clicked){
		Debug.Log (clicked.ToString ()); 
		if(rangeUStructure != null && rangeUStructure is Warehouse){
			if(rangeUStructure.PlayerNumber == playerNumber){
				rangeUStructure.City.TradeFromShip (this,clicked);
			} else {
				Player p = PlayerController.Instance.GetPlayer (playerNumber);
				rangeUStructure.City.SellToCity (clicked.ID,p,(Ship)this,clicked.count);
			}
		}
	}

	public void AddPatrolCommand(float targetX,float targetY){
		Tile tile = World.Current.GetTileAt(targetX, targetY);
		if(tile == null){
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
        PatrolPositions.Add( new Vector2 (targetX, targetY) );
        if(PatrolPositions.Count>0)
            CurrentMainMode = UnitMainModes.Patrol;
    }
    public void ClearPatrolCommands() {
        PatrolPositions.Clear();
    }
    public bool GiveMovementCommand(Vector2 vec2, bool overrideCurrent = false) {
        return GiveMovementCommand(vec2.x, vec2.y, overrideCurrent);
    }
    public bool GiveMovementCommand(Tile t, bool overrideCurrent = false) {
		if(t==null){
			//not really an error it can happen
			return false;
		} else {
			return GiveMovementCommand(t.X,t.Y);
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
                break;
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
        if (CanReach(x,y) == false) {
            return false;
        }
		if(pathfinding is IslandPathfinding){
			((IslandPathfinding)pathfinding).SetDestination (x,y);
		}
        return true;
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
    public int TryToAddItemMaxAmount(Item item , int amount){
		Item t = item.Clone ();
		t.count = amount;
		return inventory.AddItem (t);
	}
	public void CallChangedCallback(){
        cbUnitChanged?.Invoke(this);
    }
    internal bool TryToAddCrate(Crate thisCrate) {
        if (inventory == null)
            return false;
        Vector2 distance = Vector2Position - thisCrate.position;
        if (distance.magnitude > Crate.pickUpDistance)
            return false;
        int pickedup = TryToAddItem(thisCrate.item);
        thisCrate.RemoveItemAmount(pickedup);
        return true;
    }

    public virtual void Destroy(){
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
	public void RegisterOnSoundCallback(Action<Unit,string> cb) {
		cbUnitSound += cb;
	}
	public void UnregisterOnSoundCallback(Action<Unit,string> cb) {
		cbUnitSound -= cb;
	}
    public void RegisterOnbCreateProjectileCallback(Action<Projectile> cb) {
        cbCreateProjectile += cb;
    }
    public void UnregisterOnbCreateProjectileCallback(Action<Projectile> cb) {
        cbCreateProjectile -= cb;
    }
    #endregion


    public bool IsAttackableFrom(IWarfare warfare) {
        return warfare.MyDamageType.GetDamageMultiplier(MyArmorType) > 0;
    }
    public void TakeDamageFrom(IWarfare warfare) {
        CurrHealth -= warfare.GetCurrentDamage(MyArmorType);
    }

    internal bool IsPlayerUnit() {
        return PlayerController.currentPlayerNumber == playerNumber;
    }

    public virtual float GetCurrentDamage(ArmorType armorType) {
        return MyDamageType.GetDamageMultiplier(armorType) * CurrentDamage;
    }

}
