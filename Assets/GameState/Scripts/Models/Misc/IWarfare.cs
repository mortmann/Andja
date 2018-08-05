using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using static Combat;

public interface IWarfare : ITargetable {
    //TODO: find a way to tell others that this one got destroyed!
    //so that they can remove this one as a target
    float CurrentDamage { get; }
    float MaximumDamage { get; }
    DamageType MyDamageType { get; }
    void TakeDamageFrom(IWarfare warfare);
    bool GiveAttackCommand(IWarfare warfare, bool overrideCurrent = false);
    void StopAttack();
}
