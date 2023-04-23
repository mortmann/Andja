using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Andja.Model.Data {
    /// <summary>
    /// Item being produced by ProducerStructure with the Items "needed",
    /// which being produced by itemProduceRatios. 
    /// SupplyChains are calculated ways to produce this item.
    /// </summary>
    public class Produce : IProduce {
        public Item Item { get; set; }
        public float ProducePerMinute { get; set; }
        public OutputPrototypData ProducerStructure { get; set; }
        public Item[] Needed { get; set; }
        public Dictionary<string, List<ProduceRatio>> itemProduceRatios = new Dictionary<string, List<ProduceRatio>>();
        public List<SupplyChain> SupplyChains = new List<SupplyChain>();

        public List<SupplyChain> CalculateSupplyChains() {
            SupplyChain supplyChain = new SupplyChain(this);
            if (Needed == null) {
                supplyChain.CalculateCost();
                supplyChain.CheckValid(Item);
                SupplyChains = new List<SupplyChain> { supplyChain };
                return SupplyChains;
            }
            if (itemProduceRatios.Count != Needed.Length) {
                Debug.LogError(ProducerStructure.ID + " has not a valid Supply Chain.");
                return null;
            }
            SupplyChains = GetNewSupplyChains(supplyChain, -1); //-1 because tier gets increased before each split each time.
            foreach (SupplyChain chain in SupplyChains) {
                if (Item == null) {
                    Debug.LogError(Item + " is null?!");
                }
                if (chain.CheckValid(Item) == false) {
                    Debug.LogError("SupplyChain for " + Item.ID + " with " + string.Join(", ", chain.ProduceRatio));
                }
                chain.CalculateCost();
                if (chain.cost.requiredFertilites != null) {
                    foreach (Fertility f in chain.cost.requiredFertilites) {
                        PrototypController.Instance.AddOnFertilityDependingItem(Item, f);
                    }
                }
            }
            return SupplyChains;
        }
        /// <summary>
        /// Returns fertilities that a required by all supply chains
        /// </summary>
        /// <param name="level">Population Level that is getting all required fertilities.</param>
        /// <returns></returns>
        public HashSet<Fertility> GetNeededFertilities(int level) {
            HashSet<Fertility> requiredFertilities = null;
            var chains = SupplyChains.Where(x => x.cost.PopulationLevel <= level);
            if (chains.Count() == 0) {
                return new HashSet<Fertility>();
            }
            foreach (SupplyChain sc in chains) {
                if (sc.cost.requiredFertilites == null)
                    return new HashSet<Fertility>();
                if (requiredFertilities == null)
                    requiredFertilities = new HashSet<Fertility>(sc.cost.requiredFertilites);
                else
                    requiredFertilities.IntersectWith(sc.cost.requiredFertilites);
            }
            //TODO: Problem fixing. What if two different Chains require completly different fertilities (same with resources)
            return requiredFertilities;
        }
        public override bool Equals(object obj) {
            return obj is Produce c && this == c;
        }

        public override int GetHashCode() {
            return ProducerStructure.ID.GetHashCode();
        }

        public static bool operator ==(Produce x, Produce y) {
            return x.ProducerStructure.ID == y.ProducerStructure.ID;
        }

        public static bool operator !=(Produce x, Produce y) {
            return !(x == y);
        }

        public List<SupplyChain> GetNewSupplyChains(SupplyChain supplyChain, int tier) {
            List<SupplyChain> chains = new List<SupplyChain>();
            if (Needed == null) {
                chains.Add(supplyChain);
                return chains;
            }
            Dictionary<string, List<SupplyChain>> itemSupplyChains = new Dictionary<string, List<SupplyChain>>();
            tier++;
            foreach (string s in itemProduceRatios.Keys) {
                itemSupplyChains[s] = new List<SupplyChain>();
                foreach (ProduceRatio ratio in itemProduceRatios[s]) {
                    itemSupplyChains[s].AddRange(ratio.CalculateSupplyChain(supplyChain.Clone(), tier));
                }
            }
            List<SupplyChain> toCombineWith = new List<SupplyChain>(itemSupplyChains[Needed[0].ID]);
            bool skipCombine = false;
            if (ProducerStructure is ProductionPrototypeData) {
                ProductionPrototypeData ppd = ProducerStructure as ProductionPrototypeData;
                skipCombine = ppd.inputTyp == InputTyp.OR;
            }
            if (skipCombine == false) {
                for (int j = 1; j < Needed.Length; j++) {
                    int count = toCombineWith.Count - 1;
                    for (int k = count; k >= 0; k--) {
                        for (int l = 0; l < itemSupplyChains[Needed[j].ID].Count; l++) {
                            toCombineWith.Add(toCombineWith[k].Clone().Combine(itemSupplyChains[Needed[j].ID][l], tier));
                        }
                    }
                }
                chains = itemSupplyChains[Needed[0].ID];
            }
            else {
                SupplyChains = new List<SupplyChain>();
                for (int j = 0; j < Needed.Length; j++) {
                    chains.AddRange(itemSupplyChains[Needed[j].ID]);
                }
            }
            return chains;
        }

        public override string ToString() {
            return "P-" + ProducerStructure.ID;
        }
    }
}