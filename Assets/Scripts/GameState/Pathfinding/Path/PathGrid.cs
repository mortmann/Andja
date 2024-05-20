using Andja.Model;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;


namespace Andja.Pathfinding {
    public enum PathGridType { Island, Ocean,/*not yet supported*/ Route}
    public enum Walkable { Never, AlmostNever, Normal }
    public class PathGrid {
        ConcurrentBag<Tile> changedTiles = new ConcurrentBag<Tile>(); 
        public string ID;
        public bool Obsolete;
        public readonly PathGridType pathGridType;
        public Node[,] Values;
        public int Width;
        public int Height;
        public int startX;
        public int startY;
        public Action<Tile> Changed;
        public bool IsDirty;
        List<Node> temporaryNodes = new List<Node>();
        int[] playerOwnedNodes; //How many tiles are owned players

        internal Node GetNodeFromWorldCoord(Vector2 pos) {
            return GetNode(pos - new Vector2(startX, startY));
        }
        internal Node GetNode(Vector2 pos) {
            if (IsInBounds(pos) == false) {
                return null;
            }
            return Values[Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y)];
        }
        internal Node GetNode(Tile t) {
            return GetNode(t.Vector2 - new Vector2(startX, startY));
        }
        //Could cache routes here with start/end -- could be really useful for route Pathfinding
        public PathGrid(Island island) {
            playerOwnedNodes = new int[Controller.PlayerController.Instance.PlayerCount];
            ID = Guid.NewGuid().ToString();
            pathGridType = PathGridType.Island;
            SetIslandValues(island);
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    SetNode(World.Current.GetTileAt(startX + x, startY + y));
                }
            }
        }
        public PathGrid() {
#if !UNITY_INCLUDE_TESTS
            Debug.LogError("Do no use this outside Tests.");
#endif
        }
        public PathGrid(Route route) {
            playerOwnedNodes = new int[Controller.PlayerController.Instance.PlayerCount];
            ID = Guid.NewGuid().ToString();
            pathGridType = PathGridType.Route;
            SetIslandValues(route.Tiles[0].Island);
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Tile t= World.Current.GetTileAt(startX + x, startY + y);
                    if (route.Tiles.Contains(t)) {
                        SetNode(t);
                    }
                    else {
                        Values[x, y] = null;
                    }
                }
            }
        }

        public PathGrid(PathGrid pathGrid) {
            playerOwnedNodes = pathGrid.playerOwnedNodes;
            ID = pathGrid.ID;
            pathGrid.Changed += SourceChanged;
            this.Width = pathGrid.Width;
            this.Height = pathGrid.Height;
            startX = pathGrid.startX;
            startY = pathGrid.startY;
            Values = new Node[Width, Height];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Values[x, y] = pathGrid.Values[x,y]?.Clone();
                }
            }
        }
        internal void SetTemporaryWalkableNode(Vector2 pos) {
            Node n = GetNodeFromWorldCoord(pos);
            if (n == null) {
                int x = Mathf.FloorToInt(pos.x - startX);
                int y = Mathf.FloorToInt(pos.y - startY);
                Tile tile = World.Current.GetTileAt(x, y);
                n = new Node(x, y, tile.BaseMovementCost, tile.BaseMovementCost, -1);
                Values[n.x, n.y] = n;
                temporaryNodes.Add(n);
            }
            else {
                n.OverrideWalkable();
            }
        }

        private void SetIslandValues(Island island) {
            Width = island.Width;
            Height = island.Height;
            startX = (int)island.Minimum.x;
            startY = (int)island.Minimum.y;
            Values = new Node[Width, Height];
        }
        private void SourceChanged(Tile t) {
            changedTiles.Add(t);
            IsDirty = true;
        }

        public PathGrid Clone() {
            return new PathGrid(this);
        }

        protected Node SetNode(Tile t) {
            if (pathGridType != PathGridType.Ocean && t.Type == TileType.Ocean)
                return null;
            if (t.City == null) {
                Debug.LogError("Tile is not an island: " + t.ToString());
                return null;
            }
            Node n = new Node(Mathf.FloorToInt(t.X - startX), Mathf.FloorToInt(t.Y - startY), 
                                t.MovementCost, t.BaseMovementCost, t.City.PlayerNumber);
            if(t.City.PlayerNumber != GameData.WorldNumber) {
                playerOwnedNodes[t.City.PlayerNumber]++;
            }
            if (n.x < 0 || n.y < 0)
                return n;
            Values[n.x,n.y] = n;
            IsDirty = true;
            Changed?.Invoke(t);
            IsDirty = false;
            return n;
        }
        public void ChangeCityNode(Tile t) {
            Node n = GetNode(t);
            if(n == null) {
                Debug.LogError("Tile " + t + " should always have a node here.");
                return;
            }
            if (t.City.PlayerNumber != GameData.WorldNumber) {
                playerOwnedNodes[t.City.PlayerNumber]++;
            }
            if (n.PlayerNumber != GameData.WorldNumber) {
                playerOwnedNodes[n.PlayerNumber]--;
            }
            n.PlayerNumber = t.City.PlayerNumber;
            IsDirty = true;
            Changed?.Invoke(t);
            IsDirty = false;
        }
        public void ChangeNode(Tile t, Walkable type = Walkable.Normal) {
            Node n = GetNode(t);
            if (n == null) {
                n = SetNode(t);
            }
            switch (type) {
                case Walkable.Never:
                    Values[t.X - startX, t.Y - startY] = null;
                    break;
                case Walkable.AlmostNever:
                    n.MovementCost = float.MaxValue;
                    n.PlayerNumber = t.City.PlayerNumber;
                    break;
                case Walkable.Normal:
                    n.MovementCost = t.MovementCost;
                    n.PlayerNumber = t.City.PlayerNumber;
                    break;
            }
            IsDirty = true;
            Changed?.Invoke(t);
            IsDirty = false;
        }

        public bool PlayerHasOwned(int player) {
            return playerOwnedNodes[player] > 0;
        }

        /// <summary>
        /// RESET does not only reset the pathgrids variables.
        /// but ALSO updates tiles that changed in the original graph.
        /// </summary>
        public void Reset() {
            if(IsDirty) {
                foreach(Tile t in changedTiles) {
                    ChangeNode(t);
                    changedTiles.TryTake(out _);
                }
            }
            foreach(Node n in temporaryNodes) {
                Values[n.x, n.y] = null;
            }
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Values[x, y]?.Reset();
                }
            }
        }
        /// <summary>
        /// Checks whether the neighbouring Node is within the grid bounds or not
        /// </summary>
        public bool IsInBounds(Vector2 v) {
            if (v.x >= 0 && v.x < this.Width &&
                v.y >= 0 && v.y < this.Height)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Returns a List of neighbouring Nodes
        /// </summary>
        public List<Node> Neighbours(Node n, bool diagonal) {
            if (n == null)
                return new List<Node>();
            List<Node> neighbours = new List<Node>();
            neighbours.Add(GetNode(new Vector2(n.x, n.y + 1)));
            neighbours.Add(GetNode(new Vector2(n.x + 1, n.y)));
            neighbours.Add(GetNode(new Vector2(n.x, n.y - 1)));
            neighbours.Add(GetNode(new Vector2(n.x - 1, n.y)));
            if(diagonal) {
                neighbours.Add(GetNode(new Vector2(n.x + 1, n.y + 1)));
                neighbours.Add(GetNode(new Vector2(n.x + 1, n.y - 1)));
                neighbours.Add(GetNode(new Vector2(n.x - 1, n.y - 1)));
                neighbours.Add(GetNode(new Vector2(n.x - 1, n.y + 1)));
            }
            return neighbours;
        }

    }
}