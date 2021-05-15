using Priority_Queue;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Pathfinding {
    public static class Pathfinder {
        public const float DIAGONAL_EXTRA_COST = 1.41421356237f;
        public const float NORMAL_COST = 1;

        public static Queue<Vector2> Find(IPathfindAgent agent, PathGrid grid, 
                                            Vector2 startPos, Vector2 endPos, 
                                            List<Vector2> startsPos = null, List<Vector2> endsPos = null) {
            Node start = grid.GetNodeFromWorldCoord(startPos);
            Node end = grid.GetNodeFromWorldCoord(endPos);
            HashSet<Node> endNodes = null;
            if(endsPos != null) {
                endNodes = new HashSet<Node>();
                foreach(Vector2 e in endsPos) {
                    endNodes.Add(grid.GetNodeFromWorldCoord(e));
                }
            }
            HashSet<Node> startNodes = null;
            if (startNodes != null) {
                startNodes = new HashSet<Node>();
                foreach (Vector2 e in startsPos) {
                    startNodes.Add(grid.GetNodeFromWorldCoord(e));
                }
            }
            Dictionary<Node, float> g_score = new Dictionary<Node, float>();
            Dictionary<Node, float> f_score = new Dictionary<Node, float>();
            Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();

            HashSet<Node> ClosedSet = new HashSet<Node>();
            SimplePriorityQueue<Node> OpenSet = new SimplePriorityQueue<Node>();

            foreach (Node n in grid.Values) {
                if (n == null)
                    continue;
                g_score.Add(n, Mathf.Infinity);
                f_score.Add(n, Mathf.Infinity);
            }
            g_score[start] = 0;
            f_score[start] = DistanceNodes(agent, start, end);
            OpenSet.Enqueue(start, 0);
            if (agent.CanEndInUnwakable == false && end.IsPassable(agent.CanEnterCities?.ToList())) {
                //cant end were it is supposed to go -- find a alternative that can be walked on
                endNodes = FindClosestWalkableNeighbours(agent, end, grid);
            }
            while (OpenSet.Count > 0) {
                Node current = OpenSet.Dequeue();
                if (current == end || endNodes != null && endNodes.Contains(current)) {
                    // We have reached our goal! or one that is equally as good but closer
                    // Let's convert this into an actual sequene of
                    // tiles to walk on, then end this constructor function!
                    return ReconstructPath(grid, cameFrom, current);
                }
                ClosedSet.Add(current);
                if (startNodes != null && startNodes.Contains(current)) {
                    cameFrom.Clear();
                }
                foreach (Node neighbour in grid.Neighbours(current, agent.CanMoveDiagonal)) {
                    //already checked this node
                    if (neighbour == null || ClosedSet.Contains(neighbour) == true)
                        continue; 
                    float movementCostToNeighbour = neighbour.MovementCost * DistanceNodes(agent, current, neighbour);
                    //if (playerCityRequired > int.MinValue) {
                    //    if (neighbor.data.City.PlayerNumber != playerCityRequired) {
                    //        movement_cost_to_neighbor = float.PositiveInfinity;
                    //    }
                    //}
                    //if (canEndInUnwakable) {
                    //    if (closestWalkableNeighbours.Contains(neighbor.data) || endTiles.Contains(neighbor.data)) {
                    //        movement_cost_to_neighbor = Dist_between(current, neighbor) + 0.1f;
                    //    }
                    //}
                    float tentative_g_score = g_score[current] + movementCostToNeighbour;

                    if (OpenSet.Contains(neighbour) && tentative_g_score >= g_score[neighbour])
                        continue;

                    cameFrom[neighbour] = current;
                    g_score[neighbour] = tentative_g_score;
                    f_score[neighbour] = g_score[neighbour] + HeuristicCostEstimate(agent, neighbour, end);

                    if (OpenSet.Contains(neighbour) == false) {
                        OpenSet.Enqueue(neighbour, f_score[neighbour]);
                    }
                    else {
                        OpenSet.UpdatePriority(neighbour, f_score[neighbour]);
                    }
                }

            }
            return null;
        }

        private static Queue<Vector2> ReconstructPath(PathGrid grid, Dictionary<Node, Node> cameFrom, Node current) {
            Stack<Node> totalPath = new Stack<Node>();
            totalPath.Push(current);
            while (cameFrom.ContainsKey(current)) {
                current = cameFrom[current];
                totalPath.Push(current);
            }
            Queue<Vector2> vectors = new Queue<Vector2>();
            while(totalPath.Count>0) {
                Node n = totalPath.Pop();
                vectors.Enqueue(new Vector2(grid.startX + n.x + 0.5f, grid.startY + n.y + 0.5f));
            }
            return vectors;
        }

        private static HashSet<Node> FindClosestWalkableNeighbours(IPathfindAgent agent, Node start, PathGrid grid) {
            HashSet<Node> tiles = new HashSet<Node>();
            foreach (Node x in grid.Neighbours(start, agent.CanMoveDiagonal)) {
                tiles.Add(x);
                if (tiles.Count == 0)
                    tiles.UnionWith(FindClosestWalkableNeighbours(agent, x, grid));
            }
            return tiles;
        }

        private static float HeuristicCostEstimate(IPathfindAgent agent, Node a, Node b) {
            switch (agent.Heuristic) {
                case PathHeuristics.Euclidean:
                    return Mathf.Sqrt(
                                Mathf.Pow(a.x - b.x, 2) +
                                Mathf.Pow(a.y - b.y, 2)
                            );

                case PathHeuristics.Manhattan:
                    return NORMAL_COST * (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y));

                case PathHeuristics.Diagonal:
                    float dx = Mathf.Abs(a.x - b.x);
                    float dy = Mathf.Abs(a.y - b.y);
                    return NORMAL_COST * (dx + dy) + (DIAGONAL_EXTRA_COST - 2 * NORMAL_COST) * Mathf.Min(dx, dy);
            }
            Debug.LogError("Path_Heuristics is not implemented!");
            return 0f;
        }

        private static float DistanceNodes(IPathfindAgent agent, Node a, Node b) {
            //When neighbours we can make shortcut to safe calculation time
            if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1) {
                return NORMAL_COST;
            }
            if (agent.CanMoveDiagonal && Mathf.Abs(a.x - b.x) == 1 && Mathf.Abs(a.y - b.y) == 1) {
                return DIAGONAL_EXTRA_COST;
            }
            // Not neighbours make the calculation for the distance
            return Mathf.Sqrt(
                Mathf.Pow(a.x - b.x, 2) +
                Mathf.Pow(a.y - b.y, 2)
            );
        }


    }

}
