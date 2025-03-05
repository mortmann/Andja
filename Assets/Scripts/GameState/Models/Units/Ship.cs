using System.Collections.Generic;
using Andja.Controller;
using Andja.Pathfinding;
using Andja.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {
    public class ShipPrototypeData : UnitPrototypeData {

        public float cannonSpeedDebuffMultiplier = 0.1f;
        public float damageSpeedDebuffMultiplier = 0.7f;
        public float inventorySpeedDebuffMultiplier = 0.15f;
        public float length;
    }

    //TODO: think about how if ships could be capturable if they are low, at war and the capturing ship can do it?
    [JsonObject(MemberSerialization.OptIn)]
    public class Ship : Unit {
        [JsonPropertyAttribute] public TradeRoute tradeRoute;
        [JsonPropertyAttribute] public bool isOffWorld;
        [JsonPropertyAttribute] private Item[] toBuy;
        [JsonPropertyAttribute] private float offWorldTime;
        [JsonPropertyAttribute] public int nextTradeRouteStop;

        private ShipPrototypeData _shipPrototypeData;

        public override bool IsShip => true;
        public override float SpeedModifier => 1 - CannonSpeedDebuff - InventorySpeedDebuff - DamageSpeedDebuff;

        protected float CannonSpeedDebuff => ShipAttack == null || ShipAttack.MaximumAmountOfCannons == 0
            ? 0
            : ShipData.cannonSpeedDebuffMultiplier * (ShipAttack.CannonItem.count / (float)ShipAttack.MaximumAmountOfCannons);


        protected float InventorySpeedDebuff =>
            ShipData.inventorySpeedDebuffMultiplier * Inventory.GetFilledPercentage();

        protected float DamageSpeedDebuff => ShipData.damageSpeedDebuffMultiplier * (1 - CurrentHealth / MaximumHealth);

        public override PathingMode PathingMode => PathingMode.World;
        public override TurningType TurnType => TurningType.TurnRadius;

        public ShipPrototypeData ShipData => _shipPrototypeData ??= (ShipPrototypeData)PrototypController.Instance.GetUnitPrototypeDataForID(ID);

        public ShipAttack ShipAttack;

        public Ship() {
        }

        public Ship(Unit unit, int playerNumber, Tile t, uint buildID) {
            ID = unit.ID;
            PatrolCommand = new PatrolCommand();
            unitData = unit.Data;
            CurrentHealth = MaximumHealth;
            this.playerNumber = playerNumber;
            Inventory = new UnitInventory((byte)InventoryPlaces.ClampZero(255), InventorySize);
            PlayerSetName = "Ship " + Random.Range(0, 1000000000);
            Pathfinding = new OceanPathfinding(t, this);
            Pathfinding.cbIsAtDestination += OnPathfindingAtDestination;
            this.BuildID = buildID;
        }

        public override Unit Clone(int playerNumber, Tile startTile, uint buildID) {
            return new Ship(this, playerNumber, startTile, buildID);
        }

        public Ship(string id, ShipPrototypeData spd) {
            ID = id;
            _shipPrototypeData = spd;
        }


        public override bool IsInRange(Target target, float range) {
            return ShipAttack.IsInRange(target, range);
        }

        public float CalculateRotateTime(float angle) {
            return Mathf.Abs(angle) / RotationSpeed;
        }

        protected override void UpdateTradeRoute(float deltaTime) {
            if (tradeRoute == null || tradeRoute.Valid == false) {
                CurrentMainMode = UnitMainModes.Idle;
                return;
            }
            if (Pathfinding.IsAtDestination == false) return;
            if (CurrentDoingMode == UnitDoModes.Idle) {
                SetDestinationIfPossible(tradeRoute.GetNextDestination(this));
            }
        }

        /// <summary>
        /// This updates the "UnitDoingMode" trade.
        /// </summary>
        /// <param name="deltaTime"></param>
        protected override void UpdateDoingTrade(float deltaTime) {
            if (TradeTime > 0) {
                TradeTime = Mathf.Clamp(TradeTime - deltaTime, 0, TradeRoute.TRADE_TIME);
                return;
            }

            tradeRoute.DoCurrentTrade(this);
            CurrentDoingMode = UnitDoModes.Idle;
        }

        public override void OnBuild(bool loading = false) {
            base.OnBuild(loading);
            ShipAttack = GetElement<ShipAttack>();
        }

        public void SetTradeRoute(TradeRoute tr) {
            if (tradeRoute != tr) {
                tradeRoute?.RemoveShip(this);
                nextTradeRouteStop = 0;
            }

            tradeRoute = tr;
            if (tradeRoute != null) {
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

        protected override void UpdateTradeRouteAtDestination() {
            Pathfinding.cbIsAtDestination += OnArriveDestination;
            TradeTime = tradeRoute.AtDestination(this);
            if (TradeTime > 0)
                CurrentDoingMode = UnitDoModes.Trade;
            else
                SetDestinationIfPossible(tradeRoute.GetNextDestination(this));
        }

        public bool HasCannonsToAddInInventory() {
            return Inventory.HasAnythingOf(ShipAttack.CannonItem);
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
        public void StopTradeRoute() {
            CurrentMainMode = UnitMainModes.Idle;
        }

        public void RemoveCannonsToInventory(bool all) {
            ShipAttack?.RemoveCannonsToInventory(all);
        }

        public void AddCannonsFromInventory(bool all) {
            ShipAttack?.AddCannonsFromInventory(all);
        }

        public bool CanRemoveCannons() {
            return ShipAttack?.CanRemoveCannons() == true;
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
        public override bool SetDestinationIfPossible(float x, float y) {
            Tile tile = World.Current.GetTileAt(x, y);
            if (tile == null) {
                return false;
            }

            ((OceanPathfinding)Pathfinding).SetDestination(x, y);
            CurrentDoingMode = UnitDoModes.Move;
            return tile.Type == TileType.Ocean;
        }

        private void SetDestinationIfPossible(Vector2? pos) {
            if (pos == null)
                return;
            SetDestinationIfPossible(pos.Value.x, pos.Value.y);
        }

        public override void Load() {
            base.Load();
            ShipAttack = GetElement<ShipAttack>();
            tradeRoute?.LoadShip(this);
        }

        public Player GetOwner() {
            return PlayerController.Instance.GetPlayer(PlayerNumber);
        }

        public void CreateProjectiles(List<Projectile> projectiles) {
            projectiles.ForEach(projectile => cbCreateProjectile?.Invoke(projectile));
            cbSoundCallback?.Invoke(this, "broadside", true);
        }

        public void ShotAtPosition(Vector3 getLastMousePosition) {
            ShipAttack.ShotAtPosition(getLastMousePosition);    
        }
    }
}