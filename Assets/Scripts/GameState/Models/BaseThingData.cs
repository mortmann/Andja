using System.Collections;
using System.Collections.Generic;

namespace Andja.Model {
    public class BaseThingData : LanguageVariables {

        public string ID;
        public float maxHealth;
        public int populationLevel = 0;
        public int populationCount = 0;
        public int upkeepCost;
        public int buildCost;
        public Item[] buildingItems;
        public string spriteBaseName;
        public bool canTakeDamage = false;

    }
}

