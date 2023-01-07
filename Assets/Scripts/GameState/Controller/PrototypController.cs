using Andja.Model;
using Andja.Model.Data;
using Andja.Model.Generator;
using Andja.Utility;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Andja.Controller {
    /// <summary>
    /// Responsible for all data loading from xml for prototypes, loadouts etc.
    /// Calculates different things like supplychains, buildItems, NeedGroups andso on.
    /// </summary>
    public class PrototypController : MonoBehaviour, IPrototypController {
        public const string GameVersion = "0.1.5"; //TODO: think about this position

        /**
* If adding a new XMLFileType please do these Steps:
*  1. add a Read*new*FromXML Function
*      1.1 create new (if needed) dictionaries
*      1.2 call it also with the ModLoader to enable Mods
*  2. modify the change ReloadLanguageVariables to include it
*      2.1 if the xml file is three deep like structures it must be added to the if
*  3. Create Debug output for it
*/
        public enum XmlFilesTypes { Other, Events, Fertilities, Items, Combat, Units, Structures, Needs, Startingloadouts, Mapgeneration }

        public int NumberOfPopulationLevels => _populationLevelDatas.Count;

        public static IPrototypController Instance;
        public IReadOnlyDictionary<string, Structure> StructurePrototypes => _structurePrototypes;
        public IReadOnlyDictionary<string, Unit> UnitPrototypes => _unitPrototypes;
        public IReadOnlyDictionary<Type, int> StructureTypeToMaxStructureLevel => _structureTypeToMaxStructureLevel;
        public IReadOnlyDictionary<string, StructurePrototypeData> StructurePrototypeDatas => _structurePrototypeDatas;
        public IReadOnlyDictionary<string, NeedPrototypeData> NeedPrototypeDatas => _needPrototypeDatas;
        public IReadOnlyDictionary<string, NeedGroupPrototypeData> NeedGroupDatas => _needGroupDatas;
        public IReadOnlyDictionary<string, NeedGroup> NeedGroups => _idToNeedGroup;
        public IReadOnlyDictionary<string, FertilityPrototypeData> FertilityPrototypeDatas => _fertilityPrototypeDatas;
        public IReadOnlyDictionary<string, UnitPrototypeData> UnitPrototypeDatas => _unitPrototypeDatas;
        public IReadOnlyDictionary<string, ItemPrototypeData> ItemPrototypeDatas => _itemPrototypeDatas;
        public IReadOnlyDictionary<string, DamageType> DamageTypeDatas => _damageTypeDatas;
        public IReadOnlyDictionary<string, EffectPrototypeData> EffectPrototypeDatas => _effectPrototypeDatas;
        public IReadOnlyDictionary<string, ArmorType> ArmorTypeDatas => _armorTypeDatas;
        public IReadOnlyDictionary<string, GameEventPrototypData> GameEventPrototypeDatas => _gameEventPrototypeDatas;
        public IReadOnlyDictionary<string, Item> AllItems => _allItems;
        public IReadOnlyDictionary<string, IslandFeaturePrototypeData> IslandFeaturePrototypeDatas => _islandFeaturePrototypeDatas;
        public IReadOnlyDictionary<Climate, List<FertilityPrototypeData>> AllFertilitiesDatasPerClimate => _allFertilitiesDatasPerClimate;
        public IReadOnlyDictionary<Size, IslandSizeGenerationInfo> IslandSizeToGenerationInfo => _islandSizeToGenerationInfo;
        public IReadOnlyDictionary<Climate, List<ResourceGenerationInfo>> ClimateToResourceGeneration => _climateToResourceGeneration;
        public IReadOnlyDictionary<Climate, List<SpawnStructureGenerationInfo>> SpawnStructureGeneration => _spawnStructureGeneration;
        public IReadOnlyList<string> AllNaturalSpawningStructureIDs => _allNaturalSpawningStructureIDs;
        /// <summary>
        /// Array: For each Level of Populations exists a dictionary
        /// <br>int: People of that population requiered for those Unlocks</br>
        /// <br>Unlocks: contains all needs, structures and units that will be unlocked for the key amount of People</br>
        /// </summary>
        public IReadOnlyDictionary<int, Unlocks>[] LevelCountToUnlocks => _levelCountToUnlocks;

        public WarehouseStructure FirstLevelWarehouse { get; private set; }

        public IReadOnlyDictionary<Climate, List<Fertility>> AllFertilities => _allFertilities;
        public IReadOnlyDictionary<string, Fertility> IdToFertilities => _idToFertilities;
        public IReadOnlyDictionary<int, PopulationLevelPrototypData> PopulationLevelDatas => _populationLevelDatas;
        public IReadOnlyDictionary<int, List<NeedGroup>> PopulationLevelToNeedGroup => _populationLevelToNeedGroup;
        public IReadOnlyDictionary<string, List<Produce>> ItemIDToProduce => _itemIdToProduce;

        public virtual Item[] BuildItems => _buildItems.CloneArray();

        public IReadOnlyList<StartingLoadout> StartingLoadouts => _startingLoadouts;

        private Dictionary<string, Structure> _structurePrototypes;
        private Dictionary<string, Unit> _unitPrototypes;
        private Dictionary<Type, int> _structureTypeToMaxStructureLevel;
        private Dictionary<string, StructurePrototypeData> _structurePrototypeDatas;
        private Dictionary<string, ItemPrototypeData> _itemPrototypeDatas;
        private Dictionary<string, NeedPrototypeData> _needPrototypeDatas;
        private Dictionary<string, FertilityPrototypeData> _fertilityPrototypeDatas;
        private Dictionary<string, UnitPrototypeData> _unitPrototypeDatas;
        private Dictionary<string, WorkerPrototypeData> _workerPrototypeDatas;
        private Dictionary<string, DamageType> _damageTypeDatas;
        private Dictionary<string, EffectPrototypeData> _effectPrototypeDatas;
        private Dictionary<string, GameEventPrototypData> _gameEventPrototypeDatas;
        private Dictionary<string, ArmorType> _armorTypeDatas;
        private Dictionary<int, PopulationLevelPrototypData> _populationLevelDatas;
        private Dictionary<string, NeedGroupPrototypeData> _needGroupDatas;
        private Dictionary<string, NeedGroup> _idToNeedGroup;
        private Dictionary<string, Item> _allItems;
        private Dictionary<Climate, List<SpawnStructureGenerationInfo>> _spawnStructureGeneration;
        private Dictionary<int, List<NeedGroup>> _populationLevelToNeedGroup;
        private Dictionary<Climate, List<Fertility>> _allFertilities;
        private Dictionary<Climate, List<FertilityPrototypeData>> _allFertilitiesDatasPerClimate;
        private Dictionary<string, IslandFeaturePrototypeData> _islandFeaturePrototypeDatas;
        private List<StartingLoadout> _startingLoadouts;
        private Dictionary<string, Fertility> _idToFertilities;
        private Dictionary<Size, IslandSizeGenerationInfo> _islandSizeToGenerationInfo;
        private Dictionary<Climate, List<ResourceGenerationInfo>> _climateToResourceGeneration;
        private ConcurrentDictionary<int, Unlocks>[] _levelCountToUnlocks;
        private ConcurrentDictionary<string, float[]> _buildItemsNeeded;
        private List<string> _allNaturalSpawningStructureIDs;
        public List<ResourceGenerationInfo> ResourceGenerations { get; private set; }
        public List<int>[] AllUnlockPeoplePerLevel;

        /// <summary>
        /// "BuildItems in terms of when something requires it to be created."
        /// </summary>
        public Dictionary<string, int[]> RecommandedBuildSupplyChains { get; private set; }
        public List<Item> MineableItems { get; private set; }
        private static Item[] _buildItems;
        private List<Need> _allNeeds;
        private List<NeedPrototypeData>[] _needsPerLevel;
        public List<Fertility> OrderUnlockFertilities { get; private set; }
        //current valid player prototyp data
        public static PlayerPrototypeData CurrentPlayerPrototypData = new PlayerPrototypeData();
        public static bool HomeRoadsNotNeeded;
        /// <summary>
        /// Item ID to the list of PRODUCE (which contains structure that PRODUCES it and supplychain)
        /// </summary>
        private Dictionary<string, List<Produce>> _itemIdToProduce;

        //TODO: need a way to get this to load in! probably with the rest
        //      of the data thats still needs to be read in like time for money ticks
        public ArmorType StructureArmor => _armorTypeDatas["woodenwall"];
        //TODO: make ai aware of ALL buildable homes -- that would require a lot of multichecks - for now simplified - since only 1 for now anyway
        public HomeStructure BuildableHomeStructure => PopulationLevelDatas[0].HomeStructure;
        public MarketStructure FirstLevelMarket { get; private set; }
        public virtual Dictionary<string, Item> GetCopieOfAllItems() {
            Dictionary<string, Item> items = new Dictionary<string, Item>();
            foreach (string item in _allItems.Keys) {
                string id = item;
                items.Add(id, _allItems[id].Clone());
            }
            return items;
        }

        public List<Need> GetCopieOfAllNeeds() {
            List<Need> needs = new List<Need>();
            foreach (Need item in _allNeeds) {
                needs.Add(item.Clone());
            }
            return needs;
        }

        public Structure GetStructureCopy(string id) {
            if (StructurePrototypes.ContainsKey(id) == false)
                return null;
            return StructurePrototypes[id].Clone();
        }

        public Structure GetStructure(string id) {
            if (StructurePrototypes.ContainsKey(id) == false)
                return null;
            return StructurePrototypes[id];
        }

        public Ship GetPirateShipPrototyp() {
            return (Ship)UnitPrototypes["pirateship"];
        }
        public Ship GetFlyingTraderPrototype() {
            return (Ship)UnitPrototypes["flyingtradeship"];
        }

        public IslandFeaturePrototypeData GetIslandFeaturePrototypeDataForID(string id) {
            if (_islandFeaturePrototypeDatas.ContainsKey(id) == false)
                return null;
            return _islandFeaturePrototypeDatas[id];
        }

        public Structure GetRoadForLevel(int level) {
            return StructurePrototypes.Values.Where(x => x is RoadStructure && x.PopulationLevel == level).First();
        }

        public bool ExistsNeed(Need need) {
            return _allNeeds.Contains(need);
        }

        /// <summary>
        /// Returns a NEW(!) set of PopulationsLevels that are UNIQUE for EACH CITY
        /// FULLY stocked WITH a NEW set of Needs in there correct GROUPS!
        /// CALL only once per CITY creation OR loading
        /// DONT call otherwise because it is pretty memory and cpu heavy!
        /// </summary>
        /// <returns></returns>
        public List<PopulationLevel> GetPopulationLevels(ICity city) {
            List<PopulationLevel> populationLevels = new List<PopulationLevel>();
            PopulationLevel previous = null;
            foreach (PopulationLevelPrototypData item in _populationLevelDatas.Values) {
                PopulationLevel clone = new PopulationLevel(item.LEVEL, city, previous);
                previous = clone;
                populationLevels.Add(clone);
            }
            return populationLevels;
        }

        public ReadOnlyCollection<Need> GetAllNeeds() {
            return new ReadOnlyCollection<Need>(_allNeeds);
        }

        public void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two world controllers.");
            }
            Instance = this;
            ModLoader.LoadMods();
            LoadFromXML();
            StructureSpriteController.LoadSprites();
        }

        public StructurePrototypeData GetStructurePrototypDataForID(string ID) {
            return _structurePrototypeDatas[ID];
        }
        public WorkerPrototypeData GetWorkerPrototypDataForID(string id) {
            return _workerPrototypeDatas[id];
        }

        public bool GameEventExists(string id) {
            return GameEventPrototypeDatas.ContainsKey(id);
        }

        public ItemPrototypeData GetItemPrototypDataForID(string ID) {
            if (_itemPrototypeDatas.ContainsKey(ID) == false) {
                Debug.Log(ID + "missing data!");
                return new ItemPrototypeData() { type = ItemType.Missing };
            }
            return _itemPrototypeDatas[ID];
        }

        public Unlocks GetUnlocksFor(int level, int count) {
            if (_levelCountToUnlocks.Length <= level)
                return null;
            return _levelCountToUnlocks[level].ContainsKey(count) == false ? null : _levelCountToUnlocks[level][count];
        }

        public PopulationLevelPrototypData GetPopulationLevelPrototypDataForLevel(int level) {
            return _populationLevelDatas[level];
        }

        public FertilityPrototypeData GetFertilityPrototypDataForID(string ID) {
            return _fertilityPrototypeDatas[ID];
        }

        public NeedPrototypeData GetNeedPrototypDataForID(string ID) {
            return _needPrototypeDatas[ID];
        }

        public NeedGroupPrototypeData GetNeedGroupPrototypDataForID(string ID) {
            return _needGroupDatas[ID];
        }

        public GameEventPrototypData GetGameEventPrototypDataForID(string ID) {
            return _gameEventPrototypeDatas[ID];
        }

        public List<NeedGroup> GetNeedPrototypDataForLevel(int level) {
            return _populationLevelToNeedGroup[level];
        }

        public EffectPrototypeData GetEffectPrototypDataForID(string id) {
            return _effectPrototypeDatas[id];
        }

        public ICollection<Fertility> GetFertilitiesForClimate(Climate c) {
            if (_allFertilities.ContainsKey(c)) return _allFertilities[c];
            Debug.Log(c);
            return null;
        }

        public int GetNeedCountLevel(int level) {
            return _needsPerLevel[level].Count;
        }

        public void LoadFromXML() {
            if (_allItems != null) {
                return;
            }
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            //SO THAT IT DOESNT USE FUCKIN COMMA AS THE DECIMAL SEPERATOR?!? WHY NOT POINT -Zoidberg
            //Why cant it be both -Fry
            //Good News everyone! Setting it to GB fixes that stupid thing! -Professor
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");
            ReadOtherFromXml(LoadXml(XmlFilesTypes.Other));
            ReadEventsFromXml(LoadXml(XmlFilesTypes.Events));
            ReadFertilitiesFromXml(LoadXml(XmlFilesTypes.Fertilities));
            ReadItemsFromXml(LoadXml(XmlFilesTypes.Items));
            ReadCombatFromXml(LoadXml(XmlFilesTypes.Combat));
            ReadUnitsFromXml(LoadXml(XmlFilesTypes.Units));
            ReadStructuresFromXml(LoadXml(XmlFilesTypes.Structures));
            ReadNeedsFromXml(LoadXml(XmlFilesTypes.Needs));
            ReadStartingLoadoutsFromXmLs(LoadXml(XmlFilesTypes.Startingloadouts));
            ReadMapGenerationInfos(LoadXml(XmlFilesTypes.Mapgeneration));

            string str = "";
            List<Structure> all = new List<Structure>(_structurePrototypes.Values);
            while (all.Count > 0) {
                List<Structure> temp = all.FindAll(x => all[0].GetType() == x.GetType());
                foreach (Structure s in temp) {
                    all.Remove(s);
                }
                str += "    -> " + temp[0].GetType() + " = " + temp.Count + " \n";
            }
            string readInThings = "###Read In Stuff###\n";
            readInThings += ("Read in structures: " + _structurePrototypes.Count + "\n" + str);
            readInThings += ("Read in fertilities: " + _allFertilities.Count + " with all " + _fertilityPrototypeDatas.Count) + "\n";
            readInThings += ("Read in units: " + _unitPrototypes.Count) + "\n";
            readInThings += ("Read in items: " + _allItems.Count) + "\n";
            string needslevel = _populationLevelDatas.Values.Aggregate("", (current, pl)
                                => current + ("[" + pl.LEVEL + ": " + _allNeeds.Count(x => x.StartLevel == pl.LEVEL) + "]"));
            readInThings += ("Read in needs: " + _allNeeds.Count + " (" + needslevel + ")") + "\n";
            readInThings += ("Read in needGroups: " + _needGroupDatas.Count) + "\n";
            readInThings += ("Read in damagetypes: " + _damageTypeDatas.Count) + "\n";
            readInThings += ("Read in armortypes: " + _armorTypeDatas.Count) + "\n";
            readInThings += ("Read in populationLevel: " + _populationLevelDatas.Count) + "\n";
            readInThings += ("Read in effects: " + _effectPrototypeDatas.Count) + "\n";
            readInThings += ("Read in gameevents: " + _gameEventPrototypeDatas.Count) + "\n";
            readInThings += ("###Read in took " + stopwatch.Elapsed.TotalSeconds + "s ###");
            Debug.Log(readInThings);
            //Set it to default so it doesnt interfer with user interface informations
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InstalledUICulture;
            _itemIdToProduce = SupplyChainCalculator.CalculateOptimalProportions();
            _needsPerLevel = NeedCalculator.CalculateNeedStuff();
            CalculatePopulationNeedGroups();
            CalculateUnlocks();
            FirstLevelMarket = _structurePrototypes[GetFirstLevelStructureIDForStructureType(typeof(MarketStructure))] as MarketStructure;
            Debug.Log("###Calculating Prototype-Stuff took " + stopwatch.Elapsed.TotalSeconds + "s ###");
        }

        public DamageType GetWorldDamageType() {
            return _damageTypeDatas["world"];
        }

        private void CalculatePopulationNeedGroups() {
            foreach (int level in _populationLevelDatas.Keys) {
                if (_populationLevelToNeedGroup.ContainsKey(level))
                    _populationLevelDatas[level].needGroupList = _populationLevelToNeedGroup[level];
                else
                    Debug.LogWarning("PopulationLevel " + _populationLevelDatas[level].Name + " " + level + " is missing its own needs!");
            }
        }

        private void CalculateUnlocks() {
            UnlockCalculator unlockCalculator = new UnlockCalculator();
            _levelCountToUnlocks = unlockCalculator.LevelCountToUnlocks;
            _buildItemsNeeded = unlockCalculator.BuildItemsNeeded;
            AllUnlockPeoplePerLevel = unlockCalculator.AllUnlockPeoplePerLevel;
            RecommandedBuildSupplyChains = unlockCalculator.RecommandedBuildSupplyChains;
            OrderUnlockFertilities = unlockCalculator.OrderUnlockFertilities;
        }

        public Unlocks GetNextUnlocks(int populationLevel, int populationCount) {
            return (from item in _levelCountToUnlocks[populationLevel] where item.Key > populationCount select item.Value).FirstOrDefault();
        }
        public int GetMaxStructureLevelForStructureType(Type type) {
            if (_structureTypeToMaxStructureLevel.ContainsKey(type) == false)
                _structureTypeToMaxStructureLevel[type] =
                    new List<Structure>(_structurePrototypes.Values).FindAll(x => type == x.GetType())
                        .OrderByDescending(item => item.PopulationLevel).First().PopulationLevel;
            return _structureTypeToMaxStructureLevel[type];
        }

        public string GetFirstLevelStructureIDForStructureType(Type type) {
            //TODO: optimize this
            return new List<Structure>(_structurePrototypes.Values).FindAll(x => type == x.GetType())
                        .OrderBy(item => item.PopulationLevel).First().ID;
        }

        public UnitPrototypeData GetUnitPrototypDataForID(string id) {
            return _unitPrototypeDatas[id];
        }

        public Unit GetUnitForID(string id) {
            return _unitPrototypes.ContainsKey(id) == false ? null : _unitPrototypes[id];
        }

        private void ReadMapGenerationInfos(string xmlText) {
            _islandSizeToGenerationInfo = new Dictionary<Size, IslandSizeGenerationInfo>();
            _climateToResourceGeneration = new Dictionary<Climate, List<ResourceGenerationInfo>>();
            _islandFeaturePrototypeDatas = new Dictionary<string, IslandFeaturePrototypeData>();
            _spawnStructureGeneration = new Dictionary<Climate, List<SpawnStructureGenerationInfo>>();
            _allNaturalSpawningStructureIDs = new List<string>();
            foreach (Climate climate in Enum.GetValues(typeof(Climate))) {
                _spawnStructureGeneration[climate] = new List<SpawnStructureGenerationInfo>();
                _climateToResourceGeneration[climate] = new List<ResourceGenerationInfo>();
            }
            
            ResourceGenerations = new List<ResourceGenerationInfo>();

            MapGenerationConverter mapGenerationConverter = new MapGenerationConverter(_spawnStructureGeneration, _islandSizeToGenerationInfo,
                _islandFeaturePrototypeDatas, _allNaturalSpawningStructureIDs, _climateToResourceGeneration, ResourceGenerations);

            mapGenerationConverter.ReadFromFile(xmlText);
            ModLoader.LoadXMLs(XmlFilesTypes.Mapgeneration, mapGenerationConverter.ReadFromFile);
            if (_islandFeaturePrototypeDatas.Count > 0) {
                MoonSharp.Interpreter.UserData.RegisterAssembly(); //Set up for exchange of Tile Data
                MoonSharp.Interpreter.UserData.RegisterType<TileType>();
            }
            foreach (IslandFeaturePrototypeData d in IslandFeaturePrototypeData.TempSetUp()) {
                _islandFeaturePrototypeDatas.Add(d.ID, d);
            }
        }

        private void ReadStartingLoadoutsFromXmLs(string xmlText) {
            _startingLoadouts = new List<StartingLoadout>();
            BaseConverter<StartingLoadout> loadoutConverter = new BaseConverter<StartingLoadout>(
                (id) => new StartingLoadout(),
                "startingloadouts/startingloadout",
                (id, data) => _startingLoadouts.Add(data)
                );

            loadoutConverter.ReadFile(xmlText);
            ModLoader.LoadXMLs(XmlFilesTypes.Startingloadouts, loadoutConverter.ReadFile);
        }

        private void ReadEventsFromXml(string file) {
            _effectPrototypeDatas = new Dictionary<string, EffectPrototypeData>();
            _gameEventPrototypeDatas = new Dictionary<string, GameEventPrototypData>();
            EventConverter eventConverter = new EventConverter(_effectPrototypeDatas, _gameEventPrototypeDatas);
            eventConverter.ReadFromFile(file);
            ModLoader.LoadXMLs(XmlFilesTypes.Events, eventConverter.ReadFromFile);
        }

        private void ReadOtherFromXml(string file) {
            _populationLevelDatas = new Dictionary<int, PopulationLevelPrototypData>();
            OtherConverter otherConverter = new OtherConverter(_populationLevelDatas);
            otherConverter.ReadFromFile(file);
            ModLoader.LoadXMLs(XmlFilesTypes.Other, otherConverter.ReadFromFile);
        }

        private void ReadCombatFromXml(string file) {
            _armorTypeDatas = new Dictionary<string, ArmorType>();
            _damageTypeDatas = new Dictionary<string, DamageType>();

            CombatConverter combatConverter = new CombatConverter(_armorTypeDatas, _damageTypeDatas);
            combatConverter.ReadFromFile(file);
            ModLoader.LoadXMLs(XmlFilesTypes.Combat, combatConverter.ReadFromFile);
            Dictionary<ArmorType, float> worldMultiplier = ArmorTypeDatas.Values.ToDictionary<ArmorType, ArmorType, float>(at => at, at => 1);
            //Hardcoded WorldDamage -- We need it and cant change yo
            _damageTypeDatas.Add("world", new DamageType() {
                ID = "world",
                damageMultiplier = worldMultiplier,
            });
        }

        private void ReadItemsFromXml(string file) {
            _allItems = new Dictionary<string, Item>();
            MineableItems = new List<Item>();
            _itemPrototypeDatas = new Dictionary<string, ItemPrototypeData>();

            BaseConverter<ItemPrototypeData> itemConverter = new BaseConverter<ItemPrototypeData>(
                (_) => new ItemPrototypeData(),
                "items/Item",
                (id, data) => {
                    _itemPrototypeDatas[id] = data;
                    _allItems[id] = new Item(id, data);
                });
            itemConverter.ReadFile(file);
            ModLoader.LoadXMLs(XmlFilesTypes.Items, itemConverter.ReadFile);
            _buildItems = _allItems.Values.Where(i => i.Type == ItemType.Build).ToArray();
        }

        private void ReadUnitsFromXml(string file) {
            _unitPrototypes = new Dictionary<string, Unit>();
            _unitPrototypeDatas = new Dictionary<string, UnitPrototypeData>();
            _workerPrototypeDatas = new Dictionary<string, WorkerPrototypeData>();

            UnitConverter unitConverter = new UnitConverter(_unitPrototypes, _unitPrototypeDatas, _workerPrototypeDatas);
            unitConverter.ReadFile(file);
            ModLoader.LoadXMLs(XmlFilesTypes.Units, unitConverter.ReadFile);
        }

        private void ReadFertilitiesFromXml(string file) {
            _allFertilities = new Dictionary<Climate, List<Fertility>>();
            _idToFertilities = new Dictionary<string, Fertility>();
            _allFertilitiesDatasPerClimate = new Dictionary<Climate, List<FertilityPrototypeData>>();
            _fertilityPrototypeDatas = new Dictionary<string, FertilityPrototypeData>();
            BaseConverter<FertilityPrototypeData> fertilityConverter = new BaseConverter<FertilityPrototypeData>(
                (_) => new FertilityPrototypeData(),
                "fertilities/Fertility",
                (id, data) => {
                    _fertilityPrototypeDatas[id] = data;
                    Fertility fertility = new Fertility(id, data);
                    _idToFertilities[id] = fertility;
                    foreach (Climate item in fertility.Climates) {
                        if (_allFertilities.ContainsKey(item) == false)
                            _allFertilities[item] = new List<Fertility>();
                        _allFertilities[item].Add(fertility);

                        if (_allFertilitiesDatasPerClimate.ContainsKey(item) == false)
                            _allFertilitiesDatasPerClimate[item] = new List<FertilityPrototypeData>();
                        _allFertilitiesDatasPerClimate[item].Add(data);
                    }
                });
            fertilityConverter.ReadFile(file);
            ModLoader.LoadXMLs(XmlFilesTypes.Fertilities, fertilityConverter.ReadFile);
        }

        private void ReadNeedsFromXml(string file) {
            _allNeeds = new List<Need>();
            _populationLevelToNeedGroup = new Dictionary<int, List<NeedGroup>>();
            _needPrototypeDatas = new Dictionary<string, NeedPrototypeData>();
            _needGroupDatas = new Dictionary<string, NeedGroupPrototypeData>();
            _idToNeedGroup = new Dictionary<string, NeedGroup>();

            NeedsConverter needsConverter = new NeedsConverter(_idToNeedGroup, _needGroupDatas, _allNeeds, _needPrototypeDatas);
            needsConverter.ReadFromFile(file);
            ModLoader.LoadXMLs(XmlFilesTypes.Needs, needsConverter.ReadFromFile);


            Dictionary<int, List<Need>> levelToNeedList = new Dictionary<int, List<Need>>();
            HomeRoadsNotNeeded = _allNeeds.All(x => x.HasToReachPerRoad == false);
            foreach (int level in levelToNeedList.Keys) {
                List<NeedGroup> ngs = new List<NeedGroup>();
                _populationLevelToNeedGroup.Add(level, ngs);
                foreach (Need need in levelToNeedList[level]) {
                    if (ngs.Exists(x => x.ID == need.Group.ID) == false) {
                        ngs.Add(new NeedGroup(need.Group.ID));
                    }
                    ngs.Find(x => x.ID == need.Group.ID).AddNeed(need.Clone());
                }
            }
        }

        private void ReadStructuresFromXml(string file) {
            _structureTypeToMaxStructureLevel = new Dictionary<Type, int>();
            _structurePrototypes = new Dictionary<string, Structure>();
            _structurePrototypeDatas = new Dictionary<string, StructurePrototypeData>();

            StructureConverter structureConverter = new StructureConverter(_structurePrototypes, _structurePrototypeDatas);
            structureConverter.ReadFile(file);
            ModLoader.LoadXMLs(XmlFilesTypes.Structures, structureConverter.ReadFile);

            FirstLevelWarehouse = _structurePrototypes[GetFirstLevelStructureIDForStructureType(typeof(WarehouseStructure))] as WarehouseStructure;
            HomePrototypeData[] sorted = StructurePrototypes.OfType<HomePrototypeData>()
                .OrderBy(x => x.populationLevel).ToArray();
            for (int i = 0; i < sorted.Length; i++) {
                if (i > 0) {
                    sorted[i].prevLevel = new HomeStructure(sorted[i].ID, sorted[i]);
                }
                else {
                    sorted[i].prevLevel = null;
                }
                if (i < sorted.Length - 1) {
                    sorted[i].nextLevel = new HomeStructure(sorted[i + 1].ID, sorted[i + 1]);
                }
                else {
                    sorted[i].nextLevel = null;
                }
            }
            MineableItems.AddRange(_structurePrototypes.OfType<MineStructure>().SelectMany(m => m.Output));
            MineableItems = MineableItems.GroupBy(x => x.ID).Select(g => g.First()).ToList();
        }

        public void ReloadLanguage() {
            LanguageHandler.ReloadLanguage();
        }

        public string LoadXml(XmlFilesTypes name) {
            string path = System.IO.Path.Combine(ConstantPathHolder.StreamingAssets, GameData.DataLocation, "GameState", name + ".xml");
            return System.IO.File.ReadAllText(path);
        }

        public void OnDestroy() {
            Instance = null;
        }
    }
}