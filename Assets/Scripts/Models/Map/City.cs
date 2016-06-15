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

	public Dictionary<Need,float> allNeeds;
	public int cityBalance;
	public int[] citizienCount;
	public float useTick;
	public float useTickTimer;

	public Action<Structure> cbStructureAdded;

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
			return;
		}
        myInv = new Inventory();
		//temporary
		Item temp = BuildController.Instance.allItems[47].Clone ();
		temp.count = 50;
		myInv.addItem (temp);

		myRoutes = new List<Route> ();
		myHomes = new List<HomeBuilding> ();
		allNeeds = new Dictionary<Need,float> ();
		for (int i = 0; i < allNeedsList.Count; i++) {
			allNeeds.Add (allNeedsList[i],0);
		}
		useTickTimer = useTick;
    }

    internal void update(float deltaTime) {
		for (int i = 0; i < myStructures.Count; i++) {
			myStructures[i].update(deltaTime);
		}
		if(playerNumber==-1){
			return;
		}
		useTickTimer -= deltaTime;
		if(useTickTimer>=0){
			foreach (Need n in allNeeds.Keys) {
				allNeeds[n] = n.TryToConsumThisIn (this,n.startLevel,citizienCount);
			}
		}
    }

	public void addStructure(Structure str){
		if(str is HomeBuilding) {
			myHomes.Add ((HomeBuilding)str);
		} else {
			cityBalance += str.maintenancecost;
			myStructures.Add (str);
		}
		if(cbStructureAdded!=null){
			cbStructureAdded (str);
		}
	}

	public void addTiles(HashSet<Tile> t){
		t.RemoveWhere (x => x.Type == TileType.Water);
		List<Tile> tiles = new List<Tile> (t);
		tiles.ForEach (x => {if(x.myCity.IsWilderness ()) x.myCity = this;});
		for (int i = 0; i < tiles.Count; i++) {
			if(tiles[i].Structure != null){
				myStructures.Add (tiles[i].Structure);
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
		return myInv.hasAnythingOf (item.ID);
	}

	public bool IsWilderness(){
		return playerNumber == -1;
	}


	/// <summary>
	/// Tries to Remove item.
	/// </summary>
	/// <returns>The to Remove item.</returns>
	/// <param name="item">Item.</param>
	/// <param name="amount">Amount.</param>
	public float TryToRemoveAmount( Item item, float amount ){
		int f = myInv.GetAmountForItem (item);
		return amount / f;
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

		writer.WriteStartElement("Structures");
		foreach (Structure s in writeStructure) {
			writer.WriteStartElement("Structure");
			s.WriteXml(writer);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();


	}
	public void ReadXml(XmlReader reader) {
		reader.ReadToDescendant ("Inventory");
		myInv = new Inventory ();
		myInv.ReadXml (reader);

		BuildController bc = BuildController.Instance;

		if(reader.ReadToDescendant("Structures") ) {
			do {
				int x = int.Parse( reader.GetAttribute("BuildingTile_X") );
				int y = int.Parse( reader.GetAttribute("BuildingTile_Y") );
				int buildID = int.Parse( reader.GetAttribute("BuildID") );
				Tile t = WorldController.Instance.world.GetTileAt (x,y);
				Structure s = bc.structurePrototypes[int.Parse (reader.GetAttribute("ID"))].Clone(); 
				if(s is MarketBuilding){
					((MarketBuilding)s).ReadXml (reader);
				} else 
				if(s is Warehouse){
					((Warehouse)s).ReadXml (reader);
				} else 
				if(s is UserStructure){
					((UserStructure)s).ReadXml (reader);
				} else 
				if(s is Growable){
					((Growable)s).ReadXml (reader);
				}
				bc.AddLoadedPlacedStructure (buildID,s,t);
				myStructures.Add (s);
			} while( reader.ReadToNextSibling("Structure") );
		}
	}
}
