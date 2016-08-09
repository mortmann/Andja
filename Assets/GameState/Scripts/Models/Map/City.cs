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
	private string _name="";
	public string name {get{
			if(this.IsWilderness ()){
				return "Wilderniss";
			}
			if(_name.Length==0){
				return "City "+island.myCities.IndexOf (this);	
			}
			return _name;
			}}

	public City(int playerNr,Island island,List<Need> allNeedsList) {
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

			myInv = new Inventory (-1, name);
			//temporary
			Item temp = BuildController.Instance.allItems [49].Clone ();
			temp.count = 50;
			myInv.addItem (temp);

			myRoutes = new List<Route> ();
			myHomes = new List<HomeBuilding> ();
			allNeeds = new Dictionary<Need,float> ();
			needsList = allNeedsList;
			idToNeed = new Dictionary<int, Need> ();
			for (int i = 0; i < allNeedsList.Count; i++) {
				allNeeds.Add (allNeedsList [i], 0);
				idToNeed.Add (allNeedsList [i].ID, allNeedsList [i]);
			}
			useTick = 30f;

		_name = "<City>" + UnityEngine.Random.Range (0, 1000);
		//		useTickTimer = useTick;
    }
	 
    internal void update(float deltaTime) {
		for (int i = 0; i < myStructures.Count; i++) {
			myStructures[i].update(deltaTime);
		}
		if(playerNumber==-1){
			return;
		}
		for (int i = 0; i < citizienCount.Length; i++) {
			citizienCount [i] = 0;
		}
		//TODO mabye make itso that callbacks add/sub from it?
		//TODO or make it so that homes are responsive for it 
		for (int i = 0; i < myHomes.Count; i++) {
			citizienCount [myHomes [i].buildingLevel] += myHomes [i].people;
		}

		useTickTimer -= deltaTime;
		if(useTickTimer<=0){
			useTickTimer = useTick;
			for(int i = 0;i<needsList.Count;i++){
				allNeeds[needsList[i]]=needsList[i].TryToConsumThisIn (this,citizienCount);
			}
		}
    }
	/// <summary>
	/// USE only for the creation of non player city aka Wilderniss
	/// </summary>
	/// <param name="tiles">Tiles.</param>
	/// <param name="island">Island.</param>
	public City(List<Tile> tiles,Island island){
//		tiles.ForEach (x => x.myCity = this);
		List<Tile> temp = new List<Tile>(tiles);
		island.wilderniss = this;
		myTiles = new HashSet<Tile> (tiles);
		for (int i = 0; i < tiles.Count; i++) {
			temp[i].myCity= null;
		}
		myInv = new Inventory (0);
		this.playerNumber = -1;
		this.island = island;
		island.wilderniss = this;
		myStructures = new List<Structure> ();
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
		} 
		if(str is Warehouse){
			if(myWarehouse!=null){
				Debug.LogError ("There should be only one Warehouse per City!");
				return;
			}
			myWarehouse = (Warehouse)str;
		}
		cityBalance += str.maintenancecost;
		myStructures.Add (str);

		if(cbStructureAdded!=null){
			cbStructureAdded (str);
		}
	}
	//current wrapper to make sure its valid
	public void RemoveTile(Tile t){
		//if it doesnt contain it there is an error
		if(myTiles.Contains (t)==false){
			Debug.LogError ("This city does not know that it had this tile!" + t.toString ()); 
			return;
		}
		myTiles.Remove (t);
		island.allReadyHighlighted = false;
	}
	public void addTiles(HashSet<Tile> t){
		// does not really needs it because tiles witout island reject cities
		//but it is a secondary security that this does not happen
		t.RemoveWhere (x => x.Type == TileType.Water);
		List<Tile> tiles = new List<Tile> (t);
		for (int i = 0; i < tiles.Count; i++) {
			tiles [i].myCity = this;
			if(tiles[i].Structure != null){
				if (myStructures.Contains (tiles [i].Structure) ==false) { 
					tiles [i].Structure.City = this;
					tiles [i].Structure.playerID = playerNumber;
					addStructure (tiles [i].Structure);
				}
			}
			myTiles.Add (tiles[i]);
		}
		island.allReadyHighlighted = false;

	}
	public void addTile(Tile t){
		if(t.Type==TileType.Water||myTiles==null||myTiles.Contains (t)){
			return;
		}
		if(t.Structure != null){
			if (myStructures.Contains (t.Structure) ==false) { 
				t.Structure.City = this;
				t.Structure.playerID = playerNumber;
 
				addStructure (t.Structure);
			}
		}
		island.allReadyHighlighted = false;
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
		return myInv.hasAnythingOf (item);
	}

	public bool IsWilderness(){
		if(playerNumber==-1 && this != island.wilderniss){
			Debug.LogError ("NOT WILDERNISS! But -1 playernumber!? " + this.name);
		}
		return this == island.wilderniss;
	}

	public void tradeWithShip(Item toTrade,int amount=50, Unit ship = null){
		if(myWarehouse==null || myWarehouse.inRangeUnits.Count==0  || toTrade ==null){
			return;
		}
		if (tradeUnit == null && ship==null) {
			myInv.moveItem (myWarehouse.inRangeUnits [0].inventory, toTrade,amount);
		} else if(ship==null){
			myInv.moveItem (tradeUnit.inventory, toTrade,amount);
		} else {
			myInv.moveItem (ship.inventory, toTrade,amount);
		}
	}
	public void tradeFromShip(Unit u,Item getTrade,int amount = 50){
		if(getTrade ==null){
			return;
		}
		u.inventory.moveItem (myInv,getTrade,amount);
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
	public void removeStructure(Structure structure, bool returnRessources=false){
		if (myStructures.Contains (structure)) {
			if(structure is HomeBuilding){
				myHomes.Remove ((HomeBuilding)structure);
			} else 
			if(structure is Warehouse){
				myWarehouse = null;
			}  
			//if were geting some of the ressources back
			//when we destroy it -> should be a setting 
			if(returnRessources){
				Item[] res = structure.buildingItems;
				for (int i = 0; i < res.Length; i++) {
					res [i].count /= 3; // FIXME do not have this hardcoded! Change it to be chooseable!
				}
				myInv.AddItems (res);
			}
			myStructures.Remove (structure);
			cityBalance -= structure.maintenancecost;
		} else {
			//this is no error if this is wilderniss
			if(structure is Warehouse){
				return;
			}
			Debug.LogError (this.name + " This structure "+structure.ToString () +" does not belong to this city "); 
		}
		island.allReadyHighlighted = false;

	}
	public void removeTiles(List<Tile> tiles){
		foreach (Tile item in tiles) {
			item.myCity = null;
			if(item.myCity!=this){
				myTiles.Remove (item);
			}
		}
		if(myTiles.Count==0){
			island.RemoveCity (this);
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
		//TODO change this to smth better
		//weird bug workaround
		//not working with nextsibling
		//not like in someplaces
		//but for now its working
		reader.ReadToFollowing ("Structures");
//		while(reader.ReadToFollowing ("Structure")==false){
//			reader.Read ();
//		}
		if (reader.ReadToDescendant ("Structure")) {
			do {
				int x = int.Parse (reader.GetAttribute ("BuildingTile_X"));
				int y = int.Parse (reader.GetAttribute ("BuildingTile_Y"));
				int buildID = int.Parse (reader.GetAttribute ("BuildID"));
				Tile t = World.current.GetTileAt (x, y);
				Structure s = bc.structurePrototypes [int.Parse (reader.GetAttribute ("ID"))].Clone (); 
				if (s is MarketBuilding) {
					((MarketBuilding)s).ReadXml (reader);
				} else if (s is Warehouse) {
					((Warehouse)s).ReadXml (reader);
					myWarehouse = ((Warehouse)s);
				} else if (s is OutputStructure) {
					((OutputStructure)s).ReadXml (reader);
				} else if (s is Growable) {
					((Growable)s).ReadXml (reader);
				} else if (s is HomeBuilding) {
					((HomeBuilding)s).ReadXml (reader);
					myHomes.Add ((HomeBuilding )s);
				}
				bc.AddLoadedPlacedStructure (buildID, s, t);
				myStructures.Add (s);
			} while(reader.ReadToNextSibling ("Structure"));
		}
//		}

//		}

	}
}
