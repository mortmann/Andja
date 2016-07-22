using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class City : IXmlSerializable{
    //TODO: set this to the player that creates this
    public int playerNumber = 0;
    public Island island { get; protected set; }
    public Inventory myInv;
    public List<Structure> myStructures;
	public List<HomeBuilding> myHomes;

	public HashSet<Tile> myTiles;
	public List<Route> myRoutes;
	public Unit tradeUnit;
	public Dictionary<Need,float> allNeeds;
	public List<Need> needsList;

	public Dictionary<int,Need> idToNeed;

	public int cityBalance;
	public int[] citizienCount;
	public float useTick;
	public float useTickTimer;
	public Warehouse myWarehouse;
	public Action<Structure> cbStructureAdded;
	public Action<Structure> cbRegisterTradeOffer;
	public string name {get{return "City "+island.myCities.IndexOf (this);}}

	public City(int playerNr,Island island,List<Need> allNeedsList, List<Tile> islandTiles = null) {
		this.playerNumber = playerNr;
		citizienCount = new int[4];
		for (int i = 0; i < citizienCount.Length; i++) {
			citizienCount [i] = 0;
		}
        this.island = island;

		myStructures = new List<Structure>();
		myTiles = new HashSet<Tile> ();
		// if this city doesnt belong to anyone it does not need
		//anything underneath here
		if(playerNr == -1){
			 
			islandTiles.ForEach (x => x.myCity = this);
			this.myTiles.UnionWith (islandTiles); 
			myInv = new Inventory (0);
			return;
		}
        myInv = new Inventory(-1,name);
		//temporary
		Item temp = BuildController.Instance.allItems[49].Clone ();
		temp.count = 50;
		myInv.addItem (temp);

		myRoutes = new List<Route> ();
		myHomes = new List<HomeBuilding> ();
		allNeeds = new Dictionary<Need,float> ();
		needsList = allNeedsList;
		idToNeed = new Dictionary<int, Need> ();
		for (int i = 0; i < allNeedsList.Count; i++) {
			allNeeds.Add (allNeedsList[i],0);
			idToNeed.Add (allNeedsList[i].ID,allNeedsList[i]);
		}
		useTick = 30f;
//		useTickTimer = useTick;
    }

    internal void update(float deltaTime) {
		for (int i = 0; i < myStructures.Count; i++) {
			myStructures[i].update(deltaTime);
		}
		if(playerNumber==-1){
			return;
		}
		useTickTimer -= deltaTime;
		if(useTickTimer<=0){
			useTickTimer = useTick;
			for(int i = 0;i<needsList.Count;i++){
				allNeeds[needsList[i]]=needsList[i].TryToConsumThisIn (this,citizienCount);
			}
		}
    }

	public void addStructure(Structure str){
		if(myStructures.Contains (str)){
			//happens on loading for loaded stuff
			//so not an actual error anymore
//			Debug.LogError ("Adding a structure that already belongs to this city.");
			return;
		}
		if(str is HomeBuilding) {
			myHomes.Add ((HomeBuilding)str);
		} else {
			if(str is Warehouse){
				if(myWarehouse!=null){
					Debug.LogError ("There should be only one Warehouse per City!");
					return;
				}
				myWarehouse = (Warehouse)str;
			}
			cityBalance += str.maintenancecost;
			myStructures.Add (str);
		}
		if(cbStructureAdded!=null){
			cbStructureAdded (str);
		}
	}

	public void addTiles(HashSet<Tile> t){
		t.RemoveWhere (x => x.Type == TileType.Water || x.myCity.IsWilderness ()==false);
		List<Tile> tiles = new List<Tile> (t);
		tiles.ForEach (x => { x.myCity = this; });
		for (int i = 0; i < tiles.Count; i++) {
			if(tiles[i].Structure != null){
				if (myStructures.Contains (tiles [i].Structure) ==false) { 
					addStructure (tiles [i].Structure);
				}
				tiles [i].Structure.city = this;
			}
			myTiles.Add (tiles[i]);
		}
	}

	public void removeRessources(Item[] remove){
		foreach (Item item in remove) {
			myInv.removeItemAmount (item);
		}
	}

	public void removeRessource(Item item, int amount){
		Item i = item.Clone ();
		i.count = amount;
		myInv.removeItemAmount (i);
	}

	public bool hasItem(Item item){
		return myInv.hasAnythingOf (item);
	}

	public bool IsWilderness(){
		return this == island.wilderniss;
	}

	public void tradeWithShip(Item toTrade){
		if(myWarehouse==null || myWarehouse.inRangeUnits.Count==0  || toTrade ==null){
			return;
		}
		if (tradeUnit == null) {
			myInv.moveItem (myWarehouse.inRangeUnits [0].inventory, toTrade,5);
		} else {
			myInv.moveItem (tradeUnit.inventory, toTrade,50);
		}
	}
	public void tradeFromShip(Unit u,Item getTrade){
		if(getTrade ==null){
			return;
		}
		u.inventory.moveItem (myInv,getTrade,50);
	}
	public float getPercentage(Need need){
		if(idToNeed.ContainsKey (need.ID)==false){
			Debug.LogError ("NEED NOT FOUND");
			return 0;
		}
		return allNeeds[idToNeed[need.ID]];
	}


	public float GetAmountForThis( Item item, float amount ){
		return myInv.GetAmountForItem (item);
	}
	
	public void AddRoute(Route route){
		this.myRoutes.Add (route);
	}

	public void RemoveRoute(Route route){
		if(myRoutes.Contains (route)){
			myRoutes.Remove (route);
		} 
	}
	public void removeStructure(Structure structure){
		if (myStructures.Contains (structure)) {
			myStructures.Remove (structure);
			cityBalance -= structure.maintenancecost;
		} else {
			Debug.LogError ("This structure "+structure.ToString () +" does not belong to this city "); 
		}

	}

	public void RegisterStructureAdded(Action<Structure> callbackfunc) {
		cbStructureAdded += callbackfunc;
	}
	public void UnregisterStructureAdded(Action<Structure> callbackfunc) {
		cbStructureAdded -= callbackfunc;
	}

	//////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 						SAVING & LOADING
	/// 
	//////////////////////////////////////////////////////////////////////////////////////
	public XmlSchema GetSchema() {
		return null;
	}

	public void WriteXml(XmlWriter writer) {
		writer.WriteAttributeString("Player", playerNumber.ToString() );
		writer.WriteStartElement("Inventory");
			myInv.WriteXml(writer);
		writer.WriteEndElement();

		Structure tempWarehouse = null;
		List<Structure> tempMarketbuildings = new List<Structure>();
		List<Structure> tempStructures = new List<Structure>();
		foreach (Structure s in myStructures) {
			if (s is MarketBuilding) {
				tempMarketbuildings.Add (s);
			} else 
			if(s is Warehouse){
				tempWarehouse = s;
			} else {
				tempStructures.Add (s);
			}
		}
		List<Structure> writeStructure = new List<Structure>();
		if (tempWarehouse != null) {
			writeStructure.Add (tempWarehouse);
		}
		writeStructure.AddRange (tempMarketbuildings);
		writeStructure.AddRange (tempStructures);
		//TODO MAKE THIS MORE PERFOMANT
		if (myHomes != null) {
			foreach (var item in myHomes) {
				writeStructure.Add (item);
			}
		}
		writer.WriteStartElement("Structures");
		foreach (Structure s in writeStructure) {
			writer.WriteStartElement("Structure");
			s.WriteXml(writer);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();


	}

	public void ReadXml(XmlReader reader) {
		playerNumber = int.Parse (reader.GetAttribute("Player"));
		reader.ReadToDescendant ("Inventory");
		myInv = new Inventory ();
		myInv.ReadXml (reader);
		BuildController bc = BuildController.Instance;
		reader.ReadToFollowing ("Structures");

		//TODO change this to smth better
		//weird bug workaround
		//not working with nextsibling
		//not like in someplaces
		//but for now its working
		while(reader.Read ()) {
			if(reader.Name=="Structures"){
				break;
			}

			if (reader.Name == "Structure" ) {
				int x = int.Parse (reader.GetAttribute ("BuildingTile_X"));
				int y = int.Parse (reader.GetAttribute ("BuildingTile_Y"));
				int buildID = int.Parse (reader.GetAttribute ("BuildID"));
				Tile t = World.current.GetTileAt (x, y);
				Structure s = bc.structurePrototypes [int.Parse (reader.GetAttribute ("ID"))].Clone (); 
				if (s is MarketBuilding) {
					((MarketBuilding)s).ReadXml (reader);
				} else if (s is Warehouse) {
					((Warehouse)s).ReadXml (reader);
				} else if (s is UserStructure) {
					((UserStructure)s).ReadXml (reader);
				} else if (s is Growable) {
					((Growable)s).ReadXml (reader);
				} else if (s is HomeBuilding) {
					((HomeBuilding)s).ReadXml (reader);
				}
				bc.AddLoadedPlacedStructure (buildID, s, t);
				myStructures.Add (s);
			}
		}

	}
}
