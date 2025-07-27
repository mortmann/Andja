using UnityEngine;

namespace Andja.Model {
    public interface IAttack {
        IBaseThing Parent { get; }
        float AttackRate { get; }
        float AttackRange { get; }
        float ProjectileSpeed { get; }
        float CurrentDamage { get; }
        float MaximumDamage { get; }
        DamageType DamageType { get; }
        ITarget CurrentTarget { get; }
        int PlayerNumber { get; }
        Vector2 CurrentPosition { get; }
        Vector2 LastMovement { get; }
        bool CanAttack(ITarget target);
        float GetCurrentDamage(ArmorType armorType);
        void OnStart(bool loading = false);
        void OnUpdate(float deltaTime);
        void TakeDamageFrom(IAttack attack);
    }
}