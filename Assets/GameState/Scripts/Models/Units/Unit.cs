using UnityEngine;
using System.Collections;
using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Unit  {
   	//save these Variables
	#region Serialize
	[JsonPropertyAttribute] public int playerNumber;
	[JsonPropertyAttribute] private string _Name;
	[JsonPropertyAttribute] private float _currHealth = 50;
	[JsonPropertyAttribute] float aggroCooldown=1f;
	//COMBAT STUFF
	[JsonPropertyAttribute] public Vector2 patrolTarget;
	[JsonPropertyAttribute] public Vector2 patrolStart;
	[JsonPropertyAttribute] public bool onWayToPatrolTarget; // false for targetPatrol, true for patrolstart
	[JsonPropertyAttribute] public bool onPatrol = false;
	[JsonPropertyAttribute] public bool isShip;
	[JsonPropertyAttribute] public bool hasChanged = false;
	[JsonPropertyAttribute] public float tradeTime=1.5f;
	[JsonPropertyAttribute] public Pathfinding pathfinding;
	#endregion
	//being calculated at runtime
	#region calculated 

	//TODO decide on this:
	public float BuildRange {
		get {
			return attackRange;
		}
	}
	//FIXME: these should be safed 
	//not quite sure how to do it
	Unit engangingUnit;
	Structure attackingStructure;
	public string Name {
		get {
			return _Name;
		}
		protected set {
			_Name = value;
		}
	}
	public float currHealth {
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
	public bool IsDead { 
		get { return _currHealth <= 0;}
	}
	#endregion
	//gets from prototyp / being loaded in from masterfile
	#region prototype
	public int maxHP=50;
	float aggroTimer=1f;
	public float attackRange=1f;
	public float damage=10;
	public DamageType myDamageType=DamageType.Blade;
	public ArmorType myArmorType=ArmorType.Leather;
	public float attackCooldown=1;
	public float attackRate=1;
	public float speed; 
	public float rotationSpeed = 2f; 

	protected internal float width;
	protected internal float height;
	#endregion




	public Inventory inventory;

	public Unit(Tile t,int playernumber) {
		this.playerNumber = playernumber;
		speed = 2f;
		Name = "Unit " + UnityEngine.Random.Range (0, 1000000000);
		pathfinding = new IslandPathfinding (this,t);
    }
	public Unit(){
	}
	public virtual void Update(float deltaTime) {
		//PATROL
		UpdateParol ();
		UpdateAggroRange (deltaTime);
		if(Fighting(deltaTime)){
			return;
		}
		if(pathfinding!=null)
			pathfinding.Update_DoMovement (deltaTime);

        cbUnitChanged?.Invoke(this);
    }
	protected void UpdateAggroRange(float deltaTime){
		if(engangingUnit!=null){
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
	public void GiveAttackCommand(Unit u, bool overridingAttack=false){
		if(overridingAttack==false&&attackingStructure!=null){
			return;
		}
		attackingStructure = null;
		engangingUnit = u;
		AddMovementCommand (u.X,u.Y);
	}
	public void GiveAttackCommand(Structure structure, bool overridingAttack=false){
		if(overridingAttack==false&&engangingUnit!=null){
			return;
		}
		if(this.isShip || this.myDamageType == DamageType.Artillery){
			attackingStructure = structure;
			Tile nearstTile = null;
			float nearDist= float.MaxValue;
			foreach (Tile item in structure.neighbourTiles) {
				if(isShip){
					if(item.Type != TileType.Ocean){
						continue;
					}
				} else {
					if(item.Type == TileType.Ocean || item.MovementCost<=0){
						continue;
					}
				}
				float currDist = (item.Vector-pathfinding.currTile.Vector).magnitude;
				if(currDist<nearDist){
					currDist = nearDist;
					nearstTile = item;
				}
			}
			if(nearstTile == null){
				return;
			}
			AddMovementCommand (nearstTile);
		} else {
			//only MarketBuildings are captureable and not warehouses
			if(structure is MarketBuilding && (structure is Warehouse)==false){
				Tile nearstTile = null;
				float nearDist= float.MaxValue;
				foreach (Tile item in structure.neighbourTiles) {
					if(isShip){
						if(item.Type != TileType.Ocean){
							continue;
						}
					} else {
						if(item.Type == TileType.Ocean || item.MovementCost<=0){
							continue;
						}
					}
					float currDist = (item.Vector-pathfinding.currTile.Vector).magnitude;
					if(currDist<nearDist){
						currDist = nearDist;
						nearstTile = item;
					}
				}
				if(nearstTile == null){
					return;
				}
				isCapturing = true;
				AddMovementCommand (nearstTile);
			}
		}
	}

	public bool Fighting(float deltaTime){
		if(engangingUnit!=null){
			if(PlayerController.Instance.ArePlayersAtWar (engangingUnit.playerNumber,playerNumber)){
				engangingUnit = null;
				return false;
			}
			float dist = (engangingUnit.VectorPosition - VectorPosition).magnitude;
			if(dist<attackRange){
				DoAttack (deltaTime);
				return true;
			}
		} else if(attackingStructure!=null){
			if(PlayerController.Instance.ArePlayersAtWar (attackingStructure.PlayerNumber,playerNumber)){
				attackingStructure = null;
				return false;
			}
			float dist = (attackingStructure.MiddleVector.magnitude - VectorPosition.magnitude);
			if(dist<attackRange){
				DoAttack (deltaTime);
				return true;
			}
		}
		return false;
	}
	public void DoAttack(float deltaTime){
		if(engangingUnit!=null){
			if(engangingUnit.currHealth<=0){
				engangingUnit = null;
			}
			attackCooldown -= deltaTime;
			if(attackCooldown>0){
				return;
			}
			attackCooldown = attackRate;

			engangingUnit.TakeDamage (myDamageType,damage);
		}
		if(isCapturing){
			if(attackingStructure.Health<=0||attackingStructure.PlayerNumber!=playerNumber){
				attackingStructure = null;
				return;
			}
			if(attackingStructure.neighbourTiles.Contains (pathfinding.currTile)){
				((MarketBuilding)attackingStructure).TakeOverMarketBuilding (deltaTime, playerNumber, 1);
			}
		} else {
			if(attackingStructure.Health<=0){
				attackingStructure = null;
				return;
			}
			attackCooldown -= deltaTime;
			if(attackCooldown>0){
				return;
			}
			attackCooldown = attackRate;

			attackingStructure.TakeDamage (damage);
		}
	}

	protected void UpdateParol(){
		//PATROL
		if(onPatrol){
			if(pathfinding.IsAtDest){
				if (onWayToPatrolTarget) {
//					pathfinding.AddMovementCommand (patrolStart.x , patrolStart.y);
				} else {
//					pathfinding.AddMovementCommand (patrolTarget.x , patrolTarget.y);
				}
				onWayToPatrolTarget = !onWayToPatrolTarget; 
			}
		}
	}
	public void isInRangeOfWarehouse(OutputStructure ware){
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

	public void AddMovementCommand(Tile t) {
		if(t==null){
			//not really an error it can happen
			return;
		} else {
			if(t==pathfinding.currTile){
				return;
			}
			AddMovementCommand (t.X,t.Y);
		}
	}
    public virtual void AddMovementCommand(float x, float y) {
		Tile tile = World.Current.GetTileAt(x, y);
        if(tile == null){
            return;
        }
        if (tile.Type == TileType.Ocean) {
            return;
        }
        if (tile.Type == TileType.Mountain) {
            return;
        }
		if(pathfinding.currTile.MyIsland!=tile.MyIsland){
			return;
		}
		onPatrol = false;

		if(pathfinding is IslandPathfinding){
			((IslandPathfinding)pathfinding).SetDestination (x,y);
		}
    }
	public int tryToAddItemMaxAmount(Item item , int amount){
		Item t = item.Clone ();
		t.count = amount;
		return inventory.addItem (t);
	}
	public void CallChangedCallback(){
        cbUnitChanged?.Invoke(this);
    }
	public void TakeDamage(DamageType dt, float amount){
		if(amount<0){
			Debug.LogError ("damage must be positive");
			return;
		}
		currHealth -= Combat.ArmorDamageReduction (myArmorType, dt) * amount;
	}
	public virtual void Destroy(){
        //Do stuff here when on destroyed
        cbUnitDestroyed?.Invoke(this);
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

}
