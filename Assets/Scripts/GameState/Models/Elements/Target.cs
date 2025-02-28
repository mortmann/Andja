using Andja;
using Andja.Controller;
using Andja.Model;
using Andja.UI.Model;
using UnityEngine;

namespace Andja.Model {
    public class TargetPrototypeData : ElementData {
        public ArmorType armorType;
    }

    public class Target : Element, ITarget {
        public Vector2 CurrentPosition => Parent.Position;
        private TargetPrototypeData _data;
        public TargetPrototypeData Data => _data ??= Parent.GetElementData<TargetPrototypeData>();
        public Vector2 LastMovement => Parent is Unit u ? u.LastMovement : CurrentPosition;
        public ArmorType ArmorType => Data.armorType;

        public bool IsAttackableFrom(IAttack attack) {
            if (Parent.CanTakeDamage == false)
                return false;
            return attack.DamageType.GetDamageMultiplier(ArmorType) > 0;
        }

        public void TakeDamageFrom(IAttack attack) {
            Parent.ReduceHealth(attack.GetCurrentDamage(ArmorType));
            if (Parent.IsDestroyed == false && PlayerController.currentPlayerNumber == Parent.PlayerNumber) {
                EventUIManager.Instance.Show(Parent, attack);
            }
        }

        public Target(BaseThing baseThing) : base(baseThing) { }

        public override void OnStart(bool loading = false) { }

        public override void OnDestroy() { }

        public override void OnUpdate(float deltaTime) { }

        public override void OnLoad() { }

        public static implicit operator Target(BaseThing baseThing) {
            return baseThing.GetElement<Target>();
        }
    }
}