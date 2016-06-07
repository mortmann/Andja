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

	public List<Tile> myTiles;
	public List<Route> myRoutes;

	public Dictionary<Need,float> allNeeds;
	public int cityBalance;
	public int[] citizienCount;
	public float useTick;
	public float useTickTimer;

	public string name {get{return "City "+island.myCities.IndexOf (this);}}

	public City(Island island,List<Need> allNeedsList) {
		citizienCount = new int[4];
		for (int i = 0; i < citizienCount.Length; i++) {
			citizienCount [i] = 0;
		}
        this.island = island;
        myInv = new Inventory();
		Item temp = BuildController.Instance.allItems[47].Clone ();
		temp.count = 50;
		myInv.addItem (temp);

        myStructures = new List<Structure>();
		myTiles = new List<Tile> ();
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
	}

	public void addTile(Tile t){
		if (t.Type == TileType.Water) {
			return;
		}
		t.myCity = this;
		myTiles.Add (t);
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
		Debug.Log("Remove this");
		if (myStructures.Contains (structure)) {
			myStructures.Remove (structure);
			cityBalance -= structure.maintenancecost;
		} else {
			Debug.LogError ("This structure "+structure.ToString () +" does not belong to this city "); 
		}

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
		playerNumber = int.Parse( reader.GetAttribute("Player") );
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
