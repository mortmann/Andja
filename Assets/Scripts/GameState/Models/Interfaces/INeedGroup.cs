using System;
using System.Collections.Generic;

namespace Andja.Model {
    public interface INeedGroup {
        NeedGroupPrototypeData Data { get; }
        float ImportanceLevel { get; }
        string Name { get; }
        string ID { get; }
        List<INeed> Needs { get; }
        float LastFulfillmentPercentage { get; }
        List<INeed> CombinedNeeds { get; }

        void AddNeed(INeed need);
        void AddNeeds(IEnumerable<INeed> need);
        void CalculateFulfillment(ICity city, IPopulationLevel populationLevel);
        INeedGroup Clone();
        INeedGroup CloneEmptyList();
        void CombineGroup(NeedGroup ng);
        Tuple<float, bool> GetFulfillmentForHome(IHomeStructure homeStructure);
        bool HasNeed(INeed need);
        bool IsUnlocked();
        void UpdateNeeds(IPlayer player);
    }
}