using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;
using Andja.Model.Components;
using UnityEngine;

namespace Andja.Model {
    public class MilitaryPrototypeData : StructurePrototypeData {
        public Unit[] canBeBuildUnits;
        public float buildTimeModifier;
        public int buildQueueLength = 1;

        public bool canBuildShips; //is set in prototypcontroller
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MilitaryStructure : TargetStructure {
        [JsonPropertyAttribute] protected float buildTimer;
        [JsonPropertyAttribute] protected Queue<Unit> toBuildUnits;
        protected List<Tile> toPlaceUnitTiles;
        public bool CanBuildShips => MilitaryStructureData.canBuildShips;
        public float ProgressPercentage => buildTimer / CurrentlyBuildingUnit?.BuildTime ?? 0;
        public Unit[] CanBeBuildUnits => MilitaryStructureData.canBeBuildUnits;
        public AttackCommand AttackCommand;
        public float BuildTimeModifier => CalculateRealValue(nameof(MilitaryStructureData.buildTimeModifier),
            MilitaryStructureData.buildTimeModifier);

        public int BuildQueueLength => CalculateRealValue(nameof(MilitaryStructureData.buildQueueLength),
            MilitaryStructureData.buildQueueLength);

        public Unit CurrentlyBuildingUnit => toBuildUnits.Count > 0 ? toBuildUnits.Peek() : null;

        protected MilitaryPrototypeData militaryStructureData;

        public MilitaryPrototypeData MilitaryStructureData =>
            militaryStructureData ??=
                (MilitaryPrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(((IBaseThing)this).ID);

        public MilitaryStructure() { }

        public MilitaryStructure(MilitaryStructure mb) {
            BaseCopyData(mb);
        }

        public MilitaryStructure(string iD, MilitaryPrototypeData mpd) {
            ((IBaseThing)this).ID = iD;
            this.militaryStructureData = mpd;
        }

        public override Structure Clone() {
            return new MilitaryStructure(this);
        }

        public override void OnBuild(bool loading = false) {
            toBuildUnits = new Queue<Unit>();
            toPlaceUnitTiles = new List<Tile>();
            foreach (Tile t in NeighbourTiles) {
                t.RegisterTileStructureChangedCallback(OnNeighbourTileStructureChange);
                if (t.Structure is { IsWalkable: false }) {
                    return;
                }

                if (CanBuildShips && t.Type == TileType.Ocean) {
                    Vector2 v = Center - t.Vector2;
                    Tile nT = World.Current.GetTileAt(t.Vector2 - v.normalized * 2);
                    toPlaceUnitTiles.Add(nT.Type == TileType.Ocean ? nT : t);
                    continue;
                }

                toPlaceUnitTiles.Add(t);
            }
        }

        public bool HasEnoughResources(Unit u) {
            return PlayerController.Instance.HasEnoughMoney(PlayerNumber, u.BuildCost)
                   && City.HasEnoughOfItems(u.BuildingItems);
        }

        public void OnNeighbourTileStructureChange(Tile tile, Structure str) {
            if (str is { IsWalkable: false }) {
                if (toPlaceUnitTiles.Contains(tile)) {
                    toPlaceUnitTiles.Remove(tile);
                }
            }
            else {
                if (toPlaceUnitTiles.Contains(tile) == false) {
                    toPlaceUnitTiles.Add(tile);
                }
            }
        }

        protected override void OnUpdate(float deltaTime) {
            if (isActive == false) {
                return;
            }

            UpdateAttackRangeCheck(deltaTime);
            UpdateBuildUnit(deltaTime);
        }

        private void UpdateAttackRangeCheck(float deltaTime) {
            Collider2D[] c2d = Physics2D.OverlapCircleAll(Center, GetElement<Attack>().AttackRange);
            foreach (var item in c2d) {
                //check for not null = only to be sure its not null
                if (!item) {
                    continue;
                }

                TargetHoldingScript targetableHoldingScript = item.transform.GetComponent<TargetHoldingScript>();
                if (!targetableHoldingScript || targetableHoldingScript.IsUnit == false) {
                    continue;
                }

                Target target = targetableHoldingScript.Holding;
                if (target == null || target.PlayerNumber == PlayerNumber) {
                    continue;
                }

                //see if players are at war
                if (PlayerController.Instance.ArePlayersAtWar(PlayerNumber, target.PlayerNumber) == false) {
                    continue;
                }

                AttackCommand = new AttackCommand(target);
            }
        }


        public void UpdateBuildUnit(float deltaTime) {
            if (CurrentlyBuildingUnit == null) return;
            buildTimer += deltaTime * BuildTimeModifier;
            if ((buildTimer > CurrentlyBuildingUnit.BuildTime) == false) return;
            buildTimer = 0;
            SpawnUnit(toBuildUnits.Dequeue());
        }

        public bool AddUnitToBuildQueue(Unit u) {
            if (toBuildUnits.Count >= BuildQueueLength) {
                return false;
            }

            if (HasEnoughResources(u) == false) {
                return false;
            }

            City.RemoveItems(u.BuildingItems);
            PlayerController.Instance.ReduceMoney(u.BuildCost, PlayerNumber);
            toBuildUnits.Enqueue(u);
            return true;
        }

        private void SpawnUnit(Unit unit) {
            if (toPlaceUnitTiles.Count == 0)
                return;
            if (unit.IsShip) {
                World.Current.CreateUnit(unit, PlayerController.Instance.GetPlayer(PlayerNumber),
                    toPlaceUnitTiles.Find(x => x.Type == TileType.Ocean));
            }
            else {
                World.Current.CreateUnit(unit, PlayerController.Instance.GetPlayer(PlayerNumber), toPlaceUnitTiles[0]);
            }
        }

        public override void ToggleActive() {
            base.ToggleActive();
            if (isActive) {
                RemoveEffect(new Effect(InactiveEffectID));
            }
            else {
                AddEffect(new Effect(InactiveEffectID));
            }
        }

        protected override void OnUpgrade() {
            base.OnUpgrade();
            militaryStructureData = null;
        }
    }
}