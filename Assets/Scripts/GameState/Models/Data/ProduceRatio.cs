using System.Collections.Generic;

namespace Andja.Model.Data {
    public struct ProduceRatio {
        public Produce Producer;
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