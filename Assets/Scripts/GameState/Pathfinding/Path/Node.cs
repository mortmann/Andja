using System.Collections.Generic;
namespace Andja.Pathfinding {
    public class Node {
        public int x;
        public int y;
        public float MovementCost;
        public int PlayerNumber;
        /// <summary>
        /// List needs to be a seperate copy of the main threads.
        /// </summary>
        /// <param name="canEnterCities"></param>
        /// <returns></returns>
        public bool IsPassable(List<int> canEnterCities = null) {
            if(canEnterCities != null) {
                return canEnterCities.Contains(PlayerNumber) && MovementCost > 0;
            }
            return MovementCost > 0;
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

        internal Node Clone() {
            return new Node(this);
        }
    }
}

