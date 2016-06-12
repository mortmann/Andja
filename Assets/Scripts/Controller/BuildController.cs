using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;

public enum BuildStateModes {Off,On};

public class BuildController : MonoBehaviour {
    public static BuildController Instance { get; protected set; }
	protected BuildStateModes _buildState;
	public BuildStateModes BuildState { 
			get { return _buildState; } 
			set { 
				if (_buildState == value) {
					return;
				}
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
		BuildState = BuildStateModes.Off;
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
		structurePrototypes.Add (1, new MarketBuilding (1));
		structurePrototypes.Add (2, new Warehouse (2));
		structurePrototypes.Add (3, new Growable (3,"tree",allItems[1]));
		Item item =  allItems[1] ;
		structurePrototypes.Add (4, new Farm(
			4,"lumberjack",
			3,item,
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
	}

	public void Update(){
		if (Input.GetButtonDown ("Rotate")) {
			if(toBuildStructure != null){
				toBuildStructure.RotateStructure ();
			}
		}
	}

	public void OnClickSettle(){
		OnClick (6);
	}

	public void OnClick(int id) {
		if(structurePrototypes.ContainsKey (id) == false){
			Debug.LogError ("BUTTON has ID that is not structure prototypes ->o_O<- ");
			return;
		}
		toBuildStructure = structurePrototypes [id].Clone ();
		if(structurePrototypes [id].BuildTyp == BuildTypes.Path){
			MouseController.Instance.mouseState = MouseState.Path;

		}
		if(structurePrototypes [id].BuildTyp == BuildTypes.Single){
			MouseController.Instance.mouseState = MouseState.Single;
			MouseController.Instance.structure = toBuildStructure;
		}
		if(structurePrototypes [id].BuildTyp == BuildTypes.Drag){
			MouseController.Instance.mouseState = MouseState.Drag;
			MouseController.Instance.structure = toBuildStructure;
		}

		BuildState = BuildStateModes.On;
    }
	public void BuildOnTile(List<Tile> tiles, bool forEachTileOnce){
		if (toBuildStructure == null) {
			return;
		}
		BuildOnTile (tiles, forEachTileOnce, toBuildStructure);
	}
	/// <summary>
	/// USED ONLY FOR LOADING
	/// DONT USE THIS FOR ANYTHING ELSE!!
	/// </summary>
	/// <param name="s">S.</param>
	/// <param name="t">T.</param>
	public void BuildOnTile(Structure s , Tile t){
		if (toBuildStructure == null) {
			return;
		}
		if (s.PlaceStructure (s.GetBuildingTiles (t.X, t.Y)) == false) {
			return;
		}
		if (cbStructureCreated != null) {
			cbStructureCreated (s);
		}
	}
	public void BuildOnTile(List<Tile> tiles, bool forEachTileOnce, Structure structure){
		if(tiles == null || tiles.Count == 0 || WorldController.Instance.isPaused){
			return;
		}
		if (forEachTileOnce == false) {
			RealBuild (tiles,structure);
		} else {
			foreach (Tile tile in tiles) {
				List<Tile> t = new List<Tile> ();
				t.Add (tile);
				RealBuild (t,structure);
			}
		}
	}
	protected void RealBuild(List<Tile> tiles,Structure structure){
		Structure s = structure.Clone ();
		//check to see if the structure can be placed there
		if (s.PlaceStructure (tiles) == false) {
			return;
		}
		//call all callbacks on structure created
		if (cbStructureCreated != null) {
			cbStructureCreated (s);
		}
		// this is for loading so everything will be placed in order
		s.buildID = buildID;
		buildID++;
		//find a tile thats on island -- only important for onshore/onwater buildings
		Tile islandTile = tiles[0];
		foreach (Tile item in tiles) {
			if(item.myIsland != null){
				islandTile = item;
				break;
			}
		}
		if(islandTile == null){
			return;
		}
	}

	public void BuildOnTile(int id, List<Tile> tiles){
		if(structurePrototypes.ContainsKey (id) == false){
			return;
		}
		BuildOnTile (tiles, true, structurePrototypes[id]);
	}
	public City CreateCity(Tile t){
		if(t.myIsland == null){
			return null;
		}
		if(t.myCity != null){
			return null;
		}
		City c = t.myIsland.CreateCity ();
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
		for (int i = 0; i < loadedToPlaceStructure.Count; i++) {
			BuildOnTile (loadedToPlaceStructure[i],loadedToPlaceTile[i]);
		}
		loadedToPlaceStructure.Clear ();
		loadedToPlaceTile.Clear ();
	}
	public void ResetBuild(){
		BuildState = BuildStateModes.Off;
		this.toBuildStructure = null;
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
	private void ReadNeedsFromXML(){
		XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		TextAsset ta = ((TextAsset)Resources.Load("XMLs/needs", typeof(TextAsset)));
		xmlDoc.LoadXml(ta.text); // load the file.
		foreach(XmlElement node in xmlDoc.SelectNodes("Needs/Need")){
			Need need = new Need ();
			need.ID = int.Parse(node.GetAttribute("ID"));
			need.name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			need.popCount =  0;// TODO: int.Parse(node.SelectSingleNode("Count").InnerText);
			need.startLevel = int.Parse(node.SelectSingleNode("Level").InnerText);
			int structure = int.Parse (node.SelectSingleNode ("Structure").InnerText);
			if (structure == -1) {
				int item = int.Parse(node.SelectSingleNode("Item").InnerText);
				if (allItems.ContainsKey (item)) {
					need.item = allItems [item];
				} else {
					continue;
				}
			} else {
				if (structurePrototypes.ContainsKey (structure)) {
					need.structure = structurePrototypes [structure];
				} else {
					continue;
				}
			}
			float[] fs = new float[4];
			fs[0] = float.Parse(node.SelectSingleNode("Peasent").InnerText);
			fs[1] = float.Parse(node.SelectSingleNode("Citizen").InnerText);
			fs[2] = float.Parse(node.SelectSingleNode("Patrician").InnerText);
			fs[3] = float.Parse(node.SelectSingleNode("Nobleman").InnerText);
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
			Road road = new Road (ID,name);
			structurePrototypes [ID] = road;
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
