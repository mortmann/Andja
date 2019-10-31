﻿using UnityEngine;
using System.Collections.Generic;
using Priority_Queue;
using System.Linq;

public class Path_AStar {

    public Queue<Tile> path;

    List<Tile> startTiles;
    List<Tile> endTiles;


    // for the way back to the first tile
    public Path_AStar(Queue<Tile> backPath) {
        path = backPath;
    }

    public Path_AStar(Island island, Tile tileStart, Tile tileEnd, bool diag = true) {
        if (island == null || tileStart == null || tileEnd == null) {
            return;
        }
        // A dictionary of all valid, walkable nodes.
        Dictionary<Tile, Path_Node<Tile>> nodes = island.TileGraphIslandTiles.nodes;
        Calculate(nodes, tileStart, tileEnd, diag);
    }

    public Path_AStar(Route route, Tile tileStart, Tile tileEnd, List<Tile> startTiles, List<Tile> endTiles) {
        if (route == null || tileStart == null || tileEnd == null) {
            return;
        }
        startTiles.RemoveAll(x => x.Structure != null && x.Structure.IsWalkable == false);
        endTiles.RemoveAll(x => x.Structure != null && x.Structure.IsWalkable == false);
        if (startTiles.Count == 0 || endTiles.Count == 0) {
            return;
        }
        this.startTiles = startTiles;
        this.endTiles = endTiles;
        // A dictionary of all valid, walkable nodes.
        Dictionary<Tile, Path_Node<Tile>> nodes = route.TileGraph.nodes;
        Calculate(nodes, tileStart, tileEnd, false);

    }
    public Path_AStar(Island island, List<Tile> startTiles, List<Tile> endTiles) {
        if (island == null || startTiles == null || endTiles == null || startTiles.Count == 0 || endTiles.Count == 0) {
            return;
        }
        startTiles.RemoveAll(x => x.Structure != null && x.Structure.IsWalkable == false);
        endTiles.RemoveAll(x => x.Structure != null && x.Structure.IsWalkable == false);
        if (startTiles.Count == 0 || endTiles.Count == 0) {
            return;
        }
        this.startTiles = startTiles;
        this.endTiles = endTiles;
        // A dictionary of all valid, walkable nodes.
        Dictionary<Tile, Path_Node<Tile>> nodes = island.TileGraphIslandTiles.nodes;
        Calculate(nodes, startTiles[0], endTiles[0]);
    }
    private void Calculate(Dictionary<Tile, Path_Node<Tile>> nodes, Tile tileStart, Tile tileEnd, bool diag = true) {
        // Make sure our start/end tiles are in the list of nodes!
        if (nodes.ContainsKey(tileStart) == false) {
            Debug.LogError("Path_AStar: The starting tile isn't in the list of nodes!");
            return;
        }
        if (nodes.ContainsKey(tileEnd) == false) {
            Debug.LogError("Path_AStar: The ending tile isn't in the list of nodes!");
            return;
        }
        Path_Node<Tile> start = nodes[tileStart];
        Path_Node<Tile> goal = nodes[tileEnd];


        // Mostly following this pseusocode:
        // https://en.wikipedia.org/wiki/A*_search_algorithm

        List<Path_Node<Tile>> ClosedSet = new List<Path_Node<Tile>>();

        /*		List<Path_Node<Tile>> OpenSet = new List<Path_Node<Tile>>();
                OpenSet.Add( start );
        */

        SimplePriorityQueue<Path_Node<Tile>> OpenSet = new SimplePriorityQueue<Path_Node<Tile>>();
        OpenSet.Enqueue(start, 0);

        Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From = new Dictionary<Path_Node<Tile>, Path_Node<Tile>>();

        Dictionary<Path_Node<Tile>, float> g_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> n in nodes.Values) {
            g_score[n] = Mathf.Infinity;
        }
        g_score[start] = 0;

        Dictionary<Path_Node<Tile>, float> f_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> n in nodes.Values) {
            f_score[n] = Mathf.Infinity;
        }
        f_score[start] = Dist_between_without_diag(start, goal);

        while (OpenSet.Count > 0) {
            Path_Node<Tile> current = OpenSet.Dequeue();
            if (current == goal || endTiles != null && endTiles.Contains(current.data)) {
                // We have reached our goal! or one that is equally as good but closer
                // Let's convert this into an actual sequene of
                // tiles to walk on, then end this constructor function!
                Reconstruct_path(Came_From, current);
                return;
            }
            ClosedSet.Add(current);
            if (startTiles != null && startTiles.Contains(current.data)) {
                Came_From.Clear();
            }
            foreach (Path_Edge<Tile> edge_neighbor in current.edges) {
                if (diag == false) {
                    if ((edge_neighbor.node.data.Vector - current.data.Vector).sqrMagnitude > 1.1) {
                        continue;
                    }
                }
                Path_Node<Tile> neighbor = edge_neighbor.node;

                if (ClosedSet.Contains(neighbor) == true)
                    continue; // ignore this already completed neighbor
                float movement_cost_to_neighbor = neighbor.data.MovementCost * Dist_between(current, neighbor);
                float tentative_g_score = g_score[current] + movement_cost_to_neighbor;

                if (OpenSet.Contains(neighbor) && tentative_g_score >= g_score[neighbor])
                    continue;

                Came_From[neighbor] = current;
                g_score[neighbor] = tentative_g_score;
                f_score[neighbor] = g_score[neighbor] + Heuristic_cost_estimate(neighbor, goal);

                if (OpenSet.Contains(neighbor) == false) {
                    OpenSet.Enqueue(neighbor, f_score[neighbor]);
                }
                else {
                    OpenSet.UpdatePriority(neighbor, f_score[neighbor]);
                }

            } // foreach neighbour
        } // while

        // If we reached here, it means that we've burned through the entire
        // OpenSet without ever reaching a point where current == goal.
        // This happens when there is no path from start to goal
        // (so there's a wall or missing floor or something).

        // We don't have a failure state, maybe? It's just that the
        // path list will be null.

    }


    float Heuristic_cost_estimate(Path_Node<Tile> a, Path_Node<Tile> b) {

        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
        );

    }

    float Dist_between(Path_Node<Tile> a, Path_Node<Tile> b) {
        // We can make assumptions because we know we're working
        // on a grid at this point.

        // Hori/Vert neighbours have a distance of 1
        if (Mathf.Abs(a.data.X - b.data.X) + Mathf.Abs(a.data.Y - b.data.Y) == 1) {
            return 1f;
        }

        // Diag neighbours have a distance of 1.41421356237	
        if (Mathf.Abs(a.data.X - b.data.X) == 1 && Mathf.Abs(a.data.Y - b.data.Y) == 1) {
            return 1.41421356237f;
        }

        // Otherwise, do the actual math.
        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
        );

    }
    float Dist_between_without_diag(Path_Node<Tile> a, Path_Node<Tile> b) {
        // We can make assumptions because we know we're working
        // on a grid at this point.

        // Hori/Vert neighbours have a distance of 1
        if (Mathf.Abs(a.data.X - b.data.X) + Mathf.Abs(a.data.Y - b.data.Y) == 1) {
            return 1f;
        }
        // Otherwise, do the actual math.
        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
        );

    }
    void Reconstruct_path(
        Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From,
        Path_Node<Tile> current
    ) {
        // So at this point, current IS the goal.
        // So what we want to do is walk backwards through the Came_From
        // map, until we reach the "end" of that map...which will be
        // our starting node!
        Queue<Tile> total_path = new Queue<Tile>();
        total_path.Enqueue(current.data); // This "final" step is the path is the goal!
        while (Came_From.ContainsKey(current)) {
            // Came_From is a map, where the
            //    key => value relation is real saying
            //    some_node => we_got_there_from_this_node

            current = Came_From[current];
            total_path.Enqueue(current.data);
        }

        // At this point, total_path is a queue that is running
        // backwards from the END tile to the START tile, so let's reverse it.

        path = new Queue<Tile>(total_path.Reverse());
    }

    public Tile Dequeue() {
        return path.Dequeue();
    }


    public int Length() {
        if (path == null)
            return 0;

        return path.Count;
    }

}