using UnityEngine;
using System.Collections.Generic;
using System;


public class Combat {

    public List<DamageType> damageTypes;
    public List<ArmorType> armorTypes;

    public class DamageType : LanguageVariables {
        public int ID;
        public String spriteBaseName;
        public Dictionary<ArmorType, float> damageMultiplier;

        public float GetDamageMultiplier(ArmorType armorType) {
            if (damageMultiplier.ContainsKey(armorType) == false) {
                Debug.Log("This damagetype " + Name + " " + ID + " is missing " 
                    + armorType.Name + " " + armorType.ID + " multiplier value.");
                return 1; // if it doesnt contain it take this default value
            }
            return damageMultiplier[armorType];
        }
    }
    public class ArmorType : LanguageVariables {
        public int ID;
        public String spriteBaseName;
    }
}

