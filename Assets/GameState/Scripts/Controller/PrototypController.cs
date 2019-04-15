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
    public const int StartID = 1;

    public int NumberOfPopulationLevels => populationLevelDatas.Count;

    public static PrototypController Instance;
    public IReadOnlyDictionary<int, Structure> StructurePrototypes => structurePrototypes;
    public IReadOnlyDictionary<int, Unit> UnitPrototypes => unitPrototypes;
    public IReadOnlyDictionary<Type, int> StructureTypeToMaxStructureLevel => structureTypeToMaxStructureLevel;
    public IReadOnlyDictionary<int, StructurePrototypeData> StructurePrototypeDatas => structurePrototypeDatas;
    public IReadOnlyDictionary<int, NeedPrototypeData> NeedPrototypeDatas => needPrototypeDatas;
    public IReadOnlyDictionary<int, FertilityPrototypeData> FertilityPrototypeDatas => fertilityPrototypeDatas;
    public IReadOnlyDictionary<int, UnitPrototypeData> UnitPrototypeDatas => unitPrototypeDatas;
    public IReadOnlyDictionary<int, ItemPrototypeData> ItemPrototypeDatas => itemPrototypeDatas;
    public IReadOnlyDictionary<int, DamageType> DamageTypeDatas => damageTypeDatas;
    public IReadOnlyDictionary<int, EffectPrototypeData> EffectPrototypeDatas => effectPrototypeDatas;
    public IReadOnlyDictionary<int, ArmorType> ArmorTypeDatas => armorTypeDatas;
    public IReadOnlyDictionary<int, GameEventPrototypData> GameEventPrototypeDatas => gameEventPrototypeDatas;
    public IReadOnlyDictionary<int, Item> AllItems => allItems;
    public IReadOnlyDictionary<int, List<NeedGroup>> PopulationLevelToNeedGroup => populationLevelToNeedGroup;
    public IReadOnlyDictionary<Climate, List<Fertility>> AllFertilities => allFertilities;
    public IReadOnlyDictionary<int, Fertility> IdToFertilities => idToFertilities;
    public IReadOnlyDictionary<int, PopulationLevelPrototypData> PopulationLevelDatas => populationLevelDatas;

    // SHOULD BE READ ONLY -- cant be done because Unity Error for multiple implementations
    public static List<Item> BuildItems => buildItems;

    Dictionary<int, Structure> structurePrototypes;
    Dictionary<int, Unit> unitPrototypes;
    Dictionary<Type, int> structureTypeToMaxStructureLevel;
    Dictionary<int, StructurePrototypeData> structurePrototypeDatas;
    Dictionary<int, ItemPrototypeData> itemPrototypeDatas;
    Dictionary<int, NeedPrototypeData> needPrototypeDatas;
    Dictionary<int, FertilityPrototypeData> fertilityPrototypeDatas;
    Dictionary<int, UnitPrototypeData> unitPrototypeDatas;
    Dictionary<int, DamageType> damageTypeDatas;
    Dictionary<int, EffectPrototypeData> effectPrototypeDatas;
    Dictionary<int, GameEventPrototypData> gameEventPrototypeDatas;
    Dictionary<int, ArmorType> armorTypeDatas;
    Dictionary<int, PopulationLevelPrototypData> populationLevelDatas;
    Dictionary<int, NeedGroupPrototypData> needGroupDatas;
    Dictionary<int, Item> allItems;
    Dictionary<int, List<NeedGroup>> populationLevelToNeedGroup;
    Dictionary<Climate, List<Fertility>> allFertilities;
    Dictionary<int, Fertility> idToFertilities;

    public List<Item> MineableItems;

    static List<Item> buildItems;
    List<Need> allNeeds;
    //current valid player prototyp data
    internal static PlayerPrototypeData CurrentPlayerPrototypData = new PlayerPrototypeData();

    //TODO: need a way to get this to load in! probably with the rest
    //      of the data thats still needs to be read in like time for money ticks
    public ArmorType StructureArmor => armorTypeDatas[1];

    public Dictionary<int, Item> GetCopieOfAllItems() {
        Dictionary<int, Item> items = new Dictionary<int, Item>();
        foreach (int item in allItems.Keys) {
            int id = item;
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
        throw new NotImplementedException();
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

        LoadFromXML();
    }

    // Update is called once per frame
    void Update() {

    }

    public StructurePrototypeData GetStructurePrototypDataForID(int ID) {
        return structurePrototypeDatas[ID];
    }
    public ItemPrototypeData GetItemPrototypDataForID(int ID) {
        if (itemPrototypeDatas.ContainsKey(ID) == false) {
            Debug.Log(ID + "missing data!");
            return null;
        }
        return itemPrototypeDatas[ID];
    }
    internal PopulationLevelPrototypData GetPopulationLevelPrototypDataForLevel(int level) {
        return populationLevelDatas[level];
    }
    public FertilityPrototypeData GetFertilityPrototypDataForID(int ID) {
        return fertilityPrototypeDatas[ID];
    }
    public NeedPrototypeData GetNeedPrototypDataForID(int ID) {
        return needPrototypeDatas[ID];
    }
    internal NeedGroupPrototypData GetNeedGroupPrototypDataForID(int ID) {
        return needGroupDatas[ID];
    }
    internal GameEventPrototypData GetGameEventPrototypDataForID(int ID) {
        return gameEventPrototypeDatas[ID];
    }
    internal List<NeedGroup> GetNeedPrototypDataForLevel(int level) {
        return populationLevelToNeedGroup[level];
    }
    internal EffectPrototypeData GetEffectPrototypDataForID(int id) {
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
        effectPrototypeDatas = new Dictionary<int, EffectPrototypeData>();
        gameEventPrototypeDatas = new Dictionary<int, GameEventPrototypData>();
        ReadEventsFromXML();

        //fertilities
        allFertilities = new Dictionary<Climate, List<Fertility>>();
        idToFertilities = new Dictionary<int, Fertility>();
        fertilityPrototypeDatas = new Dictionary<int, FertilityPrototypeData>();
        ReadFertilitiesFromXML();

        // prototypes of items
        allItems = new Dictionary<int, Item>();
        buildItems = new List<Item>();
        itemPrototypeDatas = new Dictionary<int, ItemPrototypeData>();
        ReadItemsFromXML();

        armorTypeDatas = new Dictionary<int, ArmorType>();
        damageTypeDatas = new Dictionary<int, DamageType>();
        ReadCombatFromXML();

        unitPrototypes = new Dictionary<int, Unit>();
        unitPrototypeDatas = new Dictionary<int, UnitPrototypeData>();
        ReadUnitsFromXML();
        // setup all prototypes of structures here 
        // load them from the 
        structureTypeToMaxStructureLevel = new Dictionary<Type, int>();
        structurePrototypes = new Dictionary<int, Structure>();
        structurePrototypeDatas = new Dictionary<int, StructurePrototypeData>();
        ReadStructuresFromXML();

        //needs
        allNeeds = new List<Need>();
        populationLevelToNeedGroup = new Dictionary<int, List<NeedGroup>>();
        needPrototypeDatas = new Dictionary<int, NeedPrototypeData>();
        needGroupDatas = new Dictionary<int, NeedGroupPrototypData>();
        ReadNeedsFromXML();


        //other
        populationLevelDatas = new Dictionary<int, PopulationLevelPrototypData>();
        ReadOtherFromXML();

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

    private void ReadEventsFromXML() {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        TextAsset ta = ((TextAsset)Resources.Load("XMLs/events", typeof(TextAsset)));
        if(ta == null) {
            Debug.LogError("Missing Events XML File -- This will cause a crash.");
            return;
        }
        xmlDoc.LoadXml(ta.text); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("events/Effect")) {
            EffectPrototypeData epd = new EffectPrototypeData();
            int id = int.Parse(node.GetAttribute("ID"));
            SetData<EffectPrototypeData>(node, ref epd);
            effectPrototypeDatas.Add(id, epd);
        }
        foreach (XmlElement node in xmlDoc.SelectNodes("events/GameEvent")) {
            GameEventPrototypData gepd = new GameEventPrototypData();
            int id = int.Parse(node.GetAttribute("ID"));
            SetData<GameEventPrototypData>(node, ref gepd);
            gameEventPrototypeDatas.Add(id, gepd);
        }
    }

    private void ReadOtherFromXML() {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        TextAsset ta = ((TextAsset)Resources.Load("XMLs/other", typeof(TextAsset)));
        xmlDoc.LoadXml(ta.text); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("Other/PopulationLevel")) {
            PopulationLevelPrototypData plpd = new PopulationLevelPrototypData();
            int level = int.Parse(node.GetAttribute("LEVEL"));
            plpd.LEVEL = level;
            SetData<PopulationLevelPrototypData>(node, ref plpd);
            if (populationLevelToNeedGroup.ContainsKey(level))
                plpd.needGroupList = populationLevelToNeedGroup[level];
            else
                Debug.LogWarning("PopulationLevel " + plpd.Name + " " + plpd.LEVEL + " is missing its own needs!");
            populationLevelDatas.Add(level, plpd);
        }
    }

    private void ReadCombatFromXML() {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        TextAsset ta = ((TextAsset)Resources.Load("XMLs/combat", typeof(TextAsset)));
        xmlDoc.LoadXml(ta.text); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("combatTypes/armorType")) {
            ArmorType at = new ArmorType();
            int id = int.Parse(node.GetAttribute("ID"));
            SetData<ArmorType>(node, ref at);
            armorTypeDatas.Add(id, at);
        }
        foreach (XmlElement node in xmlDoc.SelectNodes("combatTypes/damageType")) {
            DamageType at = new DamageType();
            int id = int.Parse(node.GetAttribute("ID"));

            SetData<DamageType>(node, ref at);
            XmlNode dict = node.SelectSingleNode("damageMultiplier");
            at.damageMultiplier = new Dictionary<ArmorType, float>();
            foreach (XmlElement child in dict.ChildNodes) {
                int armorID = -1;
                if (int.TryParse(child.GetAttribute("ArmorTyp"), out armorID) == false) {
                    Debug.LogError("ID is not an int for ArmorType " + child.GetAttribute("ArmorTyp"));
                }
                if (armorID < 0)
                    continue;
                float multiplier = 1;
                if (float.TryParse(child.InnerText, out multiplier) == false) {
                    Debug.LogError("ID is not an float for ArmorType ");
                }
                at.damageMultiplier[armorTypeDatas[armorID]] = multiplier;
            }
            damageTypeDatas.Add(id, at);
        }
    }


    internal int GetMaxStructureLevelForStructureType(Type type) {
        if (structureTypeToMaxStructureLevel.ContainsKey(type) == false)
            structureTypeToMaxStructureLevel[type] =
                new List<Structure>(structurePrototypes.Values).FindAll(x => type == x.GetType())
                    .OrderByDescending(item => item.StructureLevel).First().StructureLevel;
        return structureTypeToMaxStructureLevel[type];
    }
    /// <summary>
    /// Is not optimized! Please do NOT call this too frequent!
    /// ascending true => +1 | false => -1 for structureLevel
    /// </summary>
    /// <param name="type"></param>
    /// <param name="structureLevel"></param>
    /// <returns></returns>
    internal int GetStructureIDForTypeNeighbourStructureLevel(Type type, int structureLevel, bool ascending) {
        List<Structure> typeListOrdered = new List<Structure>(structurePrototypes.Values);
        typeListOrdered = typeListOrdered.FindAll(x => type == x.GetType());
        if (typeListOrdered.Count <= 1) {
            return -1;
        }
        if (ascending) {
            typeListOrdered.OrderByDescending(item => item.StructureLevel);
        }
        else {
            typeListOrdered.OrderBy(item => item.StructureLevel);
        }
        return typeListOrdered[typeListOrdered.FindIndex(x => x.StructureLevel == structureLevel) + 1].ID;
    }
    internal UnitPrototypeData GetUnitPrototypDataForID(int id) {
        return unitPrototypeDatas[id];
    }
    internal Unit GetUnitForID(int id) {
        if (unitPrototypes.ContainsKey(id) == false)
            return null;
        return unitPrototypes[id];
    }
    ///////////////////////////////////////
    /// XML LOADING FROM FILE
    /// 
    ///////////////////////////////////////
    private void ReadItemsFromXML() {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        TextAsset ta = ((TextAsset)Resources.Load("XMLs/items", typeof(TextAsset)));
        xmlDoc.LoadXml(ta.text); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("items/Item")) {
            ItemPrototypeData ipd = new ItemPrototypeData();
            int id = int.Parse(node.GetAttribute("ID"));
            SetData<ItemPrototypeData>(node, ref ipd);

            itemPrototypeDatas[id] = ipd;
            Item item = new Item(id, ipd);

            if (item.Type == ItemType.Build) {
                buildItems.Add(item);
            }
            allItems[id] = item;
        }

    }
    private void ReadUnitsFromXML() {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        TextAsset ta = ((TextAsset)Resources.Load("XMLs/units", typeof(TextAsset)));
        xmlDoc.LoadXml(ta.text); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("units/unit")) {
            UnitPrototypeData upd = new UnitPrototypeData();
            int id = int.Parse(node.GetAttribute("ID"));
            SetData<UnitPrototypeData>(node, ref upd);
            unitPrototypeDatas[id] = upd;
            unitPrototypes.Add(id, new Unit(id, upd));
        }
        foreach (XmlElement node in xmlDoc.SelectNodes("units/ship")) {
            ShipPrototypeData spd = new ShipPrototypeData();
            int id = int.Parse(node.GetAttribute("ID"));
            SetData<ShipPrototypeData>(node, ref spd);
            unitPrototypeDatas[id] = spd;
            unitPrototypes.Add(id, new Ship(id, spd));
        }
    }
    private void ReadFertilitiesFromXML() {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        TextAsset ta = ((TextAsset)Resources.Load("XMLs/fertilities", typeof(TextAsset)));
        xmlDoc.LoadXml(ta.text); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("fertilities/Fertility")) {
            int ID = int.Parse(node.GetAttribute("ID"));

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
    private void ReadNeedsFromXML() {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        TextAsset ta = ((TextAsset)Resources.Load("XMLs/needs", typeof(TextAsset)));
        xmlDoc.LoadXml(ta.text); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("needs/NeedGroup")) {
            NeedGroupPrototypData ngpd = new NeedGroupPrototypData();
            int ID = int.Parse(node.GetAttribute("ID"));
            ngpd.ID = ID;
            SetData<NeedGroupPrototypData>(node, ref ngpd);
            needGroupDatas.Add(ID, ngpd);
        }

        Dictionary<int, List<Need>> levelToNeedList = new Dictionary<int, List<Need>>();
        foreach (XmlElement node in xmlDoc.SelectNodes("needs/Need")) {
            NeedPrototypeData npd = new NeedPrototypeData();
            int ID = int.Parse(node.GetAttribute("ID"));
            SetData<NeedPrototypeData>(node, ref npd);


            needPrototypeDatas.Add(ID, npd);
            if (npd.item == null && npd.structures == null)
                continue;
            if(npd.structures != null) {
                foreach(Structure str in npd.structures) {
                    if(npd.startLevel > str.PopulationLevel) {
                        npd.startLevel = str.PopulationLevel;
                    } 
                    if(npd.startLevel == str.PopulationLevel) {
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

        foreach (int level in levelToNeedList.Keys) {
            List<NeedGroup> ngs = new List<NeedGroup>();
            populationLevelToNeedGroup.Add(level, ngs);
            foreach (Need need in levelToNeedList[level]) {
                if (ngs.Exists(x => x.ID == need.Group.ID) == false)
                    ngs.Add(new NeedGroup(need.Group.ID));
                ngs[need.Group.ID].AddNeed(need.Clone());
            }
        }

    }
    private void ReadStructuresFromXML() {
        XmlDocument xmlDoc = new XmlDocument();
        TextAsset ta = ((TextAsset)Resources.Load("XMLs/structures", typeof(TextAsset)));
        xmlDoc.LoadXml(ta.text); // load the file.
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
            int ID = int.Parse(node.GetAttribute("ID"));

            ServiceStructurePrototypeData sspd = new ServiceStructurePrototypeData();
            //THESE are fix and are not changed for any 
            //!not anymore
            SetData<ServiceStructurePrototypeData>(node, ref sspd);
            foreach(Effect effect in sspd.effectsOnTargets) {
                effect.Serialize = false;
            }
            structurePrototypeDatas.Add(ID, sspd);
            structurePrototypes[ID] = new ServiceStructure(ID);
        }
    }

    private void ReadMilitaryStructures(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("militarystructure")) {
            int ID = int.Parse(node.GetAttribute("ID"));

            MilitaryStructurePrototypeData mpd = new MilitaryStructurePrototypeData();
            //THESE are fix and are not changed for any 
            //!not anymore
            SetData<MilitaryStructurePrototypeData>(node, ref mpd);
            structurePrototypeDatas.Add(ID, mpd);
            structurePrototypes[ID] = new MilitaryStructure(ID, mpd);
        }
    }

    private void ReadRoads(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("road")) {
            int ID = int.Parse(node.GetAttribute("ID"));

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

            structurePrototypeDatas.Add(ID, spd);
            structurePrototypes[ID] = new RoadStructure(ID, spd);

        }
    }
    private void ReadGrowables(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("growable")) {
            int ID = int.Parse(node.GetAttribute("ID"));

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
            structurePrototypeDatas.Add(ID, gpd);
            structurePrototypes[ID] = new GrowableStructure(ID, gpd);
        }
    }
    private void ReadFarms(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("farm")) {
            int ID = int.Parse(node.GetAttribute("ID"));

            FarmPrototypData fpd = new FarmPrototypData();
            //THESE are fix and are not changed for any 
            //!not anymore
            SetData<FarmPrototypData>(node, ref fpd);
            structurePrototypeDatas.Add(ID, fpd);
            structurePrototypes[ID] = new FarmStructure(ID, fpd);
        }
    }
    private void ReadMarketStructures(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("market")) {
            int ID = int.Parse(node.GetAttribute("ID"));
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

            structurePrototypeDatas.Add(ID, mpd);
            structurePrototypes[ID] = new MarketStructure(ID, mpd);
        }
    }
    private void ReadProductionStructures(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("production")) {

            int ID = int.Parse(node.GetAttribute("ID"));

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

            structurePrototypeDatas.Add(ID, ppd);
            structurePrototypes[ID] = new ProductionStructure(ID, ppd);
        }
    }

    private void ReadNeedsStructures(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("needstructure")) {
            int ID = int.Parse(node.GetAttribute("ID"));
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

            structurePrototypeDatas.Add(ID, spd);
            structurePrototypes[ID] = new NeedStructure(ID, spd);
        }
    }

    private void ReadHomeStructures(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("home")) {
            int ID = int.Parse(node.GetAttribute("ID"));
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
            structurePrototypeDatas.Add(ID, hpd);
            structurePrototypes[ID] = new HomeStructure(ID, hpd);

            int prevID = GetStructureIDForTypeNeighbourStructureLevel(typeof(HomeStructure), hpd.structureLevel, false);
            if (prevID != -1) {
                HomePrototypeData prev = (HomePrototypeData)structurePrototypeDatas[prevID];
                ((HomePrototypeData)hpd).previouseMaxLivingSpaces = prev == null ? 0 : prev.maxLivingSpaces;
                prev.upgradeItems = hpd.buildingItems;
                prev.upgradeCost = hpd.buildcost;
            }
        }
    }

    private void ReadWarehouse(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("warehouse")) {
            int ID = int.Parse(node.GetAttribute("ID"));
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
            structurePrototypeDatas.Add(ID, mpd);
            structurePrototypes[ID] = new WarehouseStructure(ID, mpd);
        }
    }
    private void ReadMineStructure(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("mine")) {
            int ID = int.Parse(node.GetAttribute("ID"));

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

            structurePrototypeDatas.Add(ID, mpd);
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
        int id = -1;
        if (int.TryParse(item.InnerXml, out id) == false) {
            Debug.LogError("ID is not an int for DamageType ");
            return null;
        }
        if (id == -1) {
            return null;//not needed
        }
        if (effectPrototypeDatas.ContainsKey(id) == false) {
            Debug.LogError("ID was not created before the depending DamageType! " + id);
            return null;
        }
        return new Effect(id);
    }

    private object NodeToDamageType(XmlNode n) {
        int id = -1;
        if (int.TryParse(n.InnerXml, out id) == false) {
            Debug.LogError("ID is not an int for DamageType ");
            return null;
        }
        if (id == -1) {
            return null;//not needed
        }
        if (damageTypeDatas.ContainsKey(id) == false) {
            Debug.LogError("ID was not created before the depending DamageType! " + id);
            return null;
        }
        return damageTypeDatas[id];
    }
    private object NodeToNeedGroupPrototypData(XmlNode n) {
        int id = -1;
        if (int.TryParse(n.InnerXml, out id) == false) {
            Debug.LogError("ID is not an int for NeedGroup ");
            return null;
        }
        if (id == -1) {
            return null;//not needed
        }
        if (needGroupDatas.ContainsKey(id) == false) {
            Debug.LogError("ID was not created before the depending NeedGroup! " + id);
            return null;
        }
        return needGroupDatas[id];
    }
    private object NodeToArmorType(XmlNode n) {
        int id = -1;
        if (int.TryParse(n.InnerXml, out id) == false) {
            Debug.LogError("ID is not an int for ArmorType ");
            return null;
        }
        if (id == -1) {
            return null;//not needed
        }
        if (armorTypeDatas.ContainsKey(id) == false) {
            Debug.LogError("ID was not created before the depending ArmorType! " + id);
            return null;
        }
        return armorTypeDatas[id];
    }

    private Item NodeToItem(XmlNode n) {
        int id = -1;
        if (int.TryParse(n.Attributes["ID"].Value, out id) == false) {
            Debug.LogError("ID is not an int for ITEM ");
            return null;
        }
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
        int id = -1;
        if (int.TryParse(n.InnerXml, out id) == false) {
            Debug.LogError("ID is not an int for Unit ");
            return null;
        }
        if (id == -1) {
            return null;//not needed
        }
        if (unitPrototypes.ContainsKey(id) == false) {
            Debug.LogError("ID was not created before the depending Unit! " + id);
            return null;
        }
        return unitPrototypes[id];
    }
    private Structure NodeToStructure(XmlNode n) {
        int id = -1;
        if (int.TryParse(n.InnerXml, out id) == false) {
            Debug.LogError("ID is not an int for Structure ");
            return null;
        }
        if (id == -1) {
            return null;//not needed
        }
        if (structurePrototypes.ContainsKey(id) == false) {
            Debug.LogError("ID was not created before the depending Structure! " + id);
            return null;
        }
        return structurePrototypes[id];
    }

    private Fertility NodeToFertility(XmlNode n) {
        int id = -1;
        if (int.TryParse(n.InnerXml, out id) == false) {
            Debug.LogError("ID is not an int for Fertility ");
            return null;
        }
        if (id == -1) {
            return null;//not needed
        }
        if (idToFertilities.ContainsKey(id) == false) {
            Debug.LogError("ID was not created before the depending Fertility! " + id);
            return null;
        }
        return idToFertilities[id];
    }

    void OnDestroy() {
        Instance = null;
    }
}
