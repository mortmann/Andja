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
	public List<Tile> myTiles;
	public List<Route> myRoutes;
	public int cityBalance;
	public int[] citizienCount;
    public City(Island island) {
		citizienCount = new int[4];
		foreach (int item in citizienCount) {
			item = 0;
		}
        this.island = island;
        myInv = new Inventory();
        myStructures = new List<Structure>();
		myTiles = new List<Tile> ();
		myRoutes = new List<Route> ();
    }

    internal void update(float deltaTime) {
        foreach(Structure s in myStructures) {
            s.update(deltaTime);
        }
    }

	public void addStructure(Structure str){
		cityBalance += str.maintenancecost;
		myStructures.Add (str);

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
	public float TryToGetItem(Item item,float amount){
		return 
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

		myInv = new Inventory ();
		myInv.ReadXml (reader);

		BuildController bc = new BuildController ();

		if(reader.ReadToDescendant("City") ) {
			do {
				int x = int.Parse( reader.GetAttribute("BuildingTile_X") );
				int y = int.Parse( reader.GetAttribute("BuildingTile_Y") );
				Tile t = WorldController.Instance.world.GetTileAt (x,y);
				Structure s = bc.structurePrototypes[int.Parse (reader.GetAttribute("ID"))].Clone(); 
				if(s is MarketBuilding){
					((MarketBuilding)s).ReadXml (reader);
				} else 
				if(s is Warehouse){
					((Warehouse)s).ReadXml (reader);
				} else 
				if(s is ProductionBuilding){
					((ProductionBuilding)s).ReadXml (reader);
				} else 
				if(s is Growable){
					((Growable)s).ReadXml (reader);
				}
				bc.BuildOnTile (s,t);
				myStructures.Add (s);
			} while( reader.ReadToNextSibling("Island") );
		}
	}
}
