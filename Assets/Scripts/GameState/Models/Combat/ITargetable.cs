using UnityEngine;

namespace Andja.Model {

    //TODO: find a way to tell others that this one got destroyed!
    //so that they can remove this one as a target
    public interface ITargetable {
        int PlayerNumber { get; }
        float MaximumHealth { get; }
        float CurrentHealth { get; }
        bool IsDestroyed { get; }
        Vector2 CurrentPosition { get; }
        Vector2 NextDestinationPosition { get; }
        Vector2 LastMovement { get; }
        ArmorType ArmorType { get; }
        float Speed { get; }
        float Width { get; }
        float Height { get; }

        bool IsAttackableFrom(IWarfare warfare);

        void TakeDamageFrom(IWarfare warfare);
    }
}