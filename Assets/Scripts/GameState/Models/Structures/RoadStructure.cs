using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Andja.Model {
    public class RoadStructurePrototypeData : StructurePrototypeData {
        public float movementCost = 0.75f;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class RoadStructure : Structure {

        #region RuntimeOrOther

        private Route _route;

        public Route Route {
            get { return _route; }
            set {
                cbRouteChanged?.Invoke(_route, value);
                _route = value;
            }
        }
        private RoadStructurePrototypeData _roadStructureData;
        public RoadStructurePrototypeData RoadStructureData {
            get {
                if (_roadStructureData == null) {
                    _roadStructureData = (RoadStructurePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
                }
                return _roadStructureData;
            }
        }
        public float MovementCost => RoadStructureData.movementCost;

        #endregion RuntimeOrOther

        private Action<RoadStructure> cbRoadChanged;
        private Action<Route, Route> cbRouteChanged;

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
            foreach (Tile t in NeighbourTiles) {
                if (t.Structure == null) {
                    continue;
                }
                if (t.Structure.BuildTyp != BuildType.Path) {
                    continue;
                }
                if (t.Structure is RoadStructure) {
                    if (((RoadStructure)t.Structure).Route != null) {
                        if (routes.Contains(((RoadStructure)t.Structure).Route) == false) {
                            routes.Add(((RoadStructure)t.Structure).Route);
                            routeCount++;
                        }
                        ((RoadStructure)t.Structure).UpdateOrientation();
                    }
                }
            }
            if (routeCount == 0) {
                //If there is no route next to it
                //so create a new route
                Route = new Route(Tiles[0]);
                City.AddRoute(Route);
            }
            else
            if (routeCount == 1) {
                // there is already a route
                // so add it and return
                routes[0].AddRoadTile(Tiles[0]);
                Route = routes[0];
            }
            else {
                routes[0].AddRoadTile(Tiles[0]);
                //add all Roads from the others to road 1!
                for (int i = 1; i < routes.Count; i++) {
                    routes[0].AddRoute(routes[i]);
                    Route = routes[0];
                }
            }
            foreach (Tile t in NeighbourTiles) {
                t.Structure?.AddRoadStructure(this);
            }
            UpdateOrientation();
            RegisterOnOwnerChange(OnCityChange);
        }

        protected void OnCityChange(Structure str, City old, City newOne) {
            if (newOne.Routes.Contains(Route) == false) {
                newOne.AddRoute(Route);
            }
            else {
                Route.CheckForCity(old);
            }
        }

        public void UpdateOrientation() {
            Tile[] neig = Tiles[0].GetNeighbours();

            connectOrientation = "_";

            if (neig[0].Structure != null) {
                if (neig[0].Structure is RoadStructure) {
                    connectOrientation += "N";
                }
            }
            if (neig[1].Structure != null) {
                if (neig[1].Structure is RoadStructure) {
                    connectOrientation += "E";
                }
            }
            if (neig[2].Structure != null) {
                if (neig[2].Structure is RoadStructure) {
                    connectOrientation += "S";
                }
            }
            if (neig[3].Structure != null) {
                if (neig[3].Structure is RoadStructure) {
                    connectOrientation += "W";
                }
            }
            cbRoadChanged?.Invoke(this);
        }

        public static string UpdateOrientation(Tile tile, IEnumerable<Tile> tiles) {
            Tile[] neig = tile.GetNeighbours();
            HashSet<Tile> temp = new HashSet<Tile>(tiles);
            string connectOrientation = "_";
            if (temp.Contains(neig[0]) || neig[0].Structure is RoadStructure) {
                connectOrientation += "N";
            }
            if (temp.Contains(neig[1]) || neig[1].Structure is RoadStructure) {
                connectOrientation += "E";
            }
            if (temp.Contains(neig[2]) || neig[2].Structure is RoadStructure) {
                connectOrientation += "S";
            }
            if (temp.Contains(neig[3]) || neig[3].Structure is RoadStructure) {
                connectOrientation += "W";
            }
            return connectOrientation;
        }

        protected override void OnDestroy() {
            cbRoadChanged = null;
            cbRouteChanged = null;
            if (Route != null) {
                Route.RemoveRoadTile(BuildTile);
            }
        }

        public override string GetSpriteName() {
            return base.GetSpriteName() + connectOrientation;
        }

        public void RegisterOnRoadCallback(Action<RoadStructure> cb) {
            cbRoadChanged += cb;
        }

        public void UnregisterOnRoadCallback(Action<RoadStructure> cb) {
            cbRoadChanged -= cb;
        }

        public void RegisterOnRouteCallback(Action<Route, Route> cb) {
            cbRouteChanged += cb;
        }

        public void UnregisterOnRouteCallback(Action<Route, Route> cb) {
            cbRouteChanged -= cb;
        }

        public override string ToString() {
            if (BuildTile == null)
                return base.ToString();
            return Name + " " + Route.ToString();
        }
    }
}