using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Combat;

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
    ArmorType MyArmorType { get; }
    float Speed { get; }
    float Width { get; }
    float Height { get; }
    float Rotation { get; }
    bool IsAttackableFrom(IWarfare warfare);
    void TakeDamageFrom(IWarfare warfare);
}
