using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TargetStructurePrototypeData : StructurePrototypeData {

}
public abstract class TargetStructure : Structure,ITargetable {

    #region ITargetableImplementation
    public float CurrentHealth => Health;
    public bool IsDestroyed => Health <= 0;
    public Vector2 CurrentPosition => MiddlePoint;
    public Combat.ArmorType MyArmorType => PrototypController.Instance.StructureArmor;
    public bool IsAttackableFrom(IWarfare warfare) {
        return warfare.MyDamageType.GetDamageMultiplier(MyArmorType) > 0;
    }
    public void TakeDamageFrom(IWarfare warfare) {
        Health -= warfare.GetCurrentDamage(MyArmorType);
    }
    public float MaximumHealth => Data.MaxHealth;
    #endregion
}
