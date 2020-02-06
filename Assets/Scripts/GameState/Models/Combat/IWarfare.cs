using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using static Combat;

public interface IWarfare : ITargetable {
    float CurrentDamage { get; }
    float MaximumDamage { get; }
    DamageType DamageType { get; }
    bool GiveAttackCommand(ITargetable warfare, bool overrideCurrent = false);
    void GoIdle();
    float GetCurrentDamage(ArmorType armorType);
}
