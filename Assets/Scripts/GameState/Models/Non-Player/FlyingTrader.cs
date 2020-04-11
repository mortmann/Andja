using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
[JsonObject(MemberSerialization.OptIn)]
public class FlyingTrader  {
    public static float TradeTime = 5f;
    public static float BuyDifference = 0.9f;

    public static FlyingTrader Instance { get; protected set; }

    [JsonPropertyAttribute] float startCooldown = 5f;
    [JsonPropertyAttribute] List<TradeShip> Ships;
    List<City> TradeCities;

    public FlyingTrader() {
        Instance = this;
        TradeCities = new List<City>();
        Ships = new List<TradeShip>();
        BuildController.Instance.RegisterCityCreated(OnCityCreated);
    }

    private void OnCityCreated(City city) {
        TradeCities.Add(city);
    }

    public void AddShip() {
        Ship ship = PrototypController.Instance.GetPirateShipPrototyp();
        Tile t = World.Current.GetTileAt(UnityEngine.Random.Range(0, World.Current.Height), 0);
        ship = (Ship)World.Current.CreateUnit(ship, null, t);
        TradeShip ts = new TradeShip(ship);
        Ships.Add(ts);
    }

    public void Update(float deltaTime) {
        if (WorldController.Instance.IsPaused) {
            return;
        }
        if (startCooldown > 0) {
            startCooldown -= deltaTime;
            return;
        }
        if (Ships.Count < 2) {
            AddShip();
        }
    }

    public void OnDestroy() {
        Instance = null;
    }
    protected City GetNextDestination(TradeShip tradeShip, List<City> visited) {
        Vector2 shipPos = tradeShip.Ship.PositionVector2;
        return TradeCities.Except(visited).Aggregate((x, y) => { // 
            return Vector2.Distance(shipPos, x.warehouse.tradeTile.Vector2) < Vector2.Distance(shipPos, y.warehouse.tradeTile.Vector2) ? x : y;
        });
    }

    [JsonObject]
    public class TradeShip {
        public Ship Ship;
        City CurrentDestination;
        List<City> visitedCities;
        float tradeTimer;
        bool isAtTrade;
        public TradeShip(Ship ship) {
            visitedCities = new List<City>();
            this.Ship = ship;
            ship.RegisterOnArrivedAtDestinationCallback(OnShipArriveDestination);
            GoToNextCity();
        }

        private void OnShipArriveDestination(Unit unit, bool goal) {
            if (Ship.isOffWorld) {
                Ship.Destroy();
            } else {
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
            DoTrade();
        }

        private void DoTrade() {
            visitedCities.Add(CurrentDestination);
            OffworldMarket market = WorldController.Instance.offworldMarket;
            foreach (string item_id in CurrentDestination.itemIDtoTradeItem.Keys) {
                TradeItem ti = CurrentDestination.itemIDtoTradeItem[item_id];
                int inInvCount = CurrentDestination.GetAmountForThis(new Item(item_id));
                switch (ti.trade) {
                    case Trade.Buy:
                        if (inInvCount > ti.count)
                            return;
                        int omBuyPrice = market.GetBuyPrice(item_id);
                        float percentage = (ti.price * BuyDifference) / omBuyPrice;
                        if (percentage > 1) {
                            int toBuy = Mathf.Clamp(Mathf.FloorToInt((inInvCount - ti.count) * percentage), 0, Ship.InventorySize);
                            CurrentDestination.SellingTradeItem(item_id, null, Ship, toBuy);
                        }
                        break;
                    case Trade.Sell:
                        if (inInvCount < ti.count)
                            return;
                        int omSellPrice = market.GetSellPrice(item_id);
                        percentage = (ti.price * BuyDifference) / omSellPrice;
                        if (percentage > 1) {
                            int toSell = Mathf.Clamp(Mathf.FloorToInt((ti.count - inInvCount) * percentage), 0, Ship.InventorySize);
                            CurrentDestination.BuyingTradeItem(item_id, null, Ship, toSell);
                        }
                        break;
                }
            }
            if (Ship.inventory.IsFullWithItems()) {
                //TODO: maybe check other cities for in inventory items?
                SendHome();
                
            } else {
                GoToNextCity();
            }
            isAtTrade = false;
        }

        private void OnNextDestinationDestroy(City city) {
            city.UnregisterCityDestroy(OnNextDestinationDestroy);
            GoToNextCity();
        }
        private void GoToNextCity() {
            CurrentDestination = FlyingTrader.Instance.GetNextDestination(this, visitedCities);
            if (CurrentDestination == null)
                SendHome();
            CurrentDestination.RegisterCityDestroy(OnNextDestinationDestroy);
        }
        private void SendHome() {
            Ship.SendToOffworldMarket(null);
        }
    }

}
