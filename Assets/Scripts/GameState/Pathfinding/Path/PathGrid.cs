using Andja.Model;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Andja.Pathfinding {
    public enum PathGridType { Island, Ocean,/*not yet supported*/ Route}
    public class PathGrid {
        readonly PathGridType pathGridType;
        public Node[,] Values;
        public int Width;
        public int Height;
        public int startX;
        public int startY;

        public bool IsDirty;
        internal Node GetNodeFromWorldCoord(Vector2 pos) {
            return GetNode(pos - new Vector2(startX, startY));
        }
        Node GetNode(Vector2 pos) {
            if (IsInBounds(pos) == false)
                return null;
            return Values[Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y)];
        }
        internal Node GetNode(Tile t) {
            return GetNode(t.Vector2 - new Vector2(startX, startY));
        }
        //Could cache routes here with start/end -- could be really useful for route pathfinding
        public PathGrid(Island island) {
            pathGridType = PathGridType.Island;
            SetIslandValues(island);
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    SetNode(World.Current.GetTileAt(startX + x, startY + y));
                }
            }
        }
        public PathGrid(Route route) {
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
        private void SetIslandValues(Island island) {
            Width = island.Width;
            Height = island.Height;
            startX = (int)island.Minimum.x;
            startY = (int)island.Minimum.y;
            Values = new Node[Width, Height];
        }
        public PathGrid(PathGrid pathGrid) {
            this.Width = pathGrid.Width;
            this.Height = pathGrid.Height;
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Values[x, y] = pathGrid.Values[x,y].Clone();
                }
            }
        }

        public PathGrid Clone() {
            return new PathGrid(this);
        }

        protected void SetNode(Tile t) {
            if (pathGridType != PathGridType.Ocean && t.Type == TileType.Ocean)
                return;
            Node n = new Node(Mathf.FloorToInt(t.X - startX), Mathf.FloorToInt(t.Y - startY), 
                                t.MovementCost, t.City.PlayerNumber);
            Values[n.x,n.y] = n;
            IsDirty = true;
        }
        public void ChangeNode(Tile t, bool overrideWalkable = true) {
            Node n = GetNode(t);
            if (n == null) {
                SetNode(t);
            }
            if(overrideWalkable) {
                n.MovementCost = t.MovementCost;
                n.PlayerNumber = t.City.PlayerNumber;
            } else {
                Values[t.X, t.Y] = null;
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
            List<Node> neighbours = new List<Node>();
            neighbours.Add(GetNode(new Vector2(n.x + 1, n.y)));
            neighbours.Add(GetNode(new Vector2(n.x - 1, n.y)));
            neighbours.Add(GetNode(new Vector2(n.x, n.y + 1)));
            neighbours.Add(GetNode(new Vector2(n.x, n.y - 1)));
            if(diagonal) {
                neighbours.Add(GetNode(new Vector2(n.x + 1, n.y + 1)));
                neighbours.Add(GetNode(new Vector2(n.x + 1, n.y - 1)));
                neighbours.Add(GetNode(new Vector2(n.x - 1, n.y + 1)));
                neighbours.Add(GetNode(new Vector2(n.x - 1, n.y - 1)));
            }
            return neighbours;
        }
    }
}