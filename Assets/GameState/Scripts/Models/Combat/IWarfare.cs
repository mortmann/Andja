using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using static Combat;

public interface IWarfare : ITargetable {
    float CurrentDamage { get; }
    float MaximumDamage { get; }
    DamageType MyDamageType { get; }
    bool GiveAttackCommand(IWarfare warfare, bool overrideCurrent = false);
    void StopAttack();
    float GetCurrentDamage(ArmorType armorType);
}
