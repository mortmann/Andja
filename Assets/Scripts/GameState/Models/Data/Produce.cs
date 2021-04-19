using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model.Data {
    public class Produce {
        public Item item;
        public float producePerMinute;
        public OutputPrototypData ProducerStructure;
        public Item[] needed;
        public Dictionary<string, List<ProduceRatio>> itemProduceRatios = new Dictionary<string, List<ProduceRatio>>();
        public List<SupplyChain> SupplyChains = new List<SupplyChain>();

        public List<SupplyChain> CalculateSupplyChains() {
            SupplyChain supplyChain = new SupplyChain(this);
            if (needed == null) {
                supplyChain.CalculateCost();
                SupplyChains = new List<SupplyChain> { supplyChain };
                return SupplyChains;
            }
            if (itemProduceRatios.Count != needed.Length) {
                Debug.LogError(ProducerStructure.ID + " has not a valid Supply Chain.");
                return null;
            }
            SupplyChains = GetNewSupplyChains(supplyChain, -1); //-1 because tier gets increased before each split each time.
            foreach (SupplyChain chain in SupplyChains) {
                if (item == null) {
                    Debug.LogError(item + " is null?!");
                }
                if (chain.CheckValid(item) == false) {
                    Debug.LogError("SupplyChain for " + item.ID + " with " + string.Join(", ", chain.ProduceRatio));
                }
                chain.CalculateCost();
                if (chain.cost.requiredFertilites != null) {
                    foreach (Fertility f in chain.cost.requiredFertilites) {
                        PrototypController.Instance.FertilityPrototypeDatas[f.ID].ItemsDependentOnThis.Add(item.ID);
                    }
                }
            }
            return SupplyChains;
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

        internal List<SupplyChain> GetNewSupplyChains(SupplyChain supplyChain, int tier) {
            List<SupplyChain> chains = new List<SupplyChain>();
            if (needed == null) {
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
            List<SupplyChain> toCombineWith = new List<SupplyChain>(itemSupplyChains[needed[0].ID]);
            bool skipCombine = false;
            if (ProducerStructure is ProductionPrototypeData) {
                ProductionPrototypeData ppd = ProducerStructure as ProductionPrototypeData;
                skipCombine = ppd.inputTyp == InputTyp.OR;
            }
            if (skipCombine == false) {
                for (int j = 1; j < needed.Length; j++) {
                    int count = toCombineWith.Count - 1;
                    for (int k = count; k >= 0; k--) {
                        for (int l = 0; l < itemSupplyChains[needed[j].ID].Count; l++) {
                            toCombineWith.Add(toCombineWith[k].Clone().Combine(itemSupplyChains[needed[j].ID][l], tier));
                        }
                    }
                }
                chains = itemSupplyChains[needed[0].ID];
            }
            else {
                SupplyChains = new List<SupplyChain>();
                for (int j = 0; j < needed.Length; j++) {
                    chains.AddRange(itemSupplyChains[needed[j].ID]);
                }
            }
            return chains;
        }

        public override string ToString() {
            return "P-" + ProducerStructure.ID;
        }
    }
}