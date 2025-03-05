using System.Collections.Generic;
using Andja.Pathfinding;
using Andja.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {
    public class ShipAttackPrototypeData : AttackPrototypeData {
        public int maximumAmountOfCannons = 0;
        public Item cannonType = null;
        public override Element GetNewElement(BaseThing thing) {
            return new ShipAttack(thing);
        }
    }
    
    public class ShipAttack : Attack {
        private new ShipAttackPrototypeData _data;
        private new ShipAttackPrototypeData Data => _data ??= Parent.GetElementData<ShipAttackPrototypeData>();

        public int MaximumAmountOfCannons => 
            Parent.CalculateRealValue(nameof(Data.maximumAmountOfCannons), Data.maximumAmountOfCannons);

        public override float CurrentDamage =>
            Parent.CalculateRealValue(nameof(CurrentDamage), Damage * CannonItem.count);

        public override float MaximumDamage =>
            Parent.CalculateRealValue(nameof(MaximumDamage), MaximumAmountOfCannons * Damage);
        
        private int CannonPerSide => Mathf.CeilToInt(CannonItem.count / 2f);
        
        public Item CannonItem => _cannonItem ??= Data.cannonType.CloneWithCount();

        //TODO: think about making it like this?
        //calculate in the check range and if in range and possible then just do the shoot calculate there?
        private Shoot nextShoot;
        [JsonProperty] private Item _cannonItem;

        private Ship Ship => (Ship)Parent;
        private BasePathfinding Pathfinding => Ship.Pathfinding;
        
        public ShipAttack(BaseThing baseThing) : base(baseThing) {
            if(baseThing is Ship == false) Log.PROTOTYPE_ERROR("SHIP ATTACK IS NOT ON SHIP");
        }
        
        protected override void DoProjectileDamage(float deltaTime) {
            if (HasAttack == false || CurrentTarget == null) return;
            float shootAngle = nextShoot.RotateToAngle;
            
            float arc = 5f;
            bool canShoot = shootAngle <= Pathfinding.rotation + arc && shootAngle >= Pathfinding.rotation - arc;
            Pathfinding.Rotate(nextShoot.RotateToAngle);
            Pathfinding.UpdateDoRotate(deltaTime);
            if (canShoot == false) {
                return;
            }
            
            if (AttackCooldownTimer > 0) {
                AttackCooldownTimer -= deltaTime;
                return;
            }
            
            Vector3 targetPosition = CurrentTarget.CurrentPosition;
            Vector3 lastMove = CurrentTarget.LastMovement;
            Vector3 projectileDestination = CurrentTarget.CurrentPosition;
            if (Projectile.PredictiveAim(CurrentPosition, ProjectileSpeed, targetPosition,
                    lastMove, GameData.Gravity, out Vector3 velocity, out projectileDestination) == false) {
                return;
            }
            
            ShotAtPosition(projectileDestination);
        }
        
        public void ShotAtPosition(Vector3 destination) {
            if (CannonItem.count == 0)
                return;
            Vector3 targetSize = new Vector3(1, 1, 0);
            Vector3 position = CurrentPosition;
            Vector2 side;
            float widthOffset = 0;
            if (nextShoot.SideAngle < 0) {
                side = Quaternion.Euler(0, 0, Pathfinding.rotation) * new Vector2(0, 1);
                widthOffset = Ship.Width / 2;
            }
            else {
                side = Quaternion.Euler(0, 0, Pathfinding.rotation) * new Vector2(0, -1);
                widthOffset = -Ship.Width / 2;
            }

            List<Projectile> projectiles = new List<Projectile>();
            for (int i = 1; i <= CannonPerSide; i++) {
                Vector3 offset = new Vector3((i) * (Ship.Height / MaximumAmountOfCannons) - Ship.Height / 2, widthOffset);
                offset = Quaternion.Euler(0, 0, Ship.Rotation) * offset;
                Vector3 targetOffset = new Vector3(
                    Random.Range(-targetSize.x / 2, targetSize.x / 2),
                    Random.Range(-targetSize.y / 2, targetSize.y / 2),
                    Random.Range(-targetSize.z / 2, targetSize.z / 2)
                );

                Vector3 velocity = (destination + targetOffset - Ship.PositionVector - offset).normalized * ProjectileSpeed;
                float distance = (destination + targetOffset - Ship.PositionVector - offset).magnitude;
                projectiles.Add(new Projectile(this, position + offset, CurrentTarget,
                    destination + targetOffset, velocity, distance, true));
            }
            Ship.CreateProjectiles(projectiles);
        }
        
        protected Shoot CalculateShootAngle(Vector3 destination) {
            Vector2 forward = Quaternion.Euler(0, 0, Ship.Pathfinding.rotation) * new Vector2(1, 0);
            Vector2 direction = destination - Ship.PositionVector;
            direction.Normalize();
            float sideAngle = Mathf.Sign(Vector2.SignedAngle(direction, forward));
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            return new Shoot {
                SideAngle = angle,
                RotateToAngle = angle + (sideAngle) * 90,
                RotateByAngle = angle - Ship.Pathfinding.rotation
            };
        }
        
        public bool IsInRange(Target target, float range) {
            if (target == null)
                return false;
            if (target.LastMovement.sqrMagnitude == 0) {
                if ((target.CurrentPosition - CurrentPosition).magnitude > AttackRange) return false;
                nextShoot = CalculateShootAngle(target.CurrentPosition);
                return true;
            }

            Vector3 targetPosition = target.CurrentPosition;
            Vector3 lastMove = target.LastMovement;
            Vector3 projectileDestination = targetPosition;
            Shoot shoot = CalculateShootAngle(projectileDestination);
            float rotateTime = Ship.CalculateRotateTime(shoot.RotateByAngle);
            targetPosition += rotateTime * lastMove;
            bool can = Projectile.PredictiveAim(CurrentPosition, ProjectileSpeed,
                targetPosition, lastMove, GameData.Gravity, out Vector3 velocity, out projectileDestination);
            if (can == false || Vector3.Distance(CurrentPosition, projectileDestination) > AttackRange)
                return false;
            nextShoot = CalculateShootAngle(projectileDestination);
            return true;
        }
        
        public void RemoveCannonsToInventory(bool all) {
            if (all)
                CannonItem.count -= Ship.Inventory.AddItem(CannonItem);
            else {
                Item temp = CannonItem.Clone();
                temp.count = 1;
                CannonItem.count -= Ship.Inventory.AddItem(temp);
            }
        }

        public void AddCannonsFromInventory(bool all) {
            Item temp = CannonItem.Clone();
            if (all) {
                temp.count = MaximumAmountOfCannons - CannonItem.count;
            }
            else {
                temp.count = Mathf.Min(1, MaximumAmountOfCannons - CannonItem.count);
            }
            CannonItem.count += Ship.Inventory.GetItemWithMaxItemCount(temp).count;
        }

        public bool CanRemoveCannons() {
            return CannonItem.count > 0 && Ship.Inventory.HasRemainingSpaceForItem(CannonItem);
        }
        
        protected struct Shoot {
            public float RotateByAngle;
            public float RotateToAngle;
            public float SideAngle;
        }
    }
}