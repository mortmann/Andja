using Andja.Controller;
using Andja.Pathfinding;
using Andja.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {

    public class ShipPrototypeData : UnitPrototypeData {
        public int maximumAmountOfCannons = 0;
        public float damagePerCannon = 1;
        public float length;
        public float cannonSpeedDebuffMultiplier = 0.1f;
        public float inventorySpeedDebuffMultiplier = 0.15f;
        public float damageSpeedDebuffMultiplier = 0.7f;

        //TODO: think about a way it doesnt require each ship to have this
        //      OR are there sometyp like HEAVY and prototype returns the associated cannon
        public Item cannonType = null;
    }

    //TODO: think about how if ships could be capturable if they are low, at war and the capturing ship can do it?
    [JsonObject(MemberSerialization.OptIn)]
    public class Ship : Unit {

        [JsonPropertyAttribute] public TradeRoute tradeRoute;
        [JsonPropertyAttribute] public bool isOffWorld;
        [JsonPropertyAttribute] private Item[] toBuy;
        [JsonPropertyAttribute] private float offWorldTime;
        [JsonPropertyAttribute] private Item _cannonItem;
        [JsonPropertyAttribute] public int nextTradeRouteStop;
        public Item CannonItem {
            get { if (_cannonItem == null) { _cannonItem = ShipData.cannonType.CloneWithCount(); } return _cannonItem; }
        }

        protected ShipPrototypeData _shipPrototypData;
        float ProjectileSpeed => ShipData.projectileSpeed;
        public float DamagePerCannon => CalculateRealValue(nameof(ShipData.damagePerCannon), ShipData.damagePerCannon);
        public int MaximumAmountOfCannons => CalculateRealValue(nameof(ShipData.maximumAmountOfCannons), ShipData.maximumAmountOfCannons);
        public override float CurrentDamage => CalculateRealValue(nameof(CurrentDamage), DamagePerCannon * CannonItem.count);
        public override float MaximumDamage => CalculateRealValue(nameof(MaximumDamage), MaximumAmountOfCannons * DamagePerCannon);
        public override bool IsShip => true;
        public override float SpeedModifier => 1 - CannonSpeedDebuff - InventorySpeedDebuff - DamageSpeedDebuff;
        protected float CannonSpeedDebuff => MaximumAmountOfCannons == 0 ? 0 : ShipData.cannonSpeedDebuffMultiplier * (CannonItem.count / (float)MaximumAmountOfCannons);
        protected float InventorySpeedDebuff => ShipData.inventorySpeedDebuffMultiplier * Inventory.GetFilledPercentage();
        protected float DamageSpeedDebuff => ShipData.damageSpeedDebuffMultiplier * (1 - CurrentHealth / MaximumHealth);

        protected int CannonPerSide => Mathf.CeilToInt(CannonItem.count / 2);
        public override PathingMode PathingMode => PathingMode.World;
        public override TurningType TurnType => TurningType.TurnRadius;

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

        public Ship(Unit unit, int playerNumber, Tile t) {
            ID = unit.ID;
            PatrolCommand = new PatrolCommand();
            prototypeData = unit.Data;
            CurrentHealth = MaxHealth;
            this.playerNumber = playerNumber;
            //TODO: replace everywhere with byte and test it
            Inventory = new UnitInventory((byte)InventoryPlaces.ClampZero(255), InventorySize);
            PlayerSetName = "Ship " + Random.Range(0, 1000000000);
            Pathfinding = new OceanPathfinding(t, this);
            Pathfinding.cbIsAtDestination += OnPathfindingAtDestination;
        }

        public override Unit Clone(int playerNumber, Tile startTile) {
            return new Ship(this, playerNumber, startTile);
        }

        public Ship(string id, ShipPrototypeData spd) {
            ID = id;
            _shipPrototypData = spd;
        }

        public override void DoAttack(float deltaTime) {
            if (CannonItem.count == 0)
                return;
            if (CurrentTarget != null) {
                float shootAngle = nextShoot.rotateToAngle;

                float arc = 5f;
                bool canShoot = shootAngle <= Pathfinding.rotation + arc && shootAngle >= Pathfinding.rotation - arc;
                Pathfinding.Rotate(nextShoot.rotateToAngle);
                Pathfinding.UpdateDoRotate(deltaTime);
                if (canShoot == false) {
                    return;
                }
                if (AttackCooldownTimer > 0) {
                    AttackCooldownTimer -= deltaTime;
                    return;
                }
                Vector3 velocity = new Vector3();
                Vector3 targetPosition = CurrentTarget.CurrentPosition;
                Vector3 lastMove = CurrentTarget.LastMovement;
                Vector3 projectileDestination = CurrentTarget.CurrentPosition;
                if (Projectile.PredictiveAim(CurrentPosition, ProjectileSpeed, targetPosition, 
                                    lastMove, GameData.Gravity, out velocity, out projectileDestination) == false) {
                    return;
                }
                ShotAtPosition(projectileDestination);
            }
        }

        public override bool IsInRange() {
            if (CurrentTarget == null)
                return false;
            if (CurrentTarget.LastMovement.sqrMagnitude == 0) {
                if ((CurrentTarget.CurrentPosition - CurrentPosition).magnitude <= AttackRange) {
                    nextShoot = CalculateShootAngle(CurrentTarget.CurrentPosition);
                    return true;
                }

                return false;
            }
            Vector3 targetPosition = CurrentTarget.CurrentPosition;
            Vector3 lastMove = CurrentTarget.LastMovement;
            Vector3 projectileDestination = targetPosition;
            Shoot shoot = CalculateShootAngle(projectileDestination);
            float rotateTime = CalculateRotateTime(shoot.rotateByAngle);
            targetPosition += rotateTime * lastMove;
            bool can = Projectile.PredictiveAim(CurrentPosition, ProjectileSpeed,
                                            targetPosition, lastMove, GameData.Gravity, out Vector3 velocity, out projectileDestination);
            if (can == false || Vector3.Distance(CurrentPosition, projectileDestination) > AttackRange)
                return false;
            nextShoot = CalculateShootAngle(projectileDestination);
            return true;
        }

        //TODO: think about making it like this?
        //calculate in the check range and if in range and possible then just do the shoot calculate there?
        private Shoot nextShoot;

        public void ShotAtPosition(Vector3 destination) {
            if (CannonItem.count == 0)
                return;
            Vector3 targetSize = new Vector3(1, 1, 0);
            Vector3 position = CurrentPosition;
            Vector2 side;
            float widthOffset = 0;
            if (nextShoot.sideAngle < 0) {
                side = Quaternion.Euler(0, 0, Pathfinding.rotation) * new Vector2(0, 1);
                widthOffset = Width / 2;
            }
            else {
                side = Quaternion.Euler(0, 0, Pathfinding.rotation) * new Vector2(0, -1);
                widthOffset = -Width / 2;
            }
            for (int i = 1; i <= CannonPerSide; i++) {
                Vector3 offset = new Vector3((i) * (Height / MaximumAmountOfCannons) - Height / 2, widthOffset);
                offset = Quaternion.Euler(0, 0, Rotation) * offset;
                Vector3 targetOffset = new Vector3(
                        Random.Range(-targetSize.x / 2, targetSize.x / 2),
                        Random.Range(-targetSize.y / 2, targetSize.y / 2),
                        Random.Range(-targetSize.z / 2, targetSize.z / 2)
                        );

                Vector3 velocity = (destination + targetOffset - PositionVector - offset).normalized * ProjectileSpeed;
                float distance = (destination + targetOffset - PositionVector - offset).magnitude;
                cbCreateProjectile?.Invoke(new Projectile(this, position + offset, CurrentTarget, destination + targetOffset, velocity, distance, true));
            }
            AttackCooldownTimer = AttackRate;
            cbSoundCallback?.Invoke(this, "broadside", true);
        }

        protected Shoot CalculateShootAngle(Vector3 destination) {
            Vector2 forward = Quaternion.Euler(0, 0, Pathfinding.rotation) * new Vector2(1, 0);
            Vector2 direction = destination - PositionVector;
            direction.Normalize();
            float sideAngle = Mathf.Sign(Vector2.SignedAngle(direction, forward));
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            return new Shoot {
                sideAngle = angle,
                rotateToAngle = angle + (sideAngle) * 90,
                rotateByAngle = angle - Pathfinding.rotation
            };
        }

        public float CalculateRotateTime(float angle) {
            return Mathf.Abs(angle) / RotationSpeed;
        }

        protected override void UpdateTradeRoute(float deltaTime) {
            if (tradeRoute == null || tradeRoute.Valid == false) {
                CurrentMainMode = UnitMainModes.Idle;
                return;
            }
            if (Pathfinding.IsAtDestination) {
                if(CurrentDoingMode == UnitDoModes.Idle) {
                    SetDestinationIfPossible(tradeRoute.GetNextDestination(this));
                }
            }
        }
        /// <summary>
        /// This updates the "UnitDoingMode" trade.
        /// </summary>
        /// <param name="deltaTime"></param>
        protected override void UpdateDoingTrade(float deltaTime) {
            if(TradeTime > 0) {
                TradeTime = Mathf.Clamp(TradeTime - deltaTime, 0, TradeRoute.TRADE_TIME);
                return;
            }
            tradeRoute.DoCurrentTrade(this);
            CurrentDoingMode = UnitDoModes.Idle;
        }
        public void SetTradeRoute(TradeRoute tr) {
            if(tradeRoute != tr) {
                tradeRoute?.RemoveShip(this);
                nextTradeRouteStop = 0;
            }
            tradeRoute = tr;
            if(tradeRoute != null) {
                StartTradeRoute();
            }
        }

        public void StartTradeRoute() {
            if (tradeRoute == null)
                return;
            CurrentMainMode = UnitMainModes.TradeRoute;
            Pathfinding.cbIsAtDestination += OnArriveDestination;
            SetDestinationIfPossible(tradeRoute.GetCurrentDestination(this));
        }
        private void SetDestinationIfPossible(Vector2? pos) {
            if (pos == null || pos.HasValue == false)
                return;
            SetDestinationIfPossible(pos.Value.x, pos.Value.y);
        }
        private void SetDestinationIfPossible(Tile tile) {
            SetDestinationIfPossible(tile.X, tile.Y);
        }
        protected override void UpdateTradeRouteAtDestination() {
            Pathfinding.cbIsAtDestination += OnArriveDestination;
            TradeTime = tradeRoute.AtDestination(this);
            if(TradeTime > 0)
                CurrentDoingMode = UnitDoModes.Trade;
            else
                SetDestinationIfPossible(tradeRoute.GetNextDestination(this));
        }

        internal bool HasCannonsToAddInInventory() {
            return Inventory.HasAnythingOf(CannonItem);
        }

        protected override void UpdateWorldMarket(float deltaTime) {
            if (IsNonPlayer)
                return;
            if (Pathfinding.IsAtDestination && isOffWorld == false) {
                isOffWorld = true;
                CallChangedCallback();
            }
            if (offWorldTime > 0) {
                offWorldTime -= deltaTime;
                return;
            }
            offWorldTime = 3;
            OffworldMarket om = WorldController.Instance.offworldMarket;
            //FIRST SELL everything in Inventory to make space for all the things
            Player Player = PlayerController.Instance.GetPlayer(playerNumber);
            Item[] i = Inventory.GetAllItemsAndRemoveThem();
            foreach (Item item in i) {
                om.SellItemToOffWorldMarket(item, Player);
            }
            foreach (Item item in toBuy) {
                Inventory.AddItem(om.BuyItemToOffWorldMarket(item, item.count, Player));
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
            CurrentMainMode = UnitMainModes.Idle;
        }

        internal void RemoveCannonsToInventory(bool all) {
            if (all)
                CannonItem.count -= Inventory.AddItem(CannonItem);
            else {
                Item temp = CannonItem.Clone();
                temp.count = 1;
                CannonItem.count -= Inventory.AddItem(temp);
            }
        }

        internal void AddCannonsFromInventory(bool all) {
            if (all) {
                Item temp = CannonItem.Clone();
                temp.count = MaximumAmountOfCannons - CannonItem.count;
                CannonItem.count += Inventory.GetItemWithMaxItemCount(temp).count;
            }
            else {
                Item temp = CannonItem.Clone();
                temp.count = Mathf.Min(1, MaximumAmountOfCannons - CannonItem.count);
                CannonItem.count += Inventory.GetItemWithMaxItemCount(temp).count;
            }
        }

        internal bool CanRemoveCannons() {
            if (CannonItem.count <= 0) {
                return false;
            }
            if (Inventory.HasRemainingSpaceForItem(CannonItem) == false) {
                return false;
            }
            return true;
        }

        public void SendToOffworldMarket(Item[] toBuy) {
            //TODO OPTIMISE THIS SO IT CHECKS THE ROUTE FOR ANY
            //ISLANDS SO IT CAN TAKE A OTHER ROUTE
            if (Mathf.Abs(World.Current.Width - X) >= Mathf.Abs(World.Current.Height - Y)) {
                SetDestinationIfPossible(0, Y);
            }
            else {
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
            ((OceanPathfinding)Pathfinding).SetDestination(x, y);
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

        public override float GetCurrentDamage(ArmorType armorType) {
            return DamageType.GetDamageMultiplier(armorType) * DamagePerCannon;
        }

        internal override void Load() {
            base.Load();
            tradeRoute?.LoadShip(this);
        }

        protected struct Shoot {
            public float rotateByAngle;
            public float rotateToAngle;
            public float sideAngle;
        }

        public Player GetOwner() {
            return PlayerController.Instance.GetPlayer(PlayerNumber);
        }
    }
}