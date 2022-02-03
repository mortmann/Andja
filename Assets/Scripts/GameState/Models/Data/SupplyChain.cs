using Andja.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model.Data {
    /// <summary>
    /// If it is valid it has a calculated Cost for this to be build.
    /// How many times it needs the next Produces.
    /// </summary>
    public class SupplyChain {
        public bool IsValid { private set; get; }
        private Produce Produce;
        public Dictionary<Produce, float> ProduceRatio = new Dictionary<Produce, float>();
        public List<List<ProduceRatio>> tiers = new List<List<ProduceRatio>>();
        public SupplyChainCost cost;

        public SupplyChain() {
        }

        public SupplyChain(Produce produce) {
            this.Produce = produce;
        }

        public void AddProduce(Produce p, float ratio, int tier) {
            if (tiers.Count <= tier) {
                tiers.Add(new List<ProduceRatio>());
            }
            tiers[tier].Add(new ProduceRatio { Producer = p, Ratio = ratio });
            ProduceRatio.Add(p, ratio);
        }

        internal SupplyChain Clone() {
            return new SupplyChain {
                Produce = Produce,
                cost = cost?.Clone(),
                ProduceRatio = new Dictionary<Produce, float>(ProduceRatio),
                tiers = new List<List<ProduceRatio>>(tiers),
            };
        }

        public void CalculateCost() {
            cost = new SupplyChainCost(Produce.ProducerStructure);
            foreach (Produce p in ProduceRatio.Keys) {
                cost.Add(p.ProducerStructure, ProduceRatio[p]);
            }
        }

        internal SupplyChain Combine(SupplyChain supplyChain, int tier = 0) {
            var keys = new List<Produce>(supplyChain.ProduceRatio.Keys);
            foreach (Produce p in keys) {
                if (ProduceRatio.ContainsKey(p)) {
                    ProduceRatio[p] += supplyChain.ProduceRatio[p];
                }
                else {
                    ProduceRatio[p] = supplyChain.ProduceRatio[p];
                }
            }
            for (int i = 0; i < supplyChain.tiers.Count; i++) {
                if (i < tier)
                    continue;
                if (tiers.Count <= i) {
                    tiers.Add(new List<ProduceRatio>());
                }
                tiers[i].AddRange(supplyChain.tiers[i]);
            }
            return this;
        }

        internal bool CheckValid(Item item) {
            foreach (Produce p in ProduceRatio.Keys) {
                if (p.needed == null)
                    continue;
                if (p.needed.Contains(item)) {
                    IsValid = false;
                    return false;
                }
                if (ProduceRatio[p] > 10f) {
                    Debug.LogWarning(item.ID + " excessive produce ratio -- " + Produce.ProducerStructure.ID + " with needed buildings " + ProduceRatio[p] + "!" +
                        "\n Wanted behaviour? ");
                }
            }
            IsValid = true;
            return true;
        }

        internal bool IsUnlocked(Player player) {
            foreach (var tier in tiers) {
                foreach (var item in tier) {
                    if (player.HasStructureUnlocked(item.Producer.ProducerStructure.ID) == false) {
                        return false;
                    }
                }
            }
            return true; // tiers.All(tier => tier.All(item => player.HasStructureUnlocked(item.Producer.ProducerStructure.ID)))
        }

        public Dictionary<string, int> StructureToBuildForOneRatio() {
            Dictionary<string, int> toBuild = new Dictionary<string, int>();
            toBuild.Add(Produce.ProducerStructure.ID, 1);
            foreach (var tier in tiers) {
                foreach (var item in tier) {
                    if (toBuild.ContainsKey(item.Producer.ProducerStructure.ID) == false)
                        toBuild[item.Producer.ProducerStructure.ID] = 0;
                    toBuild[item.Producer.ProducerStructure.ID] += Mathf.CeilToInt(item.Ratio);
                }
            }
            return toBuild;
        }
    }
}