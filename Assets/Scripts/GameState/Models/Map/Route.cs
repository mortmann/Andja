using Andja.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    //DOESNT NEED TO BE SAVED
    //GETS CREATED WHEN NEEDED
    /// <summary>
    /// Represents a single network of all connected RoadStructures.
    /// It handles all related things like merging, splitting and city changes.
    /// </summary>
    public class Route {
        public PathGrid Grid { get; protected set; }

        public List<Tile> Tiles;
        public HashSet<MarketStructure> MarketStructures;
        public Route() {
#if !UNITY_INCLUDE_TESTS
            Debug.LogError("Wrong Constructor! Only for testing");
#endif
        }
        public Route(Tile startTile, bool floodfill = false) {
            Tiles = new List<Tile> {
                startTile
            };
            if (floodfill) {
                RouteFloodFill(startTile);
            }
            Grid = new PathGrid(this);
        }

        protected void RouteFloodFill(Tile tile) {
            if (tile == null) {
                // We are trying to flood fill off the map, so just return
                // without doing anything.
                Debug.LogError("Data corrupted!");
                return;
            }
            if (tile.Structure == null) {
                // There is no road or structure at all
                Debug.LogError("Path got not updated previously!");
                return;
            }
            HashSet<Tile> alreadyChecked = new HashSet<Tile>();
            Queue<Tile> tilesToCheck = new Queue<Tile>();
            tilesToCheck.Enqueue(tile);
            while (tilesToCheck.Count > 0) {
                Tile t = tilesToCheck.Dequeue();
                alreadyChecked.Add(t);
                if (t.Type == TileType.Ocean || t.Structure == null) {
                    continue;
                }
                if (t.Structure is RoadStructure rs) {
                    Tiles.Add(t);
                    rs.Route = this;
                    Tile[] ns = t.GetNeighbours();
                    foreach (Tile t2 in ns) {
                        if (alreadyChecked.Contains(t2)) {
                            continue;
                        }
                        tilesToCheck.Enqueue(t2);
                    }
                }
            }
        }

        public void AddRoadTile(Tile tile) {
            Tiles.Add(tile);
            Grid.ChangeNode(tile, Walkable.Normal);
        }

        public void RemoveRoadTile(Tile tile) {
            if (Tiles.Count == 1) {
                //this route does not have any more roadtiles so kill it
                Tiles[0].City.RemoveRoute(this);
                MarketStructures.Clear();
                Grid.Obsolete = true;
                return;
            }
            Tiles.Remove(tile);
            Grid.ChangeNode(tile, Walkable.Never);
            //cheack if it can split up in to routes
            int neighboursOfRoute = 0;
            foreach (Tile t in tile.GetNeighbours(false)) {
                if (Tiles.Contains(t)) {
                    neighboursOfRoute++;
                }
            }
            if (neighboursOfRoute == 1) {
                //it was an endtile so it couldnt destroy the route so just
                //end here
                return;
            }
            //yes it can, check if every roadtile has a connection to the routestarttile
            //if not create new route for those seperated roads
            List<Tile> oldTiles = new List<Tile>(Tiles);
            Tiles.Clear();
            RouteFloodFill(oldTiles[0]);
            //so we have all tiles we can reach from starttile
            //now we need to check if that is all
            //if not create on the others new routes!
            oldTiles = oldTiles.Except(Tiles).ToList();
            foreach(Tile t in oldTiles) {
                Grid.ChangeNode(t, Walkable.Never);
            }
            if (oldTiles.Count > 0) {
                //we have tiles that the flood fill didnt reach
                //that means we have to create new routes and floodfill them from there
                foreach (Tile item in oldTiles) {
                    if (((RoadStructure)item.Structure).Route != this) {
                        continue;
                    }
                    //create and add the new route to our city
                    //this can be prolematic if the new route is not part of this city
                    //eg conection to other city got cut at the border
                    //thats why the new items city gets the route added
                    item.City.AddRoute(new Route(item, true));
                }
            }
        }

        public void AddMarketStructure(MarketStructure ms) {
            if (MarketStructures == null)
                MarketStructures = new HashSet<MarketStructure>();
            MarketStructures.Add(ms);
        }

        public void RemoveMarketStructure(MarketStructure ms) {
            MarketStructures.Remove(ms);
        }

        internal void CheckForCity(City old) {
            if (Tiles.Exists(t => t.City == old) == false) {
                old.RemoveRoute(this);
            }
        }

        public void AddRoute(Route route) {
            Tiles.AddRange(route.Tiles);
            foreach (Tile item in route.Tiles) {
                if (item.Structure != null)
                    ((RoadStructure)item.Structure).Route = this;
            }
            var cities = route.Tiles.Select(t => t.City).Distinct();
            foreach (City c in cities) {
                if (c.Routes.Contains(this) == false) {
                    c.AddRoute(this);
                }
                c.RemoveRoute(route);
            }
            foreach(Tile t in route.Tiles) {
                Grid.ChangeNode(t, Walkable.Normal);
            }
            route.Tiles.Clear();
        }

        ///for debug purpose only if no longer needed delete
        public override string ToString() {
            if (Tiles.Count == 0)
                return "EMPTY";
            return Tiles[0].X + ":" + Tiles[0].Y + "_Route " + Tiles[0].City + " - " + Tiles[0].City.Routes.IndexOf(this);
        }
    }
}