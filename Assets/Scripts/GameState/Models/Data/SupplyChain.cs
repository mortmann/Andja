using Andja.Model;
using Andja.Utility;
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
        private IProduce Produce;
        public Dictionary<IProduce, float> ProduceRatio = new Dictionary<IProduce, float>();
        public Dictionary<int, List<ProduceRatio>> tiers = new Dictionary<int, List<ProduceRatio>>();
        public SupplyChainCost cost;

        public SupplyChain() {
        }

        public SupplyChain(IProduce produce) {
            this.Produce = produce;
        }

        public void AddProduce(IProduce p, float ratio, int tier) {
            if (tiers.ContainsKey(tier) == false) {
                tiers[tier] = new List<ProduceRatio>();
            }
            tiers[tier].Add(new ProduceRatio { Producer = p, Ratio = ratio });
            ProduceRatio.Add(p, ratio);
        }

        internal SupplyChain Clone() {
            return new SupplyChain {
                Produce = Produce,
                cost = cost?.Clone(),
                ProduceRatio = new Dictionary<IProduce, float>(ProduceRatio),
                tiers = tiers.ToDictionary(
                    pair => pair.Key,
                    pair => new List<ProduceRatio>(pair.Value)
                )
            };
        }

        public void CalculateCost() {
            cost = new SupplyChainCost(Produce.ProducerStructure);
            foreach (Produce p in ProduceRatio.Keys) {
                cost.Add(p.ProducerStructure, ProduceRatio[p]);
            }
        }

        internal SupplyChain Combine(SupplyChain supplyChain, int tier = 0) {
            var keys = new List<IProduce>(supplyChain.ProduceRatio.Keys);
            foreach (IProduce p in keys) {
                if (ProduceRatio.ContainsKey(p)) {
                    ProduceRatio[p] += supplyChain.ProduceRatio[p];
                }
                else {
                    ProduceRatio[p] = supplyChain.ProduceRatio[p];
                }
            }
            for (int i = 0; i < supplyChain.tiers.Count; i++) {
                if(tiers.ContainsKey(i)) {
                    tiers[i].AddRange(supplyChain.tiers[i]);
                } else {
                    tiers[i] = supplyChain.tiers[i];
                }
            }
            return this;
        }

        internal bool CheckValid(Item item) {
            foreach (Produce p in ProduceRatio.Keys) {
                if (p.Needed == null)
                    continue;
                if (p.Needed.Contains(item)) {
                    IsValid = false;
                    return false;
                }
                if (ProduceRatio[p] > 10f) {
                    Log.PROTOTYPE_WARNING(item.ID + " excessive produce ratio -- " + Produce.ProducerStructure.ID + " with needed buildings " + ProduceRatio[p] + "!" +
                        "\n Wanted behaviour? ");
                }
            }
            return IsValid = true;
        }

        public bool IsUnlocked(IPlayer player) {
            return tiers.All(tier => tier.Value.All(item => player.HasStructureUnlocked(item.Producer.ProducerStructure.ID)));
        }

        public Dictionary<string, int> StructureToBuildForOneRatio() {
            Dictionary<string, int> toBuild = new Dictionary<string, int> {
                { Produce.ProducerStructure.ID, 1 }
            };
            foreach (var tier in tiers.Keys) {
                foreach (var item in tiers[tier]) {
                    if (toBuild.ContainsKey(item.Producer.ProducerStructure.ID) == false)
                        toBuild[item.Producer.ProducerStructure.ID] = 0;
                    toBuild[item.Producer.ProducerStructure.ID] += Mathf.CeilToInt(item.Ratio);
                }
            }
            return toBuild;
        }
    }
}