using Priority_Queue;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using Andja.Utility;
using EpPathFinding.cs;
using Andja.Model;

namespace Andja.Pathfinding {
    public static class Pathfinder {
        public const float DIAGONAL_EXTRA_COST = 1.41421356237f;
        public const float NORMAL_COST = 1;
        static bool[][] WorldTileMap;
        private static StaticGrid worldGrid;
        private static bool[][] worldTilemap;

        public static Queue<Vector2> Find(IPathfindAgent agent, PathGrid grid, 
                                            Vector2? startPos, Vector2? endPos, 
                                            List<Vector2> startsPos = null, List<Vector2> endsPos = null) {
            Node start = null;
            Node end = null;
            if(startsPos != null && endsPos != null) {
                Vector2[] points = Utility.Util.FindClosestPoints(startsPos, endsPos);
                start = grid.GetNodeFromWorldCoord(points[0]);
                end = grid.GetNodeFromWorldCoord(points[1]);
            } else {
                if (startPos != null) {
                    start = grid.GetNodeFromWorldCoord(startPos.Value);
                }
                if (endPos != null) {
                    end = grid.GetNodeFromWorldCoord(endPos.Value);
                } else {
                    end = grid.GetNodeFromWorldCoord(endsPos.OrderBy(x => Vector2.Distance(x, startPos.Value)).First());
                }
            }
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
            f_score[start] = DistanceNodes(false, start.Pos, end.Pos);
            OpenSet.Enqueue(start, 0);
            if (agent.CanEndInUnwakable == false && end.IsPassable(agent.CanEnterCities?.ToList()) == false) {
                //cant end were it is supposed to go -- find a alternative that can be walked on
                endNodes = FindClosestWalkableNeighbours(agent, end, grid);
            }
            while (OpenSet.Count > 0) {
                Node current = OpenSet.Dequeue();
                if (current == end || endNodes != null && endNodes.Contains(current)) {
                    return ReconstructPath(grid, cameFrom, current); //we are at any destination node make the path
                }
                ClosedSet.Add(current);
                if (startNodes != null && startNodes.Contains(current)) {
                    cameFrom.Clear();
                }
                List<Node> neis = grid.Neighbours(current, agent.CanMoveDiagonal);
                for (int i = 0; i < neis.Count; i++) {
                    Node neighbour = neis[i];
                    if (neighbour == null || ClosedSet.Contains(neighbour) == true) {
                        continue;
                    }
                    float costToNeighbour = neighbour.MovementCost * DistanceNodes(agent.CanMoveDiagonal, current.Pos, neighbour.Pos);
                    if (agent.CanEnterCities != null) {
                        if (agent.CanEnterCities.Contains(neighbour.PlayerNumber) == false) {
                            costToNeighbour = float.MaxValue;
                        }
                    }
                    if (agent.CanEndInUnwakable) {
                        if (endNodes != null && endNodes.Contains(neighbour)) {
                            costToNeighbour = DistanceNodes(agent.CanMoveDiagonal, current.Pos, neighbour.Pos);
                        }
                    }
                    float tentative_g_score = g_score[current] + costToNeighbour;

                    if (OpenSet.Contains(neighbour) && tentative_g_score >= g_score[neighbour])
                        continue;
                    cameFrom[neighbour] = current;
                    g_score[neighbour] = tentative_g_score;
                    f_score[neighbour] = tentative_g_score + HeuristicCostEstimate(agent, neighbour.Pos, end.Pos);
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
        public static Queue<Vector2> FindOceanPath(IPathfindAgent agent, Vector2 startPos, Vector2 endPos) {
            if (World.Current != null) {
                worldGrid = World.Current.TilesGrid;
                worldTilemap = World.Current.Tilesmap;
            }
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            Queue<Vector2> tempQueue = new Queue<Vector2>();
            if (Utility.Util.CheckLine(worldTilemap, startPos, endPos)) {
                tempQueue.Enqueue(endPos);
                return tempQueue;
            }
            //Debug.Log("Starting took " + stopwatch.ElapsedMilliseconds + "(" + stopwatch.Elapsed.TotalSeconds + "s)");
            Queue<Vector2> worldPoints = FindWorldPath(agent, startPos, endPos);
            //Debug.Log("FindWorldPath took " + stopwatch.ElapsedMilliseconds + "(" + stopwatch.Elapsed.TotalSeconds + "s)");
            if (Utility.Util.CheckLine(worldTilemap, startPos, worldPoints.Peek()) == false) {
                JumpPointParam jpParam = new JumpPointParam(worldGrid, 
                    new GridPos(Mathf.FloorToInt(startPos.x), Mathf.FloorToInt(startPos.y)), 
                    new GridPos(Mathf.FloorToInt(worldPoints.Peek().x), Mathf.FloorToInt(worldPoints.Peek().y)), 
                    true, DiagonalMovement.OnlyWhenNoObstacles);
                List<GridPos> pos = JumpPointFinder.FindPath(jpParam);
                foreach(GridPos gp in pos) {
                    tempQueue.Enqueue(new Vector2(gp.x, gp.y));
                }
            }
            //Debug.Log("Start Pathfinder took " + stopwatch.ElapsedMilliseconds + "(" + stopwatch.Elapsed.TotalSeconds + "s)");
            foreach (Vector2 v in worldPoints) {
                tempQueue.Enqueue(v);
            }
            Vector2 lastWorld = worldPoints.Last();
            if (worldTilemap[Mathf.FloorToInt(lastWorld.x)][Mathf.FloorToInt(lastWorld.y)] == false
                || Utility.Util.CheckLine(worldTilemap, endPos, lastWorld) == false) {
                JumpPointParam jpParam = new JumpPointParam(worldGrid,
                    new GridPos(Mathf.FloorToInt(lastWorld.x), Mathf.FloorToInt(lastWorld.y)),
                    new GridPos(Mathf.FloorToInt(endPos.x), Mathf.FloorToInt(endPos.y)),
                    true, DiagonalMovement.OnlyWhenNoObstacles);
                List<GridPos> pos = JumpPointFinder.FindPath(jpParam);
                foreach (GridPos gp in pos) {
                    tempQueue.Enqueue(new Vector2(gp.x, gp.y));
                }
            } 
            tempQueue.Enqueue(endPos);

            Queue<Vector2> finalQueue = new Queue<Vector2>();
            finalQueue.Enqueue(tempQueue.Dequeue());
            while (tempQueue.Count>0) {
                Vector2 current = tempQueue.Dequeue();
                Vector2 next = tempQueue.Count == 0 ? endPos : tempQueue.Peek();
                if (Utility.Util.CheckLine(worldTilemap, finalQueue.Last(), next) == false) {
                    finalQueue.Enqueue(current);
                }
            }
            finalQueue.Enqueue(endPos);
            stopwatch.Stop();
            //Debug.Log("Total Ocean Pathfinder took " + stopwatch.ElapsedMilliseconds + "(" + stopwatch.Elapsed.TotalSeconds + "s)");
            return finalQueue;
        }
        private static Queue<Vector2> FindWorldPath(IPathfindAgent agent, Vector2 startPos, Vector2 endPos) {
            WorldNode start = WorldGraph.GetNodeFromWorldCoord(startPos);
            WorldNode end = WorldGraph.GetNodeFromWorldCoord(endPos);
            Dictionary<WorldNode, float> g_score = new Dictionary<WorldNode, float>();
            Dictionary<WorldNode, float> f_score = new Dictionary<WorldNode, float>();
            Dictionary<WorldNode, WorldNode> cameFrom = new Dictionary<WorldNode, WorldNode>();
            HashSet<WorldNode> ClosedSet = new HashSet<WorldNode>();
            SimplePriorityQueue<WorldNode> OpenSet = new SimplePriorityQueue<WorldNode>();

            foreach (WorldNode n in WorldGraph.Nodes) {
                if (n == null)
                    continue;
                g_score.Add(n, Mathf.Infinity);
                f_score.Add(n, Mathf.Infinity);
            }
            g_score[start] = 0;
            f_score[start] = DistanceNodes(false, start.Pos, end.Pos);
            OpenSet.Enqueue(start, 0);
            while (OpenSet.Count > 0) {
                WorldNode current = OpenSet.Dequeue();
                if (current == end) {
                    return ReconstructWorldPath(cameFrom, current); //we are at any destination node make the path
                }
                ClosedSet.Add(current);
                WorldEdge[] neis = current.Edges;
                for (int i = 0; i < neis.Length; i++) {
                    WorldEdge edge = neis[i];
                    WorldNode neighbour = edge.Node;
                    if (neighbour == null || ClosedSet.Contains(neighbour) == true) {
                        continue;
                    }
                    float movementCostToNeighbour = edge.MovementCost * DistanceNodes(agent.CanMoveDiagonal, current.Pos, neighbour.Pos);

                    float tentative_g_score = g_score[current] + movementCostToNeighbour;

                    if (OpenSet.Contains(neighbour) && tentative_g_score >= g_score[neighbour])
                        continue;

                    cameFrom[neighbour] = current;
                    g_score[neighbour] = tentative_g_score;
                    f_score[neighbour] = g_score[neighbour] + HeuristicCostEstimate(agent, neighbour.Pos, end.Pos);
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
        /// <summary>
        /// Does not return it in the "correct" order because it will be enqueued it the correct order later.
        /// </summary>
        /// <param name="cameFrom"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        private static Queue<Vector2> ReconstructWorldPath(Dictionary<WorldNode, WorldNode> cameFrom, WorldNode current) {
            Stack<WorldNode> totalPath = new Stack<WorldNode>();
            totalPath.Push(current);
            while (cameFrom.ContainsKey(current)) {
                current = cameFrom[current];
                totalPath.Push(current);
            }
            Queue<Vector2> vectors = new Queue<Vector2>();
            while (totalPath.Count > 0) {
                WorldNode n = totalPath.Pop();
                vectors.Enqueue(new Vector2(n.x, n.y));
            }
            return vectors;
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

        public static float HeuristicCostEstimate(IPathfindAgent agent, Vector2 a, Vector2 b) {
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

        private static float DistanceNodes(bool diagonal, Vector2 a, Vector2 b) {
            //When neighbours we can make shortcut to safe calculation time
            if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1) {
                return NORMAL_COST;
            }
            if (diagonal && Mathf.Abs(a.x - b.x) == 1 && Mathf.Abs(a.y - b.y) == 1) {
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
