using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Events;

public class BuildController : MonoBehaviour {
    public static BuildController Instance { get; protected set; }

	public Structure toBuildStructure;
	public Dictionary<string,Structure>  structurePrototypes;
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
		allItems.Add (0,new Item(0,"wood"));
		allItems.Add (1,new Item(1,"stone"));
		allItems.Add (2,new Item(2,"tools"));
		// setup all prototypes of structures here 
		// load them from the 
		structurePrototypes = new Dictionary<string, Structure> ();
		structurePrototypes.Add ("dirtroad", new Road ("dirtroad"));
		structurePrototypes.Add ("market", new MarketBuilding ());
		structurePrototypes.Add ("warehouse", new Warehouse ());
		structurePrototypes.Add ("tree", new Growable ("tree",allItems[0]));
		Item[] items = { allItems[0] };
		structurePrototypes.Add ("lumberjack", new ProductionBuilding(
			"lumberjack",
			null,null,null,
			3,items,5,
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
		OnClick ("warehouse");
	}

	public void OnClick(string name) {
		toBuildStructure = structurePrototypes [name].Clone ();
		if(structurePrototypes [name].BuildTyp == BuildTypes.Path){
			MouseController.Instance.mouseState = MouseState.Path;

		}
		if(structurePrototypes [name].BuildTyp == BuildTypes.Single){
			MouseController.Instance.mouseState = MouseState.Single;
			MouseController.Instance.structure = toBuildStructure;
		}
		if(structurePrototypes [name].BuildTyp == BuildTypes.Drag){
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
		if(tiles == null || tiles.Count == 0){
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


	public void BuildOnTile(string name, List<Tile> tiles){
		if(structurePrototypes.ContainsKey (name) == false){
			return;
		}
		BuildOnTile (tiles, true, structurePrototypes[name]);
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

}
