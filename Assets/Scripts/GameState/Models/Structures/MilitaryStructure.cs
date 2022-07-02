using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public class MilitaryStructurePrototypeData : StructurePrototypeData {
        public Unit[] canBeBuildUnits;
        public float buildTimeModifier;
        public int buildQueueLength = 1;
        public DamageType damageType;
        //kinda double with units but no clue yet how to reduce this duplication
        public float damage;
        public float attackRate;
        public float attackRange;
        public float projectileSpeed;

        public bool canBuildShips; //is set in prototypcontroller
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MilitaryStructure : TargetStructure, IWarfare {
        [JsonPropertyAttribute] private float buildTimer;
        [JsonPropertyAttribute] private Queue<Unit> toBuildUnits;
        [JsonPropertyAttribute] ITargetable CurrentTarget;
        [JsonPropertyAttribute] float attackCooldownTimer = 0;
        bool CanBuildShips => MilitaryStructureData.canBuildShips;
        private List<Tile> toPlaceUnitTiles;
        public float ProgressPercentage => CurrentlyBuildingUnit != null ? buildTimer / CurrentlyBuildingUnit.BuildTime : 0;
        public Unit[] CanBeBuildUnits => MilitaryStructureData.canBeBuildUnits;
        public float AttackRate => MilitaryStructureData.attackRate;
        public float AttackRange => MilitaryStructureData.attackRange;
        float ProjectileSpeed => MilitaryStructureData.projectileSpeed;
        public float BuildTimeModifier => CalculateRealValue(nameof(MilitaryStructureData.buildTimeModifier), MilitaryStructureData.buildTimeModifier);
        public int BuildQueueLength => CalculateRealValue(nameof(MilitaryStructureData.buildQueueLength), MilitaryStructureData.buildQueueLength);

        public Unit CurrentlyBuildingUnit => toBuildUnits.Count > 0 ? toBuildUnits.Peek() : null;

        protected MilitaryStructurePrototypeData _militaryStructureData;

        public MilitaryStructurePrototypeData MilitaryStructureData {
            get {
                if (_militaryStructureData == null) {
                    _militaryStructureData = (MilitaryStructurePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
                }
                return _militaryStructureData;
            }
        }

        public MilitaryStructure() {
        }

        public MilitaryStructure(MilitaryStructure mb) {
            BaseCopyData(mb);
        }

        internal bool HasEnoughResources(Unit u) {
            if (PlayerController.Instance.HasEnoughMoney(PlayerNumber, u.BuildCost) == false) {
                return false;
            }
            return City.HasEnoughOfItems(u.BuildingItems);
        }

        public MilitaryStructure(string iD, MilitaryStructurePrototypeData mpd) {
            ID = iD;
            this._militaryStructureData = mpd;
        }

        public override Structure Clone() {
            return new MilitaryStructure(this);
        }

        public override void OnBuild() {
            toBuildUnits = new Queue<Unit>();
            toPlaceUnitTiles = new List<Tile>();
            foreach (Tile t in NeighbourTiles) {
                t.RegisterTileStructureChangedCallback(OnNeighbourTileStructureChange);
                if (t.Structure != null && t.Structure.IsWalkable == false) {
                    return;
                }
                if (CanBuildShips && t.Type == TileType.Ocean) {
                    Vector2 v = Center - t.Vector2;
                    Tile nT = World.Current.GetTileAt(t.Vector2 - v.normalized * 2);
                    if(nT.Type == TileType.Ocean)
                        toPlaceUnitTiles.Add(nT);
                    else
                        toPlaceUnitTiles.Add(t);
                    continue;
                }
                toPlaceUnitTiles.Add(t);
            }
        }

        public void OnNeighbourTileStructureChange(Tile tile, Structure str) {
            if (str != null && str.IsWalkable == false) {
                if (toPlaceUnitTiles.Contains(tile)) {
                    toPlaceUnitTiles.Remove(tile);
                }
            }
            toPlaceUnitTiles.Add(tile);
        }

        public override void OnUpdate(float deltaTime) {
            if (isActive == false) {
                return;
            }
            if(CurrentlyBuildingUnit != null) {
                buildTimer += deltaTime * BuildTimeModifier;
                if (buildTimer > CurrentlyBuildingUnit.BuildTime) {
                    //Spawn Unit here and reset the timer!
                    buildTimer = 0;
                    SpawnUnit(toBuildUnits.Dequeue());
                }
            }
            if(CurrentTarget != null) {
                if (CanAttack(CurrentTarget) == false) {
                    CurrentTarget = null;
                    return;
                }
                if (attackCooldownTimer>0) {
                    attackCooldownTimer = Mathf.Clamp(attackCooldownTimer, 0, AttackRate);
                    return;
                }
                if (Projectile.PredictiveAim(CurrentPosition, ProjectileSpeed, 
                                                CurrentTarget.CurrentPosition, CurrentTarget.LastMovement, GameData.Gravity, 
                                                out Vector3 pSpeed, out Vector3 pDestination)) {
                    float distance = (new Vector3(CurrentPosition.x,CurrentPosition.y) - pDestination).magnitude;
                    //TODO: think about this direct call to UnitSpriteController...
                    UnitSpriteController.Instance.OnProjectileCreated(
                        new Projectile(this, Center, CurrentTarget, pDestination, pSpeed, distance, true)
                        );
                }
            }
        }

        public bool AddUnitToBuildQueue(Unit u) {
            //cant build more -> if we make a buildqueue!
            if (toBuildUnits.Count >= BuildQueueLength) {
                return false;
            }
            //we need to know if we have all the resources!
            if (City.HasEnoughOfItems(u.BuildingItems) == false) {
                return false;
            }
            City.RemoveResources(u.BuildingItems);
            PlayerController.Instance.ReduceMoney(u.BuildCost, PlayerNumber);
            toBuildUnits.Enqueue(u);
            return true;
        }

        private void SpawnUnit(Unit unit) {
            if (toPlaceUnitTiles.Count == 0)
                return;
            if(unit.IsUnit) {
                World.Current.CreateUnit(unit, PlayerController.GetPlayer(PlayerNumber), toPlaceUnitTiles[0]);
            }
            else {
                World.Current.CreateUnit(unit, PlayerController.GetPlayer(PlayerNumber), 
                                                        toPlaceUnitTiles.Find(x=>x.Type == TileType.Ocean));
            }
        }
        public override void ToggleActive() {
            base.ToggleActive();
            if(isActive) {
                RemoveEffect(new Effect("inactive"));
            }
            else {
                AddEffect(new Effect("inactive"));
            }
        }

        #region IWarfareImplementation
        public bool GiveAttackCommand(ITargetable target, bool overrideCurrent = false) {
            if (CanAttack(target) == false)
                return false;
            if (overrideCurrent == false && CurrentTarget != null)
                return false;
            CurrentTarget = target;
            return true;
        }
        public bool CanAttack(ITargetable target) {
            if (CurrentDamage <= 0)
                return false;
            if (PlayerController.Instance.ArePlayersAtWar(CurrentTarget.PlayerNumber, PlayerNumber) == false) {
                return false;
            }
            if (IsInRange() == false) {
                return false;
            }
            return true;
        }
        public bool IsInRange() {
            if (CurrentTarget == null)
                return false;
            return (CurrentTarget.CurrentPosition - CurrentPosition).magnitude <= AttackRange;
        }
        public void GoIdle() {
            CurrentTarget = null;
        }

        public float GetCurrentDamage(ArmorType armorType) {
            return DamageType.GetDamageMultiplier(armorType) * CurrentDamage;
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            _militaryStructureData = null;
        }
        public float CurrentDamage => isActive ? 0 : MilitaryStructureData.damage;
        public float MaximumDamage => MilitaryStructureData.damage;
        public DamageType DamageType => MilitaryStructureData.damageType;

        #endregion IWarfareImplementation
    }
}