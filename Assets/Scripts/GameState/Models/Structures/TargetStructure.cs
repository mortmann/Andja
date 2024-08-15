using Andja.Controller;
using UnityEngine;

namespace Andja.Model {

    public class TargetStructurePrototypeData : StructurePrototypeData {
    }

    public abstract class TargetStructure : Structure, ITargetable {

        public Vector2 CurrentPosition => Center;
        public ArmorType ArmorType => PrototypController.Instance.StructureArmor;

        public bool IsAttackableFrom(IWarfare warfare) {
            if (CanTakeDamage == false)
                return false;
            return warfare.DamageType.GetDamageMultiplier(ArmorType) > 0;
        }

        public void TakeDamageFrom(IWarfare warfare) {
            ReduceHealth(warfare.GetCurrentDamage(ArmorType));
            if (IsDestroyed == false && PlayerController.currentPlayerNumber == City.PlayerNumber) {
                UI.Model.EventUIManager.Instance.Show(this, warfare);
            }
        }

        public Vector2 NextDestinationPosition => CurrentPosition;
        public Vector2 LastMovement => Vector2.zero;

        public float Speed => 0;

        public float Width => TileWidth;
        public float Height => TileHeight;

    }
}