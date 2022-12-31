using System.Collections.Generic;

namespace Andja.Model {

    public enum Target {
        World, Player, Island, City,
        AllUnit, Ship, LandUnit,
        AllStructure, DamageableStructure, BurnableStructure,
        RoadStructure, NeedStructure, MilitaryStructure, HomeStructure, ServiceStructure,
        GrowableStructure, OutputStructure, MarketStructure, WarehouseStructure, MineStructure,
        FarmStructure, ProductionStructure,
    }

    public class TargetGroup {
        public static List<Target> GetStructureTargets() {
            return new List<Target> { Target.AllStructure, Target.DamageableStructure, Target.BurnableStructure,
                Target.RoadStructure, Target.NeedStructure, Target.MilitaryStructure, Target.HomeStructure, 
                Target.ServiceStructure, Target.GrowableStructure, Target.OutputStructure, Target.MarketStructure, 
                Target.WarehouseStructure, Target.MineStructure, Target.FarmStructure, Target.ProductionStructure };
        }
        public static List<Target> GetUnitTargets() {
            return new List<Target> { Target.AllUnit, Target.Ship, Target.LandUnit };
        }

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

        public bool HasStructureTarget() {
            return IsTargeted(GetStructureTargets());
        }
        public bool HasUnitTarget() {
            return IsTargeted(GetUnitTargets());
        }
    }
}