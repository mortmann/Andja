using System.Collections.Generic;

namespace Andja.Pathfinding {
    public interface IPathfindAgent {
        public bool IsAlive {
            get;
        }
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
        public PathDiagonal DiagonalType {
            get;
        }

        public IReadOnlyList<int> CanEnterCities {
            get;
        }

        void PathInvalidated();
    }
}

