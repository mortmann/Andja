using Priority_Queue;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Andja.Model;
using System;

namespace Andja.Pathfinding {
    public static class Pathfinder {
        public const float DIAGONAL_EXTRA_COST = 1.41421356237f;
        public const float NORMAL_COST = 1;
        private static bool[][] worldTilemap;

        public static Queue<Vector2> Find(PathJob job, PathGrid grid, 
                                            Vector2? startPos, Vector2? endPos, 
                                            List<Vector2> startsPos = null, List<Vector2> endsPos = null) {
            if(grid == null) {
                job.SetStatus(JobStatus.NoPath);
                return null;
            }
            IPathfindAgent agent = job.agent;
            Node start = null;
            Node end = null;
            if (agent.CanEndInUnwalkable) {
                if(endsPos != null) {
                    foreach (Vector2 s in endsPos) {
                        grid.SetTemporaryWalkableNode(s);
                    }
                } else {
                    if (endPos != null) {
                        grid.SetTemporaryWalkableNode(endPos.Value);
                    }
                }
            }
            if (startsPos != null && endsPos != null) {
                foreach (Vector2 s in startsPos) {
                    grid.SetTemporaryWalkableNode(s);
                }
                Vector2[] points = Utility.Util.FindClosestPoints(startsPos, endsPos);
                start = grid.GetNodeFromWorldCoord(points[0]);
                end = grid.GetNodeFromWorldCoord(points[1]);
            } else {
                if (startPos != null) {
                    grid.SetTemporaryWalkableNode(startPos.Value);
                    start = grid.GetNodeFromWorldCoord(startPos.Value);
                }
                if (endPos != null) {
                    end = grid.GetNodeFromWorldCoord(endPos.Value);
                }
                else {
                    end = grid.GetNodeFromWorldCoord(endsPos.OrderBy(x => Vector2.Distance(x, startPos.Value)).First());
                }
            }
            if (start == null || end == null) {
                Debug.LogError(startPos + " or " + endPos + " not in grid " + grid.pathGridType);
            }

            HashSet<Node> endNodes = null;
            if(endsPos != null) {
                endNodes = new HashSet<Node>();
                foreach(Vector2 e in endsPos) {
                    endNodes.Add(grid.GetNodeFromWorldCoord(e));
                }
            }
            HashSet<Node> startNodes = null;
            if (startsPos != null) {
                startNodes = new HashSet<Node>();
                foreach (Vector2 e in startsPos) {
                    startNodes.Add(grid.GetNodeFromWorldCoord(e));
                }
            }
            SimplePriorityQueue<Node> OpenSet = new SimplePriorityQueue<Node>();

            start.g_Score = 0;
            start.f_Score = DistanceNodes(false, start.Pos, end.Pos);
            OpenSet.Enqueue(start, 0);
            if (agent.CanEndInUnwalkable == false && end.IsPassable(agent.CanEnterCities?.ToList()) == false) {
                //cant end were it is supposed to go -- find a alternative that can be walked on
                if(endNodes != null) {
                    HashSet<Node> temp = new HashSet<Node>();
                    foreach (var item in endNodes) {
                        temp.UnionWith(FindClosestWalkableNeighbours(agent, item, grid));
                    }
                    endNodes = temp;
                } else {
                    endNodes = FindClosestWalkableNeighbours(agent, end, grid);
                }
            }
            while (OpenSet.Count > 0) {
                if (job.IsCanceled || PathfindingThreadHandler.FindPaths == false)
                    return null;
                Node current = OpenSet.Dequeue();
                //if (agent is BuildPathAgent) {
                //    Controller.TileSpriteController.positions.Add(current.Pos + new Vector2(grid.startX,grid.startY));
                //}
                if (current == end || endNodes != null && endNodes.Contains(current)) {
                    return ReconstructPath(grid, current); //we are at any destination node make the path
                }
                if (job.IsCanceled)
                    return null;
                current.isClosed = true;
                if (startNodes != null && startNodes.Contains(current)) {
                    current.parent = null;
                }
                for (int x = -1; x < 2; x++) {
                    for (int y = -1; y < 2; y++) {
                        if(x == 1 && y == 1) {
                            continue; // current tile
                        }
                        if (job.agent.DiagonalType == PathDiagonal.None) {
                            if (Mathf.Abs(x) + Mathf.Abs(y) == 2) {
                                continue; //skip diagonal here
                            }
                        } else
                        if(job.agent.DiagonalType == PathDiagonal.OnlyNoObstacle) {
                            if (Mathf.Abs(x) + Mathf.Abs(y) == 2) {
                                //check for corner unwalkable
                                if (grid.GetNode(current.Pos + new Vector2(x, 0))?.IsPassable() == false) {
                                    continue;
                                }
                                if (grid.GetNode(current.Pos + new Vector2(0, y))?.IsPassable() == false) {
                                    continue;
                                }
                            }
                        }
                        Node neighbour = grid.GetNode(current.Pos + new Vector2(x,y));
                        if (neighbour == null || neighbour.isClosed) {
                            continue;
                        }
                        float costToNeighbour = neighbour.MovementCost * 
                            DistanceNodes(agent.DiagonalType != PathDiagonal.None, current.Pos, neighbour.Pos);
                        if (agent.CanEnterCities != null) {
                            if (agent.CanEnterCities.Contains(neighbour.PlayerNumber) == false) {
                                costToNeighbour = float.MaxValue;
                            }
                        }
                        if (agent.CanEndInUnwalkable) {
                            if (endNodes != null && endNodes.Contains(neighbour)) {
                                costToNeighbour = DistanceNodes(agent.DiagonalType != PathDiagonal.None, current.Pos, neighbour.Pos);
                            }
                        }
                        float tentative_g_score = current.g_Score + costToNeighbour;
                        if (tentative_g_score >= neighbour.g_Score)
                            continue;
                        neighbour.parent = current;
                        neighbour.g_Score = tentative_g_score;
                        float penalty = 0;
                        //Prefer long straight lines instead of moving diagonal 
                        //biased in form of a penalty added as tie breaker 
                        if (agent.Heuristic == PathHeuristics.Manhattan) {
                            Vector2 d1 = current.Pos - end.Pos;
                            Vector2 d2 = start.Pos - end.Pos;
                            float cross = Mathf.Abs(d1.x * d2.y - d2.x * d1.y);
                            penalty = 1 - cross * 0.0000001f;
                        }
                        if (agent.Heuristic == PathHeuristics.Manhattan) {
                            Vector2 d1 = (current.parent ?? start).Pos;
                            Vector2 d2 = neighbour.Pos;
                            float dist = Mathf.Abs((d1.x - d2.x) * (d1.y - d2.y));
                            penalty += dist * 0.00001f;
                        }
                        neighbour.f_Score = tentative_g_score + penalty + HeuristicCostEstimate(agent, neighbour.Pos, end.Pos);

                        if (OpenSet.Contains(neighbour) == false) {
                            if (agent is BuildPathAgent) {
                                Controller.TileSpriteController.positionsCost[neighbour.Pos + new Vector2(grid.startX, grid.startY)] = neighbour.f_Score;
                            }
                            OpenSet.Enqueue(neighbour, neighbour.f_Score);
                        }
                        else {
                            if (agent is BuildPathAgent) {
                                Controller.TileSpriteController.positionsCost[neighbour.Pos + new Vector2(grid.startX, grid.startY)] = neighbour.f_Score;
                            }
                            OpenSet.UpdatePriority(neighbour, neighbour.f_Score);
                        }

                    }
                }
            }
            return null;
        }

        public static Queue<Vector2> FindOceanPath(PathJob job, IPathfindAgent agent, WorldGraph graph, Vector2 startPos, Vector2 endPos) {
            if (World.Current != null) {
                worldTilemap = World.Current.TilesMap;
            }
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            if (Utility.Util.CheckLine(worldTilemap, startPos, endPos)) {
                return new Queue<Vector2>(new []{ endPos });
            }
            Queue<Vector2> worldPoints = FindWorldPath(job, agent, graph, startPos, endPos);
            
            if (worldPoints == null)
                return new Queue<Vector2>();
            worldPoints = new Queue<Vector2>(worldPoints.Reverse());


            Queue<Vector2> shortestQueue = CalculatePathWithDirectLineOfSight(job, worldPoints.ToArray(), startPos, endPos);

            shortestQueue = new Queue<Vector2>(shortestQueue.Reverse());
            Queue<Vector2> finalQueue = new Queue<Vector2>();
            while (shortestQueue.Count > 0) {
                finalQueue.Enqueue(shortestQueue.Dequeue() + new Vector2(0.5f, 0.5f));
            }
            if (worldTilemap[Mathf.FloorToInt(endPos.x)][Mathf.FloorToInt(endPos.y)]) {
                finalQueue.Enqueue(endPos);
            }
            if (agent is Ship s && s.IsOwnedByCurrentPlayer()) {
                finalQueue.AsParallel().ForAll(a => Controller.TileSpriteController.positions.Add(a));
            }
            stopwatch.Stop();
            //Debug.Log("Total Ocean Pathfinder took " + stopwatch.ElapsedMilliseconds + "(" + stopwatch.Elapsed.TotalSeconds + "s)");
            return finalQueue;
        }

        private static Queue<Vector2> CalculatePathWithDirectLineOfSight(PathJob job, Vector2[] worldPathArray, Vector2 startPos, Vector2 endPos) {
            //start at the end go to start
            Vector2 current = endPos;
            Queue<Vector2> tempQueue = new Queue<Vector2>();
            tempQueue.Enqueue(current);
            for (int i = 0; i < worldPathArray.Length; i++) {
                if (job.IsCanceled || PathfindingThreadHandler.FindPaths == false)
                    return null;
                Vector2 next = startPos;
                if (i < worldPathArray.Length - 1) {
                    next = worldPathArray[i + 1];
                }
                if (Utility.Util.CheckLine(worldTilemap, current, next) == false) {
                    tempQueue.Enqueue(worldPathArray[i]);
                    current = worldPathArray[i];
                }
                //if (agent is Ship s && s.IsOwnedByCurrentPlayer()) {
                //    Controller.TileSpriteController.positions.Add(current);
                //}
            }
            //remove the added endposition so it does not get 0.5 added later
            tempQueue.Dequeue();
            return tempQueue;
        }

        private static Queue<Vector2> FindWorldPath(PathJob job, IPathfindAgent agent, WorldGraph graph, 
                                                        Vector2 startPos, Vector2 endPos) {
            WorldNode start = graph.GetNodeFromWorldCoord(startPos);
            WorldNode end = graph.GetNodeFromWorldCoord(endPos);
            SimplePriorityQueue<WorldNode> OpenSet = new SimplePriorityQueue<WorldNode>();
            start.g_Score = 0;
            start.f_Score = DistanceNodes(false, start.Pos, end.Pos);
            OpenSet.Enqueue(start, 0);
            while (OpenSet.Count > 0) {
                if (job.IsCanceled || PathfindingThreadHandler.FindPaths == false)
                    return null;
                WorldNode current = OpenSet.Dequeue();
                if (current == end) {
                    return ReconstructWorldPath(current); //we are at any destination node make the path
                }
                current.isClosed = true;
                List<WorldEdge> neis = current.Edges;
                for (int i = 0; i < neis.Count; i++) {
                    WorldEdge edge = neis[i];
                    WorldNode neighbour = edge.Node;
                    if (neighbour == null || neighbour.isClosed == true) {
                        continue;
                    }
                    
                    float movementCostToNeighbour = 1 
                        * DistanceNodes(agent.DiagonalType != PathDiagonal.None, current.Pos, neighbour.Pos);
                    //if (agent is Ship s && s.IsOwnedByCurrentPlayer()) {
                    //    Controller.TileSpriteController.positions.Add(neighbour.Pos);
                    //}
                    float tentative_g_score = current.g_Score + movementCostToNeighbour;

                    if (tentative_g_score >= neighbour.g_Score)
                        continue;

                    neighbour.parent = current;
                    neighbour.g_Score = tentative_g_score;
                    neighbour.f_Score = neighbour.g_Score + HeuristicCostEstimate(agent, neighbour.Pos, end.Pos);
                    if (OpenSet.Contains(neighbour) == false) {
                        OpenSet.Enqueue(neighbour, neighbour.f_Score);
                    }
                    else {
                        OpenSet.UpdatePriority(neighbour, neighbour.f_Score);
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
        private static Queue<Vector2> ReconstructWorldPath(WorldNode current) {
            Stack<WorldNode> totalPath = new Stack<WorldNode>();
            totalPath.Push(current);
            while (current.parent != null) {
                current = current.parent;
                totalPath.Push(current);
            }
            Queue<Vector2> vectors = new Queue<Vector2>();
            while (totalPath.Count > 0) {
                WorldNode n = totalPath.Pop();
                vectors.Enqueue(new Vector2(n.x, n.y));
            }
            return vectors;
        }

        private static Queue<Vector2> ReconstructPath(PathGrid grid, Node current) {
            Stack<Node> totalPath = new Stack<Node>();
            totalPath.Push(current);
            while (current.parent != null) {
                current = current.parent;
                totalPath.Push(current);
            }
            Queue<Vector2> vectors = new Queue<Vector2>();
            while(totalPath.Count > 0) {
                Node n = totalPath.Pop();
                vectors.Enqueue(new Vector2(grid.startX + n.x + 0.5f, grid.startY + n.y + 0.5f));
            }
            return vectors;
        }

        private static HashSet<Node> FindClosestWalkableNeighbours(IPathfindAgent agent, Node start, PathGrid grid) {
            HashSet<Node> tiles = new HashSet<Node>();
            if (agent.CanEnterCities != null && agent.CanEnterCities.Any(x=>grid.PlayerHasOwned(x)) == false) {
                return tiles;
            }
            HashSet<Node> alreadyChecked = new HashSet<Node>();
            Queue<Node> nodesToCheck = new Queue<Node>();
            nodesToCheck.Enqueue(start);
            while (nodesToCheck.Count > 0) {
                Node node = nodesToCheck.Dequeue();
                alreadyChecked.Add(node);
                if (node.IsPassable(agent.CanEnterCities?.ToList()) == false) {
                    //Only enqueue new nodes if we dont already have atleast one
                    //if we have one just check until all queued are done
                    if(tiles.Count == 0) {
                        foreach (Node item in grid.Neighbours(node, false)) {
                            if (item != null && alreadyChecked.Contains(item) == false)
                                nodesToCheck.Enqueue(item);
                        }
                    }
                    continue;
                }
                tiles.Add(node);
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
