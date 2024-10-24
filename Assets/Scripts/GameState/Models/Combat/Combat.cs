﻿using Andja.Model;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja {

    public class Combat {
        public List<DamageType> damageTypes;
        public List<ArmorType> armorTypes;
    }
    public class DamageType : LanguageVariables {
            public string ID;
            public string spriteBaseName;
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
        public string ID;
        public string spriteBaseName;
    }
}