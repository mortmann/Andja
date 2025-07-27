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
            if (thing is Unit) {
                return new UnitAttack(thing);
            }
            return new StructureAttack(thing);
        }
    }
    public abstract class Attack : Target, IAttack {
        [JsonProperty] public float Cooldown = 1;
        
        public float AttackRate => Data.attackRate;
        public float AttackRange => Data.attackRange;
        public float ProjectileSpeed => Data.projectileSpeed;
        public virtual float CurrentDamage => Parent.IsActive ? Damage : 0;
        public virtual float MaximumDamage => Damage;
        public DamageType DamageType => Data.damageType;
        protected float Damage => Parent.CalculateRealValue(nameof(Data.damage), Data.damage);
        protected AttackPrototypeData _data;
        private new AttackPrototypeData Data => _data ??= Parent.GetElementData<AttackPrototypeData>();
        protected virtual AttackCommand AttackCommand => null;
        public ITarget CurrentTarget => AttackCommand.Target;
        public bool HasAttack => CurrentDamage > 0;


        protected Attack(BaseThing baseThing) : base(baseThing) {
        }

        protected virtual void DoProjectileDamage(float deltaTime) {
            if (CurrentTarget == null) return;
            if (CanAttack(CurrentTarget) == false) return;

            if (Cooldown > 0) {
                Cooldown = Mathf.Clamp(Cooldown - deltaTime, 0, AttackRate);
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

        public bool CanAttack(ITarget target) {
            return IsAllowedToAttack(target) && Parent.IsInRange(target, AttackRange);
        }

        private bool IsAllowedToAttack(ITarget target) {
            if (CurrentDamage <= 0)
                return false;
            return target.IsAttackableFrom(this)
                   && PlayerController.Instance.ArePlayersAtWar(CurrentTarget.PlayerNumber, Parent.PlayerNumber);
        }

        public float GetCurrentDamage(ArmorType armorType) {
            return DamageType.GetDamageMultiplier(armorType) * CurrentDamage;
        }

        public override void OnStart(bool loading = false) { }

        public override void OnDestroy() { }

        public override void OnUpdate(float deltaTime) {
            if(HasAttack == false) return;
            if (Cooldown > 0) {
                Cooldown -= deltaTime;
                return;
            }
            if (CurrentTarget == null) {
                StopAttack();
                return;
            }

            if (CurrentTarget.Parent.IsDestroyed) {
                StopAttack();
                return;
            }

            if (PlayerController.Instance.ArePlayersAtWar(CurrentTarget.PlayerNumber, Parent.PlayerNumber) == false) {
                StopAttack();
                return;
            }

            if (Parent.IsInRange(CurrentTarget, AttackRange) == false) {
                return;
            }

            if (DamageType.isProjectile) {
                DoProjectileDamage(deltaTime);
            }
            else {
                DoDirectDamage(deltaTime);
            }
        }

        protected virtual void StopAttack() {
            
        }

        private void DoDirectDamage(float deltaTime) {
            if (Parent is Unit unit)
                unit.Pathfinding.UpdateDoRotate(deltaTime);
            Cooldown = AttackRate;
            CurrentTarget.TakeDamageFrom(this);
        }

        public override void OnLoad() { }

        public static implicit operator Attack(BaseThing baseThing) {
            return baseThing.GetElement<Attack>();
        }
    }
}