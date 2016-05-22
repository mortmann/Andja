﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;

public class BuildController : MonoBehaviour {
    public static BuildController Instance { get; protected set; }

	public Structure toBuildStructure;
	public Dictionary<int,Structure>  structurePrototypes;
	public Dictionary<int, Item> allItems;

	Action<Structure> cbStructureCreated;
	Action<City> cbCityCreated;

	public Dictionary<int, Item> getCopieOfAllItems(){
		Dictionary<int, Item> items = new Dictionary<int, Item>();
		foreach (int item in allItems.Keys) {
			items.Add (item,allItems [item].Clone ());
		}
		return items;
	}

	public void Awake(){

		// prototypes of items
		allItems = new Dictionary<int, Item> ();
		ReadItemsFromXML();
		// setup all prototypes of structures here 
		// load them from the 
		structurePrototypes = new Dictionary<int, Structure> ();
		ReadStructuresFromXML();


		structurePrototypes.Add (0, new Road (0,"dirtroad"));
		structurePrototypes.Add (1, new MarketBuilding (1));
		structurePrototypes.Add (2, new Warehouse (2));
		structurePrototypes.Add (3, new Growable (3,"tree",allItems[1]));
		Item[] items = { allItems[1] };
		structurePrototypes.Add (4, new ProductionBuilding(
			4,"lumberjack",
			null,null,3,items,
			2,2,500,null,50
		));

        if (Instance != null) {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;
	}

	public void Update(){
		if (Input.GetButtonDown ("Rotate")) {
			if(toBuildStructure != null){
				toBuildStructure.RotatedStructure ();
			}
		}
	}

	public void OnClickSettle(){
//		OnClick ("warehouse");
	}

	public void OnClick(int id) {
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
		if (t.myCity != null) {
			t.myCity.addStructure (s);
			s.city = t.myCity;
		}
	}
	public void BuildOnTile(List<Tile> tiles, bool forEachTileOnce, Structure structure){
		if(tiles == null || tiles.Count == 0 || WorldController.Instance.isPaused){
			return;
		}
		if (forEachTileOnce == false) {
			Structure s = structure.Clone ();
			if (s.PlaceStructure (tiles) == false) {
				return;
			}
			if (cbStructureCreated != null) {
				cbStructureCreated (s);
			}
			if (tiles [0].myCity != null) {
				tiles [0].myCity.addStructure (s);
				s.city = tiles [0].myCity;
			}
		} else {
			foreach (Tile tile in tiles) {
				Structure s = structure.Clone ();
				List<Tile> temp = new List<Tile> ();
				temp.Add (tile);
				if (s.PlaceStructure (temp) == false) {
					continue;
				}
				if (cbStructureCreated != null) {
					cbStructureCreated (s);
				}
				if (tiles [0].myCity != null) {
					tiles [0].myCity.addStructure (s);
					s.city = tiles [0].myCity;
				}
			}
		}
	}


	public void BuildOnTile(int id, List<Tile> tiles){
		if(structurePrototypes.ContainsKey (id) == false){
			return;
		}
		BuildOnTile (tiles, true, structurePrototypes[id]);
	}
	public City CreateCity(Tile t){
		if(t.myCity != null){
			return null;
		}
		City c = new City (t.myIsland);
		if(cbCityCreated != null) {
			cbCityCreated (c);
		}
		return c; 
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

	private void ReadItemsFromXML(){
		XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		TextAsset ta = ((TextAsset)Resources.Load("XMLs/items", typeof(TextAsset)));
		xmlDoc.LoadXml(ta.text); // load the file.
		foreach(XmlElement node in xmlDoc.SelectNodes("Items/Item")){
			Item item = new Item ();
			item.ID = int.Parse(node.SelectSingleNode("ID").InnerText);
			item.name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			allItems [item.ID] = item;
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
//
//		ta = ((TextAsset)Resources.Load("XMLs/marketbuildings", typeof(TextAsset)));
//		xmlDoc.LoadXml(ta.text); // load the file.
//		ReadMarketBuildings (xmlDoc);
//
//		ta = ((TextAsset)Resources.Load("XMLs/produktionbuildings", typeof(TextAsset)));
//		xmlDoc.LoadXml(ta.text); // load the file.
//		ReadProduktionBuildings (xmlDoc);

	}
	private void ReadRoads(XmlDocument xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("Buildings/Road")){
			int ID = int.Parse(node.GetAttribute("ID"));
			string name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			Road road = new Road (ID,name);
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
