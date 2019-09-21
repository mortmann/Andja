using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class RoadStructure : Structure {
    #region Serialize
    #endregion
    #region RuntimeOrOther

    private Route _route;
    public Route Route {
        get { return _route; }
        set {
            _route = value;
        }
    }

    #endregion


    Action<RoadStructure> cbRoadChanged;

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
        foreach (Tile t in myStructureTiles[0].GetNeighbours()) {
            if (t.Structure == null) {
                continue;
            }
            if (t.Structure.BuildTyp != BuildTypes.Path) {
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
        UpdateOrientation();
        if (routeCount == 0) {
            //If there is no route next to it 
            //so create a new route 
            Route = new Route(myStructureTiles[0]);
            myStructureTiles[0].MyCity.AddRoute(Route);
            return;
        }
        if (routeCount == 1) {
            // there is already a route 
            // so add it and return
            routes[0].AddRoadTile(myStructureTiles[0]);
            Route = routes[0];
            return;
        }
        //add all Roads from the others to road 1!
        for (int i = 1; i < routes.Count; i++) {
            routes[0].AddRoute(routes[i]);
            Route = routes[0];
        }

    }
    public void UpdateOrientation(IEnumerable<Tile> futureRoads = null) {
        Tile[] neig = myStructureTiles[0].GetNeighbours();

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
    protected override void OnDestroy() {
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

}
