using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using static Combat;

public class PrototypController : MonoBehaviour {
    public const string GameVersion = "0.1.5"; //TODO: think about this position 

    public const int StartID = 1;

    public int NumberOfPopulationLevels => populationLevelDatas.Count;

    public static PrototypController Instance;
    public IReadOnlyDictionary<string, Structure> StructurePrototypes => structurePrototypes;
    public IReadOnlyDictionary<string, Unit> UnitPrototypes => unitPrototypes;
    public IReadOnlyDictionary<Type, int> StructureTypeToMaxStructureLevel => structureTypeToMaxStructureLevel;
    public IReadOnlyDictionary<string, StructurePrototypeData> StructurePrototypeDatas => structurePrototypeDatas;
    public IReadOnlyDictionary<string, NeedPrototypeData> NeedPrototypeDatas => needPrototypeDatas;
    public IReadOnlyDictionary<string, FertilityPrototypeData> FertilityPrototypeDatas => fertilityPrototypeDatas;
    public IReadOnlyDictionary<string, UnitPrototypeData> UnitPrototypeDatas => unitPrototypeDatas;
    public IReadOnlyDictionary<string, ItemPrototypeData> ItemPrototypeDatas => itemPrototypeDatas;
    public IReadOnlyDictionary<string, DamageType> DamageTypeDatas => damageTypeDatas;
    public IReadOnlyDictionary<string, EffectPrototypeData> EffectPrototypeDatas => effectPrototypeDatas;
    public IReadOnlyDictionary<string, ArmorType> ArmorTypeDatas => armorTypeDatas;
    public IReadOnlyDictionary<string, GameEventPrototypData> GameEventPrototypeDatas => gameEventPrototypeDatas;
    public IReadOnlyDictionary<string, Item> AllItems => allItems;
    public IReadOnlyDictionary<Climate, List<Fertility>> AllFertilities => allFertilities;
    public IReadOnlyDictionary<string, Fertility> IdToFertilities => idToFertilities;
    public IReadOnlyDictionary<int, PopulationLevelPrototypData> PopulationLevelDatas => populationLevelDatas;
    public IReadOnlyDictionary<int, List<NeedGroup>> PopulationLevelToNeedGroup => populationLevelToNeedGroup;

    // SHOULD BE READ ONLY -- cant be done because Unity Error for multiple implementations
    public static List<Item> BuildItems => buildItems;

    Dictionary<string, Structure> structurePrototypes;
    Dictionary<string, Unit> unitPrototypes;
    Dictionary<Type, int> structureTypeToMaxStructureLevel;
    Dictionary<string, StructurePrototypeData> structurePrototypeDatas;
    Dictionary<string, ItemPrototypeData> itemPrototypeDatas;
    Dictionary<string, NeedPrototypeData> needPrototypeDatas;
    Dictionary<string, FertilityPrototypeData> fertilityPrototypeDatas;
    Dictionary<string, UnitPrototypeData> unitPrototypeDatas;
    Dictionary<string, DamageType> damageTypeDatas;
    Dictionary<string, EffectPrototypeData> effectPrototypeDatas;
    Dictionary<string, GameEventPrototypData> gameEventPrototypeDatas;
    Dictionary<string, ArmorType> armorTypeDatas;
    Dictionary<int, PopulationLevelPrototypData> populationLevelDatas;
    Dictionary<string, NeedGroupPrototypData> needGroupDatas;
    Dictionary<string, Item> allItems;
    Dictionary<int, List<NeedGroup>> populationLevelToNeedGroup;
    Dictionary<Climate, List<Fertility>> allFertilities;
    Dictionary<string, Fertility> idToFertilities;

    public List<Item> MineableItems;

    static List<Item> buildItems;
    List<Need> allNeeds;
    //current valid player prototyp data
    internal static PlayerPrototypeData CurrentPlayerPrototypData = new PlayerPrototypeData();

    //TODO: need a way to get this to load in! probably with the rest
    //      of the data thats still needs to be read in like time for money ticks
    public ArmorType StructureArmor => armorTypeDatas["woodenwall"];

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
    void Awake() {
        if (Instance != null) {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;
        ModLoader.LoadMods();
        ModLoader.AvaibleMods();
        LoadFromXML();
    }

    public StructurePrototypeData GetStructurePrototypDataForID(string ID) {
        return structurePrototypeDatas[ID];
    }

    public ItemPrototypeData GetItemPrototypDataForID(string ID) {
        if (itemPrototypeDatas.ContainsKey(ID) == false) {
            Debug.Log(ID + "missing data!");
            return new ItemPrototypeData() { type = ItemType.Missing };
        }
        return itemPrototypeDatas[ID];
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
    public void LoadFromXML() {
        if (allItems != null) {
            return;
        }

        //SO THAT IT DOESNT USE FUCKIN COMMA AS THE DECIMAL SEPERATOR?!? WHY NOT POINT -Zoidberg
        //Why cant it be both -Fry
        //Good News everyone! Setting it to GB fixes that stupid thing! -Professor
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

        //GAMEEVENTS
        effectPrototypeDatas = new Dictionary<string, EffectPrototypeData>();
        gameEventPrototypeDatas = new Dictionary<string, GameEventPrototypData>();
        ReadEventsFromXML(LoadXML("events"));
        ModLoader.LoadXMLs("events", ReadEventsFromXML);

        //fertilities
        allFertilities = new Dictionary<Climate, List<Fertility>>();
        idToFertilities = new Dictionary<string, Fertility>();
        fertilityPrototypeDatas = new Dictionary<string, FertilityPrototypeData>();
        ReadFertilitiesFromXML(LoadXML("fertilities"));
        ModLoader.LoadXMLs("fertilities", ReadFertilitiesFromXML);

        // prototypes of items
        allItems = new Dictionary<string, Item>();
        buildItems = new List<Item>();
        itemPrototypeDatas = new Dictionary<string, ItemPrototypeData>();
        ReadItemsFromXML(LoadXML("items"));
        ModLoader.LoadXMLs("items", ReadItemsFromXML);

        armorTypeDatas = new Dictionary<string, ArmorType>();
        damageTypeDatas = new Dictionary<string, DamageType>();
        ReadCombatFromXML(LoadXML("combat"));
        ModLoader.LoadXMLs("combat", ReadCombatFromXML);

        unitPrototypes = new Dictionary<string, Unit>();
        unitPrototypeDatas = new Dictionary<string, UnitPrototypeData>();
        ReadUnitsFromXML(LoadXML("units"));
        ModLoader.LoadXMLs("units", ReadUnitsFromXML);

        // setup all prototypes of structures here 
        // load them from the 
        structureTypeToMaxStructureLevel = new Dictionary<Type, int>();
        structurePrototypes = new Dictionary<string, Structure>();
        structurePrototypeDatas = new Dictionary<string, StructurePrototypeData>();
        ReadStructuresFromXML(LoadXML("structures"));
        ModLoader.LoadXMLs("structures", ReadStructuresFromXML);

        //needs
        allNeeds = new List<Need>();
        populationLevelToNeedGroup = new Dictionary<int, List<NeedGroup>>();
        needPrototypeDatas = new Dictionary<string, NeedPrototypeData>();
        needGroupDatas = new Dictionary<string, NeedGroupPrototypData>();
        ReadNeedsFromXML(LoadXML("needs"));
        ModLoader.LoadXMLs("needs", ReadNeedsFromXML);

        //other
        populationLevelDatas = new Dictionary<int, PopulationLevelPrototypData>();
        ReadOtherFromXML(LoadXML("other"));
        ModLoader.LoadXMLs("other", ReadOtherFromXML);

        MineableItems = new List<Item>();
        List<Structure> mines = new List<Structure>(structurePrototypes.Values);
        mines.RemoveAll(x => x.GetType() != typeof(MineStructure));
        foreach (Structure s in mines) {
            MineableItems.Add(((MineStructure)s).Output[0]);
        }

        Debug.Log("Read in fertilities types: " + allFertilities.Count + " with all " + fertilityPrototypeDatas.Count);
        string str = "";
        List<Structure> all = new List<Structure>(structurePrototypes.Values);
        while (all.Count > 0) {
            List<Structure> temp = all.FindAll(x => all[0].GetType() == x.GetType());
            foreach (Structure s in temp) {
                all.Remove(s);
            }
            str += "    -> " + temp[0].GetType() + " = " + temp.Count + " \n";
        }
        Debug.Log("Read in structures: " + structurePrototypes.Count + "\n" + str);
        Debug.Log("Read in units: " + unitPrototypes.Count);
        Debug.Log("Read in items: " + allItems.Count);

        string needslevel = "";
        foreach (PopulationLevelPrototypData pl in populationLevelDatas.Values) {
            needslevel += "[" + pl.LEVEL + ": " + allNeeds.Count(x => x.StartLevel == pl.LEVEL) + "]";
        }
        Debug.Log("Read in needs: " + allNeeds.Count + " (" + needslevel + ")");
        Debug.Log("Read in needGroups: " + needGroupDatas.Count);
        Debug.Log("Read in damagetypes: " + damageTypeDatas.Count);
        Debug.Log("Read in armortypes: " + armorTypeDatas.Count);
        Debug.Log("Read in populationLevel: " + populationLevelDatas.Count);
        Debug.Log("Read in effects: " + effectPrototypeDatas.Count);
        Debug.Log("Read in gameevents: " + gameEventPrototypeDatas.Count);

        //Set it to default so it doesnt interfer with user interface informations
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InstalledUICulture;
    }

    private void ReadEventsFromXML(string file) {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        // ((TextAsset)Resources.Load("XMLs/GameState/events", typeof(TextAsset)));
        xmlDoc.LoadXml(file); // load the file.
        XmlNodeList listEffect = xmlDoc.SelectNodes("events/Effect");
        if (listEffect != null) {
            foreach (XmlElement node in listEffect) {
                EffectPrototypeData epd = new EffectPrototypeData();
                string id = node.GetAttribute("ID");
                SetData<EffectPrototypeData>(node, ref epd);
                effectPrototypeDatas[id]=epd;
            }
        }
        XmlNodeList listGameEvent = xmlDoc.SelectNodes("events/GameEvent");
        if (listGameEvent != null) {
            foreach (XmlElement node in listGameEvent) {
                GameEventPrototypData gepd = new GameEventPrototypData();
                string id = node.GetAttribute("ID");
                SetData<GameEventPrototypData>(node, ref gepd);
                gameEventPrototypeDatas[id] = gepd;
            }
        }
        
    }

    private void ReadOtherFromXML(string file) {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        //((TextAsset)Resources.Load("XMLs/GameState/other", typeof(TextAsset)));
        xmlDoc.LoadXml(file); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("Other/PopulationLevel")) {
            PopulationLevelPrototypData plpd = new PopulationLevelPrototypData();
            int level = int.Parse(node.GetAttribute("LEVEL"));
            plpd.LEVEL = level;
            SetData<PopulationLevelPrototypData>(node, ref plpd);
            if (populationLevelToNeedGroup.ContainsKey(level))
                plpd.needGroupList = populationLevelToNeedGroup[level];
            else
                Debug.LogWarning("PopulationLevel " + plpd.Name + " " + plpd.LEVEL + " is missing its own needs!");
            populationLevelDatas[level] = plpd;
        }
    }

    private void ReadCombatFromXML(string file) {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        // ((TextAsset)Resources.Load("XMLs/GameState/combat", typeof(TextAsset)));
        xmlDoc.LoadXml(file); // load the file.
        XmlNodeList listArmorType = xmlDoc.SelectNodes("combatTypes/armorType");
        if (listArmorType != null) {
            foreach (XmlElement node in listArmorType) {
                ArmorType at = new ArmorType();
                string id = node.GetAttribute("ID");
                SetData<ArmorType>(node, ref at);
                armorTypeDatas[id] = at;
            }
        }
        XmlNodeList listDamageType = xmlDoc.SelectNodes("combatTypes/damageType");
        if (listDamageType != null) {
            foreach (XmlElement node in listDamageType) {
                DamageType at = new DamageType();
                string id = node.GetAttribute("ID");

                SetData<DamageType>(node, ref at);
                XmlNode dict = node.SelectSingleNode("damageMultiplier");
                at.damageMultiplier = new Dictionary<ArmorType, float>();
                foreach (XmlElement child in dict.ChildNodes) {
                    string armorID = child.GetAttribute("ArmorTyp");
                    if (string.IsNullOrEmpty(armorID))
                        continue;
                    float multiplier = 1;
                    if (float.TryParse(child.InnerText, out multiplier) == false) {
                        Debug.LogError("ID is not an float for ArmorType ");
                    }

                    at.damageMultiplier[armorTypeDatas[armorID]] = multiplier;
                }
                damageTypeDatas[id] = at;
            }
        }
        
    }


    internal int GetMaxStructureLevelForStructureType(Type type) {
        if (structureTypeToMaxStructureLevel.ContainsKey(type) == false)
            structureTypeToMaxStructureLevel[type] =
                new List<Structure>(structurePrototypes.Values).FindAll(x => type == x.GetType())
                    .OrderByDescending(item => item.StructureLevel).First().StructureLevel;
        return structureTypeToMaxStructureLevel[type];
    }
    internal string GetFirstLevelStructureIDForStructureType(Type type) {
        //TODO: optimize this
        return new List<Structure>(structurePrototypes.Values).FindAll(x => type == x.GetType())
                    .OrderByDescending(item => item.StructureLevel).Last().ID;
    }
    /// <summary>
    /// Is not optimized! Please do NOT call this too frequent!
    /// ascending true => +1 | false => -1 for structureLevel
    /// </summary>
    /// <param name="type"></param>
    /// <param name="structureLevel"></param>
    /// <returns></returns>
    internal string GetStructureIDForTypeNeighbourStructureLevel(Type type, int structureLevel, bool ascending) {
        List<Structure> typeListOrdered = new List<Structure>(structurePrototypes.Values);
        typeListOrdered = typeListOrdered.FindAll(x => type == x.GetType());
        if (typeListOrdered.Count <= 1) {
            return null;
        }
        if (ascending) {
            typeListOrdered.OrderByDescending(item => item.StructureLevel);
        }
        else {
            typeListOrdered.OrderBy(item => item.StructureLevel);
        }
        return typeListOrdered[typeListOrdered.FindIndex(x => x.StructureLevel == structureLevel) + 1].ID;
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
    private void ReadItemsFromXML(string file) {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        // ((TextAsset)Resources.Load("XMLs/GameState/items", typeof(TextAsset)));
        xmlDoc.LoadXml(file); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("items/Item")) {
            ItemPrototypeData ipd = new ItemPrototypeData();
            string id = node.GetAttribute("ID");
            SetData<ItemPrototypeData>(node, ref ipd);

            itemPrototypeDatas[id] = ipd;
            Item item = new Item(id, ipd);

            if (item.Type == ItemType.Build) {
                buildItems.Add(item);
            }
            allItems[id] = item;
        }

    }
    private void ReadUnitsFromXML(string file) {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        // ((TextAsset)Resources.Load("XMLs/GameState/units", typeof(TextAsset)));
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
                spd.width = 1;
                spd.height = 1;
                unitPrototypeDatas[id] = spd;
                unitPrototypes[id] = new Ship(id, spd);
            }
        }
        
    }
    private void ReadFertilitiesFromXML(string file) {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        // ((TextAsset)Resources.Load("XMLs/GameState/fertilities", typeof(TextAsset)));
        xmlDoc.LoadXml(file); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("fertilities/Fertility")) {
            string ID = node.GetAttribute("ID");

            FertilityPrototypeData fpd = new FertilityPrototypeData();

            SetData<FertilityPrototypeData>(node, ref fpd);

            Fertility fer = new Fertility(ID, fpd);
            idToFertilities.Add(fer.ID, fer);
            fertilityPrototypeDatas[ID] = fpd;
            foreach (Climate item in fer.Climates) {
                if (allFertilities.ContainsKey(item) == false) {
                    List<Fertility> f = new List<Fertility> {
                        fer
                    };
                    allFertilities.Add(item, f);
                }
                else {
                    allFertilities[item].Add(fer);
                }
            }
        }
    }
    private void ReadNeedsFromXML(string file) {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        // ((TextAsset)Resources.Load("XMLs/GameState/needs", typeof(TextAsset)));
        xmlDoc.LoadXml(file); // load the file.
        XmlNodeList listNeedGroup = xmlDoc.SelectNodes("needs/NeedGroup");
        if (listNeedGroup != null) {
            foreach (XmlElement node in listNeedGroup) {
                NeedGroupPrototypData ngpd = new NeedGroupPrototypData();
                string ID = node.GetAttribute("ID");
                ngpd.ID = ID;
                SetData<NeedGroupPrototypData>(node, ref ngpd);
                needGroupDatas[ID] = ngpd;
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
                    foreach (Structure str in npd.structures) {
                        if (npd.startLevel > str.PopulationLevel) {
                            npd.startLevel = str.PopulationLevel;
                        }
                        if (npd.startLevel == str.PopulationLevel) {
                            if (npd.popCount > str.PopulationCount) {
                                npd.popCount = str.PopulationCount;
                            }
                        }
                    }
                }
                Need n = new Need(ID, npd);
                allNeeds.Add(n);
                if (levelToNeedList.ContainsKey(npd.startLevel) == false) {
                    levelToNeedList[npd.startLevel] = new List<Need>();
                }
                levelToNeedList[npd.startLevel].Add(n.Clone());
            }
        }
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
         // ((TextAsset)Resources.Load("XMLs/GameState/structures", typeof(TextAsset)));
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
            //THESE are fix and are not changed for any 
            //!not anymore
            SetData<ServiceStructurePrototypeData>(node, ref sspd);
            foreach(Effect effect in sspd.effectsOnTargets) {
                effect.Serialize = false;
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

            MilitaryStructurePrototypeData mpd = new MilitaryStructurePrototypeData();
            //THESE are fix and are not changed for any 
            //!not anymore
            SetData<MilitaryStructurePrototypeData>(node, ref mpd);
            structurePrototypeDatas[ID] = mpd;
            structurePrototypes[ID] = new MilitaryStructure(ID, mpd);
        }
    }

    private void ReadRoads(XmlNode xmlDoc) {
        if (xmlDoc == null)
            return;
        foreach (XmlElement node in xmlDoc.SelectNodes("road")) {
            string ID = node.GetAttribute("ID");

            StructurePrototypeData spd = new StructurePrototypeData {
                //THESE are fix and are not changed for any road
                tileWidth = 1,
                tileHeight = 1,
                buildTyp = BuildTypes.Path,
                myStructureTyp = StructureTyp.Pathfinding,
                canBeUpgraded = true,
                //!not anymore
                maintenanceCost = 0,
                buildcost = 25,
                Name = "Testroad",
                structureRange = 0,
                structureLevel = 0
            };

            SetData<StructurePrototypeData>(node, ref spd);

            structurePrototypeDatas[ID] = spd;
            structurePrototypes[ID] = new RoadStructure(ID, spd);

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
                myStructureTyp = StructureTyp.Free,
                buildTyp = BuildTypes.Drag,
                buildcost = 50,
                maxOutputStorage = 1
            };
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

            FarmPrototypData fpd = new FarmPrototypData();
            //THESE are fix and are not changed for any 
            //!not anymore
            SetData<FarmPrototypData>(node, ref fpd);
            structurePrototypeDatas[ID] = fpd;
            structurePrototypes[ID] = new FarmStructure(ID, fpd);
        }
    }
    private void ReadMarketStructures(XmlNode xmlDoc) {
        if (xmlDoc == null)
            return;
        foreach (XmlElement node in xmlDoc.SelectNodes("market")) {
            string ID = node.GetAttribute("ID");
            MarketPrototypData mpd = new MarketPrototypData {
                //THESE are fix and are not changed for any MarketStructure
                hasHitbox = true,
                tileWidth = 4,
                tileHeight = 4,
                buildTyp = BuildTypes.Single,
                myStructureTyp = StructureTyp.Blocking,
                structureRange = 18,
                canTakeDamage = true,

                Name = "market",
                buildcost = 500,
                maintenanceCost = 10
            };

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
                myStructureTyp = StructureTyp.Blocking,
                buildTyp = BuildTypes.Single,
                canTakeDamage = true,
                forMarketplace = true,
                //!not anymore

                Name = "TEST Production",
                maxNumberOfWorker = 1
            };
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
            StructurePrototypeData spd = new StructurePrototypeData {
                //THESE are fix and are not changed for any NeedsStructure
                //!not anymore
                tileWidth = 2,
                tileHeight = 2,
                buildTyp = BuildTypes.Single,
                myStructureTyp = StructureTyp.Blocking,
                Name = "NeedStructure",
                maintenanceCost = 100
            };

            SetData<StructurePrototypeData>(node, ref spd);

            structurePrototypeDatas[ID] = spd;
            structurePrototypes[ID] = new NeedStructure(ID, spd);
        }
    }

    private void ReadHomeStructures(XmlNode xmlDoc) {
        if (xmlDoc == null)
            return;
        foreach (XmlElement node in xmlDoc.SelectNodes("home")) {
            string ID = node.GetAttribute("ID");
            HomePrototypeData hpd = new HomePrototypeData {
                //THESE are fix and are not changed for any HomeStructure
                tileWidth = 2,
                tileHeight = 2,
                buildTyp = BuildTypes.Drag,
                myStructureTyp = StructureTyp.Blocking,
                structureRange = 0,
                hasHitbox = true,
                canTakeDamage = true,
                maintenanceCost = 0
            };

            SetData<HomePrototypeData>(node, ref hpd);

            structurePrototypeDatas[ID] = hpd;
            structurePrototypes[ID] = new HomeStructure(ID, hpd);

            string prevID = GetStructureIDForTypeNeighbourStructureLevel(typeof(HomeStructure), hpd.structureLevel, false);
            if (String.IsNullOrEmpty(prevID)==false) {
                HomePrototypeData prev = (HomePrototypeData)structurePrototypeDatas[prevID];
                ((HomePrototypeData)hpd).previouseMaxLivingSpaces = prev == null ? 0 : prev.maxLivingSpaces;
                prev.upgradeItems = hpd.buildingItems;
                prev.upgradeCost = hpd.buildcost;
            }
        }
    }

    private void ReadWarehouse(XmlNode xmlDoc) {
        if (xmlDoc == null)
            return;
        foreach (XmlElement node in xmlDoc.SelectNodes("warehouse")) {
            string ID = node.GetAttribute("ID");
            MarketPrototypData mpd = new MarketPrototypData {
                //THESE are fix and are not changed for any Warehouse
                contactRange = 6.3f,
                buildTyp = BuildTypes.Single,
                hasHitbox = true,
                canTakeDamage = true,
                structureRange = 18,

                //!not anymore
                tileWidth = 3,
                tileHeight = 3,
                Name = "warehouse",
                buildcost = 500,
                maintenanceCost = 10,
                mustFrontBuildDir = Direction.W
            };

            SetData<MarketPrototypData>(node, ref mpd);

            structurePrototypeDatas[ID] = mpd;
            structurePrototypes[ID] = new WarehouseStructure(ID, mpd);
        }
    }
    private void ReadMineStructure(XmlNode xmlDoc) {
        if (xmlDoc == null)
            return;
        foreach (XmlElement node in xmlDoc.SelectNodes("mine")) {
            string ID = node.GetAttribute("ID");

            MinePrototypData mpd = new MinePrototypData {
                //THESE are fix and are not changed for any Warehouse
                tileWidth = 2,
                tileHeight = 3,
                Name = "Mine",
                myStructureTyp = StructureTyp.Blocking,
                buildTyp = BuildTypes.Single,
                hasHitbox = true,
                structureRange = 0
            };

            SetData<MinePrototypData>(node, ref mpd);

            structurePrototypeDatas[ID] = mpd;
            structurePrototypes[ID] = new MineStructure(ID, mpd);

        }
    }
    private void SetData<T>(XmlElement node, ref T data) {
        FieldInfo[] fields = typeof(T).GetFields();
        HashSet<String> langs = new HashSet<String>();
        foreach (FieldInfo f in typeof(LanguageVariables).GetFields()) {
            langs.Add(f.Name);
        }
        foreach (FieldInfo fi in fields) {
            XmlNode currentNode = node.SelectSingleNode(fi.Name);
            if (langs.Contains(fi.Name)) {
                if (currentNode == null) {
                    //TODO activate this warning when all data is correctly created
                    //				Debug.LogWarning (fi.Name + " selected language not avaible!");
                    continue;
                }
                XmlNode textNode = currentNode.SelectSingleNode("entry[@lang='" + UILanguageController.selectedLanguage.ToString() + "']");

                if (textNode != null) {
                    fi.SetValue(data, Convert.ChangeType(textNode.InnerXml, fi.FieldType));
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
                if (fi.FieldType.IsSubclassOf(typeof(Structure[])) || fi.FieldType == (typeof(Structure[]))) {
                    List<Structure> items = new List<Structure>();
                    foreach (XmlNode item in currentNode.ChildNodes) {
                        items.Add(NodeToStructure(item));
                    }
                    fi.SetValue(data, items.ToArray());
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
                    List<float> items = new List<float>();
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
                if (fi.FieldType.IsArray && fi.FieldType.GetElementType().IsEnum) {
                    var listType = typeof(List<>);
                    var constructedListType = listType.MakeGenericType(fi.FieldType.GetElementType());
                    var list = (IList)Activator.CreateInstance(constructedListType);
                    int i = 0;
                    foreach (XmlNode item in currentNode.ChildNodes) {
                        if (item.Name != fi.FieldType.GetElementType().ToString()) {
                            continue;
                        }
                        list.Add(Enum.Parse(fi.FieldType.GetElementType(), item.InnerXml, true));
                        i++;
                    }
                    Array enumArray = Array.CreateInstance(fi.FieldType.GetElementType(), list.Count);
                    list.CopyTo(enumArray, 0);
                    fi.SetValue(data, Convert.ChangeType(enumArray, fi.FieldType));
                    continue;
                }
                if (fi.FieldType == typeof(Dictionary<ArmorType, float>)) {
                    // this will get set in load xml directly and not here!
                    continue;
                }
                if (fi.FieldType == typeof(Dictionary<Target, List<int>>)) {
                    Dictionary<Target, List<int>> range = new Dictionary<Target, List<int>>();
                    foreach (XmlNode child in currentNode.ChildNodes) {
                        Target target = Target.World;
                        if(child.Attributes[0] == null)
                            continue;
                        if (Enum.TryParse<Target>(child.Attributes[0].InnerXml, true, out target) == false)
                            continue;
                        String[] ids = child.InnerXml.Split(',');
                        if(ids.Length == 0) {
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
                    foreach(Target t in targets) {
                        if (range[t].Count == 0)
                            targets.Remove(t);
                    }
                    //only if it has stuff we need to set it
                    if(range.Count > 0)
                        fi.SetValue(data, range);
                    continue;
                }
                if (fi.FieldType == typeof(TargetGroup)) {
                    List<Target> targets = new List<Target>();
                    foreach (XmlNode child in currentNode.ChildNodes) {
                        Target target = Target.World;
                        if (Enum.TryParse<Target>(child.InnerXml, true, out target) == false)
                            continue;
                        targets.Add(target);
                    }
                    fi.SetValue(data, new TargetGroup(targets));
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
            Debug.LogError("ITEM ID was not created! " + id);
            return null;
        }
        Item clone = allItems[id].Clone();
        if (n.SelectSingleNode("count") != null) {
            int count = 0;
            if (int.TryParse(n.SelectSingleNode("count").InnerXml, out count) == false) {
                Debug.LogError("Count is not an int");
                return null;
            }
            clone.count = count;
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
        string id = n.InnerXml;
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
    private string LoadXML(string name) {
        string path = System.IO.Path.Combine(ConstantPathHolder.StreamingAssets,"XMLs", "GameState", name + ".xml");
        return System.IO.File.ReadAllText(path);
    }
    void OnDestroy() {
        Instance = null;
    }
}
