using System.Collections.Generic;

namespace Andja.Model {
    public interface IHomeStructure {
        bool CanBeUpgraded { get; }
        HomeStructure.CitizenMoods CurrentMood { get; }
        float DecreaseTime { get; }
        HomePrototypeData HomeData { get; }
        float IncreaseTime { get; }
        bool IsAbandoned { get; }
        int MaxLivingSpaces { get; }
        HomeStructure NextLevel { get; }
        int People { get; }
        HomeStructure PrevLevel { get; }

        void CalculateMood();
        Structure Clone();
        void DowngradeHouse();
        List<INeedGroup> GetNeedGroups();
        float GetTaxPercentage();
        bool IsMaxLevel();
        bool IsStructureNeedFulfilled(Need need);
        void OnBuild();
        void OnCityChange(Structure str, ICity old, ICity newOne);
        void OnDestroy();
        void OnUpdate(float deltaTime);
        void OpenExtraUI();
        bool UpgradeHouse();
    }
}