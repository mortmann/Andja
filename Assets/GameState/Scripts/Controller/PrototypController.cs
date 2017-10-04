using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;

public enum Language {English,German}

public class PrototypController : MonoBehaviour {
	public const int StartID = 1;
	public Language selectedLanguage = Language.English;

	public static PrototypController Instance;
	public Dictionary<int,Structure>  structurePrototypes;
	public Dictionary<int,StructurePrototypeData>  structurePrototypeDatas;
	public Dictionary<int,ItemPrototypeData>  itemPrototypeDatas;
	public Dictionary<int,NeedPrototypeData>  needPrototypeDatas;
	public Dictionary<int,FertilityPrototypeData>  fertilityPrototypeDatas;

	public Dictionary<int, Item> allItems;
	public static List<Item> buildItems;

	public List<Need> allNeeds;
	public Dictionary<Climate,List<Fertility>> allFertilities;
	public Dictionary<int,Fertility> idToFertilities;

	public Dictionary<int, Item> getCopieOfAllItems(){
		Dictionary<int, Item> items = new Dictionary<int, Item>();
		foreach (int item in allItems.Keys) {
			int id = item;
			items.Add (id,allItems [id].Clone ());
		}
		return items;
	}
	// Use this for initialization
	void Awake () {
		if (Instance != null) {
			Debug.LogError("There should never be two world controllers.");
		}
		Instance = this;

		LoadFromXML ();

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
	public FertilityPrototypeData GetFertilityPrototypDataForID(int ID){
		return fertilityPrototypeDatas [ID];
	}
	public NeedPrototypeData GetNeedPrototypDataForID(int ID){
		return needPrototypeDatas [ID];
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

		allNeeds = new List<Need>();
		needPrototypeDatas = new Dictionary<int, NeedPrototypeData> ();
		ReadNeedsFromXML ();

		// setup all prototypes of structures here 
		// load them from the 
		structurePrototypes = new Dictionary<int, Structure> ();
		structurePrototypeDatas = new Dictionary<int, StructurePrototypeData> ();
		ReadStructuresFromXML();
//		structurePrototypes.Add (5, new MineStructure (5));
//		structurePrototypes.Add (30, new NeedsBuilding (30));
//		structurePrototypes.Add (1, new MarketBuilding (1));
//		structurePrototypes.Add (2, new Warehouse (2));
//		structurePrototypes.Add (3, new Growable (3));
//		,"tree",allItems[1]));
//		Item item =  allItems[1] ;
//		structurePrototypes.Add (4, new Farm(
//			4,"lumberjack",
//			3,item,structurePrototypes[3],
//			2,2,500,50
//		));
//		structurePrototypes.Add (6,new HomeBuilding (6));
//		Item[] temp1 = new Item[1];
//		temp1 [0] = allItems [47].Clone ();
//		Item[] temp2 = new Item[1];
//		temp2 [0] = allItems [48].Clone();
//		int[] ints = { 1 };
//		structurePrototypes.Add(7,new ProductionBuilding(7,"Hanfweber",temp1,ints,1,temp2,3,2,1000,null,100));
		//needs



		Debug.Log ("Read in structures: " +structurePrototypes.Count);
		Debug.Log ("Read in items: " + allItems.Count); 
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
	private void ReadFertilitiesFromXML(){
		XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		TextAsset ta = ((TextAsset)Resources.Load("XMLs/fertilities", typeof(TextAsset)));
		xmlDoc.LoadXml(ta.text); // load the file.
		foreach(XmlElement node in xmlDoc.SelectNodes("fertilities/Fertility")){
			int ID = int.Parse(node.GetAttribute("ID"));

			FertilityPrototypeData fpd = new FertilityPrototypeData ();

			SetData<FertilityPrototypeData> (node,ref fpd);

			XmlNodeList xnl= node.SelectNodes ("climates/Climate");
			fpd.climates = new Climate[xnl.Count];
			for(int i=0; i<xnl.Count;i++){
				fpd.climates [i] = (Climate)int.Parse ( xnl [i].InnerXml );
			}
			Fertility fer = new Fertility (ID,fpd);
			idToFertilities.Add (fer.ID,fer); 
			fertilityPrototypeDatas [ID] = fpd;
			foreach (Climate item in fer.climates) {
				if (allFertilities.ContainsKey (item)==false) {
					List<Fertility> f = new List<Fertility> ();
					f.Add (fer);
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
		foreach(XmlElement node in xmlDoc.SelectNodes("Needs/Need")){
			NeedPrototypeData npd = new NeedPrototypeData ();
			int ID = int.Parse(node.GetAttribute("ID"));
			SetData<NeedPrototypeData> (node,ref npd);

			float[] fs = new float[4];
			fs[0] = float.Parse(node.SelectSingleNode("Peasent").InnerText);
			fs[1] = float.Parse(node.SelectSingleNode("Citizen").InnerText);
			fs[2] = float.Parse(node.SelectSingleNode("Patrician").InnerText);
			fs[3] = float.Parse(node.SelectSingleNode("Nobleman").InnerText);
			npd.uses = fs;
			needPrototypeDatas.Add (ID,npd);
			allNeeds.Add (new Need(ID,npd));
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
	}
	private void ReadRoads(XmlNode xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("road")){
			int ID = int.Parse(node.GetAttribute("ID"));
			
			StructurePrototypeData spd = new StructurePrototypeData ();
			//THESE are fix and are not changed for any road
			spd.tileWidth = 1;
			spd.tileHeight = 1;
			spd.BuildTyp = BuildTypes.Path;
			spd.myBuildingTyp = BuildingTyp.Pathfinding;
			spd.canBeUpgraded = true;
			//!not anymore
			spd.maintenancecost = 0;
			spd.buildcost = 25;
			spd.Name = "Testroad";
			spd.buildingRange = 0;
			spd.StructureLevel = 0;

			SetData<StructurePrototypeData> (node,ref spd);

			structurePrototypeDatas.Add (ID,spd);
			structurePrototypes [ID] =  new Road (ID,spd);

		}
	}
	private void ReadGrowables(XmlNode xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("growable")){
			int ID = int.Parse(node.GetAttribute("ID"));

			GrowablePrototypData gpd = new GrowablePrototypData ();
			//THESE are fix and are not changed for any growable
			gpd.forMarketplace = false;
			gpd.maxNumberOfWorker = 0;
			gpd.tileWidth = 1;
			gpd.tileHeight = 1;
			gpd.myBuildingTyp = BuildingTyp.Free;
			gpd.BuildTyp = BuildTypes.Drag;
			gpd.buildcost = 50;
			gpd.maxOutputStorage = 1;
			//!not anymore

//			growTime = 100f;
//			hasHitbox = false;
//			canBeBuildOver = true;
//			this.name = "Testgrowable";
//			canBeBuildOver = true;
//			gpd.output = new Item[]{produceItem};
//			gpd.fer = fer;

			SetData<GrowablePrototypData> (node,ref  gpd);
			structurePrototypeDatas.Add (ID,gpd);
			structurePrototypes [ID] = new Growable (ID,gpd);
		}
	}
	private void ReadFarms(XmlNode xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("farm")){
			int ID = int.Parse(node.GetAttribute("ID"));

			FarmPrototypData fpd = new FarmPrototypData ();
			//THESE are fix and are not changed for any growable
			//!not anymore
			SetData<FarmPrototypData> (node,ref  fpd);
			structurePrototypeDatas.Add (ID,fpd);
			structurePrototypes [ID] = new Farm (ID,fpd);
		}
	}
	private void ReadMarketBuildings(XmlNode xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("market")){
			int ID = int.Parse(node.GetAttribute("ID"));
			MarketPrototypData mpd = new MarketPrototypData ();
			//THESE are fix and are not changed for any growable
			mpd.hasHitbox = true;
			mpd.tileWidth = 4;
			mpd.tileHeight = 4;
			mpd.BuildTyp = BuildTypes.Single;
			mpd.myBuildingTyp = BuildingTyp.Blocking;
			mpd.buildingRange = 18;
			mpd.canTakeDamage = true;

			mpd.Name = "market";
			mpd.buildcost = 500;
			mpd.maintenancecost = 10;

			SetData<MarketPrototypData> (node,ref  mpd);

			structurePrototypeDatas.Add (ID,mpd);
			structurePrototypes [ID] = new MarketBuilding (ID,mpd);
		}
	}
	private void ReadProductionBuildings(XmlNode xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("production")){
			
			int ID = int.Parse(node.GetAttribute("ID"));

			ProductionPrototypeData ppd = new ProductionPrototypeData ();

			//THESE are fix and are not changed for any ProduktionBuilding
			ppd.maxOutputStorage = 5; // hardcoded 5 ? need this to change?
			ppd.hasHitbox = true;
			ppd.myBuildingTyp = BuildingTyp.Blocking;
			ppd.BuildTyp = BuildTypes.Single;
			ppd.canTakeDamage = true;
			ppd.forMarketplace = true;
			//!not anymore

			ppd.Name = "TEST Production";
			ppd.maxNumberOfWorker = 1;
//			ppd.mustBeBuildOnShore = mustBeBuildOnShore;
//			ppd.maintenancecost = maintenancecost;
//			ppd.intake = intake;
//			ppd.needIntake = needIntake;
//			ppd.produceTime = produceTime;
//			ppd.output = output;
//			ppd.tileWidth = tileWidth;
//			ppd.tileHeight = tileHeight;

			SetData<ProductionPrototypeData> (node, ref ppd);

			//DO After loading from file
			ppd.maxIntake= new int[ppd.intake.Length];
			if (ppd.intake != null && ppd.intake!=null) {
				int i=0;
				foreach(Item needed in ppd.intake){
					ppd.maxIntake[i] = 5*needed.count; // make it 5 times the needed
					i++;
				}
			}

			structurePrototypeDatas.Add (ID,ppd);
			structurePrototypes [ID] = new ProductionBuilding (ID,ppd);
		}
	}

	private void ReadNeedsBuildings(XmlNode xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("needsbuilding")) {
			int ID = int.Parse (node.GetAttribute ("ID"));
			StructurePrototypeData spd = new StructurePrototypeData ();
			//THESE are fix and are not changed for any NeedsBuilding
			 
			//!not anymore
			spd.tileWidth = 2;
			spd.tileHeight = 2;
			spd.BuildTyp = BuildTypes.Single;
			spd.myBuildingTyp =	BuildingTyp.Blocking;
			spd.Name = "NeedsBuilding";
			spd.maintenancecost = 100;

			SetData<StructurePrototypeData> (node,ref spd);

			structurePrototypeDatas.Add (ID,spd);
			structurePrototypes [ID] = new NeedsBuilding (ID,spd);
		}
	}
		
	private void ReadHomeBuildings(XmlNode xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("home")) {
			int ID = int.Parse (node.GetAttribute ("ID"));
			HomePrototypeData hpd = new HomePrototypeData ();
			//THESE are fix and are not changed for any HomeBuilding
			hpd.tileWidth = 2;
			hpd.tileHeight = 2;
			hpd.BuildTyp = BuildTypes.Drag;
			hpd.myBuildingTyp =	BuildingTyp.Blocking;
			hpd.buildingRange = 0;
			hpd.hasHitbox = true;
			hpd.canTakeDamage = true;
			hpd.maintenancecost = 0;
			//!not anymore
//			hpd.people = 1;
//			hpd.maxLivingSpaces = 8;
//			hpd.buildingLevel = 0;
//			hpd.Name = "Home";
//			hpd.increaseSpeed = 3;
//			hpd.decreaseSpeed = 2;

			SetData<HomePrototypeData> (node,ref hpd);

			structurePrototypeDatas.Add (ID,hpd);
			structurePrototypes [ID] = new HomeBuilding (ID,hpd);
		}
	}

	private void ReadWarehouse(XmlNode xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("warehouse")) {
			int ID = int.Parse (node.GetAttribute ("ID"));
			MarketPrototypData mpd = new MarketPrototypData ();
			//THESE are fix and are not changed for any Warehouse
			mpd.contactRange = 6.3f;
			mpd.mustBeBuildOnShore = true;
			mpd.BuildTyp = BuildTypes.Single;
			mpd.showExtraUI = true;
			mpd.hasHitbox = true;
			mpd.canTakeDamage = true;
			mpd.buildingRange = 18;

			//!not anymore
			mpd.tileWidth = 3;
			mpd.tileHeight = 3;
			mpd.Name = "warehouse";
			mpd.buildcost = 500;
			mpd.maintenancecost = 10;
			mpd.mustFrontBuildDir = Direction.W;

			SetData<MarketPrototypData> (node,ref mpd);
			structurePrototypeDatas.Add (ID,mpd);
			structurePrototypes [ID] = new Warehouse (ID,mpd);
		}
	}
	private void ReadMineStructure(XmlNode xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("mine")) {
			int ID = int.Parse (node.GetAttribute ("ID"));

			MinePrototypData mpd = new MinePrototypData ();
			//THESE are fix and are not changed for any Warehouse
			mpd.mustBeBuildOnMountain = true;
			mpd.tileWidth = 2;
			mpd.tileHeight = 3;
			mpd.Name = "Mine";
			mpd.myBuildingTyp = BuildingTyp.Blocking;
			mpd.BuildTyp = BuildTypes.Single;
			mpd.hasHitbox = true;
			mpd.buildingRange = 0;

			//!not anymore
			mpd.output = new Item[1];
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
				XmlNode textNode = n.SelectSingleNode("entry[@lang='"+selectedLanguage.ToString ()+"']");

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
				if(fi.FieldType==typeof(Fertility)){
					fi.SetValue (data, NodeToFertility (n));
					continue;
				}
				if(fi.FieldType.IsEnum){
					int ordinal = -1;
					if(int.TryParse (n.InnerXml,out ordinal)==false){
						Debug.LogError ("Enum was not a int");
						continue;
					}
					fi.SetValue(data, Convert.ChangeType (ordinal,Enum.GetUnderlyingType (fi.FieldType)));
					continue;
				}
				if(fi.FieldType.IsArray && fi.FieldType.GetElementType ().IsEnum){
					int t = Enum.GetValues (fi.FieldType.GetElementType ()).Length;
					var enumArray = Array.CreateInstance (fi.FieldType.GetElementType (),t );
					foreach (XmlNode item in n.ChildNodes) {
						if(item.Name!=fi.FieldType.GetElementType ().ToString ()){
							continue;
						}
						int ordinal = -1;
						if(int.TryParse (item.InnerXml,out ordinal)==false){
							Debug.LogError ("Enum was not a int");
							continue;
						}
					}
					fi.SetValue(data, Convert.ChangeType (enumArray,fi.FieldType));
					continue;
				}

				fi.SetValue(data, Convert.ChangeType (n.InnerXml,fi.FieldType));
			}
		}
	}

	private Item NodeToItem(XmlNode n){
		int id = -1;
		if(int.TryParse (n.Attributes["ID"].Value,out id)==false){
			Debug.LogError ("ID is not an int for ITEM ");
			return null;
		}
		if(allItems.ContainsKey (id)==false){
			Debug.LogError ("ID was not created! " + id);
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
			Debug.LogError ("ID was not created before the depending Structure!");
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
			Debug.LogError ("ID was not created before the depending Fertility!");
			return null;
		}
		return idToFertilities [id];
	}


}
