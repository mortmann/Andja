using Andja.Controller;
using System;

namespace Andja.Model {
    public abstract class Operation {
        public AIPlayer Player;
        public Operation(AIPlayer player) {
            Player = player;
        }
        public abstract bool Do();
    }
    public class MoveUnitOperation : Operation {
        Unit Unit;
        Tile Destination;
        bool OverrideCurrentCommand;
        public MoveUnitOperation(AIPlayer player, Unit unit, Tile destination, bool overrideCurrentCommand = false) : base(player) {
            Unit = unit;
            Destination = destination;
            OverrideCurrentCommand = overrideCurrentCommand;
        }
        public override bool Do() {
            return Unit.GiveMovementCommand(Destination, OverrideCurrentCommand);
        }
    }
    public class UnitAttackOperation : Operation {
        Unit[] UnitGroup;
        ITargetable Target;
        bool OverrideCurrentCommand;
        public UnitAttackOperation(AIPlayer player, Unit[] unitGroup, ITargetable target, Tile destination, 
            bool overrideCurrentCommand = false) : base(player) {
            Target = target;
            UnitGroup = unitGroup;
            OverrideCurrentCommand = overrideCurrentCommand;
        }
        public override bool Do() {
            if(Array.TrueForAll(UnitGroup, u => u.CanReach(Target.CurrentPosition.x, Target.CurrentPosition.y))) {
                Array.ForEach(UnitGroup, (u) => u.GiveAttackCommand(Target, OverrideCurrentCommand));
                return true;
            }
            return false;
        }
    }

    public class BuildStructureOperation : Operation {
        Unit BuildUnit;
        Tile BuildTile;
        Structure Structure;
        public BuildStructureOperation(AIPlayer player, Tile buildTile, Structure structure, Unit unit = null) : base(player) {
            BuildUnit = unit;
            BuildTile = buildTile;
            Structure = structure;
        }
        public override bool Do() {
            return AIController.BuildStructure(Player, Structure, BuildTile, BuildUnit);
        }
    }

}

