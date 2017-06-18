using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;

public class PrototypController : MonoBehaviour {

	public string selectedLanguage = "EN";

	public static PrototypController Instance;
	public Dictionary<int,Structure>  structurePrototypes;
	public Dictionary<int,StructurePrototypeData>  structurePrototypeDatas;

	public Dictionary<int, Item> allItems;
	public static List<Item> buildItems;

	public List<Need> allNeeds;
	public Dictionary<Climate,List<Fertility>> allFertilities;
	public Dictionary<int,Fertility> idToFertilities;

	public Dictionary<int, Item> getCopieOfAllItems(){
		Dictionary<int, Item> items = new Dictionary<int, Item>();
		foreach (int item in allItems.Keys) {
			items.Add (item,allItems [item].Clone ());
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

	public StructurePrototypeData GetPrototypDataForID(int ID){
		return new StructurePrototypeData();
	}

	public void LoadFromXML(){
		if(allItems != null){
			return;
		}
		// prototypes of items
		allItems = new Dictionary<int, Item> ();
		buildItems = new List<Item> ();
		ReadItemsFromXML();

		// setup all prototypes of structures here 
		// load them from the 
		structurePrototypes = new Dictionary<int, Structure> ();
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
		allNeeds = new List<Need>();
		ReadNeedsFromXML ();
		idToFertilities = new Dictionary<int, Fertility> ();
		allFertilities = new Dictionary<Climate,List<Fertility>> ();
		ReadFertilitiesFromXML ();
	}

	///////////////////////////////////////
	/// XML LOADING FROM FILE
	/// 
	///////////////////////////////////////
	private void ReadItemsFromXML(){
		XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		TextAsset ta = ((TextAsset)Resources.Load("XMLs/items", typeof(TextAsset)));
		xmlDoc.LoadXml(ta.text); // load the file.
		foreach(XmlElement node in xmlDoc.SelectNodes("Items/Item")){
			Item item = new Item ();
			item.ID = int.Parse(node.GetAttribute("ID"));
			item.name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			item.Type = (ItemType) int.Parse (node.SelectSingleNode("Type").InnerText);
			allItems [item.ID] = item;
			if(item.Type == ItemType.Build){
				buildItems.Add (item); 
			}
		}
	}
	private void ReadFertilitiesFromXML(){
		XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		TextAsset ta = ((TextAsset)Resources.Load("XMLs/fertilities", typeof(TextAsset)));
		xmlDoc.LoadXml(ta.text); // load the file.
		foreach(XmlElement node in xmlDoc.SelectNodes("Fertilities/Fertility")){
			Fertility fer = new Fertility ();
			fer.ID = int.Parse(node.GetAttribute("ID"));
			fer.name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			idToFertilities.Add (fer.ID,fer); 
			string[] climates = node.SelectSingleNode("Climate").InnerText.Split (';');
			fer.climates = new Climate[climates.Length];
			for (int i = 0; i < climates.Length; i++) {
				fer.climates [i] = (Climate)int.Parse (climates [i]);
			}
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
			Need need = new Need ();
			need.ID = int.Parse(node.GetAttribute("ID"));
			need.name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			need.popCount = 0;//int.Parse(node.SelectSingleNode("Count").InnerText);
			need.startLevel = int.Parse(node.SelectSingleNode("Level").InnerText);
			int structure = int.Parse (node.SelectSingleNode ("Structure").InnerText);
			if (structure == -1) {
				int item = int.Parse(node.SelectSingleNode("Item").InnerText);
				if (allItems.ContainsKey (item)) {
					need.item = allItems [item];
				} else {
					Debug.Log (item + " is not in itemspool "+need.name);
					continue;
				}
			} else {
				if (structurePrototypes.ContainsKey (structure)) {
					//TODO maybe add a validation here and give error to user?
					need.structure = (NeedsBuilding)structurePrototypes [structure];
				} else {
					continue;
				}
			}
			float[] fs = new float[4];
			fs[0] = float.Parse(node.SelectSingleNode("Peasent").InnerText);
			fs[1] = float.Parse(node.SelectSingleNode("Citizen").InnerText);
			fs[2] = float.Parse(node.SelectSingleNode("Patrician").InnerText);
			fs[3] = float.Parse(node.SelectSingleNode("Nobleman").InnerText);
			need.uses = fs;
			allNeeds.Add (need);
		}
	}
	private void ReadStructuresFromXML(){
		XmlDocument xmlDoc = new XmlDocument();
		TextAsset ta = ((TextAsset)Resources.Load("XMLs/roads", typeof(TextAsset)));
		xmlDoc.LoadXml(ta.text); // load the file.
		ReadRoads (xmlDoc);

		//		ta = ((TextAsset)Resources.Load("XMLs/growables", typeof(TextAsset)));
		//		xmlDoc.LoadXml(ta.text); // load the file.
		//		ReadGrowables (xmlDoc);

		//		ta = ((TextAsset)Resources.Load("XMLs/marketbuildings", typeof(TextAsset)));
		//		xmlDoc.LoadXml(ta.text); // load the file.
		//		ReadMarketBuildings (xmlDoc);

		//		ta = ((TextAsset)Resources.Load("XMLs/produktionbuildings", typeof(TextAsset)));
		//		xmlDoc.LoadXml(ta.text); // load the file.
		//		ReadProduktionBuildings (xmlDoc);

	}
	private void ReadRoads(XmlDocument xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("Buildings/Road")){
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
			SetLanguageData (node,spd);

//			road.PopulationLevel= int.Parse(node.SelectSingleNode("Pop_Level").InnerText); 
			structurePrototypes [ID] =  new Road (ID,spd);
		}
	}
	private void ReadGrowables(XmlDocument xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("Buildings/Growable")){
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
			SetLanguageData (node,gpd);

//			string name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			structurePrototypes [ID] = new Growable (ID,gpd);
		}
	}
	private void ReadMarketBuildings(XmlDocument xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("Buildings/Logistic")){
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
			SetLanguageData (node, mpd);

			structurePrototypes [ID] = new MarketBuilding (ID,mpd);
		}
	}
	private void ReadProduktionBuildings(XmlDocument xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("Buildings/Produktion")){
			
			int ID = int.Parse(node.GetAttribute("ID"));

			ProductionPrototypeData ppd = new ProductionPrototypeData ();

			//THESE are fix and are not changed for any ProduktionBuilding
			ppd.maxOutputStorage = 5; // hardcoded 5 ? need this to change?
			ppd.hasHitbox = true;
			ppd.myBuildingTyp = BuildingTyp.Blocking;
			ppd.BuildTyp = BuildTypes.Single;
			ppd.canTakeDamage = true;
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
			SetLanguageData (node,ppd);

			//DO After loading from file
			ppd.maxIntake= new int[ppd.needIntake.Length];
			if (ppd.intake != null && ppd.needIntake!=null) {
				int i=0;
				foreach(int needed in ppd.needIntake){
					ppd.maxIntake[i] = 5*needed; // make it 5 times the needed
					i++;
				}
			}

			structurePrototypes [ID] = new ProductionBuilding (ID,ppd);
		}
	}

	private void ReadNeedsBuildings(XmlDocument xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("Buildings/NeedsBuilding")) {
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
			SetLanguageData (node, spd);

			structurePrototypes [ID] = new NeedsBuilding (ID,spd);
		}
	}
		
	private void ReadHomeBuildings(XmlDocument xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("Buildings/HomeBuilding")) {
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
			SetLanguageData (node, hpd);

			structurePrototypes [ID] = new HomeBuilding (ID,hpd);
		}
	}

	private void ReadWarehouse(XmlDocument xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("Buildings/Warehouse")) {
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
			SetLanguageData (node, mpd);

			structurePrototypes [ID] = new Warehouse (ID,mpd);
		}
	}
	private void ReadMineStructure(XmlDocument xmlDoc){
		foreach (XmlElement node in xmlDoc.SelectNodes("Buildings/MineStructure")) {
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
			SetLanguageData (node,mpd);

			structurePrototypes [ID] = new MineStructure (ID,mpd);

		}
	}
	private void SetData<T>(XmlElement node, ref T data){
		FieldInfo[] fields = typeof(T).GetFields();
		foreach(FieldInfo fi in fields){
			XmlNode n = node.SelectSingleNode(fi.Name);
			if(n!=null){
				if(fi.FieldType == typeof(Item)){
					
				} else
				if(fi.FieldType == typeof(Item[])){
						
				}
				fi.SetValue(data, Convert.ChangeType (n.InnerText,fi.FieldType));
			}
		}
	}
	private void SetLanguageData(XmlElement node, LanguageVariables data){
		FieldInfo[] fields = typeof(StructurePrototypeData).GetFields();
		string lang = selectedLanguage+"_";
		foreach(FieldInfo fi in fields){
			XmlNode n = node.SelectSingleNode(lang + fi.Name);
			if(n!=null){
				fi.SetValue(data, Convert.ChangeType (n.InnerText,fi.FieldType));
			}
		}
	}

}
