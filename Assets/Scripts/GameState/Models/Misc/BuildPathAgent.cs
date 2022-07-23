using Andja.Pathfinding;
using System.Collections.Generic;

namespace Andja.Model {
    public class BuildPathAgent : IPathfindAgent {

        List<int> canEnterCities;
        public BuildPathAgent(int playerNumber) {
            canEnterCities = new List<int> { playerNumber };
        }
        public bool IsAlive => true;
        public float Speed => 0;
        public float RotationSpeed => 0;
        public TurningType TurnType => TurningType.OnPoint;
        public PathDestination PathDestination => PathDestination.Tile;
        public PathingMode PathingMode => PathingMode.IslandSinglePoint;
        public PathHeuristics Heuristic => PathHeuristics.Manhattan;
        public bool CanEndInUnwalkable => false;
        public PathDiagonal DiagonalType => PathDiagonal.None;
        public IReadOnlyList<int> CanEnterCities => canEnterCities;

        public void PathInvalidated() {
            
        }
    }
}

