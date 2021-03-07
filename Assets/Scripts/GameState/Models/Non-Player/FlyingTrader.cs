﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
[JsonObject(MemberSerialization.OptIn)]
public class FlyingTrader  {
    public static float TradeTime = 5f;
    public static float BuyDifference = 0.9f;
    public static int Number => GameData.FlyingTraderNumber;
    public static float WaitBetweenNewShipsTime = 30f;
    public float WaitBetweenNewShipsTimer = WaitBetweenNewShipsTime;
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
        ship = (Ship)World.Current.CreateUnit(ship, null, t, Number);
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
        if (Ships.Count < 1 && TradeCities.Count>0) {
            WaitBetweenNewShipsTimer -= deltaTime;
            if (WaitBetweenNewShipsTimer <= 0) {
                AddShip();
                WaitBetweenNewShipsTimer = WaitBetweenNewShipsTime;
            }
        }
        for (int i = Ships.Count-1; i >= 0; i--) {
            Ships[i].Update(deltaTime);
        }
    }

    public void OnDestroy() {
        Instance = null;
    }
    protected City GetNextDestination(TradeShip tradeShip, List<City> visited) {
        Vector2 shipPos = tradeShip.Ship.PositionVector2;
        if (TradeCities.Count == 0)
            return null;
        IEnumerable<City> remaining = TradeCities.Except(visited);
        if (remaining.Count() == 0)
            return null;
        return remaining.Aggregate((x, y) => { 
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
        public TradeShip() {

        }
        public TradeShip(Ship ship) {
            visitedCities = new List<City>();
            this.Ship = ship;
            ship.RegisterOnArrivedAtDestinationCallback(OnShipArriveDestination);
            tradeTimer = TradeTime;
            GoToNextCity();
        }

        private void OnShipArriveDestination(Unit unit, bool goal) {
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
            if (CurrentDestination.itemIDtoTradeItem != null) { 
            foreach (string item_id in CurrentDestination.itemIDtoTradeItem.Keys) {
                    TradeItem ti = CurrentDestination.itemIDtoTradeItem[item_id];
                    int inInvCount = CurrentDestination.GetAmountForThis(new Item(item_id));
                    switch (ti.trade) {
                        case Trade.Buy:
                            if (inInvCount > ti.count)
                                continue;
                            int omSellPrice = market.GetSellPrice(item_id);
                            float percentage = (ti.price * BuyDifference) / omSellPrice;
                            if (percentage > 1) {
                                int toSell = Mathf.Clamp(Mathf.FloorToInt((ti.count - inInvCount) * (percentage - 1)), 0, Ship.InventorySize);
                                Ship.inventory.AddItem(new Item(item_id, toSell)); // ... cheater ...
                                CurrentDestination.BuyingTradeItem(item_id, null, Ship, toSell);
                            }
                            break;
                        case Trade.Sell:
                            if (inInvCount < ti.count)
                                continue;
                            int omBuyPrice = market.GetBuyPrice(item_id);
                            percentage = (ti.price * BuyDifference) / omBuyPrice;
                            if (percentage < 1) {
                                int toBuy = Mathf.Clamp(Mathf.FloorToInt((inInvCount - ti.count) * (1 - percentage)), 0, Ship.InventorySize);
                                CurrentDestination.SellingTradeItem(item_id, null, Ship, toBuy);
                            }
                            break;
                    }
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
            CurrentDestination = Instance.GetNextDestination(this, visitedCities);
            if (CurrentDestination == null) {
                SendHome();
                return;
            }
            CurrentDestination.RegisterCityDestroy(OnNextDestinationDestroy);
            if (CurrentDestination.warehouse == null) {
                GoToNextCity();
                visitedCities.Add(CurrentDestination);
                return;
            }
            CurrentDestination.warehouse.RegisterOnDestroyCallback(OnWarehouseDestroy);
            Ship.GiveMovementCommand(CurrentDestination.warehouse.tradeTile);
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
    }

}
