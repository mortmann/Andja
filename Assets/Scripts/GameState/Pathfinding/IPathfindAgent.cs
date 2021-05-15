using System.Collections.Generic;

namespace Andja.Pathfinding {
    public interface IPathfindAgent {

        public float Speed {
            get;
        }
        public float RotationSpeed {
            get;
        }
        public TurningType TurnType {
            get;
        }
        public PathDestination PathDestination {
            get;
        }
        public PathingMode PathingMode {
            get;
        }
        public PathHeuristics Heuristic {
            get;
        }
        public bool CanEndInUnwakable {
            get;
        }
        public bool CanMoveDiagonal {
            get;
        }

        public IReadOnlyList<int> CanEnterCities {
            get;
        }
    }
}

