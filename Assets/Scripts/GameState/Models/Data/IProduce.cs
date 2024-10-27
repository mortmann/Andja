using System.Collections.Generic;

namespace Andja.Model.Data {
    public interface IProduce {
        Item[] Needed { get; }
        OutputPrototypData ProducerStructure { get; }
        Item Item { get; }
        float ProducePerMinute { get; }

        List<SupplyChain> CalculateSupplyChains();
        bool Equals(object obj);
        int GetHashCode();
        HashSet<Fertility> GetNeededFertilities(int level);
        List<SupplyChain> GetNewSupplyChains(SupplyChain supplyChain, int tier);
        string ToString();
    }
}