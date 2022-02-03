using Andja.Controller;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {
    public enum OperationStatus { Pending, Success, Failure}
    public abstract class Operation {
        public OperationStatus Status;

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

    public class UnitCityMoveItemOperation : Operation {
        Unit Unit;
        City City;
        Item[] Items;
        bool ToShip;
        public UnitCityMoveItemOperation(AIPlayer player, Unit unit, City city, Item[] items, bool toShip) : base(player) {
            Unit = unit;
            City = city;
            Items = items;
            ToShip = toShip;
        }
        public override bool Do() {
            bool did = false;
            if(ToShip) {
                foreach(Item item in Items) {
                    did = City.TradeWithShip(item, () => { return item.count; }, Unit) > 0;
                }
            } else {
                foreach (Item item in Items) {
                    did = Unit.TradeItemToNearbyWarehouse(item, item.count);
                }
            }
            return did;
        }
    }

    public class BuildStructureOperation : Operation {
        public Unit BuildUnit;
        public Tile BuildTile;
        public Structure Structure;

        public BuildStructureOperation(AIPlayer player, PlaceStructure ps) : base(player) {
            BuildTile = ps.buildTile;
            Structure = PrototypController.Instance.GetStructure(ps.ID);
            Structure.rotation = ps.rotation;
        }

        public BuildStructureOperation(AIPlayer player, Tile buildTile, Structure structure, Unit unit = null) : base(player) {
            BuildUnit = unit;
            BuildTile = buildTile;
            Structure = structure;

        }
        public override bool Do() {
            bool temp = AIController.BuildStructure(Player, Structure, BuildTile, BuildUnit);
            if(temp)
                Structure = BuildTile.Structure; // Replace old link with actual structure
            return temp;
        }
    }
    public class BuildSingleStructureOperation : Operation {
        public Unit BuildUnit;
        public List<Vector2> BuildTiles;
        public Structure Structure;

        public BuildSingleStructureOperation(AIPlayer player, List<Vector2> buildTile, Structure structure, Unit unit = null) : base(player) {
            BuildUnit = unit;
            BuildTiles = buildTile;
            Structure = structure;
        }
        public override bool Do() {
            foreach(Vector2 t in BuildTiles)
                AIController.BuildStructure(Player, Structure, World.Current.GetTileAt(t), BuildUnit);
            //TODO: check if tiles are correctly done?
            return true;
        }
    }

    public class TradeItemOperation : Operation {

        public City city;
        public bool Add;
        public List<TradeItem> TradeItems;

        public TradeItemOperation(AIPlayer player, City city, List<TradeItem> TradeItems, bool add) : base(player) {
            this.city = city;
            this.TradeItems = TradeItems;
            this.Add = add;
        }

        public override bool Do() {
            bool did = true;
            if(Add) {
                foreach (TradeItem item in TradeItems) {
                    did &= city.AddTradeItem(item);
                }
            } else {
                foreach (TradeItem item in TradeItems) {
                    did &= city.RemoveTradeItem(item.ItemId);
                }
            }
            return did;
        }
    }

}

