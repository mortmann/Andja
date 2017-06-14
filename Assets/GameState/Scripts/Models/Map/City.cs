using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class City : IGEventable {
	public const int TargetType = 12;
	#region Serialize
	[JsonPropertyAttribute] public int playerNumber = 0;
	[JsonPropertyAttribute] public Inventory myInv;
	[JsonPropertyAttribute] public List<Structure> myStructures;
	[JsonPropertyAttribute] public Dictionary<Need,float> allNeeds; // i think this is what is missing
	[JsonPropertyAttribute] public float useTickTimer;
	/// <summary>
	/// ITEM which is to trade
	/// bool = true -> SELL
	/// 	 = false -> BUY
	/// </summary>
	[JsonPropertyAttribute] public Dictionary<int,TradeItem> itemIDtoTradeItem;
	[JsonPropertyAttribute] private string _name=""; 
	[JsonPropertyAttribute] public int[] citizienCount;

	#endregion
	#region RuntimeOrOther
	public string name {get{
			if(this.IsWilderness ()){
				return "Wilderniss";
			}
			if(_name.Length==0){
				return "City "+island.myCities.IndexOf (this);	
			}
			return _name;
	}}
	public Island island { get; protected set; }
	//TODO: set this to the player that creates this
	public List<HomeBuilding> myHomes;
	public HashSet<Tile> myTiles;
	public List<Route> myRoutes;
	public Unit tradeUnit;
	public Dictionary<int,Need> idToNeed;
	public int cityBalance;
	public float useTick;
	public Warehouse myWarehouse;
	Action<Structure> cbStructureAdded;
	Action<Structure> cbStructureRemoved;
	Action<City> cbCityDestroy;

	Action<Structure> cbRegisterTradeOffer;
	Action<GameEvent> cbEventCreated;
	Action<GameEvent> cbEventEnded;
	#endregion

	public City(int playerNr,Island island) {
		this.playerNumber = playerNr;
		this.island = island;
		citizienCount = new int[4];
		itemIDtoTradeItem = new Dictionary<int, TradeItem> ();
		myStructures = new List<Structure>();
		myInv = new Inventory (-1, name);
		allNeeds = new Dictionary<Need,float> ();
		for (int i = 0; i < World.allNeeds.Count; i++) {
			allNeeds.Add (World.allNeeds [i], 0);
		}
		Setup ();

		for (int i = 0; i < citizienCount.Length; i++) {
			citizienCount [i] = 0;
		}
		//temporary
		Item temp = BuildController.Instance.allItems [49].Clone ();
		temp.count = 50;
		myInv.addItem (temp);
		_name = "<City>" + UnityEngine.Random.Range (0, 1000);
		//		useTickTimer = useTick;
    }
	/// <summary>
	/// DO NOT USE! ONLY serialization!
	/// </summary>
	public City(){
	}

	private void Setup(){
		
		myTiles = new HashSet<Tile> ();
		myHomes = new List<HomeBuilding> ();
		myRoutes = new List<Route> ();
		useTick = 30f;
		idToNeed = new Dictionary<int, Need> ();
		for (int i = 0; i < World.allNeeds.Count; i++) {
			idToNeed.Add (World.allNeeds [i].ID, World.allNeeds [i]);
		}
	}
	public IEnumerable<Structure> Load(){
		Setup ();
		foreach (Structure item in myStructures) {
			if(item is Warehouse){
				myWarehouse =(Warehouse) item;
			} else 
			if(item is HomeBuilding){
				myHomes.Add ((HomeBuilding)item);
			}
//			item.Load ();
		}
		return myStructures;
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
		//TODO or make it so that homes are responsible for it 
		for (int i = 0; i < myHomes.Count; i++) {
			citizienCount [myHomes [i].buildingLevel] += myHomes [i].people;
		}

		useTickTimer -= deltaTime;
		if(useTickTimer<=0){
			useTickTimer = useTick;
			foreach(Need need in allNeeds.Keys){
				allNeeds[need]=need.TryToConsumThisIn (this,citizienCount);
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
		t.RemoveWhere (x => x.Type == TileType.Ocean);
		List<Tile> tiles = new List<Tile> (t);
		for (int i = 0; i < tiles.Count; i++) {
			tiles [i].myCity = this;
			if(tiles[i].Structure != null){
				if (myStructures.Contains (tiles [i].Structure) ==false) { 
					tiles [i].Structure.City = this;
					tiles [i].Structure.playerNumber = playerNumber;
					addStructure (tiles [i].Structure);
				}
			}
			myTiles.Add (tiles[i]);
		}
		island.allReadyHighlighted = false;

	}
	public void addTile(Tile t){
		if(t.Type==TileType.Ocean||myTiles==null||myTiles.Contains (t)){
			return;
		}
		if(t.Structure != null){
			if (myStructures.Contains (t.Structure) ==false) { 
				t.Structure.City = this;
				t.Structure.playerNumber = playerNumber;
 
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
	/// <summary>
	/// Ship buys from city means 
	/// SELLING IT from perspectiv City
	/// </summary>
	/// <param name="itemID">Item I.</param>
	/// <param name="player">Player.</param>
	/// <param name="ship">Ship.</param>
	/// <param name="amount">Amount.</param>
	public void BuyFromCity(int itemID,Player player, Ship ship, int amount=50){
		if(itemIDtoTradeItem.ContainsKey (itemID)==false){
			return;
		}
		//true = BUY
		TradeItem ti = itemIDtoTradeItem [itemID];
		if(ti.selling==false){
			Debug.Log ("this item is not to buy"); 
			return;
		}
		Item i = ti.SellItemAmount (myInv.GetItemWithIDClone (itemID));
		Player myPlayer = PlayerController.Instance.GetPlayer (playerNumber);
		int am = tradeWithShip (i,Mathf.Clamp (amount,0,i.count),ship);
		myPlayer.addMoney (am * ti.price);
		player.reduceMoney (am * ti.price);

	}
	/// <summary>
	/// Ship sells to city.
	/// City BUYs it.
	/// </summary>
	/// <param name="itemID">Item I.</param>
	/// <param name="player">Player.</param>
	/// <param name="ship">Ship.</param>
	/// <param name="amount">Amount.</param>
	public void SellToCity(int itemID,Player player, Ship ship, int amount=50){
		if(itemIDtoTradeItem.ContainsKey (itemID)==false){
			return;
		}
		TradeItem ti = itemIDtoTradeItem [itemID];
		//TRUE = sell
		if(ti.selling==true){
			Debug.Log ("this item is not to sell here"); 
			return;
		}
		Item i = ti.BuyItemAmount (myInv.GetItemWithIDClone (itemID));
		Player myPlayer = PlayerController.Instance.GetPlayer (playerNumber);
		int am = tradeFromShip (ship,i,Mathf.Clamp (amount,0,i.count));
		myPlayer.reduceMoney (am * ti.price);
		player.addMoney (am * ti.price);

	}
	public int tradeWithShip(Item toTrade,int amount=50, Unit ship = null){
		if(myWarehouse==null || myWarehouse.inRangeUnits.Count==0  || toTrade ==null){
			Debug.Log ("myWarehouse==null || myWarehouse.inRangeUnits.Count==0  || toTrade ==null"); 
			return 0;
		}
		if (tradeUnit == null && ship==null) {
			return myInv.moveItem (myWarehouse.inRangeUnits [0].inventory, toTrade,amount);
		} else if(ship==null){
			return myInv.moveItem (tradeUnit.inventory, toTrade,amount);
		} else {
			return myInv.moveItem (ship.inventory, toTrade,amount);
		}
	}
	public int tradeFromShip(Unit u,Item getTrade,int amount = 50){
		if(getTrade ==null){
			return 0;
		}
		return u.inventory.moveItem (myInv,getTrade,amount);
	}
	public float getPercentage(Need need){
		if(idToNeed.ContainsKey (need.ID)==false){
			Debug.LogError ("NEED NOT FOUND");
			return 0;
		}
		return allNeeds[idToNeed[need.ID]];
	}
	public void RemoveTradeItem(Item item){
		itemIDtoTradeItem.Remove (item.ID);
	}
	public void ChangeTradeItemAmount(Item item){
		itemIDtoTradeItem [item.ID].count = item.count;
	}
	public void ChangeTradeItemPrice(int id, int price){
		itemIDtoTradeItem [id].price = price;
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
		if(structure==null){
			Debug.Log ("null"); 
			return;
		}
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
			if(cbStructureRemoved!=null)
				cbStructureRemoved (structure);
		} else {
			//this is no error if this is wilderniss
			if(structure is Warehouse){
				return;
			}
			Debug.LogError (this.name + " This structure "+structure.ToString () +" does not belong to this city "); 
		}
		island.allReadyHighlighted = false;

	}

	public void removeTiles(IEnumerable<Tile> tiles){
		foreach (Tile item in tiles) {
			item.myCity = null;
			if(item.myCity!=this){
				myTiles.Remove (item);
			}
		}
		if(myTiles.Count==0){
			Destroy ();
		}
	}
	public void Destroy(){
		if(playerNumber==-1){
			return; // this is the wilderniss it cant be removed! or destroyed
		}
		if(myTiles.Count > 0){
			removeTiles (myTiles);
		}
		island.RemoveCity (this);
		if(cbCityDestroy!=null)
			cbCityDestroy (this);
	}
	public void RegisterCityDestroy(Action<City> callbackfunc) {
		cbCityDestroy += callbackfunc;
	}
	public void UnregisterCityDestroy(Action<City> callbackfunc) {
		cbCityDestroy -= callbackfunc;
	}
	public void RegisterStructureAdded(Action<Structure> callbackfunc) {
		cbStructureAdded += callbackfunc;
	}
	public void UnregisterStructureAdded(Action<Structure> callbackfunc) {
		cbStructureAdded -= callbackfunc;
	}
	public void RegisterStructureRemove(Action<Structure> callbackfunc) {
		cbStructureRemoved += callbackfunc;
	}
	public void UnregisterStructureRemove(Action<Structure> callbackfunc) {
		cbStructureRemoved -= callbackfunc;
	}
	public void RegisterOnEvent(Action<GameEvent> create,Action<GameEvent> ending){
		cbEventCreated += create;
		cbEventEnded += ending;
	}
	public void OnEventCreate(GameEvent ge){
		//this only gets called in two cases
		//either event is on this island or in one of its cities
		if(ge.IsTarget (island)||ge.IsTarget(this)){
			ge.InfluenceTarget (this, true);
			if(cbEventCreated!=null){
				cbEventCreated (ge);
			}
		}
	}
	public void OnEventEnded(GameEvent ge){
		//this only gets called in two cases
		//either event is on this island or in one of its cities
		if(ge.IsTarget (island)||ge.IsTarget(this)){
			ge.InfluenceTarget (this, false);
			if(cbEventEnded!=null){
				cbEventEnded (ge);
			}
		}
	}

	public bool HasFertility(Fertility fer){
		//this is here so we could make it 
		//That cities can have additional fertirilies as the island
		//for now its an easier way to get the information
		return island.myFertilities.Contains (fer);
	}

	public int GetPlayerNumber(){
		return playerNumber;
	}
	public int GetTargetType(){
		return TargetType;
	}

}
