using UnityEngine;
using System.Collections;
using System;
using Newtonsoft.Json;
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
    //public DamageType myDamageType = DamageType.Blade;
    //public ArmorType myArmorType = ArmorType.Leather;
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

//TODO: add feature to capture structures back! -> own command tho!

[JsonObject(MemberSerialization.OptIn)]
public class Unit : IWarfare {
    //save these Variables
    #region Serialize
    [JsonPropertyAttribute] public int ID;
    [JsonPropertyAttribute] public int playerNumber;
	[JsonPropertyAttribute] private string _UserSetName;
	[JsonPropertyAttribute] private float _currHealth;
	[JsonPropertyAttribute] float aggroCooldown=1f;
	//COMBAT STUFF
	[JsonPropertyAttribute] public Vector2 patrolTarget;
	[JsonPropertyAttribute] public Vector2 patrolStart;
	[JsonPropertyAttribute] public bool onWayToPatrolTarget; // false for targetPatrol, true for patrolstart
	[JsonPropertyAttribute] public bool onPatrol = false;
	[JsonPropertyAttribute] public bool hasChanged = false;
	[JsonPropertyAttribute] public float tradeTime=1.5f;
	[JsonPropertyAttribute] public Pathfinding pathfinding;
    [JsonPropertyAttribute] public Inventory inventory;
    [JsonPropertyAttribute] IWarfare _currentTarget;
    #endregion
    //being calculated at runtime
    #region calculated 

    //TODO decide on this:
    public float BuildRange {
		get {
			return AttackRange;
		}
	}

    public IWarfare CurrentTarget {
        get {
            return _currentTarget;
        }
        set {
            _currentTarget = value;
        }
    }

    public string UserSetName {
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
	bool isCapturing;
	protected Action<Unit> cbUnitChanged;
	protected Action<Unit> cbUnitDestroyed;
	protected Action<Unit,string> cbUnitSound;
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

	public Unit(){
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
        UserSetName = "Unit " + UnityEngine.Random.Range(0, 1000000000);
        pathfinding = new IslandPathfinding(this, t);
    }
    public virtual void Update(float deltaTime) {
		//PATROL
		UpdateParol ();
		UpdateAggroRange (deltaTime);
        if (Fighting(deltaTime)) {
            return;
        }
        if (pathfinding!=null)
			pathfinding.Update_DoMovement (deltaTime);

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

		Collider2D[] c2d = Physics2D.OverlapCircleAll (new Vector2(X,Y),2);
		foreach (var item in c2d) {
			//check for not null = only to be sure its not null
			//if its not my own gameobject
			if (item == null){
				continue;
			}
			if (item.gameObject.GetComponent<UnitHoldingScript> () == null) {
				continue;
			}
			Unit u = item.gameObject.GetComponent<UnitHoldingScript> ().unit;
			if(u.playerNumber==playerNumber){
				continue;
			}
			//see if players are at war
			if(PlayerController.Instance.ArePlayersAtWar (playerNumber,u.playerNumber)){
				GiveAttackCommand (u);
			}
		}
	}
    public bool GiveAttackCommand(IWarfare warfare, bool overrideCurrent = false) {
        if (overrideCurrent == false && CurrentTarget != null)
            return false;
        if (warfare.IsAttackableFrom(this) == false) {
            return false;
        }
        if (PlayerController.Instance.ArePlayersAtWar(PlayerNumber, warfare.PlayerNumber) == false) {
            return false;
        }
        float distance = (warfare.CurrentPosition - CurrentPosition).magnitude;
        //can it reach it?
        if (distance > AttackRange == false && AddMovementCommand(ClosestTargetPosition(warfare)) == false)
            return false;
        CurrentTarget = warfare;
        return true;
    }
    public void StopAttack() {
        CurrentTarget = null;
    }

    public Vector2 ClosestTargetPosition(IWarfare target) {
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
	public bool Fighting(float deltaTime){
		if(CurrentTarget!=null){
			if(PlayerController.Instance.ArePlayersAtWar (CurrentTarget.PlayerNumber,playerNumber) == false){
                CurrentTarget = null;
				return false;
			}
			float dist = (CurrentTarget.CurrentPosition  - CurrentPosition).magnitude;
			if(dist<AttackRange){
				DoAttack (deltaTime);
				return true;
			}
		} 
		return false;
	}
	public void DoAttack(float deltaTime){
		if(CurrentTarget != null){
			if(CurrentTarget.IsDestroyed){
                CurrentTarget = null;
			}
			attackTimer -= deltaTime;
			if(attackTimer>0){
				return;
			}
			attackTimer = AttackRate;
            CurrentTarget.TakeDamageFrom (this);
		}
	}

	protected void UpdateParol(){
		//PATROL
		if(onPatrol){
			if(pathfinding.IsAtDest){
				if (onWayToPatrolTarget) {
					pathfinding.SetDestination (patrolStart.x , patrolStart.y);
				} else {
					pathfinding.SetDestination (patrolTarget.x , patrolTarget.y);
				}
				onWayToPatrolTarget = !onWayToPatrolTarget; 
			}
		}
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
		if (tile.Type == TileType.Ocean) {
			return;
		}
		if (tile.Type == TileType.Mountain) {
			return;
		}
		OverrideCurrentMission ();
		onWayToPatrolTarget = true;
		onPatrol = true;
		patrolStart = new Vector2 (X, Y);
		patrolTarget = new Vector2 (targetX, targetY);
//		pathfinding.AddMovementCommand(targetX, targetY);

	}
	protected virtual void OverrideCurrentMission(){
		onWayToPatrolTarget = false;
		onPatrol = false;
	}
    public bool AddMovementCommand(Vector2 vec2) {
        return AddMovementCommand(vec2.x, vec2.y);
    }

    public bool AddMovementCommand(Tile t) {
		if(t==null){
			//not really an error it can happen
			return false;
		} else {
			if(t==pathfinding.CurrTile){
				return true;
			}
			return AddMovementCommand (t.X,t.Y);
		}
	}
    public virtual bool AddMovementCommand(float x, float y) {
		Tile tile = World.Current.GetTileAt(x, y);
        if(tile == null){
            return false;
        }
        if (tile.Type == TileType.Ocean) {
            return false;
        }
        if (tile.Type == TileType.Mountain) {
            return false;
        }
		if(pathfinding.CurrTile.MyIsland!=tile.MyIsland){
			return false;
		}
		onPatrol = false;

		if(pathfinding is IslandPathfinding){
			((IslandPathfinding)pathfinding).SetDestination (x,y);
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
	public virtual void Destroy(){
        //Do stuff here when on destroyed
        cbUnitDestroyed?.Invoke(this);
        _currHealth = 0;
    }
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

    public bool IsAttackableFrom(IWarfare warfare) {
        return warfare.MyDamageType.GetDamageMultiplier(MyArmorType) > 0;
    }

    public void TakeDamageFrom(IWarfare warfare) {
        CurrHealth -= warfare.MyDamageType.GetDamageMultiplier(MyArmorType) * warfare.CurrentDamage;
    }

    internal bool IsPlayerUnit() {
        return PlayerController.currentPlayerNumber == playerNumber;
    }

}
