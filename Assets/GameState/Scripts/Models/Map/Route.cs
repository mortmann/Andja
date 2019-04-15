using UnityEngine;
using System.Collections.Generic;
using System.Linq;
//DOESNT NEED TO BE SAVED
//GETS CREATED WHEN NEEDED
public class Route {
    public Path_TileGraph TileGraph { get; protected set; }
    public List<Tile> myTiles;
    public Route(Tile startTile, bool floodfill = false) {
        myTiles = new List<Tile> {
            startTile
        };
        if (floodfill) {
            RouteFloodFill(startTile);
        }
        TileGraph = new Path_TileGraph(this);
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
            if (t.Structure.MyStructureTyp == StructureTyp.Pathfinding) {
                myTiles.Add(t);
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
        myTiles.Add(tile);
        TileGraph.AddNodeToRouteTileGraph(tile);
    }
    public void RemoveRoadTile(Tile tile) {
        if (myTiles.Count == 1) {
            //this route does not have any more roadtiles so kill it
            myTiles[0].MyCity.RemoveRoute(this);
            return;
        }
        myTiles.Remove(tile);
        //cheack if it can split up in to routes
        int neighboursOfRoute = 0;
        foreach (Tile t in tile.GetNeighbours(false)) {
            if (myTiles.Contains(t)) {
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
        List<Tile> oldTiles = new List<Tile>(myTiles);
        RouteFloodFill(myTiles[0]);
        //so we have all tiles we can reach from starttile
        //now we need to check if that is all
        //if not create on the others new routes!
        oldTiles = oldTiles.Except(myTiles).ToList();
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
                item.MyCity.AddRoute(new Route(item, true));
            }
        }
    }

    public void AddRoute(Route route) {
        foreach (Tile item in route.myTiles) {
            ((RoadStructure)item.Structure).Route = this;
        }
        TileGraph.addNodes(route.TileGraph);
        myTiles[0].MyCity.RemoveRoute(route);

    }

    ///for debug purpose only if no longer needed delete
    public override string ToString() {
        return myTiles[0].ToString() + "_Route";
    }

}
