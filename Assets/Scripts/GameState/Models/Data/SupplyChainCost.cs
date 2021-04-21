using Andja.Model;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model.Data {
    /// <summary>
    /// How many things the SupplyChain cost or requires.
    /// </summary>
    public class SupplyChainCost {
        public float TotalBuildCost = 0;
        public float TotalMaintenance = 0;
        private Item[] totalItemCost;
        public int PopulationLevel = 0;
        public Dictionary<string, float> ItemCostTemp = new Dictionary<string, float>();
        public List<Fertility> requiredFertilites;

        public SupplyChainCost() {
        }

        public SupplyChainCost(StructurePrototypeData producerStructure) {
            Add(producerStructure, 1);
        }

        public void Add(StructurePrototypeData structure, float ratio) {
            TotalBuildCost += structure.buildCost * ratio;
            TotalMaintenance += structure.upkeepCost * ratio;
            AddBuildItems(structure.buildingItems, ratio);
            PopulationLevel = Mathf.Max(structure.populationLevel, PopulationLevel);
            if (structure is FarmPrototypeData) {
                Fertility f = ((FarmPrototypeData)structure).growable?.Fertility;
                if (f != null) {
                    if (requiredFertilites == null)
                        requiredFertilites = new List<Fertility>();
                    requiredFertilites.Add(f);
                }
            }
        }

        public Item[] TotalItemCost {
            get {
                if (totalItemCost == null)
                    CalculateTotalItemCost();
                return totalItemCost;
            }
            set => totalItemCost = value;
        }

        private void AddBuildItems(Item[] buildingItems, float ratio) {
            foreach (Item item in buildingItems) {
                if (ItemCostTemp.ContainsKey(item.ID) == false)
                    ItemCostTemp[item.ID] = 0;
                ItemCostTemp[item.ID] += item.count * ratio;
            }
        }

        private void CalculateTotalItemCost() {
            TotalItemCost = new Item[ItemCostTemp.Count];
            int i = 0;
            foreach (string id in ItemCostTemp.Keys) {
                TotalItemCost[i] = new Item(id, Mathf.CeilToInt(ItemCostTemp[id]));
                i++;
            }
        }

        internal SupplyChainCost Clone() {
            return new SupplyChainCost {
                ItemCostTemp = new Dictionary<string, float>(ItemCostTemp),
                TotalBuildCost = TotalBuildCost,
                totalItemCost = totalItemCost,
                PopulationLevel = PopulationLevel,
                TotalMaintenance = TotalMaintenance,
                requiredFertilites = new List<Fertility>(requiredFertilites),
            };
        }
    }
}