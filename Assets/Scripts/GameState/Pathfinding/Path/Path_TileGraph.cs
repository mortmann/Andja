//=======================================================================
// Copyright Martin "quill18" Glaude 2015.
//		http://quill18.com
//=======================================================================

using Andja.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Pathfinding {
    [Obsolete("Not used any more", true)]
    public class Path_TileGraph {
        // This class constructs a simple path-finding compatible graph
        // of our world.  Each tile is a node. Each WALKABLE neighbour
        // from a tile is linked via an edge connection.

        public Dictionary<Tile, Path_Node<Tile>> nodes;
        public HashSet<Tile> Tiles;
        private Route Route = null;

        public Path_TileGraph(Route route) {
            Route = route;
            this.Tiles = new HashSet<Tile>(route.Tiles);
            nodes = new Dictionary<Tile, Path_Node<Tile>>();
            foreach (Tile t in Tiles) {
                Path_Node<Tile> n = new Path_Node<Tile> {
                    data = t
                };
                nodes.Add(t, n);
            }
            // Now loop through all nodes again
            // Create edges for neighbours
            int edgeCount = 0;

            foreach (Tile t in nodes.Keys) {
                Path_Node<Tile> n = nodes[t];

                List<Path_Edge<Tile>> edges = new List<Path_Edge<Tile>>();

                // Get a list of neighbours for the tile
                Tile[] neighbours = t.GetNeighbours(false);  // NOTE: Some of the array spots could be null.
                                                             // If neighbour is walkable, create an edge to the relevant node.
                for (int i = 0; i < neighbours.Length; i++) {
                    // neighbours[i] != null && neighbours[i].Type != TileType.Water && IsClippingCorner(t, neighbours[i]) == false
                    if (nodes.ContainsKey(neighbours[i])) {
                        // This neighbour exists, is walkable, and doesn't requiring clipping a corner --> so create an edge.

                        Path_Edge<Tile> e = new Path_Edge<Tile> {
                            cost = neighbours[i].MovementCost,
                            node = nodes[neighbours[i]]
                        };

                        // Add the edge to our temporary (and growable!) list
                        edges.Add(e);

                        edgeCount++;
                    }
                }

                n.edges = edges.ToArray();
            }
        }

        //internal void RemoveNode(Tile tile) {
        //Path_Node<Tile> remove = nodes[tile];
        //foreach (Tile t in tile.GetNeighbours()) {
        //    if (nodes.ContainsKey(t)) {
        //        var list = nodes[t].edges.ToList();
        //        list.RemoveAll(x => x.node == remove);
        //        nodes[t].edges = list.ToArray();
        //    }
        //}
        //nodes.Remove(tile);
        //Tiles.Remove(tile);
        //}

        public void AddNodeToRouteTileGraph(Tile toAdd) {
            if (nodes.ContainsKey(toAdd)) {
                return;
            }
            Path_Node<Tile> toAdd_node = new Path_Node<Tile> {
                data = toAdd
            };
            List<Path_Edge<Tile>> toAdd_edges = new List<Path_Edge<Tile>>();
            List<Tile> tiles = new List<Tile>(toAdd.GetNeighbours());
            if (Route != null) {
                tiles.RemoveAll(x => x.Structure is RoadStructure == false);
                tiles.RemoveAll(x => Route.Tiles.Contains(x) == false);
                if (tiles.Count == 0) {
                    return;
                }
            }

            nodes.Add(toAdd, toAdd_node);
            foreach (Tile t in tiles) {
                if (nodes.ContainsKey(t)) {
                    //add the edges to the new node
                    Path_Edge<Tile> newEdge = new Path_Edge<Tile> {
                        cost = toAdd.MovementCost,
                        node = nodes[t]
                    };
                    toAdd_edges.Add(newEdge);
                    //update the present nodes with the new edge to the new node
                    Path_Node<Tile> n = nodes[t];
                    List<Path_Edge<Tile>> neighbours_edges = new List<Path_Edge<Tile>>();
                    if (n.edges != null)
                        neighbours_edges.AddRange(n.edges);
                    Path_Edge<Tile> oldEdge = new Path_Edge<Tile> {
                        cost = toAdd.MovementCost,
                        node = nodes[toAdd]
                    };
                    neighbours_edges.Add(oldEdge);
                    n.edges = neighbours_edges.ToArray();
                }
            }
            toAdd_node.edges = toAdd_edges.ToArray();

            if (toAdd_edges.Count == 0) {
                nodes.Remove(toAdd);
            }
            else {
                Tiles.Add(toAdd);
            }
        }

        internal void RemoveNodes(params Tile[] oldTiles) {
            foreach (Tile tile in oldTiles) {
                nodes.Remove(tile);
                Tiles.Remove(tile);
            }
            foreach (Tile tile in oldTiles) {
                foreach (Tile t in tile.GetNeighbours()) {
                    if (nodes.ContainsKey(t)) {
                        var list = nodes[t].edges.ToList();
                        list.RemoveAll(x => x.node.data == tile);
                        nodes[t].edges = list.ToArray();
                    }
                }
            }
        }

        public void AddNodes(Path_TileGraph ptg) {
            foreach (Tile item in ptg.nodes.Keys) {
                if (this.nodes.ContainsKey(item)) {
                    continue;
                }
                Tiles.Add(item);
                this.nodes.Add(item, ptg.nodes[item]);
                foreach (Tile neigh in item.GetNeighbours()) {
                    if (Tiles.Contains(neigh)) {
                        //we need to create a connection between those two
                        Path_Edge<Tile> item_neigh = new Path_Edge<Tile> {
                            cost = item.MovementCost,
                            node = nodes[item]
                        };
                        var list = nodes[neigh].edges.ToList();
                        list.Add(item_neigh);
                        nodes[neigh].edges = list.ToArray();

                        Path_Edge<Tile> neigh_item = new Path_Edge<Tile> {
                            cost = neigh.MovementCost,
                            node = nodes[neigh]
                        };
                        list = nodes[item].edges.ToList();
                        list.Add(neigh_item);
                        nodes[item].edges = list.ToArray();
                    }
                }
            }
        }

        public Path_TileGraph(Island island) {
            this.Tiles = new HashSet<Tile>(island.Tiles);
            nodes = new Dictionary<Tile, Path_Node<Tile>>();
            foreach (Tile t in Tiles) {
                Path_Node<Tile> n = new Path_Node<Tile> {
                    data = t
                };
                nodes.Add(t, n);
            }
            // Now loop through all nodes again
            // Create edges for neighbours

            int edgeCount = 0;

            foreach (Tile t in nodes.Keys) {
                Path_Node<Tile> n = nodes[t];

                List<Path_Edge<Tile>> edges = new List<Path_Edge<Tile>>();

                // Get a list of neighbours for the tile
                //this will allow diagonal movement which is fine
                //for freewalking units on islands
                //TODO: Create better Pathfinding for those units, which is not gridbased?
                Tile[] neighbours = t.GetNeighbours(true);  // NOTE: Some of the array spots could be null.

                // If neighbour is walkable, create an edge to the relevant node.
                for (int i = 0; i < neighbours.Length; i++) {
                    // neighbours[i] != null && neighbours[i].Type != TileType.Water && IsClippingCorner(t, neighbours[i]) == false
                    if (neighbours[i] == null) {
                        continue;
                    }
                    if (nodes.ContainsKey(neighbours[i])) {
                        // This neighbour exists, is walkable, and doesn't requiring clipping a corner --> so create an edge.

                        Path_Edge<Tile> e = new Path_Edge<Tile> {
                            cost = neighbours[i].MovementCost,
                            node = nodes[neighbours[i]]
                        };

                        // Add the edge to our temporary (and growable!) list
                        edges.Add(e);

                        edgeCount++;
                    }
                }

                n.edges = edges.ToArray();
            }
        }

        public Path_TileGraph Clone() {
            return new Path_TileGraph(this.nodes, this.Tiles, this.Route);
        }

        private Path_TileGraph(Dictionary<Tile, Path_Node<Tile>> nodes, HashSet<Tile> myTiles, Route route) {
            this.nodes = new Dictionary<Tile, Path_Node<Tile>>(nodes);
            this.Tiles = new HashSet<Tile>(myTiles);
            this.Route = route;
        }

        public Path_TileGraph(Vector2 lowerBounds, Vector2 upperBounds) {

            // Loop through all tiles of the world
            // For each tile, create a node
            //  Do we create nodes for non-water tiles?  NO!

            nodes = new Dictionary<Tile, Path_Node<Tile>>();

            for (int x = (int)lowerBounds.x - 2; x < upperBounds.x + 2; x++) {
                for (int y = (int)lowerBounds.y - 2; y < upperBounds.y + 2; y++) {
                    Tile t = World.Current.GetTileAt(x, y);

                    if (t.Type == TileType.Ocean) {
                        Path_Node<Tile> n = new Path_Node<Tile> {
                            data = t
                        };
                        nodes.Add(t, n);
                    }
                }
            }
            //		Debug.Log (nodes.Count);

            // Now loop through all nodes again
            // Create edges for neighbours

            int edgeCount = 0;

            foreach (Tile t in nodes.Keys) {
                Path_Node<Tile> n = nodes[t];

                List<Path_Edge<Tile>> edges = new List<Path_Edge<Tile>>();

                // Get a list of neighbours for the tile
                Tile[] neighbours = t.GetNeighbours(true);  // NOTE: Some of the array spots could be null.

                // If neighbour is walkable, create an edge to the relevant node.
                for (int i = 0; i < neighbours.Length; i++) {
                    if (nodes.ContainsKey(neighbours[i]) && neighbours[i] != null && neighbours[i].Type == TileType.Ocean && IsClippingCornerWater(t, neighbours[i]) == false) {
                        // This neighbour exists, is walkable, and doesn't requiring clipping a corner --> so create an edge.

                        Path_Edge<Tile> e = new Path_Edge<Tile> {
                            cost = neighbours[i].MovementCost,
                            node = nodes[neighbours[i]]
                        };

                        // Add the edge to our temporary (and growable!) list
                        edges.Add(e);

                        edgeCount++;
                    }
                }

                n.edges = edges.ToArray();
            }
        }

        private bool IsClippingCorner(Tile curr, Tile neigh) {
            // If the movement from curr to neigh is diagonal (e.g. N-E)
            // Then check to make sure we aren't clipping (e.g. N and E are both walkable)

            int dX = curr.X - neigh.X;
            int dY = curr.Y - neigh.Y;

            if (Mathf.Abs(dX) + Mathf.Abs(dY) == 2) {
                // We are diagonal

                if (World.Current.GetTileAt(curr.X - dX, curr.Y).MovementCost == 0) {
                    // East or West is unwalkable, therefore this would be a clipped movement.
                    return true;
                }

                if (World.Current.GetTileAt(curr.X, curr.Y - dY).MovementCost == 0) {
                    // North or South is unwalkable, therefore this would be a clipped movement.
                    return true;
                }

                // If we reach here, we are diagonal, but not clipping
            }

            // If we are here, we are either not clipping, or not diagonal
            return false;
        }

        private bool IsClippingCornerWater(Tile curr, Tile neigh) {
            // If the movement from curr to neigh is diagonal (e.g. N-E)
            // Then check to make sure we aren't clipping (e.g. N and E are both walkable)

            int dX = curr.X - neigh.X;
            int dY = curr.Y - neigh.Y;

            if (Mathf.Abs(dX) + Mathf.Abs(dY) == 2) {
                // We are diagonal

                if (World.Current.GetTileAt(curr.X - dX, curr.Y).Type != TileType.Ocean) {
                    // East or West is unfloatable, therefore this would be a driving on ground movement.
                    return true;
                }

                if (World.Current.GetTileAt(curr.X, curr.Y - dY).Type != TileType.Ocean) {
                    // North or South is unfloatable, therefore this would be a driving on ground movement.
                    return true;
                }

                // If we reach here, we are diagonal, but not driving on ground movement
            }

            // If we are here, we are either not clipping, or not diagonal
            return false;
        }
    }
}