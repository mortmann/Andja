//=======================================================================
// Copyright Martin "quill18" Glaude 2015.
//		http://quill18.com
//=======================================================================

using UnityEngine;
using System.Collections.Generic;


public class Path_TileGraph {

	// This class constructs a simple path-finding compatible graph
	// of our world.  Each tile is a node. Each WALKABLE neighbour
	// from a tile is linked via an edge connection.

	public Dictionary<Tile, Path_Node<Tile>> nodes;
    public List<Tile> myTiles;

	public Path_TileGraph(Route route) {
		this.myTiles = route.myTiles;
        nodes = new Dictionary<Tile, Path_Node<Tile>>();
        foreach(Tile t in myTiles) {
            Path_Node<Tile> n = new Path_Node<Tile>();
            n.data = t;
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

                    Path_Edge<Tile> e = new Path_Edge<Tile>();
                    e.cost = neighbours[i].MovementCost;
                    e.node = nodes[neighbours[i]];

                    // Add the edge to our temporary (and growable!) list
                    edges.Add(e);

                    edgeCount++;
                }
            }

            n.edges = edges.ToArray();
        }

    }
	public void AddNodeToRouteTileGraph(Tile toAdd){
		if(nodes.ContainsKey(toAdd) == true){
			return;
		}
		Path_Node<Tile> toAdd_node= new Path_Node<Tile>();
		toAdd_node.data = toAdd;
		nodes.Add (toAdd,toAdd_node);
		List<Path_Edge<Tile>> toAdd_edges = new List<Path_Edge<Tile>>();
		foreach (Tile t in toAdd.GetNeighbours ()) {
			if(nodes.ContainsKey (t)){
				//add the edges to the new node
				Path_Edge<Tile> newEdge = new Path_Edge<Tile>();
				newEdge.cost = toAdd.MovementCost;
				newEdge.node = nodes[toAdd];
				toAdd_edges.Add (newEdge);


				//update the present nodes with the new edge to the new node
				Path_Node<Tile> n = nodes [t];
				List<Path_Edge<Tile>> neighbours_edges = new List<Path_Edge<Tile>>(n.edges);
				Path_Edge<Tile> oldEdge = new Path_Edge<Tile>();
				oldEdge.cost = toAdd.MovementCost;
				oldEdge.node = nodes[toAdd];
				neighbours_edges.Add (oldEdge);
				n.edges = neighbours_edges.ToArray ();
			}
			toAdd_node.edges = toAdd_edges.ToArray ();

		}



	}
	public void addNodes(Path_TileGraph ptg){
		foreach (Tile item in ptg.nodes.Keys) {
			if(this.nodes.ContainsKey(item)){
				continue;
			}
			this.nodes.Add (item,ptg.nodes[item]);
		}
	}

	public Path_TileGraph(Island island) {
		this.myTiles = island.myTiles;
		nodes = new Dictionary<Tile, Path_Node<Tile>>();
		foreach(Tile t in myTiles) {
			Path_Node<Tile> n = new Path_Node<Tile>();
			n.data = t;
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

					Path_Edge<Tile> e = new Path_Edge<Tile>();
					e.cost = neighbours[i].MovementCost;
					e.node = nodes[neighbours[i]];

					// Add the edge to our temporary (and growable!) list
					edges.Add(e);

					edgeCount++;
				}
			}

			n.edges = edges.ToArray();
		}

	}

    public Path_TileGraph(World world) {


		// Loop through all tiles of the world
		// For each tile, create a node
		//  Do we create nodes for non-floor tiles?  NO!
		//  Do we create nodes for tiles that are completely unwalkable (i.e. walls)?  NO!

		nodes = new Dictionary<Tile, Path_Node<Tile>>();

		for (int x = 0; x < world.Width; x++) {
			for (int y = 0; y < world.Height; y++) {

				Tile t = world.GetTileAt(x,y);

				if(t.Type == TileType.Water) {	
					Path_Node<Tile> n = new Path_Node<Tile>();
					n.data = t;
					nodes.Add(t, n);
				}

			}
		}



		// Now loop through all nodes again
		// Create edges for neighbours

		int edgeCount = 0;

		foreach(Tile t in nodes.Keys) {
			Path_Node<Tile> n = nodes[t];

			List<Path_Edge<Tile>> edges = new List<Path_Edge<Tile>>();

			// Get a list of neighbours for the tile
			Tile[] neighbours = t.GetNeighbours(true);	// NOTE: Some of the array spots could be null.

			// If neighbour is walkable, create an edge to the relevant node.
			for (int i = 0; i < neighbours.Length; i++) {
				if(neighbours[i] != null && neighbours[i].Type == TileType.Water && IsClippingCornerWater( t, neighbours[i] ) == false) {
					// This neighbour exists, is walkable, and doesn't requiring clipping a corner --> so create an edge.

					Path_Edge<Tile> e = new Path_Edge<Tile>();
					e.cost = neighbours[i].MovementCost;
					e.node = nodes[ neighbours[i] ];

					// Add the edge to our temporary (and growable!) list
					edges.Add(e);

					edgeCount++;
				}
			}

			n.edges = edges.ToArray();
		}

	}

	bool IsClippingCorner( Tile curr, Tile neigh ) {
		// If the movement from curr to neigh is diagonal (e.g. N-E)
		// Then check to make sure we aren't clipping (e.g. N and E are both walkable)

		int dX = curr.X - neigh.X;
		int dY = curr.Y - neigh.Y;

		if( Mathf.Abs(dX) + Mathf.Abs(dY) == 2 ) {
			// We are diagonal

			if( World.current.GetTileAt( curr.X - dX, curr.Y ).MovementCost == 0 ) {
				// East or West is unwalkable, therefore this would be a clipped movement.
				return true;
			}

			if( World.current.GetTileAt( curr.X, curr.Y - dY ).MovementCost == 0 ) {
				// North or South is unwalkable, therefore this would be a clipped movement.
				return true;
			}

			// If we reach here, we are diagonal, but not clipping
		}

		// If we are here, we are either not clipping, or not diagonal
		return false;
	}
    bool IsClippingCornerWater(Tile curr, Tile neigh) {
        // If the movement from curr to neigh is diagonal (e.g. N-E)
        // Then check to make sure we aren't clipping (e.g. N and E are both walkable)

        int dX = curr.X - neigh.X;
        int dY = curr.Y - neigh.Y;

        if (Mathf.Abs(dX) + Mathf.Abs(dY) == 2) {
            // We are diagonal

			if (World.current.GetTileAt(curr.X - dX, curr.Y).Type != TileType.Water) {
                // East or West is unfloatable, therefore this would be a driving on ground movement.
                return true;
            }

			if (World.current.GetTileAt(curr.X, curr.Y - dY).Type != TileType.Water) {
                // North or South is unfloatable, therefore this would be a driving on ground movement.
                return true;
            }

            // If we reach here, we are diagonal, but not driving on ground movement
        }

        // If we are here, we are either not clipping, or not diagonal
        return false;
    }
}
