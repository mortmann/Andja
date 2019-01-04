using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using static Combat;

public class PrototypController : MonoBehaviour {
	public const int StartID = 1;

    public static int NumberOfPopulationLevels => 4;

	public static PrototypController Instance;
	public Dictionary<int,Structure>  structurePrototypes;
    private Dictionary<int, Unit> unitPrototypes;
    public Dictionary<Type, int> structureTypeToMaxBuildingLevel;

    public Dictionary<int,StructurePrototypeData>  structurePrototypeDatas;
	public Dictionary<int,ItemPrototypeData>  itemPrototypeDatas;
	public Dictionary<int,NeedPrototypeData>  needPrototypeDatas;
	public Dictionary<int,FertilityPrototypeData>  fertilityPrototypeDatas;
    public Dictionary<int,UnitPrototypeData> unitPrototypeDatas;

    public Dictionary<int, DamageType> damageTypeDatas;
    public Dictionary<int, ArmorType> armorTypeDatas;
    public Dictionary<int, PopulationLevelPrototypData> populationLevelDatas;
    public Dictionary<int, NeedGroupPrototypData> needGroupDatas;
    

    public Dictionary<int, Item> allItems;
	public static List<Item> buildItems;

	private List<Need> allNeeds;
    private Dictionary<int,List<NeedGroup>> populationLevelToNeedGroup;
    public Dictionary<Climate,List<Fertility>> allFertilities;

   

    public Dictionary<int,Fertility> idToFertilities;

    //TODO: need a way to get this to load in! probably with the rest
    //      of the data thats still needs to be read in like time for money ticks
    public ArmorType StructureArmor => armorTypeDatas[1];

    public Dictionary<int, Item> GetCopieOfAllItems(){
		Dictionary<int, Item> items = new Dictionary<int, Item>();
		foreach (int item in allItems.Keys) {
			int id = item;
			items.Add (id,allItems [id].Clone ());
		}
		return items;
	}
	public List<Need> GetCopieOfAllNeeds(){
		List<Need> needs = new List<Need> ();
		foreach (Need item in allNeeds) {
			needs.Add (item.Clone ());
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
            PopulationLevel clone = new PopulationLevel(item.Level, city, previous);
            previous = clone;
            populationLevels.Add(clone);
        }
        return populationLevels;
    }
	public ReadOnlyCollection<Need> GetAllNeeds(){
		return new ReadOnlyCollection<Need> (allNeeds);
	}
	// Use this for initialization
	void Awake () {		
		if (Instance != null) {
			Debug.LogError("There should never be two world controllers.");
		}
		Instance = this;

        LoadFromXML();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

	public StructurePrototypeData GetStructurePrototypDataForID(int ID){
		return structurePrototypeDatas [ID];
	}
	public ItemPrototypeData GetItemPrototypDataForID(int ID){
		if(itemPrototypeDatas.ContainsKey (ID)==false){
			Debug.Log (ID + "missing data!"); 
			return null;
		}
		return itemPrototypeDatas [ID];
	}
    internal PopulationLevelPrototypData GetPopulationLevelPrototypDataForLevel(int level) {
        return populationLevelDatas[level];
    }
    public FertilityPrototypeData GetFertilityPrototypDataForID(int ID){
		return fertilityPrototypeDatas [ID];
	}
	public NeedPrototypeData GetNeedPrototypDataForID(int ID){
		return needPrototypeDatas [ID];
	}
    internal NeedGroupPrototypData GetNeedGroupPrototypDataForID(int ID) {
        return needGroupDatas[ID];
    }
    internal List<NeedGroup> GetNeedPrototypDataForLevel(int level) {
        return populationLevelToNeedGroup[level];
    }
    public ICollection<Fertility> GetFertilitiesForClimate(Climate c){
		if(allFertilities.ContainsKey (c)==false){
			Debug.Log (c); 
			return null;
		}
		return allFertilities [c];
	}
	public void LoadFromXML(){
		if(allItems != null){
			return;
		}

        //SO THAT IT DOESNT USE FUCKIN COMMA AS THE DECIMAL SEPERATOR?!? WHY NOT POINT -Zoidberg
        //Why cant it be both -Fry
        //Good News everyone! Setting it to GB fixes that stupid thing! -Professor
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

        //fertilities
        allFertilities = new Dictionary<Climate,List<Fertility>> ();
		idToFertilities = new Dictionary<int, Fertility> ();
		fertilityPrototypeDatas = new Dictionary<int, FertilityPrototypeData> ();
		ReadFertilitiesFromXML ();

		// prototypes of items
		allItems = new Dictionary<int, Item> ();
		buildItems = new List<Item> ();
		itemPrototypeDatas = new Dictionary<int, ItemPrototypeData> ();
		ReadItemsFromXML();

        armorTypeDatas = new Dictionary<int, ArmorType>();
        damageTypeDatas = new Dictionary<int, DamageType>();
        ReadCombatFromXML();

        unitPrototypes = new Dictionary<int, Unit>();
        unitPrototypeDatas = new Dictionary<int, UnitPrototypeData>();
        ReadUnitsFromXML();
        // setup all prototypes of structures here 
        // load them from the 
        structureTypeToMaxBuildingLevel = new Dictionary<Type, int>();
        structurePrototypes = new Dictionary<int, Structure> ();
		structurePrototypeDatas = new Dictionary<int, StructurePrototypeData> ();
		ReadStructuresFromXML();

		//needs
		allNeeds = new List<Need>();
        populationLevelToNeedGroup = new Dictionary<int, List<NeedGroup>>();
        needPrototypeDatas = new Dictionary<int, NeedPrototypeData> ();
        needGroupDatas = new Dictionary<int, NeedGroupPrototypData>();
		ReadNeedsFromXML ();

        //other
        populationLevelDatas = new Dictionary<int, PopulationLevelPrototypData>();
        ReadOtherFromXML();

        Debug.Log("Read in fertilities types: " + allFertilities.Count + " with all " + fertilityPrototypeDatas.Count);
        Debug.Log ("Read in structures: " + structurePrototypes.Count);
        Debug.Log("Read in units: " + unitPrototypes.Count);
        Debug.Log ("Read in items: " + allItems.Count); 
		Debug.Log ("Read in needs: " + allNeeds.Count);
        Debug.Log("Read in needGroups: " + needGroupDatas.Count);
        Debug.Log("Read in damagetypes: " + damageTypeDatas.Count);
        Debug.Log("Read in armortypes: " + armorTypeDatas.Count);
        Debug.Log("Read in populationLevel: " + populationLevelDatas.Count);

        //Set it to default so it doesnt interfer with user interface informations
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InstalledUICulture;
    }

    private void ReadOtherFromXML() {
        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        TextAsset ta = ((TextAsset)Resources.Load("XMLs/other", typeof(TextAsset)));
        xmlDoc.LoadXml(ta.text); // load the file.
        foreach (XmlElement node in xmlDoc.SelectNodes("Other/PopulationLevel")) {
            PopulationLevelPrototypData plpd = new PopulationLevelPrototypData();
            int level = int.Parse(node.GetAttribute("Level"));
            SetData<PopulationLevelPrototypData>(node, ref plpd);
            plpd.needGroupList = populationLevelToNeedGroup[plpd.Level];
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
            foreach(XmlElement child in dict.ChildNodes) {
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
        if (structureTypeToMaxBuildingLevel.ContainsKey(type) == false)
            structureTypeToMaxBuildingLevel[type] =
                new List<Structure>(structurePrototypes.Values).FindAll(x => type == x.GetType())
                    .OrderByDescending(item => item.StructureLevel).First().StructureLevel;
        return structureTypeToMaxBuildingLevel[type];
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
        return typeListOrdered[typeListOrdered.FindIndex(x => x.StructureLevel == structureLevel) + 1 ].ID;
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
    private void ReadItemsFromXML(){
		XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		TextAsset ta = ((TextAsset)Resources.Load("XMLs/items", typeof(TextAsset)));
		xmlDoc.LoadXml(ta.text); // load the file.
		foreach(XmlElement node in xmlDoc.SelectNodes("items/Item")){
			ItemPrototypeData ipd = new ItemPrototypeData ();
			int id = int.Parse(node.GetAttribute("ID"));
			SetData<ItemPrototypeData> (node,ref ipd);

			itemPrototypeDatas [id] = ipd;
			Item item = new Item (id,ipd);

			if(item.Type == ItemType.Build){
				buildItems.Add (item); 
			}
			allItems [id] = item;
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
            unitPrototypes.Add(id, new Unit(id,upd));
        }
        foreach (XmlElement node in xmlDoc.SelectNodes("units/ship")) {
            ShipPrototypeData spd = new ShipPrototypeData();
            int id = int.Parse(node.GetAttribute("ID"));
            SetData<ShipPrototypeData>(node, ref spd);
            unitPrototypeDatas[id] = spd;
            unitPrototypes.Add(id, new Ship(id, spd));
        }
    }
    private void ReadFertilitiesFromXML(){
		XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		TextAsset ta = ((TextAsset)Resources.Load("XMLs/fertilities", typeof(TextAsset)));
		xmlDoc.LoadXml(ta.text); // load the file.
		foreach(XmlElement node in xmlDoc.SelectNodes("fertilities/Fertility")){
			int ID = int.Parse(node.GetAttribute("ID"));

			FertilityPrototypeData fpd = new FertilityPrototypeData ();

			SetData<FertilityPrototypeData> (node,ref fpd);
			
			Fertility fer = new Fertility (ID,fpd);
			idToFertilities.Add (fer.ID,fer); 
			fertilityPrototypeDatas [ID] = fpd;
			foreach (Climate item in fer.Climates) {
				if (allFertilities.ContainsKey (item)==false) {
                    List<Fertility> f = new List<Fertility> {
                        fer
                    };
                    allFertilities.Add (item, f);
				} else {
					allFertilities [item].Add (fer);
				}
			}
		}
	}
	private void ReadNeedsFromXML(){
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
        foreach (XmlElement node in xmlDoc.SelectNodes("needs/Need")){
			NeedPrototypeData npd = new NeedPrototypeData ();
			int ID = int.Parse(node.GetAttribute("ID"));
			SetData<NeedPrototypeData> (node,ref npd);
            

            needPrototypeDatas.Add (ID,npd);
            if (npd.item == null && npd.structures == null)
                continue;

            Need n = new Need(ID, npd);
            allNeeds.Add (n);

            if (levelToNeedList.ContainsKey(npd.startLevel) == false) {
                levelToNeedList[npd.startLevel] = new List<Need>();
            }
            levelToNeedList[npd.startLevel].Add(n.Clone());
        }

        foreach(int level in levelToNeedList.Keys) {
            List<NeedGroup> ngs = new List<NeedGroup>();
            populationLevelToNeedGroup.Add(level, ngs);
            foreach (Need need in levelToNeedList[level]) {
                if (ngs.Exists(x=> x.ID == need.Group.ID) == false)
                    ngs.Add(new NeedGroup(need.Group.ID));
                ngs[need.Group.ID].AddNeed(need.Clone()); 
            }
        }

    }
        private void ReadStructuresFromXML(){
		XmlDocument xmlDoc = new XmlDocument();
		TextAsset ta = ((TextAsset)Resources.Load("XMLs/structures", typeof(TextAsset)));
		xmlDoc.LoadXml(ta.text); // load the file.
		ReadRoads (xmlDoc.SelectSingleNode ("structures/roads"));
		ReadGrowables (xmlDoc.SelectSingleNode ("structures/growables"));
		ReadFarms (xmlDoc.SelectSingleNode ("structures/farms"));
		ReadMarketBuildings (xmlDoc.SelectSingleNode ("structures/markets"));
		ReadProductionBuildings (xmlDoc.SelectSingleNode ("structures/productions"));
		ReadNeedsBuildings (xmlDoc.SelectSingleNode ("structures/needsbuildings"));
		ReadMineStructure (xmlDoc.SelectSingleNode ("structures/mines"));
		ReadHomeBuildings (xmlDoc.SelectSingleNode ("structures/homes"));
		ReadWarehouse (xmlDoc.SelectSingleNode ("structures/warehouses"));
        ReadMilitaryBuildings(xmlDoc.SelectSingleNode("structures/militarybuildings"));

    }

    private void ReadMilitaryBuildings(XmlNode xmlDoc) {
        foreach (XmlElement node in xmlDoc.SelectNodes("militarybuilding")) {
            int ID = int.Parse(node.GetAttribute("ID"));

            MilitaryBuildingPrototypeData mpd = new MilitaryBuildingPrototypeData();
            //THESE are fix and are not changed for any 
            //!not anymore
            SetData<MilitaryBuildingPrototypeData>(node, ref mpd);
            structurePrototypeDatas.Add(ID, mpd);
            structurePrototypes[ID] = new MilitaryBuilding(ID, mpd);
        }
    }

    private void ReadRoads(XmlNode xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("road")){
			int ID = int.Parse(node.GetAttribute("ID"));

            StructurePrototypeData spd = new StructurePrototypeData {
                //THESE are fix and are not changed for any road
                tileWidth = 1,
                tileHeight = 1,
                BuildTyp = BuildTypes.Path,
                myBuildingTyp = BuildingTyp.Pathfinding,
                canBeUpgraded = true,
                //!not anymore
                maintenancecost = 0,
                buildcost = 25,
                Name = "Testroad",
                buildingRange = 0,
                StructureLevel = 0
            };

            SetData<StructurePrototypeData> (node,ref spd);

			structurePrototypeDatas.Add (ID,spd);
			structurePrototypes [ID] =  new Road (ID,spd);

		}
	}
	private void ReadGrowables(XmlNode xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("growable")){
			int ID = int.Parse(node.GetAttribute("ID"));

            GrowablePrototypeData gpd = new GrowablePrototypeData {
                //THESE are fix and are not changed for any growable
                forMarketplace = false,
                maxNumberOfWorker = 0,
                tileWidth = 1,
                tileHeight = 1,
                myBuildingTyp = BuildingTyp.Free,
                BuildTyp = BuildTypes.Drag,
                buildcost = 50,
                maxOutputStorage = 1
            };
            SetData<GrowablePrototypeData> (node,ref  gpd);
			structurePrototypeDatas.Add (ID,gpd);
			structurePrototypes [ID] = new Growable (ID,gpd);
		}
	}
	private void ReadFarms(XmlNode xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("farm")){
			int ID = int.Parse(node.GetAttribute("ID"));

			FarmPrototypData fpd = new FarmPrototypData ();
			//THESE are fix and are not changed for any 
			//!not anymore
			SetData<FarmPrototypData> (node,ref  fpd);
			structurePrototypeDatas.Add (ID,fpd);
			structurePrototypes [ID] = new Farm (ID,fpd);
		}
	}
	private void ReadMarketBuildings(XmlNode xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("market")){
			int ID = int.Parse(node.GetAttribute("ID"));
            MarketPrototypData mpd = new MarketPrototypData {
                //THESE are fix and are not changed for any MarketBuilding
                hasHitbox = true,
                tileWidth = 4,
                tileHeight = 4,
                BuildTyp = BuildTypes.Single,
                myBuildingTyp = BuildingTyp.Blocking,
                buildingRange = 18,
                canTakeDamage = true,

                Name = "market",
                buildcost = 500,
                maintenancecost = 10
            };

            SetData<MarketPrototypData> (node,ref  mpd);

			structurePrototypeDatas.Add (ID,mpd);
			structurePrototypes [ID] = new MarketBuilding (ID,mpd);
		}
	}
	private void ReadProductionBuildings(XmlNode xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("production")){
			
			int ID = int.Parse(node.GetAttribute("ID"));

            ProductionPrototypeData ppd = new ProductionPrototypeData {

                //THESE are fix and are not changed for any ProduktionBuilding
                maxOutputStorage = 5, // hardcoded 5 ? need this to change?
                hasHitbox = true,
                myBuildingTyp = BuildingTyp.Blocking,
                BuildTyp = BuildTypes.Single,
                canTakeDamage = true,
                forMarketplace = true,
                //!not anymore

                Name = "TEST Production",
                maxNumberOfWorker = 1
            };
            SetData<ProductionPrototypeData> (node, ref ppd);

			//DO After loading from file

			structurePrototypeDatas.Add (ID,ppd);
			structurePrototypes [ID] = new ProductionBuilding (ID,ppd);
		}
	}

	private void ReadNeedsBuildings(XmlNode xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("needsbuilding")) {
			int ID = int.Parse (node.GetAttribute ("ID"));
            StructurePrototypeData spd = new StructurePrototypeData {
                //THESE are fix and are not changed for any NeedsBuilding
                //!not anymore
                tileWidth = 2,
                tileHeight = 2,
                BuildTyp = BuildTypes.Single,
                myBuildingTyp = BuildingTyp.Blocking,
                Name = "NeedsBuilding",
                maintenancecost = 100
            };

            SetData<StructurePrototypeData> (node,ref spd);

			structurePrototypeDatas.Add (ID,spd);
			structurePrototypes [ID] = new NeedsBuilding (ID,spd);
		}
	}
		
	private void ReadHomeBuildings(XmlNode xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("home")) {
			int ID = int.Parse (node.GetAttribute ("ID"));
            HomePrototypeData hpd = new HomePrototypeData {
                //THESE are fix and are not changed for any HomeBuilding
                tileWidth = 2,
                tileHeight = 2,
                BuildTyp = BuildTypes.Drag,
                myBuildingTyp = BuildingTyp.Blocking,
                buildingRange = 0,
                hasHitbox = true,
                canTakeDamage = true,
                maintenancecost = 0
            };

            SetData<HomePrototypeData> (node,ref hpd);
            structurePrototypeDatas.Add(ID, hpd);
            structurePrototypes[ID] = new HomeBuilding(ID, hpd);

            int prevID = GetStructureIDForTypeNeighbourStructureLevel(typeof(HomeBuilding), hpd.StructureLevel, false);
            if(prevID != -1) {
                HomePrototypeData prev = (HomePrototypeData)structurePrototypeDatas[prevID];
                ((HomePrototypeData)hpd).previouseMaxLivingSpaces = prev == null ? 0 : prev.maxLivingSpaces;
                prev.UpgradeItems = hpd.buildingItems;
                prev.UpgradeCost = hpd.buildcost;
            } 
		}
	}

	private void ReadWarehouse(XmlNode xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("warehouse")) {
			int ID = int.Parse (node.GetAttribute ("ID"));
            MarketPrototypData mpd = new MarketPrototypData {
                //THESE are fix and are not changed for any Warehouse
                contactRange = 6.3f,
                BuildTyp = BuildTypes.Single,
                hasHitbox = true,
                canTakeDamage = true,
                buildingRange = 18,

                //!not anymore
                tileWidth = 3,
                tileHeight = 3,
                Name = "warehouse",
                buildcost = 500,
                maintenancecost = 10,
                mustFrontBuildDir = Direction.W
            };

            SetData<MarketPrototypData> (node,ref mpd);
			structurePrototypeDatas.Add (ID,mpd);
			structurePrototypes [ID] = new Warehouse (ID,mpd);
		}
	}
	private void ReadMineStructure(XmlNode xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("mine")) {
			int ID = int.Parse (node.GetAttribute ("ID"));

            MinePrototypData mpd = new MinePrototypData {
                //THESE are fix and are not changed for any Warehouse
                tileWidth = 2,
                tileHeight = 3,
                Name = "Mine",
                myBuildingTyp = BuildingTyp.Blocking,
                BuildTyp = BuildTypes.Single,
                hasHitbox = true,
                buildingRange = 0,

                //!not anymore
                output = new Item[1]
            };
            mpd.output[0] = PrototypController.Instance.allItems [3];
			mpd.myRessource = "stone";
			mpd.maxOutputStorage = 5;
			mpd.produceTime = 15f;

			SetData<MinePrototypData> (node,ref mpd);

			structurePrototypeDatas.Add (ID,mpd);
			structurePrototypes [ID] = new MineStructure (ID,mpd);

		}
	}
	private void SetData<T>(XmlElement node, ref T data){
		FieldInfo[] fields = typeof(T).GetFields();
		HashSet<String> langs = new HashSet<String> ();
		foreach(FieldInfo f in typeof(LanguageVariables).GetFields ()){
			langs.Add (f.Name);
		}
		foreach(FieldInfo fi in fields){
			XmlNode n = node.SelectSingleNode(fi.Name);
			if(langs.Contains (fi.Name)){
				if(n==null){
					//TODO activate this warning when all data is correctly created
					//				Debug.LogWarning (fi.Name + " selected language not avaible!");
					continue;
				}
				XmlNode textNode = n.SelectSingleNode("entry[@lang='"+UILanguageController.selectedLanguage.ToString ()+"']");

				if(textNode!=null){
					fi.SetValue(data, Convert.ChangeType (textNode.InnerXml,fi.FieldType));
				}
				continue;
			}
			if(n!=null){
				if(fi.FieldType == typeof(Item)){
					fi.SetValue (data,NodeToItem (n));
					continue;
				} 
				if(fi.FieldType == typeof(Item[])){
					List<Item> items = new List<Item> ();
					foreach(XmlNode item in n.ChildNodes){
						items.Add (NodeToItem (item));
					}
					fi.SetValue (data,items.ToArray ());
					continue;
				}
				if(fi.FieldType.IsSubclassOf (typeof(Structure))){
					fi.SetValue (data, NodeToStructure (n));
					continue;
				}
                if (fi.FieldType.IsSubclassOf(typeof(Structure[])) || fi.FieldType == (typeof(Structure[]))) {
                    List<Structure> items = new List<Structure>();
                    foreach (XmlNode item in n.ChildNodes) {
                        items.Add(NodeToStructure(item));
                    }
                    fi.SetValue(data, items.ToArray());
                    continue;
                }
                if (fi.FieldType == typeof(NeedGroupPrototypData)) {
                    fi.SetValue(data, NodeToNeedGroupPrototypData(n));
                    continue;
                }
                if (fi.FieldType == typeof(ArmorType)) {
                    fi.SetValue(data, NodeToArmorType(n));
                    continue;
                }
                if (fi.FieldType == typeof(DamageType)) {
                    fi.SetValue(data, NodeToDamageType(n));
                    continue;
                }
                if (fi.FieldType==typeof(Fertility)){
					fi.SetValue (data, NodeToFertility (n));
					continue;
				}
                if (fi.FieldType.IsSubclassOf(typeof(Unit))) {
                    fi.SetValue(data, NodeToUnit(n));
                    continue;
                }
                if (fi.FieldType.IsSubclassOf(typeof(Unit[])) || fi.FieldType == (typeof(Unit[]))) {
                    List<Unit> items = new List<Unit>();
                    foreach (XmlNode item in n.ChildNodes) {
                        items.Add(NodeToUnit(item));
                    }
                    fi.SetValue(data, items.ToArray());
                    continue;
                }
                if (fi.FieldType == (typeof(float[]))) {
                    List<float> items = new List<float>();
                    foreach (XmlNode item in n.ChildNodes) {
                        int id = int.Parse(item.Attributes[0].InnerXml);
                        items.Insert(id, float.Parse(item.InnerXml));
                    }
                    fi.SetValue(data, items.ToArray());
                    continue;
                }
                if (fi.FieldType.IsEnum){
                    fi.SetValue(data, Enum.Parse(fi.FieldType, n.InnerXml, true) );
					continue;
				}
				if(fi.FieldType.IsArray && fi.FieldType.GetElementType ().IsEnum){
                    var listType = typeof(List<>);
                    var constructedListType = listType.MakeGenericType(fi.FieldType.GetElementType());

                    var list = (IList)Activator.CreateInstance(constructedListType);

                    int i = 0;
					foreach (XmlNode item in n.ChildNodes) {
						if(item.Name!=fi.FieldType.GetElementType ().ToString ()){
							continue;
						}
                        list.Add(Enum.Parse(fi.FieldType.GetElementType(), item.InnerXml, true));
                        i++;
                    }
                    Array enumArray = Array.CreateInstance(fi.FieldType.GetElementType(), list.Count);
                    list.CopyTo(enumArray, 0);
                    fi.SetValue(data, Convert.ChangeType(enumArray,fi.FieldType));
					continue;
				}
                if(fi.FieldType == typeof(Dictionary<ArmorType, float>)) {
                    // this will get set in load xml directly and not here!
                    continue;
                }
                try {
                    fi.SetValue(data, Convert.ChangeType(n.InnerXml, fi.FieldType,System.Globalization.CultureInfo.InvariantCulture));
                }
                catch {
                    Debug.Log(fi.Name + " is faulty!");
                }
			}
		}

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

    private Item NodeToItem(XmlNode n){
		int id = -1;
		if(int.TryParse (n.Attributes["ID"].Value,out id)==false){
			Debug.LogError ("ID is not an int for ITEM ");
			return null;
		}
		if(allItems.ContainsKey (id)==false){
			Debug.LogError ("ITEM ID was not created! " + id);
			return null;
		}
		Item clone = allItems [id].Clone ();
		if(n.SelectSingleNode ("count")!=null){
			int count = 0;
			if(int.TryParse (n.SelectSingleNode ("count").InnerXml,out count)==false){
				Debug.LogError ("Count is not an int");
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
    private Structure NodeToStructure(XmlNode n){
		int id = -1;
		if(int.TryParse (n.InnerXml,out id)==false){
			Debug.LogError ("ID is not an int for Structure ");
			return null;
		}
		if(id==-1){
			return null;//not needed
		}
		if(structurePrototypes.ContainsKey (id)==false){
			Debug.LogError ("ID was not created before the depending Structure! " + id);
			return null;
		}
		return structurePrototypes [id];
	}
    
    private Fertility NodeToFertility(XmlNode n){
		int id = -1;
		if(int.TryParse (n.InnerXml,out id)==false){
			Debug.LogError ("ID is not an int for Fertility ");
			return null;
		}
		if(id==-1){
			return null;//not needed
		}
		if(idToFertilities.ContainsKey (id)==false){
			Debug.LogError ("ID was not created before the depending Fertility! " + id);
			return null;
		}
		return idToFertilities [id];
	}

    void OnDestroy() {
        Instance = null;
    }
}
