using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Andja.Model {

    public enum TradeTyp { Load, Unload }

    [JsonObject(MemberSerialization.OptIn)]
    public class TradeRoute {
        public const float TRADE_TIME = 1.5f; //TODO: make it a setable value

        private int NumberOfStops { get { return Goals.Count; } }
        [JsonPropertyAttribute] public List<Stop> Goals { get; set; }
        [JsonPropertyAttribute] public Dictionary<Ship, int> shipToNextStop;
        [JsonPropertyAttribute] public string Name = "Temporary";
        /// <summary>
        /// Double data just for a more convient access (and faster) to trades-- could be done with everytime select the type
        /// but for now we will keep it like this
        /// </summary>
        List<Trade> _Trades; 
        public List<Trade> Trades {
            get {
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
            shipToNextStop = new Dictionary<Ship, int>();
        }

        public TradeRoute(TradeRoute tr) {
            this.Goals = tr.Goals;
            this.shipToNextStop = tr.shipToNextStop;
        }

        public void AddCity(City c) {
            Trade t = new Trade(c);
            Trades.Add(t);
            Goals.Add(t);
        }

        public void SetCityTrade(City city, List<Item> getting, List<Item> giving) {
            Trade t = Trades.Find(x => x.city == city);
            if (t != null) {
                t.load = getting;
                t.unload = giving;
            }
        }

        internal void RemoveStop(Stop stop) {
            Goals.Remove(stop);
        }

        public void SetName(string name) {
            Name = name;
        }
        public Trade GetCurrentGoal(Ship ship) {
            Stop s = Goals[shipToNextStop[ship]];
            if (s is Trade t)
                return t;
            else
                return null;
        }

        public void RemoveCity(City city) {
            Trade t = GetTradeFor(city);
            if (t == null) {
                return; 
            }
            foreach (Ship ship in shipToNextStop.Keys.ToArray()) {
                int currentDestination = shipToNextStop[ship];
                if (Goals.IndexOf(t) < currentDestination) {
                    currentDestination--; // smaller then we must remove to be on the same still
                }
                else
                if (Goals.IndexOf(t) == currentDestination) {
                    //if its behind the otherone so decrease the destination pointer
                    currentDestination--;
                    currentDestination = Mathf.Clamp(currentDestination, 0, NumberOfStops - 1);
                }
                shipToNextStop[ship] = currentDestination;
            }
            Goals.Remove(t);
            Trades.Remove(t);
        }

        public int GetLastNumber() {
            return Trades.Count;
        }

        public int GetNumberFor(City city) {
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
            if (Goals[shipToNextStop[ship]].Destination == null) {
                return null;
            }
            return Goals[shipToNextStop[ship]].Destination;
        }

        public Vector2? GetNextDestination(Ship ship) {
            if (Valid == false) {
                return null;
            }
            //Go through the Route until it finds a valid target.
            for (int i = 0; i < NumberOfStops; i++) {
                IncreaseDestination(ship);
                if (Goals[shipToNextStop[ship]].Destination != null) {
                    return Goals[shipToNextStop[ship]].Destination;
                }
            }
            return null;
        }

        public void IncreaseDestination(Ship ship) {
            shipToNextStop[ship] = (shipToNextStop[ship] + 1) % Goals.Count;
        }

        public bool Contains(City c) {
            return GetTradeFor(c) != null;
        }

        public Trade GetTrade(int number) {
            if (Trades.Count <= number || number < 0)
                return null;
            return Trades[number];
        }

        public Trade GetTradeFor(City c) {
            return Trades.Find(x => x.city == c);
        }

        public void DoCurrentTrade(Ship ship) {
            Trade t = GetCurrentGoal(ship);
            City c = t.city;
            Inventory inv = ship.inventory;
            //FIRST unload THEN load !

            //give as much as possible but max the choosen one
            foreach (Item item in t.unload) {
                c.TradeFromShip(ship, item, item.count);
            }
            //only get some if its needed
            foreach (Item item in t.load) {
                int needed = item.count - inv.GetTotalAmountFor(item);
                if (needed <= 0) {
                    continue;
                }
                c.TradeWithShip(item, needed, ship);
            }
        }

        public bool AddItemToTrade(City city, Item item, TradeTyp typ) {
            Trade t = GetTradeFor(city);
            switch (typ) {
                case TradeTyp.Load:
                    return t.AddLoadItem(item);

                case TradeTyp.Unload:
                    return t.AddUnloadItem(item);
            }
            return false;
        }

        public bool RemoveItemToTrade(City city, Item item, TradeTyp typ) {
            Trade t = GetTradeFor(city);
            switch (typ) {
                case TradeTyp.Load:
                    return t.RemoveLoadItem(item);

                case TradeTyp.Unload:
                    return t.RemoveUnloadItem(item);
            }
            return false;
        }

        public bool RemoveItemFromTrade(City city, Item currentlySelectedItem) {
            Trade t = GetTradeFor(city);
            return t.RemoveItem(currentlySelectedItem);
        }

        public void Destroy() {
            foreach (Ship s in shipToNextStop.Keys) {
                s.StopTradeRoute();
            }
        }
        [JsonObject(MemberSerialization.OptIn)]
        public class Stop {
            [JsonPropertyAttribute] private Vector2 Position;

            public Stop() {
            }

            public Stop(Vector2 position) {
                Position = position;
            }
            public void SetPosition(Vector2 position) {
                this.Position = position;
            }
            public virtual Vector2 Destination => Position;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Trade : Stop {
            [JsonPropertyAttribute] public City city;
            [JsonPropertyAttribute] public List<Item> load;
            [JsonPropertyAttribute] public List<Item> unload;
            public override Vector2 Destination => (city.warehouse?.GetTradeTile().Vector2).Value;
            public Trade(City c) {
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
                if (RemoveLoadItem(item))
                    return true;
                if (RemoveUnloadItem(item))
                    return true;
                return false;
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
            shipToNextStop.Add(ship, 0);
            ship.SetTradeRoute(this);
        }

        internal void RemoveShip(Ship ship) {
            shipToNextStop.Remove(ship);
            ship.StopTradeRoute();
        }

        internal float AtDestination(Ship ship) {
            if(GetCurrentGoal(ship) is Trade t) {
                return TRADE_TIME;
            } else {
                return 0;
            }
        }
    }
}