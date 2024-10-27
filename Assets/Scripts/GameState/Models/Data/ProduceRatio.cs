using System.Collections.Generic;

namespace Andja.Model.Data {
    /// <summary>
    /// Produce need in Ratio by the owning Produce
    /// </summary>
    public struct ProduceRatio {
        public IProduce Producer;
        public float Ratio;

        internal List<SupplyChain> CalculateSupplyChain(SupplyChain supplyChain, int tier) {
            supplyChain.AddProduce(Producer, Ratio, tier);
            return Producer.GetNewSupplyChains(supplyChain, tier);
        }

        public override string ToString() {
            return Producer.ToString() + " (" + Ratio + ")";
        }
    }
}