using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Combat;

public interface ITargetable {
    int PlayerNumber { get; }
    float MaximumHealth { get; }
    float CurrentHealth { get; }
    bool IsDestroyed { get; }
    Vector2 CurrentPosition { get; }
    ArmorType MyArmorType { get; }
    bool IsAttackableFrom(IWarfare warfare);
}
