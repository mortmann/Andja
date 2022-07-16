using System;
using System.Collections.Generic;

namespace Andja.Model {
    public interface INeedGroup {
        NeedGroupPrototypeData Data { get; }
        float ImportanceLevel { get; }
        string Name { get; }
        string ID { get; }
        List<Need> Needs { get; }
        float LastFulfillmentPercentage { get; }
        List<Need> CombinedNeeds { get; }

        void AddNeed(Need need);
        void AddNeeds(IEnumerable<Need> need);
        void CalculateFulfillment(ICity city, PopulationLevel populationLevel);
        NeedGroup Clone();
        void CombineGroup(NeedGroup ng);
        Tuple<float, bool> GetFulfillmentForHome(HomeStructure homeStructure);
        bool HasNeed(Need need);
        bool IsUnlocked();
        void UpdateNeeds(Player player);
    }
}