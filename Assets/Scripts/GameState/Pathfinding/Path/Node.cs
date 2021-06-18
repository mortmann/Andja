using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Pathfinding {
    public class Node {
        public Vector2 Pos => new Vector2(x, y);
        public int x;
        public int y;
        public float MovementCost;
        public int PlayerNumber;

        public float f_Score; 
        public float g_Score;

        public bool isClosed;
        public Node parent;

        public bool overrideWalkable;
        /// <summary>
        /// List needs to be a seperate copy of the main threads.
        /// </summary>
        /// <param name="canEnterCities"></param>
        /// <returns></returns>
        public bool IsPassable(List<int> canEnterCities = null) {
            if (overrideWalkable)
                return overrideWalkable;
            if (canEnterCities != null) {
                return canEnterCities.Contains(PlayerNumber) && MovementCost > 0;
            }
            return MovementCost > 0;
        }
        public Node(Model.Tile t) : this(t.X,t.Y, t.MovementCost, t.City.PlayerNumber) {
        }
        public Node(int x, int y, float movementCost, int PlayerNumber) {
            this.x = x;
            this.y = y;
            MovementCost = movementCost;
            this.PlayerNumber = PlayerNumber;
        }

        public Node(Node node) {
            this.x = node.x;
            this.y = node.y;
            MovementCost = node.MovementCost;
            PlayerNumber = node.PlayerNumber;
        }

        internal void OverrideWalkable() {
            overrideWalkable = true;
        }

        internal Node Clone() {
            return new Node(this);
        }

        public void Reset() {
            this.f_Score = 0;
            this.g_Score = 0;
            this.isClosed = false;
            this.parent = null;
            overrideWalkable = false;
        }
    }
}

