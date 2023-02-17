using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {
    public interface IPlayer {
        int CurrentPopulationLevel { get; }
        bool HasLost { get; }
        bool IsHuman { get; }
        int LastTreasuryChange { get; }
        int MaxPopulationCount { get; }
        int[] MaxPopulationCounts { get; }
        int MaxPopulationLevel { get; set; }
        string Name { get; }
        Statistics Statistics { get; }
        List<TradeRoute> TradeRoutes { get; }
        int TreasuryBalance { get; }
        int TreasuryChange { get; }
        List<Unit>[] unitGroups { get; }
        HashSet<string>[] UnlockedItemNeeds { get; }
        HashSet<string>[] UnlockedStructureNeeds { get; }
        HashSet<string> UnlockedStructures { get; }
        HashSet<string> UnlockedUnits { get; }

        void AddToTreasure(int money);
        void AddTradeRoute(TradeRoute route);
        void AddTreasureChange(int amount);
        void CalculateBalance();
        HashSet<string> GetALLUnlockedStructureNeedsTill(int level);
        List<Need> GetCopyStructureNeeds(int level);
        int GetCurrentPopulation(int level);
        IEnumerable<IIsland> GetIslandList();
        IEnumerable<Unit> GetLandUnits();
        Vector2 GetMainCityPosition();
        int GetPlayerNumber();
        IEnumerable<Ship> GetShipUnits();
        IReadOnlyList<int> GetUnitCityEnterable();
        HashSet<string> GetUnlockedStructureNeeds(int level);
        bool HasEnoughMoney(int buildCost);
        bool HasNeedUnlocked(INeed need);
        bool HasStructureUnlocked(string iD);
        bool HasUnitUnlocked(string ID);
        bool HasUnlockedAllNeeds(int level);
        bool IsCurrent();
        void Load();
        void OnCityCreated(ICity city);
        void OnCityDestroy(ICity city);
        void OnEventCreate(GameEvent ge);
        void OnEventEnded(GameEvent ge);
        void OnStructureAdded(Structure structure);
        void OnStructureLost(Structure structure, IWarfare destroyer);
        void OnUnitCreated(Unit unit);
        void OnUnitDestroy(Unit unit, IWarfare warfare);
        void ReduceTreasure(int money);
        void ReduceTreasureChange(int amount);
        void RegisterCityCreated(Action<ICity> callbackfunc);
        void RegisterCityDestroy(Action<ICity> callbackfunc);
        void RegisterHasLost(Action<Player> callbackfunc);
        void RegisterLostStructure(Action<Structure> callbackfunc);
        void RegisterMaxPopulationCountChange(Action<int, int> callbackfunc);
        void RegisterNeedUnlock(Action<Need> callbackfunc);
        void RegisterNewStructure(Action<Structure> callbackfunc);
        void RegisterStructureNeedUnlock(Action<Need> onStructureNeedUnlock);
        void RegisterStructuresUnlock(Action<IEnumerable<Structure>> onStructuresUnlock);
        void RegisterUnitsUnlock(Action<IEnumerable<Unit>> onUnitsUnlock);
        bool RemoveTradeRoute(TradeRoute route);
        void UnregisterCityCreated(Action<ICity> callbackfunc);
        void UnregisterCityDestroy(Action<ICity> callbackfunc);
        void UnregisterHasLost(Action<Player> callbackfunc);
        void UnregisterLostStructure(Action<Structure> callbackfunc);
        void UnregisterMaxPopulationCountChange(Action<int, int> callbackfunc);
        void UnregisterNeedUnlock(Action<Need> callbackfunc);
        void UnregisterNewStructure(Action<Structure> callbackfunc);
        void UnregisterStructureNeedUnlock(Action<Need> onStructureNeedUnlock);
        void UnregisterStructuresUnlock(Action<IEnumerable<Structure>> onStructuresUnlock);
        void UpdateBalance(float partialPayAmount);
        void UpdateMaxPopulationCount(int level, int count);
    }
}