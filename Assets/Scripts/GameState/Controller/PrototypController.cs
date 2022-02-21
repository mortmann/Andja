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
    public class PrototypController : MonoBehaviour {
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
        public enum XMLFilesTypes { other, events, fertilities, items, combat, units, structures, needs, startingloadouts, mapgeneration }

        public int NumberOfPopulationLevels => populationLevelDatas.Count;

        public static PrototypController Instance;
        public IReadOnlyDictionary<string, Structure> StructurePrototypes => structurePrototypes;
        public IReadOnlyDictionary<string, Unit> UnitPrototypes => unitPrototypes;
        public IReadOnlyDictionary<Type, int> StructureTypeToMaxStructureLevel => structureTypeToMaxStructureLevel;
        public IReadOnlyDictionary<string, StructurePrototypeData> StructurePrototypeDatas => structurePrototypeDatas;
        public IReadOnlyDictionary<string, NeedPrototypeData> NeedPrototypeDatas => needPrototypeDatas;
        public IReadOnlyDictionary<string, NeedGroup> NeedGroups => idToNeedGroup;
        public IReadOnlyDictionary<string, FertilityPrototypeData> FertilityPrototypeDatas => fertilityPrototypeDatas;
        public IReadOnlyDictionary<string, UnitPrototypeData> UnitPrototypeDatas => unitPrototypeDatas;
        public IReadOnlyDictionary<string, ItemPrototypeData> ItemPrototypeDatas => itemPrototypeDatas;
        public IReadOnlyDictionary<string, DamageType> DamageTypeDatas => damageTypeDatas;
        public IReadOnlyDictionary<string, EffectPrototypeData> EffectPrototypeDatas => effectPrototypeDatas;
        public IReadOnlyDictionary<string, ArmorType> ArmorTypeDatas => armorTypeDatas;
        public IReadOnlyDictionary<string, GameEventPrototypData> GameEventPrototypeDatas => gameEventPrototypeDatas;
        public IReadOnlyDictionary<string, Item> AllItems => allItems;
        public IReadOnlyDictionary<string, IslandFeaturePrototypeData> IslandFeaturePrototypeDatas => islandFeaturePrototypeDatas;
        public IReadOnlyDictionary<Climate, List<FertilityPrototypeData>> AllFertilitiesDatasPerClimate => allFertilitiesDatasPerClimate;
        public IReadOnlyDictionary<Size, IslandSizeGenerationInfo> IslandSizeToGenerationInfo => islandSizeToGenerationInfo;
        public IReadOnlyDictionary<Climate, List<ResourceGenerationInfo>> ClimateToResourceGeneration => climateToResourceGeneration;
        public IReadOnlyDictionary<Climate, List<SpawnStructureGenerationInfo>> SpawnStructureGeneration => spawnStructureGeneration;
        public IReadOnlyList<string> AllNaturalSpawningStructureIDs => allNaturalSpawningStructureIDs;
        /// <summary>
        /// Array: For each Level of Populations exists a dictionary
        /// <br>int: people of that population requiered for those Unlocks</br>
        /// <br>Unlocks: contains all needs, structures and units that will be unlocked for the key amount of people</br>
        /// </summary>
        public IReadOnlyDictionary<int, Unlocks>[] LevelCountToUnlocks => levelCountToUnlocks;

        public WarehouseStructure FirstLevelWarehouse { get; private set; }

        public IReadOnlyDictionary<Climate, List<Fertility>> AllFertilities => allFertilities;
        public IReadOnlyDictionary<string, Fertility> IdToFertilities => idToFertilities;
        public IReadOnlyDictionary<int, PopulationLevelPrototypData> PopulationLevelDatas => populationLevelDatas;
        public IReadOnlyDictionary<int, List<NeedGroup>> PopulationLevelToNeedGroup => populationLevelToNeedGroup;
        public IReadOnlyDictionary<string, List<Produce>> ItemIDToProduce => itemIDToProduce;

        // SHOULD BE READ ONLY -- cant be done because Unity Error for multiple implementations
        public static Item[] BuildItems => _buildItems;

        public IReadOnlyList<StartingLoadout> StartingLoadouts => _startingLoadouts;

        private Dictionary<string, Structure> structurePrototypes;
        private Dictionary<string, Unit> unitPrototypes;
        private Dictionary<Type, int> structureTypeToMaxStructureLevel;
        private Dictionary<string, StructurePrototypeData> structurePrototypeDatas;
        private Dictionary<string, ItemPrototypeData> itemPrototypeDatas;
        private Dictionary<string, NeedPrototypeData> needPrototypeDatas;
        private Dictionary<string, FertilityPrototypeData> fertilityPrototypeDatas;
        private Dictionary<string, UnitPrototypeData> unitPrototypeDatas;
        private Dictionary<string, WorkerPrototypeData> workerPrototypeDatas;
        private Dictionary<string, DamageType> damageTypeDatas;
        private Dictionary<string, EffectPrototypeData> effectPrototypeDatas;
        private Dictionary<string, GameEventPrototypData> gameEventPrototypeDatas;
        private Dictionary<string, ArmorType> armorTypeDatas;
        private Dictionary<int, PopulationLevelPrototypData> populationLevelDatas;
        private Dictionary<string, NeedGroupPrototypData> needGroupDatas;
        private Dictionary<string, NeedGroup> idToNeedGroup;
        private Dictionary<string, Item> allItems;
        private Dictionary<Climate, List<SpawnStructureGenerationInfo>> spawnStructureGeneration;

        private List<Item> buildItemsList;
        private Dictionary<int, List<NeedGroup>> populationLevelToNeedGroup;
        private Dictionary<Climate, List<Fertility>> allFertilities;
        private Dictionary<Climate, List<FertilityPrototypeData>> allFertilitiesDatasPerClimate;
        private Dictionary<string, IslandFeaturePrototypeData> islandFeaturePrototypeDatas;
        private List<StartingLoadout> _startingLoadouts;
        private Dictionary<string, Fertility> idToFertilities;
        private Dictionary<Size, IslandSizeGenerationInfo> islandSizeToGenerationInfo;
        private Dictionary<Climate, List<ResourceGenerationInfo>> climateToResourceGeneration;
        private ConcurrentDictionary<int, Unlocks>[] levelCountToUnlocks;
        private ConcurrentDictionary<string, float[]> buildItemsNeeded;
        private List<string> allNaturalSpawningStructureIDs;
        public List<ResourceGenerationInfo> ResourceGenerations { get; private set; }
        public List<int>[] AllUnlockPeoplePerLevel;

        /// <summary>
        /// "BuildItems in terms of when something requires it to be created."
        /// </summary>
        public Dictionary<string, int[]> recommandedBuildSupplyChains;
        public List<Item> MineableItems;
        public static Item[] _buildItems;
        private List<Need> allNeeds;
        private List<NeedPrototypeData>[] needsPerLevel;
        public List<Fertility> orderUnlockFertilities;
        //current valid player prototyp data
        internal static PlayerPrototypeData CurrentPlayerPrototypData = new PlayerPrototypeData();
        public static bool HomeRoadsNotNeeded;
        /// <summary>
        /// Item ID to the list of PRODUCE (which contains structure that PRODUCES it and supplychain)
        /// </summary>
        private Dictionary<string, List<Produce>> itemIDToProduce;

        //TODO: need a way to get this to load in! probably with the rest
        //      of the data thats still needs to be read in like time for money ticks
        public ArmorType StructureArmor => armorTypeDatas["woodenwall"];
        //TODO: make ai aware of ALL buildable homes -- that would require a lot of multichecks - for now simplified - since only 1 for now anyway
        public HomeStructure BuildableHomeStructure => PopulationLevelDatas[0].HomeStructure;
        public MarketStructure FirstLevelMarket;
        public Dictionary<string, Item> GetCopieOfAllItems() {
            Dictionary<string, Item> items = new Dictionary<string, Item>();
            foreach (string item in allItems.Keys) {
                string id = item;
                items.Add(id, allItems[id].Clone());
            }
            return items;
        }

        public List<Need> GetCopieOfAllNeeds() {
            List<Need> needs = new List<Need>();
            foreach (Need item in allNeeds) {
                needs.Add(item.Clone());
            }
            return needs;
        }

        internal Structure GetStructureCopy(string id) {
            if (StructurePrototypes.ContainsKey(id) == false)
                return null;
            return StructurePrototypes[id].Clone();
        }

        internal Structure GetStructure(string id) {
            if (StructurePrototypes.ContainsKey(id) == false)
                return null;
            return StructurePrototypes[id];
        }

        internal Ship GetPirateShipPrototyp() {
            return (Ship)UnitPrototypes["pirateship"];
        }
        internal Ship GetFlyingTraderPrototype() {
            return (Ship)UnitPrototypes["flyingtradeship"];
        }

        internal IslandFeaturePrototypeData GetIslandFeaturePrototypeDataForID(string id) {
            if (islandFeaturePrototypeDatas.ContainsKey(id) == false)
                return null;
            return islandFeaturePrototypeDatas[id];
        }

        internal Structure GetRoadForLevel(int level) {
            return StructurePrototypes.Values.Where(x => x is RoadStructure && x.PopulationLevel == level).First();
        }

        internal bool ExistsNeed(Need need) {
            return allNeeds.Contains(need);
        }

        /// <summary>
        /// Returns a NEW(!) set of PopulationsLevels that are UNIQUE for EACH CITY
        /// FULLY stocked WITH a NEW set of Needs in there correct GROUPS!
        /// CALL only once per CITY creation OR loading
        /// DONT call otherwise because it is pretty memory and cpu heavy!
        /// </summary>
        /// <returns></returns>
        public List<PopulationLevel> GetPopulationLevels(City city) {
            List<PopulationLevel> populationLevels = new List<PopulationLevel>();
            PopulationLevel previous = null;
            foreach (PopulationLevelPrototypData item in populationLevelDatas.Values) {
                PopulationLevel clone = new PopulationLevel(item.LEVEL, city, previous);
                previous = clone;
                populationLevels.Add(clone);
            }
            return populationLevels;
        }

        public ReadOnlyCollection<Need> GetAllNeeds() {
            return new ReadOnlyCollection<Need>(allNeeds);
        }

        // Use this for initialization
        private void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two world controllers.");
            }
            Instance = this;
            ModLoader.LoadMods();
            //ModLoader.AvaibleMods();
            LoadFromXML();
            StructureSpriteController.LoadSprites();
        }

        public StructurePrototypeData GetStructurePrototypDataForID(string ID) {
            return structurePrototypeDatas[ID];
        }
        internal WorkerPrototypeData GetWorkerPrototypDataForID(string id) {
            return workerPrototypeDatas[id];
        }

        internal bool GameEventExists(string id) {
            return GameEventPrototypeDatas.ContainsKey(id);
        }

        public ItemPrototypeData GetItemPrototypDataForID(string ID) {
            if (itemPrototypeDatas.ContainsKey(ID) == false) {
                Debug.Log(ID + "missing data!");
                return new ItemPrototypeData() { type = ItemType.Missing };
            }
            return itemPrototypeDatas[ID];
        }

        internal Unlocks GetUnlocksFor(int level, int count) {
            if (levelCountToUnlocks.Length <= level)
                return null;
            if (levelCountToUnlocks[level].ContainsKey(count) == false)
                return null;
            return levelCountToUnlocks[level][count];
        }

        internal PopulationLevelPrototypData GetPopulationLevelPrototypDataForLevel(int level) {
            return populationLevelDatas[level];
        }

        public FertilityPrototypeData GetFertilityPrototypDataForID(string ID) {
            return fertilityPrototypeDatas[ID];
        }

        public NeedPrototypeData GetNeedPrototypDataForID(string ID) {
            return needPrototypeDatas[ID];
        }

        internal NeedGroupPrototypData GetNeedGroupPrototypDataForID(string ID) {
            return needGroupDatas[ID];
        }

        internal GameEventPrototypData GetGameEventPrototypDataForID(string ID) {
            return gameEventPrototypeDatas[ID];
        }

        internal List<NeedGroup> GetNeedPrototypDataForLevel(int level) {
            return populationLevelToNeedGroup[level];
        }

        internal EffectPrototypeData GetEffectPrototypDataForID(string id) {
            return effectPrototypeDatas[id];
        }

        public ICollection<Fertility> GetFertilitiesForClimate(Climate c) {
            if (allFertilities.ContainsKey(c) == false) {
                Debug.Log(c);
                return null;
            }
            return allFertilities[c];
        }

        internal int GetNeedCountLevel(int level) {
            return needsPerLevel[level].Count;
        }

        public void LoadFromXML() {
            if (allItems != null) {
                return;
            }
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            //SO THAT IT DOESNT USE FUCKIN COMMA AS THE DECIMAL SEPERATOR?!? WHY NOT POINT -Zoidberg
            //Why cant it be both -Fry
            //Good News everyone! Setting it to GB fixes that stupid thing! -Professor
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");
            //other
            populationLevelDatas = new Dictionary<int, PopulationLevelPrototypData>();
            ReadOtherFromXML(LoadXML(XMLFilesTypes.other));
            ModLoader.LoadXMLs(XMLFilesTypes.other, ReadOtherFromXML);

            //GAMEEVENTS
            effectPrototypeDatas = new Dictionary<string, EffectPrototypeData>();
            gameEventPrototypeDatas = new Dictionary<string, GameEventPrototypData>();
            ReadEventsFromXML(LoadXML(XMLFilesTypes.events));
            ModLoader.LoadXMLs(XMLFilesTypes.events, ReadEventsFromXML);
            
           
            //fertilities
            allFertilities = new Dictionary<Climate, List<Fertility>>();
            idToFertilities = new Dictionary<string, Fertility>();
            allFertilitiesDatasPerClimate = new Dictionary<Climate, List<FertilityPrototypeData>>();
            fertilityPrototypeDatas = new Dictionary<string, FertilityPrototypeData>();
            ReadFertilitiesFromXML(LoadXML(XMLFilesTypes.fertilities));
            ModLoader.LoadXMLs(XMLFilesTypes.fertilities, ReadFertilitiesFromXML);

            // prototypes of items
            allItems = new Dictionary<string, Item>();
            buildItemsList = new List<Item>();
            MineableItems = new List<Item>();
            itemPrototypeDatas = new Dictionary<string, ItemPrototypeData>();
            ReadItemsFromXML(LoadXML(XMLFilesTypes.items));
            ModLoader.LoadXMLs(XMLFilesTypes.items, ReadItemsFromXML);
            _buildItems = buildItemsList.ToArray();
            buildItemsList = null;

            armorTypeDatas = new Dictionary<string, ArmorType>();
            damageTypeDatas = new Dictionary<string, DamageType>();
            ReadCombatFromXML(LoadXML(XMLFilesTypes.combat));
            ModLoader.LoadXMLs(XMLFilesTypes.combat, ReadCombatFromXML);
            Dictionary<ArmorType, float> worldMultiplier = new Dictionary<ArmorType, float>();
            foreach (ArmorType at in ArmorTypeDatas.Values)
                worldMultiplier.Add(at, 1);
            //Hardcoded WorldDamage -- We need it and cant change yo
            damageTypeDatas.Add("world", new DamageType() {
                ID = "world",
                damageMultiplier = worldMultiplier,
            });

            unitPrototypes = new Dictionary<string, Unit>();
            unitPrototypeDatas = new Dictionary<string, UnitPrototypeData>();
            workerPrototypeDatas = new Dictionary<string, WorkerPrototypeData>();
            ReadUnitsFromXML(LoadXML(XMLFilesTypes.units));
            ModLoader.LoadXMLs(XMLFilesTypes.units, ReadUnitsFromXML);

            // setup all prototypes of structures here
            // load them from the
            structureTypeToMaxStructureLevel = new Dictionary<Type, int>();
            structurePrototypes = new Dictionary<string, Structure>();
            structurePrototypeDatas = new Dictionary<string, StructurePrototypeData>();
            ReadStructuresFromXML(LoadXML(XMLFilesTypes.structures));
            ModLoader.LoadXMLs(XMLFilesTypes.structures, ReadStructuresFromXML);

            //needs
            allNeeds = new List<Need>();
            populationLevelToNeedGroup = new Dictionary<int, List<NeedGroup>>();
            needPrototypeDatas = new Dictionary<string, NeedPrototypeData>();
            needGroupDatas = new Dictionary<string, NeedGroupPrototypData>();
            idToNeedGroup = new Dictionary<string, NeedGroup>();
            ReadNeedsFromXML(LoadXML(XMLFilesTypes.needs));
            ModLoader.LoadXMLs(XMLFilesTypes.needs, ReadNeedsFromXML);

            _startingLoadouts = new List<StartingLoadout>();
            ReadStartingLoadoutsFromXMLs(LoadXML(XMLFilesTypes.startingloadouts));
            ModLoader.LoadXMLs(XMLFilesTypes.startingloadouts, ReadStartingLoadoutsFromXMLs);

            islandSizeToGenerationInfo = new Dictionary<Size, IslandSizeGenerationInfo>();
            climateToResourceGeneration = new Dictionary<Climate, List<ResourceGenerationInfo>>();
            islandFeaturePrototypeDatas = new Dictionary<string, IslandFeaturePrototypeData>();
            spawnStructureGeneration = new Dictionary<Climate, List<SpawnStructureGenerationInfo>>();
            allNaturalSpawningStructureIDs = new List<string>();
            foreach (Climate climate in Enum.GetValues(typeof(Climate))) {
                spawnStructureGeneration[climate] = new List<SpawnStructureGenerationInfo>();
            }
            ResourceGenerations = new List<ResourceGenerationInfo>();
            ReadMapGenerationInfos(LoadXML(XMLFilesTypes.mapgeneration));
            ModLoader.LoadXMLs(XMLFilesTypes.mapgeneration, ReadMapGenerationInfos);
            if (islandFeaturePrototypeDatas.Count > 0) {
                MoonSharp.Interpreter.UserData.RegisterAssembly(); //Set up for exchange of Tile Data
                MoonSharp.Interpreter.UserData.RegisterType<TileType>();
            }

            foreach (IslandFeaturePrototypeData d in IslandFeaturePrototypeData.TempSetUp()) {
                islandFeaturePrototypeDatas.Add(d.ID, d);
            }

            string str = "";
            List<Structure> all = new List<Structure>(structurePrototypes.Values);
            while (all.Count > 0) {
                List<Structure> temp = all.FindAll(x => all[0].GetType() == x.GetType());
                foreach (Structure s in temp) {
                    all.Remove(s);
                }
                str += "    -> " + temp[0].GetType() + " = " + temp.Count + " \n";
            }
            string readInThings = "###Read In Stuff###\n";
            readInThings += ("Read in structures: " + structurePrototypes.Count + "\n" + str);
            readInThings += ("Read in fertilities: " + allFertilities.Count + " with all " + fertilityPrototypeDatas.Count) + "\n";
            readInThings += ("Read in units: " + unitPrototypes.Count) + "\n";
            readInThings += ("Read in items: " + allItems.Count) + "\n";
            string needslevel = "";
            foreach (PopulationLevelPrototypData pl in populationLevelDatas.Values) {
                needslevel += "[" + pl.LEVEL + ": " + allNeeds.Count(x => x.StartLevel == pl.LEVEL) + "]";
            }
            readInThings += ("Read in needs: " + allNeeds.Count + " (" + needslevel + ")") + "\n";
            readInThings += ("Read in needGroups: " + needGroupDatas.Count) + "\n";
            readInThings += ("Read in damagetypes: " + damageTypeDatas.Count) + "\n";
            readInThings += ("Read in armortypes: " + armorTypeDatas.Count) + "\n";
            readInThings += ("Read in populationLevel: " + populationLevelDatas.Count) + "\n";
            readInThings += ("Read in effects: " + effectPrototypeDatas.Count) + "\n";
            readInThings += ("Read in gameevents: " + gameEventPrototypeDatas.Count) + "\n";
            readInThings += ("###Read in took " + stopwatch.Elapsed.TotalSeconds + "s ###");
            Debug.Log(readInThings);
            //Set it to default so it doesnt interfer with user interface informations
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InstalledUICulture;
            CalculateOptimalProportions();
            CalculateNeedStuff();
            CalculatePopulationNeedGroups();
            CalculateUnlocks();
            FirstLevelMarket = structurePrototypes[GetFirstLevelStructureIDForStructureType(typeof(MarketStructure))] as MarketStructure;
            Debug.Log("###Calculating Prototype-Stuff took " + stopwatch.Elapsed.TotalSeconds + "s ###");
        }

        internal DamageType GetWorldDamageType() {
            return damageTypeDatas["world"];
        }

        private void ReadMapGenerationInfos(string xmlText) {
            XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
            xmlDoc.LoadXml(xmlText); // load the file.

            foreach (XmlElement node in xmlDoc.SelectNodes("generationInfos/islandSizes/islandSize")) {
                //SetData<StartingLoadout>(node, ref sl);
                IslandSizeGenerationInfo islandSize = new IslandSizeGenerationInfo();
                SetData<IslandSizeGenerationInfo>((XmlElement)node, ref islandSize);
                Enum.TryParse(node.GetAttribute("size"), true, out Size size);
                islandSizeToGenerationInfo[size] = islandSize;
            }
            foreach (Climate climate in Enum.GetValues(typeof(Climate))) {
                climateToResourceGeneration[climate] = new List<ResourceGenerationInfo>();
            }
            foreach (XmlElement node in xmlDoc.SelectNodes("generationInfos/resources/resource")) {
                ResourceGenerationInfo generationInfo = new ResourceGenerationInfo();
                generationInfo.ID = node.GetAttribute("ID");
                SetData<ResourceGenerationInfo>(node, ref generationInfo);
                ResourceGenerations.Add(generationInfo);
                generationInfo.resourceRange = new Dictionary<Size, Range>();
                foreach (XmlElement child in node["distributionMap"].ChildNodes) {
                    string sizeS = child.GetAttribute("islandSize");
                    Enum.TryParse(sizeS, true, out Size size);
                    Range range = new Range(child["range"]["lower"].GetIntValue(), child["range"]["upper"].GetIntValue());
                    generationInfo.resourceRange[size] = range;
                    if (range.upper > 0) {
                        IslandSizeToGenerationInfo[size].resourceGenerationsInfo.Add(generationInfo);
                    }
                    if (generationInfo.climate == null) {
                        generationInfo.climate = (Climate[])Enum.GetValues(typeof(Climate));
                    }
                }
                foreach (Climate c in generationInfo.climate) {
                    ClimateToResourceGeneration[c].Add(generationInfo);
                }
            }
            foreach (XmlElement node in xmlDoc.SelectNodes("generationInfos/islandFeatures/islandFeature")) {
                IslandFeaturePrototypeData feature = new IslandFeaturePrototypeData();
                feature.ID = node.GetAttribute("ID");
                SetData<IslandFeaturePrototypeData>(node, ref feature);
                islandFeaturePrototypeDatas[feature.ID] = feature;
            }
            foreach (XmlElement node in xmlDoc.SelectNodes("generationInfos/structures/structure")) {
                SpawnStructureGenerationInfo sps = new SpawnStructureGenerationInfo();
                sps.ID = node.GetAttribute("ID");
                SetData<SpawnStructureGenerationInfo>(node, ref sps);
                if(sps.climate != null) {
                    foreach(Climate c in sps.climate) {
                        spawnStructureGeneration[c].Add(sps);
                    }
                } else {
                    foreach (Climate c in Enum.GetValues(typeof(Climate))) {
                        spawnStructureGeneration[c].Add(sps);
                    }
                }
                if(sps.structureType == StructureType.Natural) {
                    allNaturalSpawningStructureIDs.Add(sps.ID);
                }
            }
        }

        private void CalculatePopulationNeedGroups() {
            foreach (int level in populationLevelDatas.Keys) {
                if (populationLevelToNeedGroup.ContainsKey(level))
                    populationLevelDatas[level].needGroupList = populationLevelToNeedGroup[level];
                else
                    Debug.LogWarning("PopulationLevel " + populationLevelDatas[level].Name + " " + level + " is missing its own needs!");
            }
        }

        private void CalculateUnlocks() {
            levelCountToUnlocks = new ConcurrentDictionary<int, Unlocks>[NumberOfPopulationLevels];
            buildItemsNeeded = new ConcurrentDictionary<string, float[]>();
            AllUnlockPeoplePerLevel = new List<int>[NumberOfPopulationLevels];
            for (int i = 0; i < NumberOfPopulationLevels; i++) {
                levelCountToUnlocks[i] = new ConcurrentDictionary<int, Unlocks>();
            }
            var one = Parallel.ForEach(StructurePrototypes.Values, structure => {
                if (levelCountToUnlocks[structure.PopulationLevel].ContainsKey(structure.PopulationCount) == false) {
                    levelCountToUnlocks[structure.PopulationLevel].TryAdd(structure.PopulationCount, new Unlocks(structure.PopulationCount, structure.PopulationLevel));
                }
                levelCountToUnlocks[structure.PopulationLevel].TryGetValue(structure.PopulationCount, out Unlocks value);
                value.structures.Add(structure);
                if (structure is OutputStructure) {
                    if (((OutputStructure)structure).Output != null) {
                        foreach (Item item in ((OutputStructure)structure).Output) {
                            lock (item) {
                                if (item.Data.UnlockLevel <= structure.PopulationLevel) {
                                    item.Data.UnlockLevel = structure.PopulationLevel;
                                    item.Data.UnlockPopulationCount = Mathf.Max(item.Data.UnlockPopulationCount, structure.PopulationCount);
                                }
                            }
                        }
                    }
                    if (structure is GrowableStructure) {
                        if (((GrowableStructure)structure).Fertility != null) {
                            Fertility f = ((GrowableStructure)structure).Fertility;
                            lock (f) {
                                if (f.Data.UnlockLevel <= structure.PopulationLevel) {
                                    f.Data.UnlockLevel = structure.PopulationLevel;
                                    f.Data.UnlockPopulationCount = Mathf.Max(f.Data.UnlockPopulationCount, structure.PopulationCount);
                                }
                            }
                        }
                    }
                }
                if (structure.BuildingItems != null) {
                    foreach (Item item in structure.BuildingItems) {
                        float[] array = new float[NumberOfPopulationLevels];
                        array[structure.PopulationLevel] = item.count;
                        buildItemsNeeded.AddOrUpdate(item.ID, array, (id, oc) => { oc[structure.PopulationLevel] += item.count; return oc; });
                    }
                }
            });
            var two = Parallel.ForEach(UnitPrototypes.Values, unit => {
                if (levelCountToUnlocks[unit.PopulationLevel].ContainsKey(unit.PopulationCount) == false) {
                    levelCountToUnlocks[unit.PopulationLevel].TryAdd(unit.PopulationCount, new Unlocks(unit.PopulationCount, unit.PopulationLevel));
                }
                levelCountToUnlocks[unit.PopulationLevel].TryGetValue(unit.PopulationCount, out Unlocks value);
                value.units.Add(unit);
                if (unit.BuildingItems != null) {
                    foreach (Item item in unit.BuildingItems) {
                        float[] array = new float[NumberOfPopulationLevels];
                        array[unit.PopulationLevel] = item.count;
                        buildItemsNeeded.AddOrUpdate(item.ID, array, (id, oc) => { oc[unit.PopulationLevel] += item.count; return oc; });
                    }
                }
            });
            var three = Parallel.ForEach(allNeeds, need => {
                if (levelCountToUnlocks[need.StartLevel].ContainsKey(need.StartPopulationCount) == false) {
                    levelCountToUnlocks[need.StartLevel].TryAdd(need.StartPopulationCount, new Unlocks(need.StartPopulationCount, need.StartLevel));
                }
                levelCountToUnlocks[need.StartLevel].TryGetValue(need.StartPopulationCount, out Unlocks value);
                value.needs.Add(need);
            });
            while ((one.IsCompleted && two.IsCompleted && three.IsCompleted) == false) {
            }
            for (int i = 0; i < NumberOfPopulationLevels; i++) {
                AllUnlockPeoplePerLevel[i] = new List<int>();
                foreach (int key in levelCountToUnlocks[i].Keys) {
                    AllUnlockPeoplePerLevel[i].Add(key);
                }
                AllUnlockPeoplePerLevel[i].Sort();
            }
            foreach (FertilityPrototypeData fertilityPrototype in fertilityPrototypeDatas.Values) {
                if (fertilityPrototype.ItemsDependentOnThis.Count == 0) {
                    Debug.LogWarning("Fertility " + fertilityPrototype.ID + " is not required by anything! -- Wanted?");
                }
            }
            //TODO: make this make more sense :)
            recommandedBuildSupplyChains = new Dictionary<string, int[]>();
            foreach (string item in buildItemsNeeded.Keys) {
                if (itemIDToProduce.ContainsKey(item) == false)
                    continue;
                recommandedBuildSupplyChains[item] = new int[NumberOfPopulationLevels];
                for (int i = 0; i < NumberOfPopulationLevels; i++) {
                    recommandedBuildSupplyChains[item][i] = Mathf.CeilToInt(buildItemsNeeded[item][i]
                        / (itemIDToProduce[item][0].producePerMinute * 60));
                }
            }
            orderUnlockFertilities = new List<Fertility>(idToFertilities.Values);
            orderUnlockFertilities.RemoveAll(x => x.Data.ItemsDependentOnThis.Count == 0);
            orderUnlockFertilities = orderUnlockFertilities.OrderBy(x => x.Data.UnlockLevel).ThenBy(x => x.Data.UnlockPopulationCount).ToList();
        }
        internal Unlocks GetNextUnlocks(int populationLevel, int populationCount) {
            foreach (var item in levelCountToUnlocks[populationLevel]) {
                if(item.Key > populationCount) {
                    return item.Value;
                }
            }
            return null;
        }

        private void CalculateNeedStuff() {
            needsPerLevel = new List<NeedPrototypeData>[NumberOfPopulationLevels];
            foreach (var pair in needPrototypeDatas) {
                NeedPrototypeData need = pair.Value;
                if (need.structures != null) {
                    int startPopulationCount = int.MaxValue;
                    int populationLevel = int.MaxValue;
                    foreach (NeedStructure str in need.structures) {
                        startPopulationCount = Mathf.Min(startPopulationCount, str.PopulationCount);
                        populationLevel = Mathf.Min(populationLevel, str.PopulationLevel);
                        str.NeedStructureData.SatisfiesNeeds.Add(new Need(pair.Key));
                    }
                    if (need.startLevel < populationLevel 
                        || need.startLevel == populationLevel && need.startPopulationCount < startPopulationCount) {
                        Debug.LogWarning("Need " + need.Name + " is misconfigured to start earlier than supposed. Fixed to unlock time." +
                            "\nCount " + need.startPopulationCount + "->" + startPopulationCount
                            + "\nLevel " + need.startLevel + "->" + populationLevel);
                        need.startPopulationCount = startPopulationCount;
                        need.startLevel = populationLevel;
                    }
                }
                if (need.item != null) {
                    if (need.item.Data.SatisfiesNeeds == null)
                        need.item.Data.SatisfiesNeeds = new List<Need>();
                    need.item.Data.SatisfiesNeeds.Add(new Need(pair.Key));
                    if(need.item.Type != ItemType.Luxury) {
                        Debug.LogWarning("Item " + need.item.ID + " is not marked as luxury good. Fix it, but change it in file.");
                        need.item.Data.type = ItemType.Luxury;
                    }
                    need.produceForPeople = new Dictionary<Produce, int[]>();
                    if(itemIDToProduce.ContainsKey(need.item.ID) == false) {
                        Debug.LogError("itemIDToProduce does not have any production for this need item " + need.item.ID);
                        continue;
                    }
                    int startPopulationCount = int.MaxValue;
                    int populationLevel = int.MaxValue;
                    foreach (Produce produce in itemIDToProduce[need.item.ID]) {
                        StructurePrototypeData str = produce.ProducerStructure;
                        startPopulationCount = Mathf.Min(startPopulationCount, str.populationCount);
                        populationLevel = Mathf.Min(populationLevel, str.populationLevel);
                        need.produceForPeople[produce] = new int[NumberOfPopulationLevels];
                        for (int i = 0; i < NumberOfPopulationLevels; i++) {
                            need.produceForPeople[produce][i] = Mathf.FloorToInt(produce.producePerMinute / need.UsageAmounts[i]);
                        }
                    }
                    if (need.startLevel < populationLevel 
                        || need.startLevel == populationLevel && need.startPopulationCount < startPopulationCount) {
                        Debug.LogWarning("Need " + need.Name + " is misconfigured to start earlier than supposed. Fixed to unlock time." +
                            "\nCount " + need.startPopulationCount + "->" + startPopulationCount
                            + "\nLevel " + need.startLevel + "->" + populationLevel);
                        need.startPopulationCount = startPopulationCount;
                        need.startLevel = populationLevel;
                    }
                }
                if (needsPerLevel[need.startLevel] == null)
                    needsPerLevel[need.startLevel] = new List<NeedPrototypeData>();
                needsPerLevel[need.startLevel].Add(need);
            }
            foreach (Item item in AllItems.Values.Where(x=>x.Type == ItemType.Luxury)) {
                item.Data.TotalUsagePerLevel = new float[NumberOfPopulationLevels];
                for (int i = 0; i < NumberOfPopulationLevels; i++) {
                    if(item.Data.SatisfiesNeeds != null)
                        item.Data.TotalUsagePerLevel[i] = item.Data.SatisfiesNeeds.Sum(x => x.Uses[i]);
                }
            }

        }

        private void ReadStartingLoadoutsFromXMLs(string xmlText) {
            XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
            xmlDoc.LoadXml(xmlText); // load the file.
            foreach (XmlElement node in xmlDoc.SelectNodes("startingloadouts/startingloadout")) {
                StartingLoadout sl = new StartingLoadout();
                SetData<StartingLoadout>(node, ref sl);
                _startingLoadouts.Add(sl);
            }
        }

        private void CalculateOptimalProportions() {
            List<StructurePrototypeData> structures = new List<StructurePrototypeData>(structurePrototypeDatas.Values);
            List<FarmPrototypeData> farms = new List<FarmPrototypeData>(structures.OfType<FarmPrototypeData>());
            List<MinePrototypeData> mines = new List<MinePrototypeData>(structures.OfType<MinePrototypeData>());
            List<ProductionPrototypeData> productions = new List<ProductionPrototypeData>(structures.OfType<ProductionPrototypeData>());
            List<Produce> productionsProduces = new List<Produce>();
            itemIDToProduce = new Dictionary<string, List<Produce>>();
            string produceDebug = "Produce Per Minute\n";
            produceDebug += "##############FARMS##############\n";
            foreach (FarmPrototypeData fpd in farms) {
                foreach (Item outItem in fpd.output) {
                    float ppm = 0;
                    int tileCount = fpd.RangeTileCount;
                    int numGrowablesPerTon = fpd.neededHarvestToProduce;
                    float growtime = fpd.growable != null ? fpd.growable.ProduceTime : 1;
                    float produceTime = fpd.produceTime;
                    float neededWorkerRatio = (float)fpd.maxNumberOfWorker / (float)fpd.neededHarvestToProduce;
                    float workPerWorker = (float)fpd.neededHarvestToProduce / (float)fpd.maxNumberOfWorker;
                    if (fpd.growable == null) {
                        ppm = 60f / (produceTime * fpd.efficiency);
                    }
                    else
                    if (produceTime * fpd.efficiency <= 0 || growtime <= 0) {
                        ppm = 0;
                    }
                    else if (fpd.maxNumberOfWorker * produceTime * fpd.efficiency >= growtime) {
                        ppm = neededWorkerRatio * (60f / produceTime);
                    }
                    else {
                        ppm = Mathf.Min(
                                60f / (workPerWorker * (produceTime * fpd.efficiency)),
                                //not sure if this is correct
                                ((float)tileCount / ((float)numGrowablesPerTon * fpd.maxNumberOfWorker)) * (60f / growtime)
                             );
                    }
                    ppm /= (float)outItem.count;
                    if (ppm == 0)
                        Debug.LogError("Farm " + fpd.ID + " does not produce anything per minute. FIX IT!");
                    produceDebug += fpd.ID + ": " + ppm + "\n";
                    fpd.ProducePerMinute = ppm;
                    Produce p = new Produce {
                        item = outItem,
                        producePerMinute = ppm,
                        ProducerStructure = fpd
                    };
                    p.CalculateSupplyChains();
                    if (itemIDToProduce.ContainsKey(outItem.ID)) {
                        itemIDToProduce[outItem.ID].Add(p);
                    }
                    else {
                        itemIDToProduce.Add(outItem.ID, new List<Produce> { p });
                    }
                    if (fpd.growable?.Fertility != null) {
                        fertilityPrototypeDatas[fpd.growable.Fertility.ID].ItemsDependentOnThis.Add(outItem.ID);
                    }
                }
            }
            produceDebug += "\n##############MINES##############\n";
            foreach (MinePrototypeData mpd in mines) {
                foreach (Item outItem in mpd.output) {
                    float ppm = mpd.produceTime == 0 ? float.MaxValue : outItem.count * (60f / mpd.produceTime);
                    mpd.ProducePerMinute = ppm;
                    Produce p = new Produce {
                        item = outItem,
                        producePerMinute = ppm,
                        ProducerStructure = mpd
                    };
                    p.CalculateSupplyChains();
                    produceDebug += mpd.ID + ": " + ppm + "\n";
                    if (itemIDToProduce.ContainsKey(outItem.ID)) {
                        itemIDToProduce[outItem.ID].Add(p);
                    }
                    else {
                        itemIDToProduce.Add(outItem.ID, new List<Produce> { p });
                    }
                }
            }
            produceDebug += "\n###########PRODUCTION############\n";
            foreach (ProductionPrototypeData ppd in productions) {
                if (ppd.output == null)
                    continue;
                foreach (Item outItem in ppd.output) {
                    float ppm = ppd.produceTime == 0 ? float.MaxValue : outItem.count * (60f / ppd.produceTime);
                    ppd.ProducePerMinute = ppm;
                    Produce p = new Produce {
                        item = outItem,
                        producePerMinute = ppm,
                        ProducerStructure = ppd,
                        needed = ppd.intake
                    };
                    produceDebug += ppd.ID + ": " + ppm + "\n";
                    productionsProduces.Add(p);
                    if (itemIDToProduce.ContainsKey(outItem.ID)) {
                        itemIDToProduce[outItem.ID].Add(p);
                    }
                    else {
                        itemIDToProduce.Add(outItem.ID, new List<Produce> { p });
                    }
                }
            }
            Debug.Log(produceDebug);
            foreach (Produce currentProduce in productionsProduces) {
                if (currentProduce.needed == null)
                    continue;
                foreach (Item need in currentProduce.needed) {
                    if (itemIDToProduce.ContainsKey(need.ID) == false) {
                        Debug.LogWarning("NEEDED ITEM CANNOT BE PRODUCED! -- Wanted beahviour? Item-ID:" + need.ID);
                        continue;
                    }
                    foreach (Produce itemProducer in itemIDToProduce[need.ID]) {
                        float f1 = (((float)need.count * (60f / currentProduce.ProducerStructure.produceTime)));
                        float f2 = (((float)itemProducer.item.count * itemProducer.producePerMinute));
                        if (f2 == 0)
                            continue;
                        float ratio = f1 / f2;
                        if (currentProduce.itemProduceRatios.ContainsKey(need.ID) == false) {
                            currentProduce.itemProduceRatios[need.ID] = new List<ProduceRatio>();
                        }
                        currentProduce.itemProduceRatios[need.ID].Add(new ProduceRatio {
                            Producer = itemProducer,
                            Ratio = ratio,
                        });
                    }
                }
            }
            foreach (Produce currentProduce in productionsProduces) {
                currentProduce.CalculateSupplyChains();
            }
            string proportionDebug = "Proportions";
            foreach (Produce currentProduce in productionsProduces) {
                proportionDebug += "\n" + currentProduce.ProducerStructure.ID + ":";
                foreach (string item in currentProduce.itemProduceRatios.Keys) {
                    proportionDebug += "\n ->" + item;
                    foreach (ProduceRatio pr in currentProduce.itemProduceRatios[item]) {
                        proportionDebug += "\n  # " + pr.Producer.ProducerStructure.ID + "= " + pr.Ratio;
                    }
                }
            }
            Debug.Log(proportionDebug);
            string supplyChains = "SupplyChains";
            foreach (Produce currentProduce in productionsProduces) {
                supplyChains += "\n" + currentProduce.ProducerStructure.ID + "(" + currentProduce.item.ID + "): ";
                foreach (SupplyChain sc in currentProduce.SupplyChains) {
                    supplyChains += "[";
                    for (int i = 0; i < sc.tiers.Count; i++) {
                        supplyChains += (i + 1) + "| " + string.Join(", ", sc.tiers[i]);
                        if (i < sc.tiers.Count - 1)
                            supplyChains += " ";
                    }
                    supplyChains += "]";
                }
            }
            Debug.Log(supplyChains);
            string supplyChainsCosts = "SupplyChainsCosts";
            foreach (Produce currentProduce in productionsProduces) {
                supplyChainsCosts += "\n" + currentProduce.ProducerStructure.ID + "(" + currentProduce.item.ID + "): ";
                foreach (SupplyChain sc in currentProduce.SupplyChains) {
                    supplyChainsCosts += "[";
                    supplyChainsCosts += "TBC " + sc.cost.TotalBuildCost;
                    supplyChainsCosts += " TMC " + sc.cost.TotalMaintenance;
                    supplyChainsCosts += " PL " + sc.cost.PopulationLevel;
                    supplyChainsCosts += " I " + string.Join(", ", (object[])sc.cost.TotalItemCost);
                    if (sc.cost.requiredFertilites != null)
                        supplyChainsCosts += " F " + string.Join(", ", (object[])sc.cost.requiredFertilites.ToArray());
                    supplyChainsCosts += "]";
                }
            }
            Debug.Log(supplyChainsCosts);
        }

        internal int GetMaxStructureLevelForStructureType(Type type) {
            if (structureTypeToMaxStructureLevel.ContainsKey(type) == false)
                structureTypeToMaxStructureLevel[type] =
                    new List<Structure>(structurePrototypes.Values).FindAll(x => type == x.GetType())
                        .OrderByDescending(item => item.PopulationLevel).First().PopulationLevel;
            return structureTypeToMaxStructureLevel[type];
        }

        internal string GetFirstLevelStructureIDForStructureType(Type type) {
            //TODO: optimize this
            return new List<Structure>(structurePrototypes.Values).FindAll(x => type == x.GetType())
                        .OrderBy(item => item.PopulationLevel).First().ID;
        }

        internal UnitPrototypeData GetUnitPrototypDataForID(string id) {
            return unitPrototypeDatas[id];
        }

        internal Unit GetUnitForID(string id) {
            if (unitPrototypes.ContainsKey(id) == false)
                return null;
            return unitPrototypes[id];
        }

        ///////////////////////////////////////
        /// XML LOADING FROM FILE
        ///
        ///////////////////////////////////////
        private void ReadEventsFromXML(string file) {
            XmlDocument xmlDoc = new XmlDocument(); 
            xmlDoc.LoadXml(file); // load the file.
            XmlNodeList listEffect = xmlDoc.SelectNodes("events/Effect");
            if (listEffect != null) {
                foreach (XmlElement node in listEffect) {
                    EffectPrototypeData epd = new EffectPrototypeData();
                    string id = node.GetAttribute("ID");
                    SetData<EffectPrototypeData>(node, ref epd);
                    effectPrototypeDatas[id] = epd;
                }
            }
            XmlNodeList listGameEvent = xmlDoc.SelectNodes("events/GameEvent");
            if (listGameEvent != null) {
                foreach (XmlElement node in listGameEvent) {
                    GameEventPrototypData gepd = new GameEventPrototypData();
                    string id = node.GetAttribute("ID");
                    gepd.ID = id;
                    SetData<GameEventPrototypData>(node, ref gepd);
                    gameEventPrototypeDatas[id] = gepd;
                }
            }
        }

        private void ReadOtherFromXML(string file) {
            XmlDocument xmlDoc = new XmlDocument(); 
            xmlDoc.LoadXml(file); // load the file.
            foreach (XmlElement node in xmlDoc.SelectNodes("Other/PopulationLevels/PopulationLevel")) {
                PopulationLevelPrototypData plpd = new PopulationLevelPrototypData();
                int level = int.Parse(node.GetAttribute("LEVEL"));
                plpd.LEVEL = level;
                SetData<PopulationLevelPrototypData>(node, ref plpd);
                populationLevelDatas[level] = plpd;
            }
        }

        private void ReadCombatFromXML(string file) {
            XmlDocument xmlDoc = new XmlDocument(); 
            xmlDoc.LoadXml(file); // load the file.
            XmlNodeList listArmorType = xmlDoc.SelectNodes("combatTypes/armorType");
            if (listArmorType != null) {
                foreach (XmlElement node in listArmorType) {
                    ArmorType at = new ArmorType();
                    string id = node.GetAttribute("ID");
                    at.ID = id;
                    SetData<ArmorType>(node, ref at);
                    armorTypeDatas[id] = at;
                }
            }
            XmlNodeList listDamageType = xmlDoc.SelectNodes("combatTypes/damageType");
            if (listDamageType != null) {
                foreach (XmlElement node in listDamageType) {
                    DamageType at = new DamageType();
                    string id = node.GetAttribute("ID");
                    at.ID = id;
                    SetData<DamageType>(node, ref at);
                    XmlNode dict = node.SelectSingleNode("damageMultiplier");
                    at.damageMultiplier = new Dictionary<ArmorType, float>();
                    foreach (XmlElement child in dict.ChildNodes) {
                        string armorID = child.GetAttribute("ArmorTyp");
                        if (string.IsNullOrEmpty(armorID))
                            continue;
                        if (float.TryParse(child.InnerText, out float multiplier) == false) {
                            Debug.LogError("ID is not an float for ArmorType ");
                        }
                        at.damageMultiplier[armorTypeDatas[armorID]] = multiplier;
                    }
                    damageTypeDatas[id] = at;
                }
            }
        }

        private void ReadItemsFromXML(string file) {
            XmlDocument xmlDoc = new XmlDocument(); 
            xmlDoc.LoadXml(file); // load the file.
            foreach (XmlElement node in xmlDoc.SelectNodes("items/Item")) {
                ItemPrototypeData ipd = new ItemPrototypeData();
                string id = node.GetAttribute("ID");
                SetData<ItemPrototypeData>(node, ref ipd);

                itemPrototypeDatas[id] = ipd;
                Item item = new Item(id, ipd);

                if (item.Type == ItemType.Build) {
                    buildItemsList.Add(item);
                }
                allItems[id] = item;
            }
        }

        private void ReadUnitsFromXML(string file) {
            XmlDocument xmlDoc = new XmlDocument(); 
            xmlDoc.LoadXml(file); // load the file.
            XmlNodeList listUnit = xmlDoc.SelectNodes("units/unit");
            if (listUnit != null) {
                foreach (XmlElement node in listUnit) {
                    UnitPrototypeData upd = new UnitPrototypeData();
                    string id = node.GetAttribute("ID");
                    SetData<UnitPrototypeData>(node, ref upd);
                    unitPrototypeDatas[id] = upd;
                    unitPrototypes[id] = new Unit(id, upd);
                }
            }
            XmlNodeList listShip = xmlDoc.SelectNodes("units/ship");
            if (listShip != null) {
                foreach (XmlElement node in listShip) {
                    ShipPrototypeData spd = new ShipPrototypeData();
                    string id = node.GetAttribute("ID");
                    SetData<ShipPrototypeData>(node, ref spd);
                    unitPrototypeDatas[id] = spd;
                    unitPrototypes[id] = new Ship(id, spd);
                }
            }
            XmlNodeList listWorker = xmlDoc.SelectNodes("units/worker");
            if (listShip != null) {
                foreach (XmlElement node in listWorker) {
                    WorkerPrototypeData wpd = new WorkerPrototypeData();
                    string id = node.GetAttribute("ID");
                    SetData<WorkerPrototypeData>(node, ref wpd);
                    workerPrototypeDatas[id] = wpd;
                }
            }
        }

        private void ReadFertilitiesFromXML(string file) {
            XmlDocument xmlDoc = new XmlDocument(); 
            xmlDoc.LoadXml(file); // load the file.
            foreach (XmlElement node in xmlDoc.SelectNodes("fertilities/Fertility")) {
                string ID = node.GetAttribute("ID");
                FertilityPrototypeData fpd = new FertilityPrototypeData();
                fpd.ID = ID;
                SetData<FertilityPrototypeData>(node, ref fpd);
                Fertility fer = new Fertility(ID, fpd);
                idToFertilities.Add(fer.ID, fer);
                fertilityPrototypeDatas[ID] = fpd;
                foreach (Climate item in fer.Climates) {
                    if (allFertilities.ContainsKey(item) == false)
                        allFertilities[item] = new List<Fertility>();
                    allFertilities[item].Add(fer);

                    if (allFertilitiesDatasPerClimate.ContainsKey(item) == false)
                        allFertilitiesDatasPerClimate[item] = new List<FertilityPrototypeData>();
                    allFertilitiesDatasPerClimate[item].Add(fpd);
                }
            }
        }

        private void ReadNeedsFromXML(string file) {
            XmlDocument xmlDoc = new XmlDocument(); 
            xmlDoc.LoadXml(file); // load the file.
            XmlNodeList listNeedGroup = xmlDoc.SelectNodes("needs/NeedGroup");
            if (listNeedGroup != null) {
                foreach (XmlElement node in listNeedGroup) {
                    NeedGroupPrototypData ngpd = new NeedGroupPrototypData();
                    string ID = node.GetAttribute("ID");
                    ngpd.ID = ID;
                    SetData<NeedGroupPrototypData>(node, ref ngpd);
                    needGroupDatas[ID] = ngpd;
                    idToNeedGroup[ID] = new NeedGroup(ID);
                }
            }
            Dictionary<int, List<Need>> levelToNeedList = new Dictionary<int, List<Need>>();

            XmlNodeList listNeed = xmlDoc.SelectNodes("needs/Need");
            if (listNeed != null) {
                foreach (XmlElement node in xmlDoc.SelectNodes("needs/Need")) {
                    NeedPrototypeData npd = new NeedPrototypeData();
                    string ID = node.GetAttribute("ID");
                    SetData<NeedPrototypeData>(node, ref npd);
                    needPrototypeDatas[ID] = npd;
                    if (npd.item == null && npd.structures == null)
                        continue;
                    if (npd.structures != null) {
                        foreach (NeedStructure str in npd.structures) {
                            if (npd.startLevel > str.PopulationLevel) {
                                npd.startLevel = str.PopulationLevel;
                            }
                            if (npd.startLevel == str.PopulationLevel) {
                                if (npd.startPopulationCount > str.PopulationCount) {
                                    npd.startPopulationCount = str.PopulationCount;
                                }
                            }
                        }
                    }
                    Need n = new Need(ID, npd);
                    if (idToNeedGroup.ContainsKey(n.Group.ID))
                        idToNeedGroup[n.Group.ID].AddNeed(n.Clone());
                    allNeeds.Add(n);
                    if (levelToNeedList.ContainsKey(npd.startLevel) == false) {
                        levelToNeedList[npd.startLevel] = new List<Need>();
                    }

                    levelToNeedList[npd.startLevel].Add(n.Clone());
                }
            }
            HomeRoadsNotNeeded = allNeeds.All(x => x.HasToReachPerRoad == false);
            foreach (int level in levelToNeedList.Keys) {
                List<NeedGroup> ngs = new List<NeedGroup>();
                populationLevelToNeedGroup.Add(level, ngs);
                foreach (Need need in levelToNeedList[level]) {
                    if (ngs.Exists(x => x.ID == need.Group.ID) == false) {
                        ngs.Add(new NeedGroup(need.Group.ID));
                    }
                    ngs.Find(x => x.ID == need.Group.ID).AddNeed(need.Clone());
                }
            }
        }

        private void ReadStructuresFromXML(string file) {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(file); // load the file.
            ReadRoads(xmlDoc.SelectSingleNode("structures/roads"));
            ReadGrowables(xmlDoc.SelectSingleNode("structures/growables"));
            ReadFarms(xmlDoc.SelectSingleNode("structures/farms"));
            ReadMarketStructures(xmlDoc.SelectSingleNode("structures/markets"));
            ReadProductionStructures(xmlDoc.SelectSingleNode("structures/productions"));
            ReadNeedsStructures(xmlDoc.SelectSingleNode("structures/needstructures"));
            ReadMineStructure(xmlDoc.SelectSingleNode("structures/mines"));
            ReadHomeStructures(xmlDoc.SelectSingleNode("structures/homes"));
            ReadWarehouse(xmlDoc.SelectSingleNode("structures/warehouses"));
            ReadMilitaryStructures(xmlDoc.SelectSingleNode("structures/militarystructures"));
            ReadServiceStructures(xmlDoc.SelectSingleNode("structures/servicestructures"));
        }

        private void ReadServiceStructures(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            foreach (XmlElement node in xmlDoc.SelectNodes("servicestructure")) {
                string ID = node.GetAttribute("ID");

                ServiceStructurePrototypeData sspd = new ServiceStructurePrototypeData();
                sspd.ID = ID;
                //THESE are fix and are not changed for any
                //!not anymore
                SetData<ServiceStructurePrototypeData>(node, ref sspd);
                if(sspd.effectsOnTargets != null)
                foreach (Effect effect in sspd.effectsOnTargets) {
                    effect.Serialize = false;
                }
                //Important is that we set the usageItem count to more than the usage amount
                //(for the case it is more than one ton
                if(node.SelectSingleNode("Usages") != null) {
                    var Nodes = node.SelectSingleNode("Usages").SelectNodes("entry");
                    List<float> usages = new List<float>();
                    List<Item> items = new List<Item>();
                    for (int i = 0; i < Nodes.Count; i++) {
                        XmlNode child = Nodes.Item(i);
                        var attribute = child.Attributes["Item"];
                        if (attribute == null || attribute.Value == null)
                            continue;
                        if (allItems.ContainsKey(attribute.Value) == false)
                            continue;
                        if (float.TryParse(child.InnerText, out float usage) == false)
                            continue;
                        if (usage <= 0)
                            continue;
                        usages.Add(usage);
                        Item item = new Item(attribute.Value) {
                            count = Mathf.Clamp(Mathf.CeilToInt(usage), 1, 100)
                        };
                        items.Add(item);
                    }
                    sspd.usagePerTick = usages.ToArray();
                    sspd.usageItems = items.ToArray();
                }

                structurePrototypeDatas[ID] = sspd;
                structurePrototypes[ID] = new ServiceStructure(ID);
            }
        }

        private void ReadMilitaryStructures(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            foreach (XmlElement node in xmlDoc.SelectNodes("militarystructure")) {
                string ID = node.GetAttribute("ID");
                MilitaryStructurePrototypeData mpd = new MilitaryStructurePrototypeData {
                    ID = ID
                };
                SetData<MilitaryStructurePrototypeData>(node, ref mpd);
                foreach (Unit u in mpd.canBeBuildUnits) {
                    if (u.IsShip) {
                        mpd.canBuildShips = true;
                    }
                }
                structurePrototypeDatas[ID] = mpd;
                structurePrototypes[ID] = new MilitaryStructure(ID, mpd);
            }
        }

        private void ReadRoads(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            foreach (XmlElement node in xmlDoc.SelectNodes("road")) {
                string ID = node.GetAttribute("ID");

                RoadStructurePrototypeData rpd = new RoadStructurePrototypeData {
                    //THESE are fix and are not changed for any road
                    tileWidth = 1,
                    tileHeight = 1,
                    buildTyp = BuildType.Path,
                    structureTyp = StructureTyp.Pathfinding,
                    //!not anymore
                    upkeepCost = 0,
                    buildCost = 25,
                    Name = "Testroad",
                    structureRange = 0,
                };

                rpd.ID = ID;
                SetData<RoadStructurePrototypeData>(node, ref rpd);

                structurePrototypeDatas[ID] = rpd;
                structurePrototypes[ID] = new RoadStructure(ID, rpd);
            }
        }

        private void ReadGrowables(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            foreach (XmlElement node in xmlDoc.SelectNodes("growable")) {
                string ID = node.GetAttribute("ID");

                GrowablePrototypeData gpd = new GrowablePrototypeData {
                    //THESE are fix and should not be changed for any growable
                    forMarketplace = false,
                    maxNumberOfWorker = 0,
                    tileWidth = 1,
                    tileHeight = 1,
                    structureTyp = StructureTyp.Free,
                    buildTyp = BuildType.Drag,
                    buildCost = 50,
                    maxOutputStorage = 1
                };
                gpd.ID = ID;
                SetData<GrowablePrototypeData>(node, ref gpd);
                structurePrototypeDatas[ID] = gpd;
                structurePrototypes[ID] = new GrowableStructure(ID, gpd);
            }
        }

        private void ReadFarms(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            foreach (XmlElement node in xmlDoc.SelectNodes("farm")) {
                string ID = node.GetAttribute("ID");

                FarmPrototypeData fpd = new FarmPrototypeData();
                fpd.ID = ID;

                SetData<FarmPrototypeData>(node, ref fpd);
                if(fpd.growable.ID == "farmland") {
                    //for now hardcoded. maybe gonna change this
                    //but this is just the "empty" setting for growable
                    fpd.growable = null;
                }
                if (fpd.output != null && fpd.output.Length > 0 && fpd.output[0].count == 0) {
                    fpd.output[0].count = 1;
                }
                else {
                }
                structurePrototypeDatas[ID] = fpd;
                structurePrototypes[ID] = new FarmStructure(ID, fpd);
            }
        }

        private void ReadMarketStructures(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            foreach (XmlElement node in xmlDoc.SelectNodes("market")) {
                string ID = node.GetAttribute("ID");
                MarketPrototypData mpd = new MarketPrototypData();
                mpd.ID = ID;
                SetData<MarketPrototypData>(node, ref mpd);
                structurePrototypeDatas[ID] = mpd;
                structurePrototypes[ID] = new MarketStructure(ID, mpd);
            }
        }

        private void ReadProductionStructures(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            foreach (XmlElement node in xmlDoc.SelectNodes("production")) {
                string ID = node.GetAttribute("ID");
                ProductionPrototypeData ppd = new ProductionPrototypeData {
                    //THESE are fix and are not changed for any ProduktionStructure
                    maxOutputStorage = 5, // hardcoded 5 ? need this to change?
                    hasHitbox = true,
                    structureTyp = StructureTyp.Blocking,
                    buildTyp = BuildType.Single,
                    canTakeDamage = true,
                    forMarketplace = true,
                    //!not anymore

                    Name = "TEST Production",
                    maxNumberOfWorker = 1
                };
                ppd.ID = ID;
                SetData<ProductionPrototypeData>(node, ref ppd);
                //DO After loading from file
                structurePrototypeDatas[ID] = ppd;
                structurePrototypes[ID] = new ProductionStructure(ID, ppd);
            }
        }

        private void ReadNeedsStructures(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            foreach (XmlElement node in xmlDoc.SelectNodes("needstructure")) {
                string ID = node.GetAttribute("ID");
                NeedStructurePrototypeData nspd = new NeedStructurePrototypeData();
                nspd.ID = ID;
                SetData<NeedStructurePrototypeData>(node, ref nspd);
                structurePrototypeDatas[ID] = nspd;
                structurePrototypes[ID] = new NeedStructure(ID, nspd);
            }
        }

        private void ReadHomeStructures(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            List<HomePrototypeData> hpds = new List<HomePrototypeData>();
            foreach (XmlElement node in xmlDoc.SelectNodes("home")) {
                string ID = node.GetAttribute("ID");
                HomePrototypeData hpd = new HomePrototypeData {
                    //THESE are fix and are not changed for any HomeStructure
                    tileWidth = 2,
                    tileHeight = 2,
                    buildTyp = BuildType.Drag,
                    structureTyp = StructureTyp.Blocking,
                    structureRange = 0,
                    hasHitbox = true,
                    canTakeDamage = true,
                    upkeepCost = 0
                };
                hpds.Add(hpd);
                hpd.ID = ID;
                SetData<HomePrototypeData>(node, ref hpd);

                structurePrototypeDatas[ID] = hpd;
                HomeStructure hs = new HomeStructure(ID, hpd);
                structurePrototypes[ID] = hs;
                populationLevelDatas[structurePrototypes[ID].PopulationLevel].HomeStructure = hs;

                
            }
            HomePrototypeData[] sorted = hpds.OrderBy(x => x.populationLevel).ToArray();
            for (int i = 0; i < sorted.Length; i++) {
                if(i > 0) {
                    sorted[i].prevLevel = GetStructure(sorted[i - 1].ID) as HomeStructure;
                } else {
                    sorted[i].prevLevel = null;
                }
                if (i < sorted.Length - 1) {
                    sorted[i].nextLevel = GetStructure(sorted[i + 1].ID) as HomeStructure; 
                }
                else {
                    sorted[i].nextLevel = null;
                }
            }

        }

        private void ReadWarehouse(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            foreach (XmlElement node in xmlDoc.SelectNodes("warehouse")) {
                string ID = node.GetAttribute("ID");
                WarehousePrototypData wpd = new WarehousePrototypData {
                    contactRange = 6.3f,
                    buildTyp = BuildType.Single,
                    hasHitbox = true,
                    canTakeDamage = true,
                    structureRange = 18,
                    tradeItemCount = 3,
                    tileWidth = 3,
                    tileHeight = 3,
                    Name = "warehouse",
                    buildCost = 500,
                    upkeepCost = 10,
                };

                wpd.ID = ID;
                SetData<WarehousePrototypData>(node, ref wpd);
                structurePrototypeDatas[ID] = wpd;
                structurePrototypes[ID] = new WarehouseStructure(ID, wpd);

                if (FirstLevelWarehouse == null || 
                    wpd.populationLevel < FirstLevelWarehouse.PopulationLevel && wpd.populationCount < FirstLevelWarehouse.PopulationCount) {
                    FirstLevelWarehouse = (WarehouseStructure)structurePrototypes[ID];
                }
            }
        }

        private void ReadMineStructure(XmlNode xmlDoc) {
            if (xmlDoc == null)
                return;
            foreach (XmlElement node in xmlDoc.SelectNodes("mine")) {
                string ID = node.GetAttribute("ID");
                MinePrototypeData mpd = new MinePrototypeData();
                mpd.ID = ID;
                SetData<MinePrototypeData>(node, ref mpd);
                MineableItems.AddRange(mpd.output);
                structurePrototypeDatas[ID] = mpd;
                structurePrototypes[ID] = new MineStructure(ID, mpd);
            }
        }

        private void SetData<T>(XmlElement node, ref T data) {
            FieldInfo[] fields = typeof(T).GetFields();
            HashSet<String> langs = new HashSet<String>();
            if (typeof(LanguageVariables).IsAssignableFrom(typeof(T))) {
                foreach (FieldInfo f in typeof(LanguageVariables).GetFields()) {
                    langs.Add(f.Name);
                }
            }
            foreach (FieldInfo fi in fields) {
                if (Attribute.IsDefined(fi, typeof(IgnoreAttribute)))
                    continue;
                XmlNode currentNode = node.SelectSingleNode(fi.Name);
                if (langs.Contains(fi.Name)) {
                    if (currentNode == null) {
                        //TODO activate this warning when all data is correctly created
                        //				Debug.LogWarning (fi.Name + " selected language not avaible!");
                        continue;
                    }
                    XmlNode textNode = currentNode.SelectSingleNode("entry[@lang='" + UILanguageController.selectedLanguage.ToString() + "']");
                    if (textNode != null) {
                        string text = ReplacePlaceHolders(data, textNode.InnerXml);
                        fi.SetValue(data, Convert.ChangeType(text, fi.FieldType));
                    }
                    continue;
                }
                if (currentNode != null) {
                    if (fi.FieldType == typeof(Item)) {
                        fi.SetValue(data, NodeToItem(currentNode));
                        continue;
                    }
                    if (fi.FieldType == typeof(Item[])) {
                        List<Item> items = new List<Item>();
                        foreach (XmlNode item in currentNode.ChildNodes) {
                            items.Add(NodeToItem(item));
                        }
                        fi.SetValue(data, items.ToArray());
                        continue;
                    }
                    if (fi.FieldType.IsSubclassOf(typeof(Structure))) {
                        fi.SetValue(data, NodeToStructure(currentNode));
                        continue;
                    }
                    if (fi.FieldType.IsArray && fi.FieldType.GetElementType().IsSubclassOf(typeof(Structure)) 
                        || fi.FieldType == (typeof(Structure[]))) {
                        Array items = (Array)Activator.CreateInstance(fi.FieldType, currentNode.ChildNodes.Count);
                        for (int i = 0; i < currentNode.ChildNodes.Count; i++) {
                            items.SetValue(NodeToStructure(currentNode.ChildNodes[i]), i);
                        }
                        fi.SetValue(data, items);
                        continue;
                    }
                    if (fi.FieldType.IsArray && fi.FieldType.GetElementType().IsSubclassOf(typeof(string))
                        || fi.FieldType == (typeof(string[]))) {
                        Array items = (Array)Activator.CreateInstance(fi.FieldType, currentNode.ChildNodes.Count);
                        for (int i = 0; i < currentNode.ChildNodes.Count; i++) {
                            items.SetValue(currentNode.ChildNodes[i].InnerText, i);
                        }
                        fi.SetValue(data, items);
                        continue;
                    }
                    if (fi.FieldType == typeof(NeedGroupPrototypData)) {
                        fi.SetValue(data, NodeToNeedGroupPrototypData(currentNode));
                        continue;
                    }
                    if (fi.FieldType == typeof(ArmorType)) {
                        fi.SetValue(data, NodeToArmorType(currentNode));
                        continue;
                    }
                    if (fi.FieldType == typeof(DamageType)) {
                        fi.SetValue(data, NodeToDamageType(currentNode));
                        continue;
                    }
                    if (fi.FieldType == typeof(Fertility)) {
                        fi.SetValue(data, NodeToFertility(currentNode));
                        continue;
                    }
                    if (fi.FieldType.IsSubclassOf(typeof(Unit))) {
                        fi.SetValue(data, NodeToUnit(currentNode));
                        continue;
                    }
                    if (fi.FieldType.IsSubclassOf(typeof(Unit[])) || fi.FieldType == (typeof(Unit[]))) {
                        List<Unit> items = new List<Unit>();
                        foreach (XmlNode item in currentNode.ChildNodes) {
                            items.Add(NodeToUnit(item));
                        }
                        fi.SetValue(data, items.ToArray());
                        continue;
                    }
                    if (fi.FieldType == (typeof(Effect[]))) {
                        List<Effect> items = new List<Effect>();
                        foreach (XmlNode item in currentNode.ChildNodes) {
                            items.Add(NodeToEffect(item));
                        }
                        fi.SetValue(data, items.ToArray());
                        continue;
                    }
                    if (fi.FieldType == (typeof(float[]))) {
                        List<float> items = new List<float>(currentNode.ChildNodes.Count);
                        foreach (XmlNode item in currentNode.ChildNodes) {
                            int id = int.Parse(item.Attributes[0].InnerXml);
                            items.Insert(id, float.Parse(item.InnerXml));
                        }
                        fi.SetValue(data, items.ToArray());
                        continue;
                    }
                    if (fi.FieldType.IsEnum) {
                        fi.SetValue(data, Enum.Parse(fi.FieldType, currentNode.InnerXml, true));
                        continue;
                    }
                    if (fi.FieldType.IsArray && fi.FieldType.GetArrayRank() == 1 && fi.FieldType.GetElementType().IsEnum) {
                        var listType = typeof(List<>);
                        var constructedListType = listType.MakeGenericType(fi.FieldType.GetElementType());
                        var list = (IList)Activator.CreateInstance(constructedListType);
                        foreach (XmlNode item in currentNode.ChildNodes) {
                            if (item.Name != fi.FieldType.GetElementType().Name) {
                                continue;
                            }
                            if (fi.DeclaringType == typeof(TileType)) {
                                if(item.Name == "BuildLand") { // shortcut to make it easy to include all buildable land
                                    foreach(TileType tt in Tile.BuildLand)
                                        list.Add(tt);
                                    continue;
                                }
                            }
                            list.Add(Enum.Parse(fi.FieldType.GetElementType(), item.InnerXml, true));
                        }
                        Array enumArray = Array.CreateInstance(fi.FieldType.GetElementType(), list.Count);
                        list.CopyTo(enumArray, 0);
                        fi.SetValue(data, Convert.ChangeType(enumArray, fi.FieldType));
                        continue;
                    }
                    if (fi.FieldType.IsArray && fi.FieldType.GetArrayRank() == 2 && fi.FieldType.GetElementType() == typeof(TileType?)) {
                        int firstLength = 0;
                        int secondLength = 0;

                        if (int.TryParse(currentNode.Attributes.GetNamedItem("length").Value, out firstLength) == false) {
                            continue;
                        }
                        var listType = typeof(List<>);
                        var constructedfirstListType = listType.MakeGenericType(fi.FieldType.GetElementType().MakeArrayType());
                        var firstlist = (IList)Activator.CreateInstance(constructedfirstListType);
                        foreach (XmlNode item in currentNode.ChildNodes) {
                            XmlNode len2 = item.Attributes.GetNamedItem("length");
                            if (len2 == null || int.TryParse(len2.Value, out secondLength) == false) {
                                continue;
                            }
                            var constructedSecondListType = listType.MakeGenericType(fi.FieldType.GetElementType());
                            var secondlist = (IList)Activator.CreateInstance(constructedSecondListType);
                            string[] singleValues = item.InnerXml.Split(',');
                            if (singleValues.Length < secondLength) {
                                continue;
                            }
                            foreach (string single in singleValues) {
                                //own try parse because in needs non nullable
                                try {
                                    secondlist.Add(Enum.Parse(typeof(TileType), single.Trim(), true));
                                }
                                catch {
                                    secondlist.Add(null);
                                }
                            }
                            Array enumArray = Array.CreateInstance(fi.FieldType.GetElementType(), secondLength);
                            secondlist.CopyTo(enumArray, 0);
                            firstlist.Add(enumArray);
                        }
                        Array twoDimEnumArray = Array.CreateInstance(fi.FieldType.GetElementType(), firstLength, secondLength);
                        for (int i = 0; i < firstlist.Count; i++) {
                            for (int j = 0; j < ((TileType?[])firstlist[i]).Length; j++) {
                                twoDimEnumArray.SetValue(((TileType?[])firstlist[i])[j], i, j);
                            }
                        }
                        fi.SetValue(data, Convert.ChangeType(twoDimEnumArray, fi.FieldType));
                        continue;
                    }
                    if (fi.FieldType == typeof(Dictionary<ArmorType, float>)) {
                        // this will get set in load xml directly and not here!
                        continue;
                    }
                    if (fi.FieldType == typeof(Range)) {
                        fi.SetValue(data, new Range(currentNode["lower"].GetIntValue(), currentNode["upper"].GetIntValue()));
                        continue;
                    }
                    if (fi.FieldType == typeof(Dictionary<Target, List<int>>)) {
                        Dictionary<Target, List<int>> range = new Dictionary<Target, List<int>>();
                        foreach (XmlNode child in currentNode.ChildNodes) {
                            Target target = Target.World;
                            if (child.Attributes[0] == null)
                                continue;
                            if (Enum.TryParse<Target>(child.Attributes[0].InnerXml, true, out target) == false)
                                continue;
                            String[] ids = child.InnerXml.Split(',');
                            if (ids.Length == 0) {
                                continue;
                            }
                            range.Add(target, new List<int>());
                            foreach (String stringid in ids) {
                                int id = -1;
                                int.TryParse(stringid, out id);
                                if (id == -1)
                                    continue;
                                range[target].Add(id);
                            }
                        }
                        //clean up empty target groups
                        List<Target> targets = new List<Target>(range.Keys);
                        foreach (Target t in targets) {
                            if (range[t].Count == 0)
                                targets.Remove(t);
                        }
                        //only if it has stuff we need to set it
                        if (range.Count > 0)
                            fi.SetValue(data, range);
                        continue;
                    }
                    if (fi.FieldType == typeof(TargetGroup)) {
                        List<Target> targets = new List<Target>();
                        foreach (XmlNode child in currentNode.ChildNodes) {
                            if (Enum.TryParse(child.InnerXml, true, out Target target) == false)
                                continue;
                            targets.Add(target);
                        }
                        fi.SetValue(data, new TargetGroup(targets));
                        continue;
                    }
                    if(fi.FieldType == typeof(Dictionary<Climate, string[]>)) {
                        Dictionary<Climate, string[]> climToString = new Dictionary<Climate, string[]>();
                        foreach (XmlNode child in currentNode.ChildNodes) {
                            if (Enum.TryParse(child.Attributes[0].InnerXml, true, out Climate climate) == false)
                                continue;
                            climToString[climate] = child.InnerXml.Split(';');
                        }
                        fi.SetValue(data, climToString);
                        continue;
                    }
                    try {
                        fi.SetValue(data, Convert.ChangeType(currentNode.InnerXml, fi.FieldType, System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch {
                        Debug.Log(data.ToString() + " -> " + fi.Name + " is faulty!");
                    }
                }
            }
        }

        private string ReplacePlaceHolders<T>(T data, string text) {
            if (text.Contains("$") == false)
                return text;
            string[] splits = text.Split('$');
            for (int i = 0; i < splits.Length - 1; i += 2) {
                string[] replaceSplit = splits[i + 1].Split(' ');
                string replace = replaceSplit[0];
                string replaceWith;
                if (replace.Contains('.')) {
                    string[] subgetsplits = replace.Split('.');
                    replaceWith = GetFieldString(data, 0, subgetsplits);
                }
                else {
                    replaceWith = GetFieldString(data, 0, replace);
                }
                replaceSplit[0] = replaceWith;
                splits[i + 1] = string.Join(" ", replaceSplit);
            }
            return string.Join("", splits);
        }

        private readonly BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private string GetFieldString(object data, int index, params string[] fields) {
            Type dataType = data.GetType();
            if (typeof(IEnumerable).IsAssignableFrom(dataType)) {
                List<string> strings = new List<string>();
                foreach (object o in data as IEnumerable) {
                    strings.Add(GetFieldString(o, index, fields));
                }
                if (strings.Count == 1)
                    return strings[0];
                string last = strings[strings.Count - 1];
                strings.RemoveAt(strings.Count - 1);
                return string.Join(", ", strings) + " " + GetLocalisedAnd() + " " + last;
            }
            if (fields.Length - 1 == index) {
                var field = dataType.GetField(fields[index], flags)?.GetValue(data);
                if (field == null)
                    field = dataType.GetProperty(fields[index], flags)?.GetValue(data);
                return field.ToString();
            }
            return GetFieldString(dataType.GetField(fields[index], flags)?.GetValue(data), ++index, fields);
        }

        private string GetLocalisedAnd() {
            return UILanguageController.Instance.GetStaticVariables(StaticLanguageVariables.And);
        }

        private Effect NodeToEffect(XmlNode item) {
            string id = item.InnerXml;
            if (String.IsNullOrEmpty(id)) {
                return null;//not needed
            }
            if (effectPrototypeDatas.ContainsKey(id) == false) {
                Debug.LogError("ID was not created before the depending DamageType! " + id);
                return null;
            }
            return new Effect(id);
        }

        private object NodeToDamageType(XmlNode n) {
            string id = n.InnerXml;

            if (String.IsNullOrEmpty(id)) {
                return null;//not needed
            }
            if (damageTypeDatas.ContainsKey(id) == false) {
                Debug.LogError("ID was not created before the depending DamageType! " + id);
                return null;
            }
            return damageTypeDatas[id];
        }

        private object NodeToNeedGroupPrototypData(XmlNode n) {
            string id = n.InnerXml;

            if (String.IsNullOrEmpty(id)) {
                return null;//not needed
            }
            if (needGroupDatas.ContainsKey(id) == false) {
                Debug.LogError("ID was not created before the depending NeedGroup! " + id);
                return null;
            }
            return needGroupDatas[id];
        }

        private object NodeToArmorType(XmlNode n) {
            string id = n.InnerXml;

            if (String.IsNullOrEmpty(id)) {
                return null;//not needed
            }
            if (armorTypeDatas.ContainsKey(id) == false) {
                Debug.LogError("ID was not created before the depending ArmorType! " + id);
                return null;
            }
            return armorTypeDatas[id];
        }

        private Item NodeToItem(XmlNode n) {
            string id = n.Attributes["ID"].Value;
            if (allItems.ContainsKey(id) == false) {
                Debug.LogError("ITEM ID was not created! " + id + " (" + n.ParentNode.Name + ")");
                return null;
            }
            Item clone = allItems[id].Clone();
            if (n.SelectSingleNode("count") != null) {
                if (int.TryParse(n.SelectSingleNode("count").InnerXml, out int count) == false) {
                    Debug.LogError("Count is not an int");
                    return null;
                }
                clone.count = Mathf.Abs(count);
            }
            return clone;
        }

        private Unit NodeToUnit(XmlNode n) {
            string id = n.InnerXml;
            if (String.IsNullOrEmpty(id)) {
                return null;//not needed
            }

            if (unitPrototypes.ContainsKey(id) == false) {
                Debug.LogError("ID was not created before the depending Unit! " + id);
                return null;
            }
            return unitPrototypes[id];
        }

        private Structure NodeToStructure(XmlNode n) {
            string id = n.InnerText;
            if (String.IsNullOrEmpty(id)) {
                return null;//not needed
            }
            if (structurePrototypes.ContainsKey(id) == false) {
                Debug.LogError("ID was not created before the depending Structure! " + id);
                return null;
            }
            return structurePrototypes[id];
        }

        private Fertility NodeToFertility(XmlNode n) {
            string id = n.InnerXml;
            if (String.IsNullOrEmpty(id)) {
                return null;//not needed
            }
            if (idToFertilities.ContainsKey(id) == false) {
                Debug.LogError("ID was not created before the depending Fertility! " + id);
                return null;
            }
            return idToFertilities[id];
        }

        private XMLFilesTypes current;
        public void ReloadLanguage() {
            foreach (XMLFilesTypes xml in Enum.GetValues(typeof(XMLFilesTypes))) {
                current = xml;
                ReloadLanguageVariables(LoadXML(xml));
                ModLoader.LoadXMLs(xml, ReloadLanguageVariables);
            }
        }

        public void ReloadLanguageVariables(string xml) {
            FieldInfo[] fields = typeof(LanguageVariables).GetFields();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            XmlNodeList nodeList = xmlDoc.ChildNodes;
            //have todo it for all three deep xml files
            if (current == XMLFilesTypes.structures) {
                nodeList = xmlDoc.FirstChild.ChildNodes;
            }
            foreach (XmlNode parent in nodeList) {
                foreach (XmlNode node in parent.ChildNodes) {
                    object data = null;
                    if (node.Attributes.Count == 0)
                        continue;
                    string id = node.Attributes?[0]?.InnerXml;
                    //if(id == null) {
                    //    id = node.Attributes.GetNamedItem("LEVEL")?.InnerXml;
                    //}
                    if (id == null)
                        continue;
                    switch (current) {
                        case XMLFilesTypes.other:
                            if (node.LocalName == "PopulationLevel")
                                data = populationLevelDatas[int.Parse(id)];
                            else
                                Debug.LogWarning("Read Language again one missing this type" + current);
                            break;

                        case XMLFilesTypes.events:
                            if (node.LocalName == "GameEvent")
                                data = gameEventPrototypeDatas[id];
                            if (node.LocalName == "Effect")
                                data = effectPrototypeDatas[id];
                            break;

                        case XMLFilesTypes.fertilities:
                            data = fertilityPrototypeDatas[id];
                            break;

                        case XMLFilesTypes.items:
                            data = itemPrototypeDatas[id];
                            break;

                        case XMLFilesTypes.combat:
                            if (node.LocalName == "damageType")
                                data = damageTypeDatas[id];
                            if (node.LocalName == "armorType")
                                data = armorTypeDatas[id];
                            break;

                        case XMLFilesTypes.units:
                            if (node.LocalName == "worker")
                                continue;
                            data = unitPrototypeDatas[id];
                            break;

                        case XMLFilesTypes.structures:
                            data = structurePrototypeDatas[id];
                            break;

                        case XMLFilesTypes.needs:
                            if (node.LocalName == "need")
                                data = NeedPrototypeDatas[id];
                            if (node.LocalName == "needGroup")
                                data = needGroupDatas[id];
                            break;

                        case XMLFilesTypes.startingloadouts:
                            break;

                        case XMLFilesTypes.mapgeneration:
                            break;

                        default:
                            Debug.LogWarning("Read Language again missing this type" + current);
                            return;
                    }
                    if (data == null)
                        continue;
                    foreach (FieldInfo fi in fields) {
                        XmlNode currentNode = node.SelectSingleNode(fi.Name);
                        if (currentNode == null) {
                            //TODO activate this warning when all data is correctly created
                            //				Debug.LogWarning (fi.Name + " selected language not avaible!");
                            continue;
                        }
                        XmlNode textNode = currentNode.SelectSingleNode("entry[@lang='" + UILanguageController.selectedLanguage.ToString() + "']");
                        if (textNode != null) {
                            string text = ReplacePlaceHolders(data, textNode.InnerXml);
                            fi.SetValue(data, Convert.ChangeType(text, fi.FieldType));
                        }
                        continue;
                    }
                }
            }
        }

        private string LoadXML(XMLFilesTypes name) {
            string path = System.IO.Path.Combine(ConstantPathHolder.StreamingAssets, GameData.DataLocation, "GameState", name + ".xml");
            return System.IO.File.ReadAllText(path);
        }

        private void OnDestroy() {
            Instance = null;
        }
    }

    public class Unlocks {
        public Unlocks(int peopleCount, int level) {
            this.peopleCount = peopleCount;
            this.populationLevel = level;
            this.requiredFullHomes = peopleCount / PrototypController.Instance.PopulationLevelDatas[level].HomeStructure.people;
        }
        public int peopleCount;
        public int populationLevel;
        public int requiredFullHomes;
        public ConcurrentBag<Structure> structures = new ConcurrentBag<Structure>();
        public ConcurrentBag<Unit> units = new ConcurrentBag<Unit>();
        public ConcurrentBag<Need> needs = new ConcurrentBag<Need>();
    }
}