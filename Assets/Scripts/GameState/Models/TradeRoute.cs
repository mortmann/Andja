using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Andja.Utility;

namespace Andja.Model {

    public enum TradeTyp { Load, Unload }

    [JsonObject(MemberSerialization.OptIn)]
    public class TradeRoute {
        public const float TRADE_TIME = 1.5f; //TODO: make it a setable value

        private int NumberOfStops { get { return Goals.Count; } }
        [JsonPropertyAttribute] public List<Stop> Goals { get; set; }
        [JsonPropertyAttribute] public string Name = "Temporary";
        /// <summary>
        /// On Load it will get them from the ship load funtion.
        /// </summary>
        public List<Ship> Ships = new List<Ship>();
        /// <summary>
        /// Double data just for a more convient access (and faster) to trades-- could be done with everytime select the type
        /// but for now we will keep it like this
        /// </summary>
        List<Trade> _Trades; 
        public List<Trade> Trades {
            get {
                if (Goals.Count == 0)
                    _Trades = new List<Trade>();
                if (_Trades == null)
                    _Trades = Goals.OfType<Trade>().ToList();
                return _Trades;
            }
        }

        public bool Valid {
            get {
                return _Trades.Count > 1;
            }
        }
        public int TradeCount => _Trades.Count;

        public TradeRoute() {
            Goals = new List<Stop>();
        }

        public TradeRoute(TradeRoute tr) {
            this.Goals = tr.Goals;
        }

        public void AddCity(ICity c) {
            Trade t = new Trade(c);
            Trades.Add(t);
            Goals.Add(t);
        }

        public void SetCityTrade(ICity city, List<Item> getting, List<Item> giving) {
            Trade t = Trades.Find(x => x.city == city);
            if (t == null) return;
            t.load = getting;
            t.unload = giving;
        }

        internal void RemoveStop(Stop stop) {
            Goals.Remove(stop);
        }

        public void SetName(string name) {
            Name = name;
        }
        public Stop GetCurrentGoal(Ship ship) {
            return Goals[ship.nextTradeRouteStop];
        }

        public void RemoveCity(ICity city) {
            Trade t = GetTradeFor(city);
            if (t == null) {
                return; 
            }
            foreach (Ship ship in Ships) {
                int currentDestination = ship.nextTradeRouteStop;
                if (Goals.IndexOf(t) < currentDestination) {
                    currentDestination--; // smaller then we must remove to be on the same still
                }
                else
                if (Goals.IndexOf(t) == currentDestination) {
                    //if its behind the otherone so decrease the destination pointer
                    currentDestination = (currentDestination - 1).ClampZero(NumberOfStops - 1);
                }
                ship.nextTradeRouteStop = currentDestination;
            }
            Goals.Remove(t);
            Trades.Remove(t);
        }

        public int GetLastNumber() {
            return Trades.Count;
        }

        public int GetNumberFor(ICity city) {
            for (int i = 0; i < Goals.Count; i++) {
                if (Trades[i].city == city) {
                    return i + 1;
                }
            }
            return -1;
        }

        public Vector2? GetCurrentDestination(Ship ship) {
            if (Goals.Count == 0) {
                return null;
            }
            if (Goals[ship.nextTradeRouteStop].IsValid) {
                return null;
            }
            return Goals[ship.nextTradeRouteStop].Destination;
        }

        public Vector2? GetNextDestination(Ship ship) {
            if (Valid == false) {
                return null;
            }
            //Go through the Route until it finds a valid target.
            for (int i = 0; i < NumberOfStops; i++) {
                IncreaseDestination(ship);
                if (Goals[ship.nextTradeRouteStop].IsValid == false) {
                    return Goals[ship.nextTradeRouteStop].Destination;
                }
            }
            return null;
        }

        public void IncreaseDestination(Ship ship) {
            ship.nextTradeRouteStop = (ship.nextTradeRouteStop + 1) % Goals.Count;
        }

        public bool Contains(ICity c) {
            return GetTradeFor(c) != null;
        }

        public Trade GetTrade(int number) {
            if (Trades.Count <= number || number < 0)
                return null;
            return Trades[number];
        }

        public Trade GetTradeFor(ICity c) {
            return Trades.Find(x => x.city == c);
        }

        public void DoCurrentTrade(Ship ship) {
            Trade trade = GetCurrentGoal(ship) as Trade;
            ICity c = trade.city;
            Inventory inv = ship.inventory;
            //FIRST unload THEN load !

            //give as much as possible but max the choosen one
            foreach (Item item in trade.unload) {
                c.TradeFromShip(ship, item, item.count);
            }
            //only get some if its needed
            foreach (Item item in trade.load) {
                int needed = item.count - inv.GetAmountFor(item);
                if (needed <= 0) {
                    continue;
                }
                c.TradeWithShip(item, () => needed, ship);
            }
        }

        public bool AddItemToTrade(ICity city, Item item, TradeTyp typ) {
            Trade t = GetTradeFor(city);
            return typ switch {
                TradeTyp.Load => t.AddLoadItem(item),
                TradeTyp.Unload => t.AddUnloadItem(item),
                _ => false
            };
        }

        public bool RemoveItemToTrade(ICity city, Item item, TradeTyp typ) {
            Trade t = GetTradeFor(city);
            return typ switch {
                TradeTyp.Load => t.RemoveLoadItem(item),
                TradeTyp.Unload => t.RemoveUnloadItem(item),
                _ => false
            };
        }

        public bool RemoveItemFromTrade(ICity city, Item currentlySelectedItem) {
            Trade t = GetTradeFor(city);
            return t.RemoveItem(currentlySelectedItem);
        }

        public void Destroy() {
            foreach (Ship item in Ships) {
                item.SetTradeRoute(null);
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Stop {
            [JsonPropertyAttribute] private SeriaziableVector2 _position;
            public virtual bool IsValid => true;
            public Stop() {
            }

            public Stop(Vector2 position) {
                _position = position;
            }
            public void SetPosition(Vector2 position) {
                this._position = position;
            }
            public virtual Vector2 Destination => _position;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Trade : Stop {
            [JsonPropertyAttribute] public ICity city;
            [JsonPropertyAttribute] public List<Item> load;
            [JsonPropertyAttribute] public List<Item> unload;
            public override bool IsValid => city.Warehouse != null;

            public override Vector2 Destination => (city.Warehouse?.TradeTile.Vector2).GetValueOrDefault();
            public Trade(ICity c) {
                city = c;
                load = new List<Item>();
                unload = new List<Item>();
            }

            public Trade(Trade t) {
                city = t.city;
                load = t.load;
                unload = t.unload;
            }

            public Trade() {
            }

            public bool RemoveItem(Item item) {
                return RemoveLoadItem(item) || RemoveUnloadItem(item);
            }

            public bool AddLoadItem(Item item) {
                if (load.Contains(item))
                    return false;
                load.Add(item);
                return true;
            }

            public bool AddUnloadItem(Item item) {
                if (unload.Contains(item))
                    return false;
                unload.Add(item);
                return true;
            }

            public bool RemoveLoadItem(Item item) {
                if (load.Contains(item))
                    return false;
                load.Remove(item);
                return true;
            }

            public bool RemoveUnloadItem(Item item) {
                if (unload.Contains(item))
                    return false;
                unload.Remove(item);
                return true;
            }
        }

        internal Stop AddStop(Stop addAfter, Vector2 stopPos) {
            Stop stop = new Stop(stopPos);
            Goals.Insert(Goals.IndexOf(addAfter)+1, stop);
            return stop;
        }

        internal void AddShip(Ship ship) {
            ship.SetTradeRoute(this);
            Ships.Add(ship);
        }

        internal void RemoveShip(Ship ship) {
            //stop it from following the last order
            ship.StopTradeRoute();
            //removes it from this
            ship.SetTradeRoute(null);
            Ships.Remove(ship);
        }

        internal float AtDestination(Ship ship) {
            if(GetCurrentGoal(ship) is Trade) {
                return TRADE_TIME;
            }
            return 0;
        }

        internal void LoadShip(Ship ship) {
            Ships.Add(ship);
        }
    }
}