using System;
using System.Collections.Generic;

namespace Andja.Model {
    public interface IPopulationLevel {
        PopulationLevelPrototypData Data { get; }
        float Happiness { get; }
        string IconSpriteName { get; }
        int TaxPerPerson { get; }
        int PopulationCount { get; }
        int Level { get; set; }
        void AddPeople(int count);
        PopulationLevel Clone();
        bool Exists();
        void FulfillNeedsAndCalcHappiness();
        List<INeedGroup> GetAllPreviousNeedGroups();
        int GetTaxIncome();
        void Load(ICity city);
        void RegisterNeedUnlock(Action<Need> onNeedUnlock);
        void RemovePeople(int count);
        void SetTaxPercentage(float percentage);
        void UnregisterNeedUnlock(Action<Need> onNeedUnlock);
    }
}