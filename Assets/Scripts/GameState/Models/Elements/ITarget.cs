using UnityEngine;

namespace Andja.Model {
    public interface ITarget {
        IBaseThing Parent { get; }
        Vector2 CurrentPosition { get; }
        Vector2 LastMovement { get; }
        int PlayerNumber { get; }
        ArmorType ArmorType { get; }
        bool IsAttackableFrom(IAttack attack);
        void TakeDamageFrom(IAttack attack);
        void OnStart(bool loading = false);
        void OnDestroy();
        void OnUpdate(float deltaTime);
        void OnLoad();
    }
}