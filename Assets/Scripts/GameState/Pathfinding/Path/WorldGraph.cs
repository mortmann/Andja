using Andja.Model;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Andja.Utility;

namespace Andja.Pathfinding {
    public static class WorldGraph {

        public static List<WorldNode> Nodes;

        public static void Calculate() {
            World world = World.Current;
            Nodes = new List<WorldNode>();
            foreach (Island i in world.Islands) {
                WorldNode[] newNodes = new WorldNode[4];
                newNodes[0] = new WorldNode(i.Minimum.x - 1, i.Minimum.y - 1); //Bottom left
                newNodes[1] = new WorldNode(i.Maximum.x + 1, i.Minimum.y - 1); //Bottom right
                newNodes[2] = new WorldNode(i.Minimum.x - 1, i.Maximum.y + 1); //Top left
                newNodes[3] = new WorldNode(i.Maximum.x + 1, i.Maximum.y + 1); //Top right
                Nodes.AddRange(newNodes);
            }
            foreach(WorldNode n in Nodes) {
                n.CalculateEdges(Nodes);
            }
        }

        internal static WorldNode GetNodeFromWorldCoord(Vector2 startPos) {
            return Nodes.OrderBy(x => Vector2.Distance(startPos, x.Pos)).First();
        }
    }

    public class WorldEdge {
        public float MovementCost;
        public WorldNode Node;

        public WorldEdge(float moveCost, WorldNode node) {
            this.MovementCost = moveCost;
            Node = node;
        }
    }

    public class WorldNode {
        public Vector2 Pos => new Vector2(x, y);
        public int x;
        public int y;
        public WorldEdge[] Edges;

        public WorldNode(float x, float y) {
            this.x = (int)x;
            this.y = (int)y;
        }

        internal void CalculateEdges(List<WorldNode> nodes) {
            List<WorldEdge> edges = new List<WorldEdge>();
            foreach (WorldNode n in nodes) {
                if (Util.CheckLine(World.Current.Tilesmap, x, y, n.x, n.y)) {
                    edges.Add(new WorldEdge(
                        Vector2.Distance(new Vector2(x, y), new Vector2(n.x, n.y))
                        , n
                        )
                    );
                }
            }
            Edges = edges.ToArray();
        }

    }
}
