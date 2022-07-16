using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Andja.Model {
    public class RoadStructurePrototypeData : StructurePrototypeData {
        public float movementCost = 0.75f;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class RoadStructure : Structure {

        #region RuntimeOrOther

        private Route _route;
        public override string SortingLayer => "Road";
        public Route Route {
            get => _route;
            set {
                _cbRouteChanged?.Invoke(_route, value);
                _route = value;
            }
        }
        private RoadStructurePrototypeData _roadStructureData;
        public RoadStructurePrototypeData RoadStructureData =>
            _roadStructureData ??= (RoadStructurePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);

        public float MovementCost => RoadStructureData.movementCost;

        #endregion RuntimeOrOther

        private Action<RoadStructure> _cbRoadChanged;
        private Action<Route, Route> _cbRouteChanged;

        public RoadStructure(string ID, StructurePrototypeData spd) {
            this.ID = ID;
            this._prototypData = spd;
        }

        protected RoadStructure(RoadStructure str) {
            BaseCopyData(str);
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public RoadStructure() { }

        public override Structure Clone() {
            return new RoadStructure(this);
        }

        public override void OnBuild() {
            List<Route> routes = new List<Route>();
            int routeCount = 0;
            foreach (var t in NeighbourTiles.Where(t => t.Structure != null)) {
                if (!(t.Structure is RoadStructure road)) continue;
                if (road.Route == null) continue;
                if (routes.Contains(road.Route) == false) {
                    routes.Add(road.Route);
                    routeCount++;
                }
                road.UpdateOrientation();
            }
            switch (routeCount) {
                case 0:
                    //If there is no route next to it
                    //so create a new route
                    Route = new Route(Tiles[0]);
                    City.AddRoute(Route);
                    break;
                case 1:
                    // there is already a route
                    // so add it and return
                    routes[0].AddRoadTile(Tiles[0]);
                    Route = routes[0];
                    break;
                default: {
                    routes[0].AddRoadTile(Tiles[0]);
                    //add all Roads from the others to road 1!
                    for (int i = 1; i < routes.Count; i++) {
                        routes[0].AddRoute(routes[i]);
                        Route = routes[0];
                    }
                    break;
                }
            }
            foreach (Tile t in NeighbourTiles) {
                t.Structure?.AddRoadStructure(this);
            }
            UpdateOrientation();
            RegisterOnOwnerChange(OnCityChange);
        }

        protected void OnCityChange(Structure str, ICity old, ICity newOne) {
            if (newOne.Routes.Contains(Route) == false) {
                newOne.AddRoute(Route);
            }
            else {
                Route.CheckForCity(old);
            }
        }

        public void UpdateOrientation() {
            connectOrientation = UpdateOrientation(BuildTile, new List<Tile>());
            _cbRoadChanged?.Invoke(this);
        }

        public static string UpdateOrientation(Tile tile, IEnumerable<Tile> tiles) {
            Tile[] neighbours = tile.GetNeighbours();
            HashSet<Tile> temp = new HashSet<Tile>(tiles);
            string connectOrientation = "_";
            if (temp.Contains(neighbours[0]) || neighbours[0].Structure is RoadStructure) {
                connectOrientation += "N";
            }
            if (temp.Contains(neighbours[1]) || neighbours[1].Structure is RoadStructure) {
                connectOrientation += "E";
            }
            if (temp.Contains(neighbours[2]) || neighbours[2].Structure is RoadStructure) {
                connectOrientation += "S";
            }
            if (temp.Contains(neighbours[3]) || neighbours[3].Structure is RoadStructure) {
                connectOrientation += "W";
            }
            return connectOrientation;
        }

        public override void OnDestroy() {
            _cbRoadChanged = null;
            _cbRouteChanged = null;
            Route?.RemoveRoadTile(BuildTile);
            foreach (Tile item in BuildTile.GetNeighbours()) {
                if(item.Structure is RoadStructure rs) {
                    rs.UpdateOrientation();
                }
            }
        }

        public override string GetSpriteName() {
            return base.GetSpriteName() + connectOrientation;
        }

        public void RegisterOnRoadCallback(Action<RoadStructure> cb) {
            _cbRoadChanged += cb;
        }

        public void UnregisterOnRoadCallback(Action<RoadStructure> cb) {
            _cbRoadChanged -= cb;
        }

        public void RegisterOnRouteCallback(Action<Route, Route> cb) {
            _cbRouteChanged += cb;
        }

        public void UnregisterOnRouteCallback(Action<Route, Route> cb) {
            _cbRouteChanged -= cb;
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            _roadStructureData = null;
        }
        public override string ToString() {
            if (BuildTile == null)
                return base.ToString();
            return Name + " " + Route;
        }
    }
}