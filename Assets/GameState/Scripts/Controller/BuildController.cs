using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;

public enum BuildStateModes {None,Build,Destroy};

public class BuildController : MonoBehaviour {
    public static BuildController Instance { get; protected set; }
	protected BuildStateModes _buildState;
	public BuildStateModes BuildState { 
			get { return _buildState; } 
			set { 
				if (_buildState == value) {
					return;
				}
				World.current.resetIslandMark ();
				_buildState = value;
				if (cbBuildStateChange != null)
					cbBuildStateChange (_buildState); 
				}
			}
	public Structure toBuildStructure;
	public Dictionary<int,Structure>  structurePrototypes;
	public Dictionary<int, Item> allItems;
	public int buildID = 0;
	public Dictionary<int,Structure> loadedToPlaceStructure;
	public Dictionary<int,Tile> loadedToPlaceTile;
	public List<Need> allNeeds;
	public Dictionary<Climate,List<Fertility>> allFertilities;
	public Dictionary<int,Fertility> idToFertilities;
	Action<Structure> cbStructureCreated;
	Action<City> cbCityCreated;
	Action<BuildStateModes> cbBuildStateChange;

	public Dictionary<int, Item> getCopieOfAllItems(){
		Dictionary<int, Item> items = new Dictionary<int, Item>();
		foreach (int item in allItems.Keys) {
			items.Add (item,allItems [item].Clone ());
		}
		return items;
	}

	public void Awake(){
		if (Instance != null) {
			Debug.LogError("There should never be two world controllers.");
		}
		Instance = this;
		BuildState = BuildStateModes.None;
		buildID = 0;
		// prototypes of items
		allItems = new Dictionary<int, Item> ();
		ReadItemsFromXML();

		loadedToPlaceTile = new Dictionary<int, Tile> ();
		loadedToPlaceStructure = new Dictionary<int, Structure> ();

		// setup all prototypes of structures here 
		// load them from the 
		structurePrototypes = new Dictionary<int, Structure> ();
		ReadStructuresFromXML();
		structurePrototypes.Add (5, new MineStructure (5));
		structurePrototypes.Add (30, new NeedsBuilding (30));
		structurePrototypes.Add (1, new MarketBuilding (1));
		structurePrototypes.Add (2, new Warehouse (2));
		structurePrototypes.Add (3, new Growable (3,"tree",allItems[1]));
		Item item =  allItems[1] ;
		structurePrototypes.Add (4, new Farm(
			4,"lumberjack",
			3,item,structurePrototypes[3],
			2,2,500,50
		));
		structurePrototypes.Add (6,new HomeBuilding (6));
		Item[] temp1 = new Item[1];
		temp1 [0] = allItems [47].Clone ();
		Item[] temp2 = new Item[1];
		temp2 [0] = allItems [48].Clone();
		int[] ints = { 1 };
		structurePrototypes.Add(7,new ProductionBuilding(7,"Hanfweber",temp1,ints,1,temp2,3,2,1000,null,100));
		//needs
		allNeeds = new List<Need>();
		ReadNeedsFromXML ();
		idToFertilities = new Dictionary<int, Fertility> ();
		allFertilities = new Dictionary<Climate,List<Fertility>> ();
		ReadFertilitiesFromXML ();
	}


	public void OnClickSettle(){
		OnClick (6);
	}
	public void DestroyStructureOnTiles( IEnumerable<Tile> tiles){
		foreach(Tile t in tiles){
			DestroyStructureOnTile (t);
		}
	}
	/// <summary>
	/// Works only for current player not for someone else
	/// </summary>
	/// <param name="t">T.</param>
	public void DestroyStructureOnTile(Tile t){
		if(t.Structure==null){
			return;
		}
		if(t.Structure.playerID==PlayerController.Instance.currentPlayerNumber){
			t.Structure.Destroy ();
		}
	}
	public void OnClick(int id) {
		if(structurePrototypes.ContainsKey (id) == false){
			Debug.LogError ("BUTTON has ID that is not a structure prototypes ->o_O<- ");
			return;
		}
		toBuildStructure = structurePrototypes [id].Clone ();
		if(structurePrototypes [id].BuildTyp == BuildTypes.Path){
			MouseController.Instance.mouseState = MouseState.Path;
			MouseController.Instance.structure = toBuildStructure;
		}
		if(structurePrototypes [id].BuildTyp == BuildTypes.Single){
			MouseController.Instance.mouseState = MouseState.Single;
			MouseController.Instance.structure = toBuildStructure;
		}
		if(structurePrototypes [id].BuildTyp == BuildTypes.Drag){
			MouseController.Instance.mouseState = MouseState.Drag;
			MouseController.Instance.structure = toBuildStructure;
		}

		BuildState = BuildStateModes.Build;
    }
	public void BuildOnTile(List<Tile> tiles, bool forEachTileOnce,int playerNumber){
		if (toBuildStructure == null) {
			return;
		}
		BuildOnTile (tiles, forEachTileOnce, toBuildStructure,playerNumber);
	}
	/// <summary>
	/// USED ONLY FOR LOADING
	/// DONT USE THIS FOR ANYTHING ELSE!!
	/// </summary>
	/// <param name="s">S.</param>
	/// <param name="t">T.</param>
	private void BuildOnTile(Structure s , Tile t){
		if(s==null||t==null){
			Debug.LogError ("Something went wrong by loading Structure!");
			return;
		}
		//TODO RETHINK THIS
		GameObject.FindObjectOfType<StructureSpriteController> ().Initiate ();
		RealBuild (s.GetBuildingTiles (t.X, t.Y), s,-1,true,true);
	}
	public void BuildOnTile(List<Tile> tiles, bool forEachTileOnce, Structure structure,int playerNumber,bool wild=false){
		if(tiles == null || tiles.Count == 0 || WorldController.Instance.IsPaused){
			return;
		}
		if (forEachTileOnce == false) {
			RealBuild (tiles,structure,playerNumber,false,wild);
		} else {
			foreach (Tile tile in tiles) {
				List<Tile> t = new List<Tile> ();
				t.AddRange (structure.GetBuildingTiles (tile.X,tile.Y));
				RealBuild (t,structure,playerNumber,false,wild);
			}
		}
	}
	protected void RealBuild(List<Tile> tiles,Structure s,int playerNumber,bool loading=false,bool wild=false){
		if (loading == false) {
			s = s.Clone ();
		}

		//if it should be build in wilderniss city
		if(wild){
			s.playerID = -1;
			s.buildInWilderniss = true;
		} else {
			//set the player id for check for city
			//has to be changed if someone takes it over
			s.playerID = playerNumber;
		}
		//before we need to check if we can build THERE
		//we need to know if there is if we COULD build 
		//it anyway? that means enough ressources and enough Money
		if(loading==false&&wild==false){
			//find a city that matches the player 
			//and check for money
			if(playerHasEnoughMoney(s,playerNumber)==false){
				Debug.Log ("not playerHasEnoughMoney"); 
				return;
			}
			//if it doesnt need ressources return
			if (s.buildingItems != null) {
				foreach (Tile item in tiles) {
					//we can build in wilderniss terrain but we need our own city
					//FIXME how do we do it with warehouses?
					if (item.myCity != null && item.myCity.IsWilderness () == false) {
						//WARNING: checking for this twice!
						//this is one is not necasserily needed
						//but it we *need* the city to check for its ressources
						//this saves a lot of cpu but it can be problematic if we want to be able 
						//to build something in enemy-terrain
						if (item.myCity.playerNumber != PlayerController.Instance.currentPlayerNumber) {
							Debug.Log ("PlayerController.Instance.number"); 
							return;
						}
						//check for ressources  
						if (item.myCity.myInv.ContainsItemsWithRequiredAmount (s.BuildingItems ()) == false) {
							Debug.Log ("ContainsItemsWithRequiredAmount==null"); 
							return;
						}
						//now we know that there is enough from everthing and it can be build
						//we dont need longer to check a city tile
						//playercontroller will handle the reduction of money/and everything else 
						//related to money - But we need to remove the Ressources
						item.myCity.removeRessources (s.BuildingItems ());
						break;
					}
				}
			}
			//remove the items from the island inventory
		} else {
			//nocosts for loadingbuildings
			s.buildcost = 0;
		}
	
		//now we know that we COULD build that structure
		//but CAN WE?
		//check to see if the structure can be placed there
		if (s.PlaceStructure (tiles) == false) {
			if(loading){
				Debug.LogError ("PLACING FAILED WHILE LOADING! " + s.name);
			}
			return;
		}

		//call all callbacks on structure created
		//FIXME remove this or smth
		if (cbStructureCreated == null)
			GameObject.FindObjectOfType<StructureSpriteController>().Initiate ();
		if (cbStructureCreated != null) {
			cbStructureCreated (s);
		} 
		if (loading == false) {
			// this is for loading so everything will be placed in order
			s.buildID = buildID;
			buildID++;
		}
		s.RegisterOnDestroyCallback (OnDestroyStructure);
	}
	public void OnDestroyStructure(Structure str){
		str.City.removeStructure (str);
	}
	public bool playerHasEnoughMoney(Structure s,int playerNumber){
		if(PlayerController.Instance.GetPlayer (playerNumber).balance >= s.buildcost){
			return true;
		}
		return false;
	}
	public void BuildOnTile(int id, List<Tile> tiles,int playerNumber){
		if(structurePrototypes.ContainsKey (id) == false){
			return;
		}
		BuildOnTile (tiles, true, structurePrototypes[id],playerNumber);
	}
	public City CreateCity(Tile t,Warehouse w){
		if(t.myIsland == null){
			Debug.LogError ("CreateCity called not on a island!");
			return null;
		}
		if(t.myCity != null && t.myCity.IsWilderness () ==false){
			Debug.LogError ("CreateCity called not on a t.myCity && t.myCity.IsWilderness () ==false!");
			return null;
		}
		City c = t.myIsland.CreateCity (w.playerID);
		// needed for mapimage
		c.addStructure (w);// dont know if this is good ...
		if(cbCityCreated != null) {
			cbCityCreated (c);
		}
		return c; 
	}

	public void AddLoadedPlacedStructure(int bid,Structure structure,Tile t){
		loadedToPlaceStructure.Add (bid,structure);
		loadedToPlaceTile.Add (bid,t);
	}
	public void PlaceAllLoadedStructure(){
		foreach (int i in loadedToPlaceStructure.Keys) {
			BuildOnTile (loadedToPlaceStructure[i],loadedToPlaceTile[i]);
		}
		loadedToPlaceStructure.Clear ();
		loadedToPlaceTile.Clear ();
	}
	public void ResetBuild(){
		BuildState = BuildStateModes.None;
		this.toBuildStructure = null;
	}
	public void DestroyToolSelect(){
		ResetBuild ();
		BuildState = BuildStateModes.Destroy;
		MouseController.Instance.mouseState = MouseState.Destroy;
	}
	public void Escape(){
		World.current.resetIslandMark ();
		ResetBuild ();
	}


	public void RegisterStructureCreated(Action<Structure> callbackfunc) {
		cbStructureCreated += callbackfunc;
	}
	public void UnregisterStructureCreated(Action<Structure> callbackfunc) {
		cbStructureCreated -= callbackfunc;
	}
	public void RegisterCityCreated(Action<City> callbackfunc) {
		cbCityCreated += callbackfunc;
	}
	public void UnregisterCityCreated(Action<City> callbackfunc) {
		cbCityCreated -= callbackfunc;
	}
	public void RegisterBuildStateChange(Action<BuildStateModes> callbackfunc) {
		cbBuildStateChange += callbackfunc;
	}
	public void UnregisterBuildStateChange(Action<BuildStateModes> callbackfunc) {
		cbBuildStateChange -= callbackfunc;
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
			allItems [item.ID] = item;
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
			string name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			Road road = new Road (ID,name);
			road.PopulationLevel= int.Parse(node.SelectSingleNode("Pop_Level").InnerText); 
			structurePrototypes [ID] = road;
		}
	}
	private void ReadGrowables(XmlDocument xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("Buildings/Growable")){
			int ID = int.Parse(node.GetAttribute("ID"));
			string name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			Growable grow = new Growable (ID,name,null,null);
			structurePrototypes [ID] = grow;
		}
	}
	private void ReadMarketBuildings(XmlDocument xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("Buildings/Logistic")){
			int ID = int.Parse(node.GetAttribute("ID"));
			string name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			Road road = new Road (ID,name);
			structurePrototypes [ID] = road;
		}
	}
	private void ReadProduktionBuildings(XmlDocument xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("Buildings/Produktion")){
			int ID = int.Parse(node.GetAttribute("ID"));
			string name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			Road road = new Road (ID,name);
			structurePrototypes [ID] = road;
		}
	}
}
