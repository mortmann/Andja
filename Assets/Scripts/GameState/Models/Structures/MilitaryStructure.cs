using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public class MilitaryPrototypeData : StructurePrototypeData {
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
        [JsonPropertyAttribute] protected float buildTimer;
        [JsonPropertyAttribute] protected Queue<Unit> toBuildUnits;
        [JsonPropertyAttribute] protected ITargetable CurrentTarget;
        [JsonPropertyAttribute] protected float attackCooldownTimer;
        protected List<Tile> toPlaceUnitTiles;
        public bool CanBuildShips => MilitaryStructureData.canBuildShips;
        public float ProgressPercentage => buildTimer / CurrentlyBuildingUnit?.BuildTime ?? 0;
        public Unit[] CanBeBuildUnits => MilitaryStructureData.canBeBuildUnits;
        public float AttackRate => MilitaryStructureData.attackRate;
        public float AttackRange => MilitaryStructureData.attackRange;
        public float ProjectileSpeed => MilitaryStructureData.projectileSpeed;
        public float BuildTimeModifier => CalculateRealValue(nameof(MilitaryStructureData.buildTimeModifier), MilitaryStructureData.buildTimeModifier);
        public int BuildQueueLength => CalculateRealValue(nameof(MilitaryStructureData.buildQueueLength), MilitaryStructureData.buildQueueLength);

        public Unit CurrentlyBuildingUnit => toBuildUnits.Count > 0 ? toBuildUnits.Peek() : null;

        protected MilitaryPrototypeData militaryStructureData;

        public MilitaryPrototypeData MilitaryStructureData =>
            militaryStructureData ??= (MilitaryPrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);

        public MilitaryStructure() {
        }

        public MilitaryStructure(MilitaryStructure mb) {
            BaseCopyData(mb);
        }

        public MilitaryStructure(string iD, MilitaryPrototypeData mpd) {
            ID = iD;
            this.militaryStructureData = mpd;
        }

        public override Structure Clone() {
            return new MilitaryStructure(this);
        }

        public override void OnBuild() {
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

        public override void OnUpdate(float deltaTime) {
            if (isActive == false) {
                return;
            }
            UpdateBuildUnit(deltaTime);
            UpdateAttackTarget(deltaTime);
        }

        public void UpdateAttackTarget(float deltaTime) {
            if (CurrentTarget == null) return;
            if (CanAttack(CurrentTarget) == false) {
                CurrentTarget = null;
                return;
            }
            if (attackCooldownTimer > 0) {
                attackCooldownTimer = Mathf.Clamp(attackCooldownTimer - deltaTime, 0, AttackRate);
                return;
            }

            if (Projectile.PredictiveAim(CurrentPosition, ProjectileSpeed,
                    CurrentTarget.CurrentPosition, CurrentTarget.LastMovement, GameData.Gravity,
                    out Vector3 pSpeed, out Vector3 pDestination)
                == false) return;
            float distance = (new Vector3(CurrentPosition.x, CurrentPosition.y) - pDestination).magnitude;
            World.Current.OnCreateProjectile(new Projectile(this, Center, CurrentTarget, pDestination, pSpeed, distance, true));
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
            if(unit.IsUnit) {
                World.Current.CreateUnit(unit, PlayerController.Instance.GetPlayer(PlayerNumber), toPlaceUnitTiles[0]);
            }
            else {
                World.Current.CreateUnit(unit, PlayerController.Instance.GetPlayer(PlayerNumber), 
                                                        toPlaceUnitTiles.Find(x=>x.Type == TileType.Ocean));
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
            if (target.IsAttackableFrom(this) == false)
                return false;
            if (PlayerController.Instance.ArePlayersAtWar(CurrentTarget.PlayerNumber, PlayerNumber) == false) {
                return false;
            }
            return IsInRange(target);
        }
        public bool IsInRange(ITargetable target) {
            return (target.CurrentPosition - CurrentPosition).magnitude <= AttackRange;
        }
        public void GoIdle() {
            CurrentTarget = null;
        }

        public float GetCurrentDamage(ArmorType armorType) {
            return DamageType.GetDamageMultiplier(armorType) * CurrentDamage;
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            militaryStructureData = null;
        }

        public uint GetBuildID() {
            return BuildID;
        }

        public float CurrentDamage => isActive ? MilitaryStructureData.damage : 0;
        public float MaximumDamage => MilitaryStructureData.damage;
        public DamageType DamageType => MilitaryStructureData.damageType;

        #endregion IWarfareImplementation
    }
}