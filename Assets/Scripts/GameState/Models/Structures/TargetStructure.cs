using Andja.Controller;
using UnityEngine;

namespace Andja.Model {
    public class TargetStructurePrototypeData : StructurePrototypeData { }

    public abstract class TargetStructure : Structure {
        public Vector2 CurrentPosition => Center;
        public ArmorType ArmorType => PrototypController.Instance.StructureArmor;

        public bool IsAttackableFrom(IAttack attack) {
            if (CanTakeDamage == false)
                return false;
            return attack.DamageType.GetDamageMultiplier(ArmorType) > 0;
        }

        public void TakeDamageFrom(IAttack attack) {
            ReduceHealth(attack.GetCurrentDamage(ArmorType));
            if (IsDestroyed == false && PlayerController.currentPlayerNumber == City.PlayerNumber) {
                UI.Model.EventUIManager.Instance.Show(this, attack);
            }
        }

        public Vector2 NextDestinationPosition => CurrentPosition;
        public Vector2 LastMovement => Vector2.zero;

        public float Speed => 0;

        public float Width => TileWidth;
        public float Height => TileHeight;
    }
}