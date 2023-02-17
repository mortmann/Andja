using Andja.Controller;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
    public abstract class UnitOperation : Operation {
        protected Unit[] Units;
        protected bool OverrideCurrentCommand;

        public UnitOperation(AIPlayer player, Unit unit) : this(player, new Unit[] { unit }) { }
        public UnitOperation(AIPlayer player, Unit[] units) : base(player) {
            Units = units;
        }
        public UnitOperation(AIPlayer player, Unit unit, bool overrideCurrentCommand = false) : this(player, new Unit[] { unit }) { }
        public UnitOperation(AIPlayer player, Unit[] units, bool overrideCurrentCommand = false) : base(player) {
            Units = units;
        }
    }

    public class MoveUnitOperation : UnitOperation {
        Vector2 Destination;
        public MoveUnitOperation(AIPlayer player, Unit unit, Tile destination, bool overrideCurrentCommand = false) : base(player, unit, overrideCurrentCommand) {
            Destination = destination.Vector2;
        }
        public MoveUnitOperation(AIPlayer player, Unit[] units, Tile destination, bool overrideCurrentCommand = false) : base(player, units, overrideCurrentCommand) {
            Destination = destination.Vector2;
        }
        public MoveUnitOperation(AIPlayer player, Unit unit, Vector2 destination, bool overrideCurrentCommand = false) : base(player, unit, overrideCurrentCommand) {
            Destination = destination;
        }
        public MoveUnitOperation(AIPlayer player, Unit[] units, Vector2 destination, bool overrideCurrentCommand = false) : base(player, units, overrideCurrentCommand) {
            Destination = destination;
        }
        public override bool Do() {
            if (Array.TrueForAll(Units, u => u.CanReach(Destination))) {
                Array.ForEach(Units, (u) => u.GiveMovementCommand(Destination, OverrideCurrentCommand));
                return true;
            }
            return false;
        }
    }
    public class UnitAttackOperation : UnitOperation {
        ITargetable Target;
        public UnitAttackOperation(AIPlayer player, Unit[] unitGroup, ITargetable target, Tile destination, 
            bool overrideCurrentCommand = false) : base(player, unitGroup, overrideCurrentCommand) {
            Target = target;
        }
        public override bool Do() {
            if(Array.TrueForAll(Units, u => u.CanReach(Target.CurrentPosition.x, Target.CurrentPosition.y))) {
                Array.ForEach(Units, (u) => u.GiveAttackCommand(Target, OverrideCurrentCommand));
                return true;
            }
            return false;
        }
    }

    public class ShipTradeRoute : Operation {
        public Ship Ship;
        public TradeRoute TradeRoute;

        public ShipTradeRoute(AIPlayer player, Ship ship, TradeRoute route) : base(player) {
            Ship = ship;
            TradeRoute = route;
        }

        public override bool Do() {
            Ship.SetTradeRoute(TradeRoute);
            return Ship.IsAlive;
        }
    }

    public class UnitCityMoveItemOperation : UnitOperation {
        ICity City;
        Item[] Items;
        bool ToShip;
        public UnitCityMoveItemOperation(AIPlayer player, Unit unit, ICity city, Item[] items, bool toShip) : base(player, unit) {
            City = city;
            Items = items.Where(i => i != null).ToArray();
            ToShip = toShip;
        }
        public override bool Do() {
            bool did = false;
            if(ToShip) {
                foreach(Item item in Items) {
                    did = City.TradeWithShip(item, () => { return item.count; }, Units[0]) > 0;
                }
            } else {
                foreach (Item item in Items) {
                    did = Units[0].TradeItemToNearbyWarehouse(item, item.count);
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
            Structure.ChangeRotation(ps.rotation);
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
        public ICity City;
        public bool Add;
        public List<TradeItem> TradeItems;

        public TradeItemOperation(AIPlayer player, ICity city, List<TradeItem> TradeItems, bool add) : base(player) {
            this.City = city;
            this.TradeItems = TradeItems;
            this.Add = add;
        }

        public override bool Do() {
            bool did = true;
            if(Add) {
                foreach (TradeItem item in TradeItems) {
                    did &= City.AddTradeItem(item);
                }
            } else {
                foreach (TradeItem item in TradeItems) {
                    did &= City.RemoveTradeItem(item.ItemId);
                }
            }
            return did;
        }
    }

    public class UpgradeHome : Operation {
        public HomeStructure Home; 
        public UpgradeHome (AIPlayer player, HomeStructure home) : base(player) {
            Home = home;
        }

        public override bool Do() {
            if (Home.IsDestroyed)
                return false;
            return Home.UpgradeHouse();
        }
    }
    public abstract class DiplomacyOperation : Operation {
        public Player Target;
        public DiplomacyOperation(AIPlayer player, Player target) : base(player) {
            Target = target;
        }
    }

    public class DemandMoneyOperation : DiplomacyOperation {
        public int Money;
        public DemandMoneyOperation(AIPlayer player, Player target, int money) : base(player, target) {
            Money = money;
        }
        public override bool Do() {
            PlayerController.Instance.TryToDemandMoney(Player.Player, Target, Money);
            return true;
        }
    }
    public class TryIncreaseDiplomaticStandingOperation : DiplomacyOperation {
        public TryIncreaseDiplomaticStandingOperation(AIPlayer player, Player target) : base(player, target) {

        }
        public override bool Do() {
            PlayerController.Instance.IncreaseDiplomaticStanding(Player.Player, Target);
            return true;
        }
    }
    public class DecreaseDiplomaticStandingOperation : DiplomacyOperation {
        public DecreaseDiplomaticStandingOperation(AIPlayer player, Player target) : base(player, target) {

        }
        public override bool Do() {
            PlayerController.Instance.DecreaseDiplomaticStanding(Player.Player, Target);
            return true;
        }
    }
    public class PraisePlayerOperation : DiplomacyOperation {
        public PraisePlayerOperation(AIPlayer player, Player target) : base(player, target) {

        }
        public override bool Do() {
            PlayerController.Instance.PraisePlayer(Player.Player, Target);
            return true;
        }
    }
}

