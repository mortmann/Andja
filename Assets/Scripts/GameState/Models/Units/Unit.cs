using System;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model.Components;
using Andja.Pathfinding;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Andja.Model {

    public class UnitPrototypeData : BaseThing {
        public int inventoryPlaces;
        public int inventorySize;

        public DamageType damageType;
        public ArmorType armorType;

        public string[] movementSoundName;
        public string[] mainAttackSoundName;

        public float buildTime = 1f;
        public float maximumHealth;
        public float attackRange = 1f;
        public float damage = 10;
        public float attackRate = 1;
        public float speed;
        public float rotationSpeed = 90f;
        public float aggroTime = 2f;
        public float captureSpeed = 0.01f;
        public float projectileSpeed = 4.5f;
        public float buildRange = 15;
        public float width = 0;
        public float height = 0;
    }

    public enum UnitDoModes { Idle, Move, Fight, Capture, Trade, OffWorld }

    public enum UnitMainModes { Idle, Moving, Aggroing, Attack, Patrol, Capture, TradeRoute, OffWorldMarket, Escort, PickUpCrate }

    [JsonObject(MemberSerialization.OptIn)]
    public class Unit : IGEventable, IWarfare, IPathfindAgent {
        public readonly float EscortDistance = 2f;

        //save these Variables

        #region Serialize

        [JsonPropertyAttribute] public string ID;
        [JsonPropertyAttribute] public int playerNumber;
        [JsonPropertyAttribute] protected string playerSetName;
        [JsonPropertyAttribute] protected float currentHealth;

        [JsonPropertyAttribute] private float aggroCooldownTimer = 1f;

        [JsonPropertyAttribute] private Queue<Command> queuedCommands;
        [JsonPropertyAttribute] public PatrolCommand PatrolCommand;

        [JsonPropertyAttribute] public float TradeTime = 1.5f;
        [JsonPropertyAttribute] public float AttackCooldownTimer = 1;
        [JsonPropertyAttribute] public BasePathfinding Pathfinding;
        [JsonPropertyAttribute] public UnitInventory Inventory;
        [JsonPropertyAttribute] protected UnitDoModes currentDoingMode = UnitDoModes.Idle;
        [JsonPropertyAttribute] protected UnitMainModes currentMainMode = UnitMainModes.Idle;

        public bool ShouldStartAggroPosition() {
            return currentMainMode == UnitMainModes.Aggroing;
        }
        public virtual bool CanAttack => CurrentDamage > 0;

        public UnitDoModes CurrentDoingMode {
            get => currentDoingMode;
            set => currentDoingMode = value;
        }

        public UnitMainModes CurrentMainMode {
            get => currentMainMode;
            set => currentMainMode = value;
        }

        #endregion Serialize

        //being calculated at runtime

        #region calculated
        public OutputStructure rangeUStructure;
        protected Action<Unit> cbUnitChanged;
        protected Action<Unit, IWarfare> cbUnitDestroyed;
        protected Action<Unit, bool> cbUnitArrivedDestination;
        protected Action<Unit, IWarfare> cbTakesDamageFrom;
        protected Action<Projectile> cbCreateProjectile;
        protected Action<Unit, string, bool> cbSoundCallback;

        public string Description => Data.Description;
        public string Name => Data.Name;
        public string HoverOver => Data.HoverOver;
        public string Short => Data.Short;

        //TODO decide on this:
        public Command CurrentCommand => queuedCommands.Count == 0 ? null : queuedCommands.Peek();

        public ITargetable CurrentTarget {
            get
            {
                return CurrentCommand switch
                {
                    AttackCommand command => command.target,
                    AggroCommand command => command.target,
                    _ => null
                };
            }
        }

        public string PlayerSetName {
            get => playerSetName;
            protected set => playerSetName = value;
        }

        public float CurrentHealth {
            get => currentHealth;
            protected set => currentHealth = value;
        }

        public float X => Pathfinding.X;

        public float Y => Pathfinding.Y;

        public float Rotation => Pathfinding.rotation;

        public Vector3 PositionVector => new Vector3(X, Y);

        public Vector2 PositionVector2 => new Vector2(X, Y);

        public bool IsDead => currentHealth <= 0;

        #endregion calculated

        //gets from prototyp / being loaded in from masterfile

        #region prototype

        public float CaptureSpeed => Data.captureSpeed;

        public float AttackRange => CalculateRealValue(nameof(Data.attackRange), Data.attackRange);
        public float Damage => CalculateRealValue(nameof(Data.damage), Data.damage);
        public float MaxHealth => CalculateRealValue(nameof(Data.maximumHealth), Data.maximumHealth);
        public float AttackRate => CalculateRealValue(nameof(Data.attackRate), Data.attackRate);
        public float Speed => CalculateRealValue(nameof(Data.attackRange), Data.speed) * SpeedModifier;

        public virtual float SpeedModifier => 1f;

        public float RotationSpeed => CalculateRealValue(nameof(Data.rotationSpeed), Data.rotationSpeed);
        public int InventoryPlaces => CalculateRealValue(nameof(Data.inventoryPlaces), Data.inventoryPlaces); //UNTESTED HOW THIS WILL WORK
        public int InventorySize => CalculateRealValue(nameof(Data.inventorySize), Data.inventorySize); //UNTESTED HOW THIS WILL WORK
        public float AggroTime => CalculateRealValue(nameof(Data.aggroTime), Data.aggroTime); //UNTESTED HOW THIS WILL WORK
        public int UpkeepCost => CalculateRealValue(nameof(Data.upkeepCost), Data.upkeepCost); //UNTESTED HOW THIS WILL WORK

        public float BuildRange => CalculateRealValue(nameof(Data.buildRange), Data.buildRange);
        public virtual bool IsShip => false;

        public float BuildTime => Data.buildTime;
        public int BuildCost => Data.buildCost;

        public virtual Unit Clone(int playerNumber, Tile startTile) {
            return new Unit(this, playerNumber, startTile);
        }

        public float Width => Data.width;
        public float Height => Data.height;
        public Item[] BuildingItems => Data.buildingItems;

        #endregion prototype

        protected UnitPrototypeData prototypeData;

        public UnitPrototypeData Data => 
            prototypeData ??= PrototypController.Instance.GetUnitPrototypDataForID(ID);

        public bool IsNonPlayer => PlayerNumber == Pirate.Number || PlayerNumber == FlyingTrader.Number;
        public Vector2 CurrentPosition => PositionVector;
        public Vector2 NextDestinationPosition => Pathfinding.NextDestination.Value;
        public Vector2 LastMovement => Pathfinding.LastMove;

        public int PlayerNumber => playerNumber;

        public float MaximumHealth => CalculateRealValue(nameof(Data.maximumHealth), Data.maximumHealth); 
        public virtual float CurrentDamage => CalculateRealValue(nameof(CurrentDamage), Data.damage);
        public virtual float MaximumDamage => CalculateRealValue(nameof(MaximumDamage), Data.damage);
        public DamageType DamageType => Data.damageType;
        public ArmorType ArmorType => Data.armorType;
        public bool IsDestroyed => IsDead;

        public List<Command> QueuedCommands => queuedCommands == null ? null : new List<Command>(queuedCommands);

        public int PopulationLevel => Data.populationLevel;
        public int PopulationCount => Data.populationCount;

        public bool IsUnit => IsShip == false;

        public virtual TurningType TurnType => TurningType.OnPoint;
        public virtual PathDestination PathDestination => PathDestination.Exact;
        public virtual PathingMode PathingMode => PathingMode.IslandSinglePoint;
        public virtual bool CanEndInUnwalkable => false;
        public virtual PathHeuristics Heuristic => PathHeuristics.Euclidean;
        public virtual PathDiagonal DiagonalType => PathDiagonal.OnlyNoObstacle;

        public IReadOnlyList<int> CanEnterCities => PlayerController.Instance.GetPlayer(PlayerNumber)?.GetUnitCityEnterable();

        public bool IsAlive => IsDead == false;

        public override string GetID() {
            return ID;
        } 

        [JsonConstructor]
        public Unit() {
            queuedCommands ??= new Queue<Command>();
            PatrolCommand ??= new PatrolCommand();
        }

        public Unit(string id, UnitPrototypeData upd) {
            ID = id;
            prototypeData = upd;
            PatrolCommand = new PatrolCommand();
        }

        public Unit(Unit unit, int playerNumber, Tile t) {
            ID = unit.ID;
            PatrolCommand = new PatrolCommand();
            prototypeData = unit.Data;
            CurrentHealth = MaxHealth;
            this.playerNumber = playerNumber;
            PlayerSetName = Name + " " + Random.Range(0, 1000000000);
            Pathfinding = new IslandPathfinding(this, t);
            queuedCommands = new Queue<Command>();
            Setup();
        }
        internal void ReduceHealth(float damage, IWarfare warfare) {
            if (damage < 0) {
                Debug.LogWarning("Damage should be never smaller than 0 - Fix it!");
                return;
            }
            CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, MaxHealth);
            if (CurrentHealth <= 0) {
                Destroy(warfare);
            }
            if(warfare != null && CurrentMainMode == UnitMainModes.Idle) {
                GiveAggroCommand(warfare);
            }
            cbTakesDamageFrom?.Invoke(this, warfare);
        }

        public void RepairHealth(float heal) {
            if (heal < 0) {
                Debug.LogWarning("Healing should be never smaller than 0 - Fix it!");
                return;
            }
            CurrentHealth += heal;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        }

        internal void ChangeHealth(float change, IWarfare warfare = null) {
            if (change < 0)
                ReduceHealth(-change, warfare); //damage should not be negativ
            if (change > 0)
                RepairHealth(change);
        }
        internal virtual void Load() {
            Setup();
            Inventory.Load();
            Pathfinding.Load(this);
            if (Pathfinding.IsAtDestination) return;
            Pathfinding.SetDestination(Pathfinding.dest_X, Pathfinding.dest_Y);
            Pathfinding.cbIsAtDestination += OnArriveDestination;
        }

        private void Setup() {
            ((World)World.Current).RegisterOnEvent(OnEventCreate, OnEventEnded);
            Pathfinding.cbIsAtDestination += OnPathfindingAtDestination;
            Inventory?.OnChanged(Inventory);
        }
        protected void OnPathfindingAtDestination(bool atDestination) {
            cbUnitArrivedDestination?.Invoke(this, atDestination);
        }

        public virtual void Update(float deltaTime) {
            if (CurrentHealth > MaxHealth) {
                //Values got changed or maybe upgrade lost? we need to reduce it slowly
                CurrentHealth = Mathf.Clamp(CurrentHealth - 10 * deltaTime, MaxHealth, CurrentHealth);
            }
            if (CurrentCommand is { IsFinished: true }) {
                queuedCommands.Dequeue();
                if (CurrentCommand == null)
                    CurrentMainMode = UnitMainModes.Idle; // no commands so be lazy
            }
            switch (CurrentMainMode) {
                case UnitMainModes.Idle:
                    if (CurrentDoingMode != UnitDoModes.Idle)
                        CurrentDoingMode = UnitDoModes.Idle;
                    if (CurrentCommand != null) {
                        CurrentMainMode = CurrentCommand.MainMode;
                    }
                    break;

                case UnitMainModes.Moving:
                    if (CurrentDoingMode != UnitDoModes.Move) {
                        Pathfinding.cbIsAtDestination += OnArriveDestination;
                        Vector2 dest = CurrentCommand.Position;
                        SetDestinationIfPossible(dest.x, dest.y);
                        CurrentDoingMode = UnitDoModes.Move;
                    }
                    break;

                case UnitMainModes.Aggroing:
                    if (CanAttack == false || CurrentTarget == null) {
                        CurrentMainMode = UnitMainModes.Idle;
                        return;
                    }
                    //not in Range -> get in range
                    if (IsInRange() == false) {
                        if (CurrentDoingMode != UnitDoModes.Move) {
                            Vector2 dest = CurrentTarget.CurrentPosition;
                            if(Vector2.Distance(dest, CurrentPosition) < AttackRange + GameData.UnitAggroRange) {
                                SetDestinationIfPossible(dest.x, dest.y);
                            } 
                        }
                        AggroCommand aggro = CurrentCommand as AggroCommand;
                        if (Vector2.Distance(aggro.StartPosition, CurrentPosition) > GameData.UnitAggroRange) {
                            //Maybe just send it back to the startposition BUT not finish aggro -> if the other 
                            //is following it could get in range again and we could reaggro without the need to 
                            //go back to the startposition completly -> which requires this to 
                            // update aggro range & move at the sametime
                            aggro.SetFinished();
                            GiveMovementCommand(aggro.StartPosition);
                            Debug.Log("Finished AGGRO returning to start");
                        }
                    } else {
                        //IN range go ahead fight
                        if(CurrentDoingMode != UnitDoModes.Fight)
                            CurrentDoingMode = UnitDoModes.Fight;
                    }
                    break;

                case UnitMainModes.Attack:
                    if (CanAttack && IsInRange() == false) {
                        if (CurrentDoingMode != UnitDoModes.Move) {
                            Pathfinding.cbIsAtDestination += OnArriveDestination;
                            Vector2 dest = CurrentTarget.CurrentPosition;
                            SetDestinationIfPossible(dest.x, dest.y);
                        }
                    }
                    else
                    if (CurrentDoingMode != UnitDoModes.Fight) {
                        //is in range start fighting
                        CurrentDoingMode = UnitDoModes.Fight;
                    }
                    break;

                case UnitMainModes.Patrol:
                    UpdateAggroRange(deltaTime);
                    if (CurrentDoingMode != UnitDoModes.Move) {
                        CurrentDoingMode = UnitDoModes.Move;
                        SetDestinationIfPossible(CurrentCommand.Position);
                    }
                    break;

                case UnitMainModes.Capture:
                    CurrentDoingMode = IsInRange()? UnitDoModes.Capture : UnitDoModes.Move;
                    break;

                case UnitMainModes.TradeRoute:
                    UpdateTradeRoute(deltaTime);
                    break;

                case UnitMainModes.OffWorldMarket:
                    if (Pathfinding.IsAtDestination)
                        UpdateWorldMarket(deltaTime);
                    break;

                case UnitMainModes.PickUpCrate:
                    TryToAddCrate(((PickUpCrateCommand)CurrentCommand).crate);
                    break;

                case UnitMainModes.Escort:
                    Debug.LogError("Not implemented yet!");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
                    Pathfinding.UpdateDoRotate(deltaTime);
                    break;

                case UnitDoModes.Trade:
                    UpdateDoingTrade(deltaTime);
                    break;

                case UnitDoModes.OffWorld:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        protected virtual void UpdateTradeRouteAtDestination() {
            UpdateDoingTrade(0);
        }
        protected virtual void UpdateDoingTrade(float deltaTime) {
            Debug.LogWarning("Unit can't trade at the moment. Please implement this feature now.");
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

        protected void UpdateMovement(float deltaTime) {
            Pathfinding.Update_DoMovement(deltaTime);
            cbUnitChanged?.Invoke(this);
        }

        protected void UpdateAggroRange(float deltaTime) {
            if (CanAttack == false || CurrentTarget != null) {
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
                ITargetable target = targetableHoldingScript.Holding;
                if (target == null || target.PlayerNumber == playerNumber) {
                    continue;
                }
                //see if players are at war
                if (PlayerController.Instance.ArePlayersAtWar(playerNumber, target.PlayerNumber) == false) {
                    continue;
                }
                GiveAggroCommand(target);
            }
            //CurrentMainMode = UnitMainModes.Idle;
        }

        private bool GiveAggroCommand(ITargetable targetable) {
            if (Vector2.Distance(targetable.CurrentPosition, CurrentPosition) > AttackRange + GameData.UnitAggroRange) {
                return false; //out of aggrorange
            }
            AddCommand(new AggroCommand(targetable, CurrentPosition), false);
            return true;
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
            }
            AddCommand(new AttackCommand(target), overrideCurrent);
            return true;
        }

        public void AddCommand(Command command, bool overrideCurrent) {
            if (overrideCurrent) {
                GoIdle();
                CurrentMainMode = command.MainMode;
            }
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
            foreach (Tile item in nearstTile.Structure.NeighbourTiles) {
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
                float currDist = (item.Vector - Pathfinding.CurrTile.Vector).magnitude;
                if (currDist < nearDist) {
                    nearDist = currDist;
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
            if (IsInRange() == false) {
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
            if (CurrentTarget == null) return;
            if (AttackCooldownTimer > 0) {
                AttackCooldownTimer -= deltaTime;
                return;
            }
            Pathfinding.UpdateDoRotate(deltaTime);
            AttackCooldownTimer = AttackRate;
            CurrentTarget.TakeDamageFrom(this);
        }

        protected void UpdateOnArriveDestinationPatrol() {
            //PATROL
            PatrolCommand.ChangeToNextPosition();
            SetDestinationIfPossible(PatrolCommand.Position);
        }

        public void IsInRangeOfWarehouse(OutputStructure ware) {
            rangeUStructure = ware;
        }

        public void TradeItemToNearbyWarehouse(Item clicked) {
            TradeItemToNearbyWarehouse(clicked, rangeUStructure.City.PlayerTradeAmount);
        }
        public bool TradeItemToNearbyWarehouse(Item clicked, int amount) {
            if (rangeUStructure is WarehouseStructure == false) {
                return false;
            }
            if (rangeUStructure.PlayerNumber == playerNumber) {
                rangeUStructure.City.TradeFromShip(this, clicked, amount);
            }
            else {
                rangeUStructure.City.BuyingTradeItem(clicked.ID, (Ship)this, amount);
            }
            return true;
        }

        public void AddPatrolCommand(float targetX, float targetY) {
            Tile tile = World.Current.GetTileAt(targetX, targetY);
            if (tile == null) {
                return;
            }

            if (CanReach(tile.Vector2) == false) {
                return;
            }
            PatrolCommand ??= new PatrolCommand();
            PatrolCommand.AddPosition(new Vector2(targetX, targetY));
            if (PatrolCommand.PositionCount <= 1) return;
            AddCommand(PatrolCommand, true);
            CurrentMainMode = UnitMainModes.Patrol;
        }

        public void ResumePatrolCommand() {
            if (CurrentMainMode == UnitMainModes.Patrol) {
                return;
            }
            AddCommand(PatrolCommand, true);
        }

        public void StopPatrolCommand() {
            if (CurrentMainMode == UnitMainModes.Patrol) {
                queuedCommands.Dequeue();
            }
        }

        public void ClearPatrolCommands() {
            StopPatrolCommand();
            PatrolCommand.ClearPositions();
        }

        public bool GiveMovementCommand(Vector2 vec2, bool overrideCurrent = false) {
            return GiveMovementCommand(vec2.x, vec2.y, overrideCurrent);
        }

        public bool GiveMovementCommand(Tile t, bool overrideCurrent = false) {
            return t != null && GiveMovementCommand(t.X, t.Y, overrideCurrent);
        }

        public bool GiveMovementCommand(float x, float y, bool overrideCurrent = false) {
            if (Math.Abs(x - X) < 0.1f && Math.Abs(y - Y) < 0.1f)
                return true;
            if (IsUnit && CanReach(x, y) == false)
                return false;
            AddCommand(new MoveCommand(new Vector2(x, y)), overrideCurrent);
            return true;
        }

        protected void OnArriveDestination(bool atDest) {
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
                    UpdateTradeRouteAtDestination();
                    break;

                case UnitMainModes.OffWorldMarket:
                    CurrentDoingMode = UnitDoModes.OffWorld;
                    break;
                case UnitMainModes.Escort:
                    break;
                case UnitMainModes.PickUpCrate:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Pathfinding.cbIsAtDestination -= OnArriveDestination;
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
            Pathfinding.SetDestination(x, y);
            return true;
        }

        
        internal void GivePickUpCrateCommand(Crate crate, bool overrideCurrent) {
            if (crate.IsInRange(CurrentPosition) && overrideCurrent) {
                TryToAddCrate(crate);
            }
            else {
                CurrentMainMode = UnitMainModes.PickUpCrate;
                CurrentDoingMode = UnitDoModes.Move;
                SetDestinationIfPossible(crate.position.x, crate.position.y);
                AddCommand(new PickUpCrateCommand(crate), false);
                Pathfinding.cbIsAtDestination += OnArriveDestination;
            }
        }
        public bool CanReach(Vector2 vec) {
            return CanReach(vec.x, vec.y);
        }

        public bool CanReach(float x, float y) {
            Tile tile = World.Current.GetTileAt(x + TileSpriteController.offset, y + TileSpriteController.offset);
            if (tile == null) {
                return false;
            }
            switch (tile.Type) {
                case TileType.Ocean when IsShip == false:
                case TileType.Mountain:
                case TileType.Shore:
                case TileType.Cliff:
                case TileType.Water:
                case TileType.Volcano:
                    return false;

                case TileType.Dirt:
                    break;
                case TileType.Grass:
                    break;
                case TileType.Stone:
                    break;
                case TileType.Desert:
                    break;
                case TileType.Steppe:
                    break;
                case TileType.Jungle:
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return Pathfinding.CurrTile.Island == tile.Island;
        }

        public int TryToAddItem(Item item) {
            return Inventory.AddItem(item);
        }

        public int TryToAddItemMaxAmount(Item item, int amount) {
            Item t = item.Clone();
            t.count = amount;
            return Inventory.AddItem(t);
        }

        internal bool TryToAddCrate(Crate thisCrate) {
            if (Inventory == null)
                return false;
            if (thisCrate.IsInRange(CurrentPosition) == false)
                return false;
            int pickedup = TryToAddItem(thisCrate.item);
            thisCrate.RemoveItemAmount(pickedup);
            return true;
        }

        public void SetName(string name) {
            PlayerSetName = name;
        }

        public virtual void Destroy(IWarfare warfare) {
            //Do stuff here when on destroyed
            cbUnitDestroyed?.Invoke(this, warfare);
            currentHealth = 0;
            Pathfinding.CancelJob();
        }
        public void CallChangedCallback() {
            cbUnitChanged?.Invoke(this);
        }
        #region RegisterCallback

        public void RegisterOnChangedCallback(Action<Unit> cb) {
            cbUnitChanged += cb;
        }

        public void UnregisterOnChangedCallback(Action<Unit> cb) {
            cbUnitChanged -= cb;
        }

        /// <summary>
        /// UNIT = destroyed
        /// IWARFARE = destroyed by! CAN BE NULL
        /// </summary>
        /// <param name="cb"></param>
        public void RegisterOnDestroyCallback(Action<Unit, IWarfare> cb) {
            cbUnitDestroyed += cb;
        }

        public void UnregisterOnDestroyCallback(Action<Unit, IWarfare> cb) {
            cbUnitDestroyed -= cb;
        }
        public void RegisterOnTakesDamageCallback(Action<Unit, IWarfare> cb) {
            cbTakesDamageFrom += cb;
        }

        public void UnregisterOnTakesDamageCallback(Action<Unit, IWarfare> cb) {
            cbTakesDamageFrom -= cb;
        }

        public void RegisterOnArrivedAtDestinationCallback(Action<Unit, bool> cb) {
            cbUnitArrivedDestination += cb;
        }

        public void UnregisterOnArrivedAtDestinationCallback(Action<Unit, bool> cb) {
            cbUnitArrivedDestination -= cb;
        }

        public void RegisterOnSoundCallback(Action<Unit, string, bool> cb) {
            cbSoundCallback += cb;
        }

        public void UnregisterOnSoundCallback(Action<Unit, string, bool> cb) {
            cbSoundCallback -= cb;
        }

        public void RegisterOnCreateProjectileCallback(Action<Projectile> cb) {
            cbCreateProjectile += cb;
        }

        public void UnregisterOnCreateProjectileCallback(Action<Projectile> cb) {
            cbCreateProjectile -= cb;
        }

        #endregion RegisterCallback

        public bool IsAttackableFrom(IWarfare warfare) {
            return warfare.DamageType.GetDamageMultiplier(ArmorType) > 0;
        }

        public void TakeDamageFrom(IWarfare warfare) {
            ReduceHealth(warfare.GetCurrentDamage(ArmorType), warfare);
        }

        internal bool IsOwnedByCurrentPlayer() {
            return PlayerController.currentPlayerNumber == playerNumber;
        }

        public virtual float GetCurrentDamage(ArmorType armorType) {
            return DamageType.GetDamageMultiplier(armorType) * CurrentDamage;
        }

        public override void OnEventCreate(GameEvent ge) {
            if (ge.IsTarget(this)) {
                ge.EffectTarget(this, true);
            }
        }

        internal bool IsTileInBuildRange(Tile tile) {
            return Vector2.Distance(tile.Vector2, PositionVector2) <= BuildRange; 
        }

        public override void OnEventEnded(GameEvent ge) {
            if (ge.IsTarget(this)) {
                ge.EffectTarget(this, false);
            }
        }

        public void PathInvalidated() {

        }

        public void ChangePlayer(int number) {
            Debug.LogWarning("Unit changed Player Number -- Only with cheats possible -- If not used report.");
            playerNumber = number;
        }
    }
}