using Andja.Model;
using System.Collections.Generic;

namespace Andja.Utility {

    /// <summary>
    /// Super simple - no deletions. Only for finding closest string using the WildCard
    /// </summary>
    public class ClosestTree {
        private Node root;

        public ClosestTree(TileType type) {
            root = new Node(null, -1, null, type.ToString().ToLower()[0]);
        }

        public void Insert(string s, string value) {
            root.Insert(s, value);
        }

        public List<string> GetClosest(string Value) {
            if (Value == null)
                return null;
            return root.GetClosest(Value);
        }

        private class Node {
            public char ID;
            public char wildCard = 'a';
            private readonly int depth = -1;
            private List<string> Values;
            private Dictionary<char, Node> childs = new Dictionary<char, Node>();
            private Node parent;
            Node Root => parent != null ? parent.Root : this;

            public Node(string Value, int depth, Node parent, char id) : this(Value, depth, parent) {
                ID = id;
            }

            public Node(string Value, int depth, Node parent) {
                if (Value != null)
                    this.Values = new List<string>() { Value };
                this.depth = depth;
                this.parent = parent;
            }

            public void Insert(string ids, string value) {
                if (depth == ids.Length - 1) {
                    this.Values = new List<string>() { value };
                    return;
                }
                if (childs.ContainsKey(ids[depth + 1]) == false) {
                    childs[ids[depth + 1]] = new Node(null, depth + 1, this);
                }
                childs[ids[depth + 1]].Insert(ids, value);
            }

            public List<string> GetClosest(string ids) {
                if (depth == ids.Length - 1)
                    return Values;
                childs.TryGetValue(ids[depth + 1], out Node n);
                if (n == null) {
                    childs.TryGetValue(Root.wildCard, out n);
                }
                if (n == null) {
                    childs.TryGetValue(Root.ID, out n);
                }
                if (n == null)
                    return null;
                return n.GetClosest(ids);
            }
        }
    }
}