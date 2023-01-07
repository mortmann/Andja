using Andja.Model;
using Andja.Model.Data;
using Andja.Model.Generator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Andja.Controller {
    public interface IPrototypController {
        IReadOnlyDictionary<Climate, List<Fertility>> AllFertilities { get; }
        IReadOnlyDictionary<Climate, List<FertilityPrototypeData>> AllFertilitiesDatasPerClimate { get; }
        IReadOnlyDictionary<string, Item> AllItems { get; }
        IReadOnlyList<string> AllNaturalSpawningStructureIDs { get; }
        IReadOnlyDictionary<string, ArmorType> ArmorTypeDatas { get; }
        HomeStructure BuildableHomeStructure { get; }
        Item[] BuildItems { get; }
        string LoadXml(PrototypController.XmlFilesTypes xml);
        List<Item> MineableItems { get; }
        public List<Fertility> OrderUnlockFertilities { get; }
        public MarketStructure FirstLevelMarket { get; }
        public Dictionary<string, int[]> RecommandedBuildSupplyChains { get; }
        IReadOnlyDictionary<Climate, List<ResourceGenerationInfo>> ClimateToResourceGeneration { get; }
        IReadOnlyDictionary<string, DamageType> DamageTypeDatas { get; }
        IReadOnlyDictionary<string, EffectPrototypeData> EffectPrototypeDatas { get; }
        IReadOnlyDictionary<string, FertilityPrototypeData> FertilityPrototypeDatas { get; }
        WarehouseStructure FirstLevelWarehouse { get; }
        IReadOnlyDictionary<string, GameEventPrototypData> GameEventPrototypeDatas { get; }
        IReadOnlyDictionary<string, Fertility> IdToFertilities { get; }
        IReadOnlyDictionary<string, IslandFeaturePrototypeData> IslandFeaturePrototypeDatas { get; }
        IReadOnlyDictionary<Size, IslandSizeGenerationInfo> IslandSizeToGenerationInfo { get; }
        IReadOnlyDictionary<string, List<Produce>> ItemIDToProduce { get; }
        IReadOnlyDictionary<string, ItemPrototypeData> ItemPrototypeDatas { get; }
        IReadOnlyDictionary<int, Unlocks>[] LevelCountToUnlocks { get; }
        IReadOnlyDictionary<string, NeedGroup> NeedGroups { get; }
        IReadOnlyDictionary<string, NeedPrototypeData> NeedPrototypeDatas { get; }
        IReadOnlyDictionary<string, NeedGroupPrototypeData> NeedGroupDatas { get; }
        int NumberOfPopulationLevels { get; }
        IReadOnlyDictionary<int, PopulationLevelPrototypData> PopulationLevelDatas { get; }
        IReadOnlyDictionary<int, List<NeedGroup>> PopulationLevelToNeedGroup { get; }
        List<ResourceGenerationInfo> ResourceGenerations { get; }
        IReadOnlyDictionary<Climate, List<SpawnStructureGenerationInfo>> SpawnStructureGeneration { get; }
        IReadOnlyList<StartingLoadout> StartingLoadouts { get; }
        ArmorType StructureArmor { get; }
        IReadOnlyDictionary<string, StructurePrototypeData> StructurePrototypeDatas { get; }
        IReadOnlyDictionary<string, Structure> StructurePrototypes { get; }
        IReadOnlyDictionary<Type, int> StructureTypeToMaxStructureLevel { get; }
        IReadOnlyDictionary<string, UnitPrototypeData> UnitPrototypeDatas { get; }
        IReadOnlyDictionary<string, Unit> UnitPrototypes { get; }

        bool ExistsNeed(Need need);
        bool GameEventExists(string id);
        ReadOnlyCollection<Need> GetAllNeeds();
        Dictionary<string, Item> GetCopieOfAllItems();
        List<Need> GetCopieOfAllNeeds();
        EffectPrototypeData GetEffectPrototypDataForID(string id);
        ICollection<Fertility> GetFertilitiesForClimate(Climate c);
        FertilityPrototypeData GetFertilityPrototypDataForID(string ID);
        string GetFirstLevelStructureIDForStructureType(Type type);
        Ship GetFlyingTraderPrototype();
        GameEventPrototypData GetGameEventPrototypDataForID(string ID);
        IslandFeaturePrototypeData GetIslandFeaturePrototypeDataForID(string id);
        ItemPrototypeData GetItemPrototypDataForID(string ID);
        int GetMaxStructureLevelForStructureType(Type type);
        int GetNeedCountLevel(int level);
        NeedGroupPrototypeData GetNeedGroupPrototypDataForID(string ID);
        NeedPrototypeData GetNeedPrototypDataForID(string ID);
        List<NeedGroup> GetNeedPrototypDataForLevel(int level);
        Unlocks GetNextUnlocks(int populationLevel, int populationCount);
        Ship GetPirateShipPrototyp();
        PopulationLevelPrototypData GetPopulationLevelPrototypDataForLevel(int level);
        List<PopulationLevel> GetPopulationLevels(ICity city);
        Structure GetRoadForLevel(int level);
        Structure GetStructure(string id);
        Structure GetStructureCopy(string id);
        StructurePrototypeData GetStructurePrototypDataForID(string ID);
        Unit GetUnitForID(string id);
        UnitPrototypeData GetUnitPrototypDataForID(string id);
        Unlocks GetUnlocksFor(int level, int count);
        WorkerPrototypeData GetWorkerPrototypDataForID(string id);
        DamageType GetWorldDamageType();
        void LoadFromXML();
        void ReloadLanguage();
    }
}