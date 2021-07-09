using Andja.Model;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Andja.Utility;

namespace Andja.Pathfinding {
    public class WorldGraph {

        public HashSet<WorldNode> Nodes;
        public WorldNode[,] Tiles;
        public WorldGraph() {
            Calculate();
        }
        public WorldGraph(HashSet<WorldNode> Nodes, WorldNode[,] Tiles) {
            this.Nodes = Nodes;
            this.Tiles = Tiles;
        }
        public void Calculate() {
            World world = World.Current;
            Nodes = new HashSet<WorldNode>();
            Tiles = new WorldNode[world.Width, world.Height];
            foreach (Island i in world.Islands) {
                WorldNode[] newNodes = new WorldNode[4];
                newNodes[0] = new WorldNode(i.Minimum.x - 1, i.Minimum.y - 1); //Bottom left
                newNodes[1] = new WorldNode(i.Maximum.x + 1, i.Minimum.y - 1); //Bottom right
                newNodes[2] = new WorldNode(i.Minimum.x - 1, i.Maximum.y + 1); //Top left
                newNodes[3] = new WorldNode(i.Maximum.x + 1, i.Maximum.y + 1); //Top right
                Nodes.UnionWith(newNodes);
            }
            foreach (WorldNode n in Nodes) {
                Tiles[n.x, n.y] = n;
                n.CalculateEdges(Nodes);
            }
            foreach (Island i in world.Islands) {
                for (int x = (int)i.Minimum.x - 1; x <= i.Maximum.x + 1; x++) {
                    for (int y = (int)i.Minimum.y - 1; y <= i.Maximum.y + 1; y++) {
                        if (x == i.Minimum.x - 1 && (y == i.Minimum.y - 1 || y == i.Maximum.y + 1)) {
                            continue;
                        }
                        if (x == i.Maximum.x + 1 && (y == i.Minimum.y - 1 || y == i.Maximum.y + 1)) {
                            continue;
                        }
                        if (World.Current.GetTileAt(x, y).Type != TileType.Ocean) {
                            continue;
                        }
                        Tiles[x, y] = new WorldNode(x, y);
                    }
                }
            }
            for (int x = 0; x < world.Width; x++) {
                for (int y = 0; y < world.Height; y++) {
                    if (Tiles[x, y] == null) {
                        continue;
                    }
                    Tiles[x, y].DoNeighbourEdges(Tiles);
                }
            }
            for (int x = 0; x < world.Width; x++) {
                for (int y = 0; y < world.Height; y++) {
                    if (Tiles[x, y] == null) {
                        continue;
                    }
                    if (Nodes.Contains(Tiles[x, y])) {
                        continue;
                    }
                    //has all neighbours and those have all neighbours
                    if (Tiles[x, y].Edges.Count == 8 && Tiles[x, y].Edges.Exists(x=>x.Node.Edges.Count<8) == false) {
                        //then it is an unnecessary node and it should be removed
                        WorldNode n = Tiles[x, y];
                        //update neighbour nodes
                        n.UpdateNeigbhours();
                        //remove unnecessary node  
                        Tiles[x, y] = null;
                    }
                    else {
                        Nodes.Add(Tiles[x, y]);
                        continue;
                    }
                }
            }
            //WorldGraph worldGraph = Clone();
            //WorldNode next = worldGraph.Nodes.First();
            //TestDelete(worldGraph, next);
        }

        //void TestDelete(WorldGraph worldGraph, WorldNode next) {
        //    worldGraph.Nodes.Remove(next);
        //    foreach (WorldEdge edge in next.Edges) {
        //        if(worldGraph.Nodes.Contains(edge.Node))
        //            TestDelete(worldGraph, edge.Node);
        //    }
        //}

        internal WorldNode GetNodeFromWorldCoord(Vector2 startPos) {
            if (Tiles[Mathf.FloorToInt(startPos.x), Mathf.FloorToInt(startPos.y)] != null)
                return Tiles[Mathf.FloorToInt(startPos.x), Mathf.FloorToInt(startPos.y)];
            WorldNode wn = null;
            float distance = float.MaxValue;
            foreach(WorldNode next in Nodes) {
                float newDist = Vector2.Distance(startPos, next.Pos);
                if (newDist < distance) {
                    wn = next;
                    distance = newDist;
                }
            }
            return wn;
        }
        public WorldGraph Clone() {
            HashSet<WorldNode> newNodes = new HashSet<WorldNode>();
            WorldNode[,] NewTiles = new WorldNode[World.Current.Width, World.Current.Height];
            for (int x = 0; x < World.Current.Width; x++) {
                for (int y = 0; y < World.Current.Height; y++) {
                    if (Tiles[x, y] == null) {
                        continue;
                    }
                    NewTiles[x, y] = Tiles[x, y].Clone();
                    newNodes.Add(NewTiles[x, y]);
                }
            }
            foreach (var item in newNodes) {
                item.UpdateEdge(NewTiles);
            }
            return new WorldGraph(newNodes,NewTiles);
        }
        internal void Reset() {
            foreach(WorldNode wn in Nodes)
                wn.Reset();
            }
        }
    public class WorldEdge {
        public float MovementCost;
        public WorldNode Node;

        public WorldEdge(float moveCost, WorldNode node) {
            this.MovementCost = moveCost;
            Node = node;
        }
        internal WorldEdge Clone() {
            return new WorldEdge(MovementCost, Node);
        }
    }
    public class WorldNode {
        public Vector2 Pos => new Vector2(x, y);
        public int x;
        public int y;
        public List<WorldEdge> Edges;
        WorldEdge[,] neighbours;

        public float f_Score;
        public float g_Score  = float.MaxValue;
        public bool isClosed;
        public WorldNode parent;

        public WorldEdge Left => neighbours[0, 1];
        public WorldEdge Right => neighbours[2, 1];
        public WorldEdge Top => neighbours[1, 2];
        public WorldEdge Bottom => neighbours[1, 0];
        public WorldEdge BottomLeft => neighbours[0, 0];
        public WorldEdge TopLeft => neighbours[0, 2];
        public WorldEdge BottomRight => neighbours[2, 0];
        public WorldEdge TopRight => neighbours[2, 2];


        public WorldNode(float x, float y) {
            this.x = (int)x;
            this.y = (int)y;
        }
        internal void CalculateEdges(IEnumerable<WorldNode> nodes) {
            List<WorldEdge> edges = new List<WorldEdge>();
            foreach (WorldNode n in nodes) {
                if (Util.CheckLine(World.Current.Tilesmap, x, y, n.x, n.y)) {
                    edges.Add(
                        new WorldEdge(Vector2.Distance(new Vector2(x, y), new Vector2(n.x, n.y)) * 0.9f, n)
                    );
                }
            }
            Edges = edges;
        }

        internal void DoNeighbourEdges(WorldNode[,] tiles) {
            if(Edges == null) {
                Edges = new List<WorldEdge>();
            }
            neighbours = new WorldEdge[3, 3];
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    if (x == 0 && y == 0)
                        continue;
                    if (tiles[this.x + x, this.y + y] == null)
                        continue;
                    if(Mathf.Abs(x) + Mathf.Abs(y) == 2) {
                        if (tiles[this.x + x, this.y] == null) {
                            continue;
                        }
                        if (tiles[this.x, this.y + y] == null) {
                            continue;
                        }
                    }
                    neighbours[x+1, y+1] = new WorldEdge(
                                                    Vector2.Distance(new Vector2(this.x + x, this.y + y), new Vector2(this.x, this.y)),
                                                    tiles[this.x + x, this.y + y]
                                                    );
                    Edges.Add(
                        neighbours[x + 1, y + 1]
                    );
                }
            }
        }

        public void Reset() {
            this.f_Score = 0;
            this.g_Score = float.MaxValue;
            this.isClosed = false;
            this.parent = null;
        }

        internal void UpdateNeigbhours() {
            Left.Node.Right.MovementCost += Right.MovementCost;
            Left.Node.Right.Node = Right.Node;
            Right.Node.Left.MovementCost += Left.MovementCost;
            Right.Node.Left.Node = Left.Node;

            Top.Node.Bottom.MovementCost += Bottom.MovementCost;
            Top.Node.Bottom.Node = Bottom.Node;
            Bottom.Node.Top.MovementCost += Top.MovementCost;
            Bottom.Node.Top.Node = Top.Node;

            TopLeft.Node.BottomRight.MovementCost += BottomRight.MovementCost;
            TopLeft.Node.BottomRight.Node = BottomRight.Node;
            BottomRight.Node.TopLeft.MovementCost += TopLeft.MovementCost;
            BottomRight.Node.TopLeft.Node = TopLeft.Node;

            TopRight.Node.BottomLeft.MovementCost += BottomLeft.MovementCost;
            TopRight.Node.BottomLeft.Node = BottomLeft.Node;
            BottomLeft.Node.TopRight.MovementCost += TopRight.MovementCost;
            BottomLeft.Node.TopRight.Node = TopRight.Node;
        }

        internal WorldNode Clone() {
            return new WorldNode(x, y) { Edges = Edges.Select(x=>x.Clone()).ToList() };
        }

        internal void UpdateEdge(WorldNode[,] newTiles) {
            foreach(WorldEdge we in Edges) {
                we.Node = newTiles[we.Node.x, we.Node.y];
            }
        }
    }
}