using System.Collections.Generic;

namespace Andja.Model {
    public interface INeed {
        string ID { get; }
        NeedPrototypeData Data { get; }
        NeedGroupPrototypeData Group { get; }
        bool HasToReachPerRoad { get; }
        Item Item { get; }
        string Name { get; }
        int StartLevel { get; }
        int StartPopulationCount { get; }
        NeedStructure[] Structures { get; }
        float[] Uses { get; }

        void CalculateFulfillment(ICity city, IPopulationLevel level);
        Need Clone();
        bool Equals(object obj);
        bool Exists();
        float GetCombinedFulfillment();
        float GetFulfillment(int populationLevel);
        int GetHashCode();
        bool IsItemNeed();
        bool IsSatisfiedThroughStructure(List<NeedStructure> strs);
        bool IsStructureNeed();
    }
}