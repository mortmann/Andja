using System;
using System.Collections;
using System.Collections.Generic;
using GameState.Models.Elements;

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
        
        public Dictionary<Type, ElementData> elements = new Dictionary<Type, ElementData>();
    }
}

