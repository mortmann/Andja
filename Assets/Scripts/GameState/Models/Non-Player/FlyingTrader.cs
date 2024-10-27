using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class FlyingTrader {
        public static float TradeTime = 5f;
        public static float BuyDifference = 0.9f;
        public static int Number => GameData.FlyingTraderNumber;
        public static float WaitBetweenNewShipsTime = 30f;
        public float WaitBetweenNewShipsTimer = 0;
        public static FlyingTrader Instance { get; protected set; }

        [JsonPropertyAttribute] private float startCooldown;
        [JsonPropertyAttribute] private List<TradeShip> Ships;
        private List<ICity> TradeCities;
        public FlyingTrader() {
            Setup();
        }
        public FlyingTrader(float startCooldown) {
            this.startCooldown = startCooldown;
            Setup();
        }

        private void Setup() {
            Instance = this;
            TradeCities = new List<ICity>();
            Ships = new List<TradeShip>();
            BuildController.Instance.RegisterCityCreated(OnCityCreated);
        }

        private void OnCityCreated(ICity city) {
            TradeCities.Add(city);
        }

        public void AddShip() {
            Ship ship = PrototypController.Instance.GetFlyingTraderPrototype();
            Tile t = World.Current.GetTileAt(UnityEngine.Random.Range(0, World.Current.Height), 0);
            ship = (Ship)World.Current.CreateUnit(ship, null, t, Number);
            TradeShip ts = new TradeShip(ship);
            Ships.Add(ts);
        }

        public void Update(float deltaTime) {
            if (WorldController.Instance.IsPaused) {
                return;
            }
            if (startCooldown > 0) {
                startCooldown = Mathf.Clamp(startCooldown - deltaTime, 0, startCooldown);
                return;
            }
            if (Ships.Count < 1 && TradeCities.Count > 0) {
                WaitBetweenNewShipsTimer -= deltaTime;
                if (WaitBetweenNewShipsTimer <= 0) {
                    AddShip();
                    WaitBetweenNewShipsTimer = WaitBetweenNewShipsTime;
                }
            }
            for (int i = Ships.Count - 1; i >= 0; i--) {
                if(Ships[i].Ship.IsDestroyed) {
                    Ships.RemoveAt(i);
                    continue;
                }
                Ships[i].Update(deltaTime);
            }
        }

        internal void Load() {
            foreach (TradeShip s in Ships) {
                s.Load();
            }
            foreach (Player p in PlayerController.Instance.GetPlayers()) {
                TradeCities.AddRange(p.Cities);
            }
        }

        public void OnDestroy() {
            Instance = null;
        }

        protected ICity GetNextDestination(TradeShip tradeShip, List<ICity> visited) {
            Vector2 shipPos = tradeShip.Ship.PositionVector2;
            if (TradeCities.Count == 0)
                return null;
            IEnumerable<ICity> remaining = TradeCities.Except(visited).Where(c => c.Warehouse != null);
            if (remaining.Count() == 0)
                return null;
            return remaining.Aggregate((x, y) => {
                return Vector2.Distance(shipPos, x.Warehouse.TradeTile.Vector2) < Vector2.Distance(shipPos, y.Warehouse.TradeTile.Vector2) ? x : y;
            });
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class TradeShip {
            [JsonPropertyAttribute] public Ship Ship;
            [JsonPropertyAttribute] private ICity CurrentDestination;
            [JsonPropertyAttribute] private List<ICity> visitedCities;
            [JsonPropertyAttribute] private float tradeTimer;
            [JsonPropertyAttribute] private bool isAtTrade;

            public TradeShip() {
            }

            public TradeShip(Ship ship) {
                visitedCities = new List<ICity>();
                this.Ship = ship;
                tradeTimer = TradeTime;
                GoToNextCity();
                Setup();
            }

            private void Setup() {
                Ship.RegisterOnArrivedAtDestinationCallback(OnShipArriveDestination);
            }

            private void OnShipArriveDestination(Unit unit, bool goal) {
                if (goal == false)
                    return;
                if (Ship.CurrentMainMode == UnitMainModes.OffWorldMarket) {
                    Ship.Destroy(null);
                    isAtTrade = false;
                }
                else {
                    isAtTrade = true;
                }
            }

            public void Update(float deltaTime) {
                if (isAtTrade == false)
                    return;
                if (tradeTimer > 0) {
                    tradeTimer -= deltaTime;
                    return;
                }
                tradeTimer = TradeTime;
                DoTrade();
            }

            private void DoTrade() {
                if (CurrentDestination == null) {
                    SendHome();
                    Debug.LogWarning("Trader has no destination?! -- Is this wanted?");
                    return;
                }
                visitedCities.Add(CurrentDestination);
                OffworldMarket market = WorldController.Instance.offworldMarket;
                if (CurrentDestination.ItemIDtoTradeItem != null) {
                    foreach (string item_id in CurrentDestination.ItemIDtoTradeItem.Keys) {
                        TradeItem ti = CurrentDestination.ItemIDtoTradeItem[item_id];
                        int inInvCount = CurrentDestination.GetAmountForThis(new Item(item_id));
                        switch (ti.trade) {
                            case Trade.Buy:
                                if (inInvCount > ti.count)
                                    continue;
                                int omSellPrice = market.GetSellPrice(item_id);
                                float percentage = (ti.price) / (omSellPrice * BuyDifference);
                                if (percentage >= 1) {
                                    int toSell = Mathf.Clamp(Mathf.FloorToInt((ti.count - inInvCount) * (percentage - BuyDifference)), 0, Ship.InventorySize);
                                    Ship.Inventory.AddItem(new Item(item_id, toSell)); // ... cheater ...
                                    CurrentDestination.BuyingTradeItem(item_id, Ship, toSell);
                                }
                                break;

                            case Trade.Sell:
                                if (inInvCount < ti.count)
                                    continue;
                                int omBuyPrice = market.GetBuyPrice(item_id);
                                percentage = (ti.price * BuyDifference) / omBuyPrice;
                                if (percentage <= 1) {
                                    int toBuy = Mathf.Clamp(Mathf.FloorToInt((inInvCount - ti.count) * (1 - percentage)), 0, Ship.InventorySize);
                                    CurrentDestination.SellingTradeItem(item_id, Ship, toBuy);
                                }
                                break;
                        }
                    }
                }
                if (Ship.Inventory.AreSlotsFilledWithItems()) {
                    //TODO: maybe check other cities for in Inventory items?
                    SendHome();
                }
                else {
                    GoToNextCity();
                }
                isAtTrade = false;
            }

            private void OnNextDestinationDestroy(ICity city) {
                city.UnregisterCityDestroy(OnNextDestinationDestroy);
                GoToNextCity();
            }

            private void GoToNextCity() {
                CurrentDestination = Instance.GetNextDestination(this, visitedCities);
                if (CurrentDestination == null) {
                    SendHome();
                    return;
                }
                CurrentDestination.RegisterCityDestroy(OnNextDestinationDestroy);
                if (CurrentDestination.Warehouse == null) {
                    GoToNextCity();
                    visitedCities.Add(CurrentDestination);
                    return;
                }
                CurrentDestination.Warehouse.RegisterOnDestroyCallback(OnWarehouseDestroy);
                Ship.GiveMovementCommand(CurrentDestination.Warehouse.TradeTile);
            }

            private void OnWarehouseDestroy(Structure str, IWarfare destroyer) {
                str.UnregisterOnDestroyCallback(OnWarehouseDestroy);
                visitedCities.Add(CurrentDestination);
                GoToNextCity();
            }

            private void SendHome() {
                Ship.SendToOffworldMarket(null);
                isAtTrade = false;
            }

            internal void Load() {
                Ship.Load();
                if (CurrentDestination == null) {
                    SendHome();
                }
                else {
                    CurrentDestination.Warehouse.RegisterOnDestroyCallback(OnWarehouseDestroy);
                    Ship.GiveMovementCommand(CurrentDestination.Warehouse.TradeTile, true);
                }
                Setup();
            }
        }
    }
}