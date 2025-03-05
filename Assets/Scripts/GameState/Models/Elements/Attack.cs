using Andja.Controller;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {
    public class AttackPrototypeData : TargetPrototypeData {
        public DamageType damageType;
        public float damage;
        public float attackRate;
        public float attackRange;
        public float projectileSpeed;
        public override Element GetNewElement(BaseThing thing) {
            return new Attack(thing);
        }
    }
    public class Attack : Target, IAttack {
        [JsonProperty] public float AttackCooldownTimer = 1;
        
        public float AttackRate => Data.attackRate;
        public float AttackRange => Data.attackRange;
        public float ProjectileSpeed => Data.projectileSpeed;
        public virtual float CurrentDamage => Parent.IsActive ? Damage : 0;
        public virtual float MaximumDamage => Damage;
        public DamageType DamageType => Data.damageType;
        protected float Damage => Parent.CalculateRealValue(nameof(Data.damage), Data.damage);
        protected AttackPrototypeData _data;
        private new AttackPrototypeData Data => _data ??= Parent.GetElementData<AttackPrototypeData>();
        private AttackCommand _attackCommand => Unit.CurrentCommand as AttackCommand;
        public Target CurrentTarget => _attackCommand.Target;
        private Unit Unit => (Unit)Parent;
        public bool HasAttack => CurrentDamage > 0;


        public Attack(BaseThing baseThing) : base(baseThing) {
        }

        protected virtual void DoProjectileDamage(float deltaTime) {
            if (CurrentTarget == null) return;
            if (CanAttack(CurrentTarget) == false) return;

            if (AttackCooldownTimer > 0) {
                AttackCooldownTimer = Mathf.Clamp(AttackCooldownTimer - deltaTime, 0, AttackRate);
                return;
            }

            if (Projectile.PredictiveAim(CurrentPosition, ProjectileSpeed,
                    CurrentTarget.CurrentPosition, CurrentTarget.LastMovement, GameData.Gravity,
                    out Vector3 pSpeed, out Vector3 pDestination)
                == false) return;
            float distance = (new Vector3(CurrentPosition.x, CurrentPosition.y) - pDestination).magnitude;
            World.Current.OnCreateProjectile(new Projectile(this, Parent.Position, CurrentTarget, pDestination, pSpeed,
                distance, true));
        }

        public bool CanAttack(Target target) {
            return IsAllowedToAttack(target) && Parent.IsInRange(target, AttackRange);
        }

        private bool IsAllowedToAttack(Target target) {
            if (CurrentDamage <= 0)
                return false;
            return target.IsAttackableFrom(this)
                   && PlayerController.Instance.ArePlayersAtWar(CurrentTarget.PlayerNumber, Parent.PlayerNumber);
        }

        public bool CanAttackNowOrReach(Target target) {
            return CanAttack(target) || Unit.CanReach(Unit.ClosestTargetPosition(target.CurrentPosition));
        }

        public float GetCurrentDamage(ArmorType armorType) {
            return DamageType.GetDamageMultiplier(armorType) * CurrentDamage;
        }

        public override void OnStart(bool loading = false) { }

        public override void OnDestroy() { }

        public override void OnUpdate(float deltaTime) {
            if(HasAttack == false) return;
            if (AttackCooldownTimer > 0) {
                AttackCooldownTimer -= deltaTime;
                return;
            }
            if (Unit.CurrentMainMode != UnitMainModes.Attack)
                return;
            
            if (CurrentTarget == null) {
                Unit.GoIdle();
                return;
            }

            if (CurrentTarget.Parent.IsDestroyed) {
                Unit.GoIdle();
                return;
            }

            if (PlayerController.Instance.ArePlayersAtWar(CurrentTarget.PlayerNumber, Parent.PlayerNumber) == false) {
                Unit.GoIdle();
                return;
            }

            if (Unit.IsInRange(CurrentTarget, AttackRange) == false) {
                return;
            }

            if (DamageType.isProjectile) {
                DoProjectileDamage(deltaTime);
            }
            else {
                DoDirectDamage(deltaTime);
            }
        }

        private void DoDirectDamage(float deltaTime) {
            if (Parent is Unit unit)
                unit.Pathfinding.UpdateDoRotate(deltaTime);
            AttackCooldownTimer = AttackRate;
            CurrentTarget.TakeDamageFrom(this);
        }

        public override void OnLoad() { }


        public void UpdateAttack() {
            if (HasAttack && Parent.IsInRange(CurrentTarget, AttackRange) == false) {
                if (Unit.CurrentDoingMode != UnitDoModes.Move) {
                    Unit.Pathfinding.cbIsAtDestination += OnArriveDestination;
                    Vector2 dest = CurrentTarget.CurrentPosition;
                    Unit.SetDestinationIfPossible(dest.x, dest.y);
                }
            }
            else if (Unit.CurrentDoingMode != UnitDoModes.Fight) {
                //is in range start fighting
                Unit.CurrentDoingMode = UnitDoModes.Fight;
            }
        }

        private void OnArriveDestination(bool atDest) {
            if (atDest == false) {
                return;
            }

            if (CurrentTarget != null)
                Unit.CurrentDoingMode = UnitDoModes.Fight;

            Unit.Pathfinding.cbIsAtDestination -= OnArriveDestination;
        }

        private void UpdateAggroing() {
            if (HasAttack == false || CurrentTarget == null) {
                Unit.CurrentMainMode = UnitMainModes.Idle;
                return;
            }

            //not in Range -> get in range
            if (Parent.IsInRange(CurrentTarget, AttackRange) == false) {
                if (Unit.CurrentDoingMode != UnitDoModes.Move) {
                    Vector2 dest = CurrentTarget.CurrentPosition;
                    if (Vector2.Distance(dest, CurrentPosition) < AttackRange + GameData.UnitAggroRange) {
                        Unit.SetDestinationIfPossible(dest.x, dest.y);
                    }
                }

                AggroCommand aggro = Unit.CurrentCommand as AggroCommand;
                if (Vector2.Distance(aggro.StartPosition, CurrentPosition) > GameData.UnitAggroRange) {
                    //Maybe just send it back to the startposition BUT not finish aggro -> if the other 
                    //is following it could get in range again and we could reaggro without the need to 
                    //go back to the startposition completly -> which requires this to 
                    // update aggro range & move at the sametime
                    aggro.SetFinished();
                    Unit.GiveMovementCommand(aggro.StartPosition);
                    Debug.Log("Finished AGGRO returning to start");
                }
            }
            else {
                //IN range go ahead fight
                if (Unit.CurrentDoingMode != UnitDoModes.Fight)
                    Unit.CurrentDoingMode = UnitDoModes.Fight;
            }
        }

        public static implicit operator Attack(BaseThing baseThing) {
            return baseThing.GetElement<Attack>();
        }
    }
}