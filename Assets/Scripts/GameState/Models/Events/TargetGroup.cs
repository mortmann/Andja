using System.Collections.Generic;

namespace Andja.Model {

    public enum EventTarget {
        World, Player, Island, City,
        AllUnit, Ship, LandUnit,
        AllStructure, DamageableStructure, BurnableStructure,
        RoadStructure, NeedStructure, MilitaryStructure, HomeStructure, ServiceStructure,
        GrowableStructure, OutputStructure, MarketStructure, WarehouseStructure, MineStructure,
        FarmStructure, ProductionStructure,
    }

    public class TargetGroup {
        public static List<EventTarget> GetStructureTargets() {
            return new List<EventTarget> { EventTarget.AllStructure, EventTarget.DamageableStructure, EventTarget.BurnableStructure,
                EventTarget.RoadStructure, EventTarget.NeedStructure, EventTarget.MilitaryStructure, EventTarget.HomeStructure, 
                EventTarget.ServiceStructure, EventTarget.GrowableStructure, EventTarget.OutputStructure, EventTarget.MarketStructure, 
                EventTarget.WarehouseStructure, EventTarget.MineStructure, EventTarget.FarmStructure, EventTarget.ProductionStructure };
        }
        public static List<EventTarget> GetUnitTargets() {
            return new List<EventTarget> { EventTarget.AllUnit, EventTarget.Ship, EventTarget.LandUnit };
        }

        public HashSet<EventTarget> Targets;

        public TargetGroup(params EventTarget[] targets) {
            Targets = new HashSet<EventTarget>();
            Targets.UnionWith(targets);
        }

        public TargetGroup(ICollection<EventTarget> targets) {
            Targets = new HashSet<EventTarget>();
            Targets.UnionWith(targets);
        }

        internal void AddTargets(TargetGroup target) {
            Targets.UnionWith(target.Targets);
        }

        public bool IsTargeted(IEnumerable<EventTarget> beingTargeted) {
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