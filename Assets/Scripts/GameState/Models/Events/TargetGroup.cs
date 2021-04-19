using System.Collections.Generic;

namespace Andja.Model {

    public enum Target {
        World, Player, Island, City,
        AllUnit, Ship, LandUnit,
        AllStructure, DamagableStructure,
        RoadStructure, NeedStructure, MilitaryStructure, HomeStructure, ServiceStructure,
        GrowableStructure, OutputStructure, MarketStructure, WarehouseStructure, MineStructure,
        FarmStructure, ProductionStructure
    }

    public class TargetGroup {
        public HashSet<Target> Targets;

        public TargetGroup(params Target[] targets) {
            Targets = new HashSet<Target>();
            Targets.UnionWith(targets);
        }

        public TargetGroup(ICollection<Target> targets) {
            Targets = new HashSet<Target>();
            Targets.UnionWith(targets);
        }

        internal void AddTargets(TargetGroup target) {
            Targets.UnionWith(target.Targets);
        }

        public bool IsTargeted(IEnumerable<Target> beingTargeted) {
            return Targets.Overlaps(beingTargeted);
        }

        public bool IsTargeted(TargetGroup other) {
            return Targets.Overlaps(other.Targets);
        }
    }
}