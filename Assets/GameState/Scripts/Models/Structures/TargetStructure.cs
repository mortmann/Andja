using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TargetStructurePrototypeData : StructurePrototypeData {

}
public abstract class TargetStructure : Structure, ITargetable {

    #region ITargetableImplementation
    public Vector2 CurrentPosition => MiddlePoint;
    public Combat.ArmorType MyArmorType => PrototypController.Instance.StructureArmor;
    public bool IsAttackableFrom(IWarfare warfare) {
        return warfare.MyDamageType.GetDamageMultiplier(MyArmorType) > 0;
    }
    public void TakeDamageFrom(IWarfare warfare) {
        ReduceHealth(warfare.GetCurrentDamage(MyArmorType));
    }
    public float MaximumHealth => Data.maxHealth;

    public Vector2 NextDestinationPosition => CurrentPosition;
    public Vector2 LastMovement => Vector2.zero;

    public float Speed => 0;
    #endregion
}
