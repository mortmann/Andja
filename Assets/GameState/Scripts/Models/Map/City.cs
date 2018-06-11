using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class City : IGEventable {
	public const int TargetType = 12;

	//TODO FIX this position of this
	public const int citizienLevels = 4;
	#region Serialize
	[JsonPropertyAttribute] public int playerNumber = 0;
	[JsonPropertyAttribute] public Inventory inventory;
	[JsonPropertyAttribute] public List<Structure> myStructures;
	[JsonPropertyAttribute] public Dictionary<int,Need> idToNeed; // i think this is what is missing
	[JsonPropertyAttribute] public float useTickTimer;
	/// <summary>
	/// ITEM which is to trade
	/// bool = true -> SELL
	/// 	 = false -> BUY
	/// </summary>
	[JsonPropertyAttribute] public Dictionary<int,TradeItem> itemIDtoTradeItem;
	[JsonPropertyAttribute] private string _name=""; 

	[JsonPropertyAttribute] public int[] citizienCount;
	[JsonPropertyAttribute] public float[] citizienHappiness;
	[JsonPropertyAttribute] public bool[] criticalAvaibilityNeed;

	[JsonPropertyAttribute] public Island island;


	#endregion
	#region RuntimeOrOther
	public string Name {get{
			if(this.IsWilderness ()){
				return "Wilderness";
			}
			if(_name.Length==0){
				return "City "+island.myCities.IndexOf (this);	
			}
			return _name;
	}}

    public HashSet<Tile> MyTiles {
        get {
            return _myTiles;
        }

        set {
            _myTiles = value;
        }
    }

    //TODO: set this to the player that creates this
    public List<HomeBuilding> myHomes;
    private HashSet<Tile> _myTiles;
    public List<Route> myRoutes;
	public Unit tradeUnit;
	public int cityBalance;
	public float useTick;
	public Warehouse myWarehouse;

	public List<Need> itemNeeds;
	public List<Need> structureNeeds;

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

        MyTiles = new HashSet<Tile>();

        itemIDtoTradeItem = new Dictionary<int, TradeItem> ();
		myStructures = new List<Structure>();
		inventory = new CityInventory (Name);
        Setup();
		citizienCount = new int[citizienLevels];
		citizienHappiness = new float[citizienLevels];
		criticalAvaibilityNeed = new bool[citizienLevels];
		for (int i = 0; i < citizienCount.Length; i++) {
			citizienCount [i] = 0;
			citizienHappiness [i] = 0.5f;
			criticalAvaibilityNeed [i] = false;
		}

		_name = "<City>" + UnityEngine.Random.Range (0, 1000);
		//		useTickTimer = useTick;
    }
	/// <summary>
	/// DO NOT USE! ONLY serialization!
	/// </summary>
	public City(){
        MyTiles = new HashSet<Tile>();
        Setup();
    }

	private void Setup(){
        myHomes = new List<HomeBuilding> ();
		myRoutes = new List<Route> ();
		if(idToNeed==null){
			return;
		}
		itemNeeds = new List<Need> ();
		structureNeeds = new List<Need> ();
		List<Need> allNeeds = World.GetCopieOfAllNeeds();
		if(citizienHappiness == null){
			citizienCount = new int[citizienLevels];
			citizienHappiness = new float[citizienLevels];
			criticalAvaibilityNeed = new bool[citizienLevels];

			for (int i = 0; i < citizienCount.Length; i++) {
				citizienCount [i] = 0;
				citizienHappiness [i] = 0.5f;
				criticalAvaibilityNeed [i] = false;
			}
		}
		for (int i = 0; i < allNeeds.Count; i++) {
			if(allNeeds[i].IsItemNeed()){
				itemNeeds.Add (allNeeds [i]);
			} else 
			if(allNeeds[i].IsStructureNeed()){
				structureNeeds.Add (allNeeds [i]);
			}
		}

		// THINGS NEED TO BE LOADED IN 
		useTick = 30f;

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

    internal void Update(float deltaTime) {
		for (int i = 0; i < myStructures.Count; i++) {
			myStructures[i].Update(deltaTime);
		}
		if(playerNumber==-1 || myHomes.Count==0){
			return;
		}
		UpdateNeeds (deltaTime);
    }
	/// <summary>
	/// USE only for the creation of non player city aka Wilderness
	/// </summary>
	/// <param name="tiles">Tiles.</param>
	/// <param name="island">Island.</param>
	public City(List<Tile> tiles,Island island){
//		tiles.ForEach (x => x.myCity = this);
		List<Tile> temp = new List<Tile>(tiles);
		island.Wilderness = this;
		MyTiles = new HashSet<Tile> (tiles);
		for (int i = 0; i < tiles.Count; i++) {
			temp[i].MyCity= null;
		}
		inventory = new Inventory (0);
		this.playerNumber = -1;
		this.island = island;
		island.Wilderness = this;
		myStructures = new List<Structure> ();
	}
	public void AddStructure(Structure str){
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
			if(myWarehouse!=null && myWarehouse.buildID!=str.buildID){
				Debug.LogError ("There should be only one Warehouse per City! ");
				return;
			}
			myWarehouse = (Warehouse)str;
		}
		cityBalance += str.Maintenancecost;
		RemoveRessources (str.GetBuildingItems ());

		myStructures.Add (str);

        cbStructureAdded?.Invoke(str);
    }

	private void UpdateNeeds(float deltaTime){
		for (int i = 0; i < citizienLevels; i++) {
			criticalAvaibilityNeed [i] = false;
		}
		useTickTimer -= deltaTime;
		if(useTickTimer<=0){
			useTickTimer = useTick;
			CalculateNeeds ();

		}
	}
	public void CalculateNeeds(){
		int[] levelCount = new int[citizienLevels];
		float[] sumOfPerc = new float[citizienLevels];
		foreach(Need need in itemNeeds){
			need.TryToConsumThisIn (this,citizienCount);
			levelCount [need.StartLevel]++;
			sumOfPerc [need.StartLevel] += need.percantageAvailability;
			if(need.percantageAvailability<0.4f){
				criticalAvaibilityNeed [need.StartLevel] = true;
			}
		}
		float[] percentagesPerLevel = new float[citizienLevels];
		float percantageSummed=0;
		for (int i = 0; i < citizienLevels; i++) {
			percentagesPerLevel [i] = sumOfPerc [i] / levelCount [i];
			for (int s = 0; s <= i; s++) {
				percantageSummed = percentagesPerLevel [s]; 
			}
			citizienHappiness [i] = percantageSummed / (i+1);
		}
	}

	public void TriggerAddCallBack(Structure str){
        cbStructureAdded?.Invoke(str);
    }
	//current wrapper to make sure its valid
	public void RemoveTile(Tile t){
		//if it doesnt contain it there is an error
		if(MyTiles.Contains (t)==false){
			Debug.LogError ("This city does not know that it had this tile! " + t.ToString () +" -> " +MyTiles.Count); 
			return;
		}
		MyTiles.Remove (t);
		island.allReadyHighlighted = false;
	}
    public void AddTiles(IEnumerable<Tile> t) {
        AddTiles(new HashSet<Tile>(t));
    }

    public void AddTiles(HashSet<Tile> t){
		// does not really needs it because tiles witout island reject cities
		//but it is a secondary security that this does not happen
		if(t==null){
			return;
		}
		t.RemoveWhere (x =>x==null || x.Type == TileType.Ocean);
        List<Tile> tiles = new List<Tile> (t);
		for (int i = 0; i < tiles.Count; i++) {
			tiles [i].MyCity = this;
			if(tiles[i].Structure != null){
				if (myStructures.Contains (tiles [i].Structure) ==false) { 
					tiles [i].Structure.City = this;
					AddStructure (tiles [i].Structure);
				}
			}
			MyTiles.Add (tiles[i]);
		}
		island.allReadyHighlighted = false;
	}
	public void AddTile(Tile t){
        if (t.Type==TileType.Ocean||MyTiles.Contains (t)){
			return;
		}
		if(t.Structure != null){
			if (myStructures.Contains (t.Structure) == false) { 
				t.Structure.City = this;
				AddStructure (t.Structure);
			}
		}
		island.allReadyHighlighted = false;
		MyTiles.Add (t);
	}
	public bool GetNeedCriticalForLevel(int buildingLevel){
		return criticalAvaibilityNeed [buildingLevel];
	}
	public void AddPeople(int level, int count){
		if(count<0){
			return;
		}
		citizienCount [level] += count;
	}
	public void RemovePeople(int level, int count){
		if(count<0){
			return;
		}
		citizienCount [level] -= count;
	}

	public void RemoveRessources(Item[] remove){
		if(remove==null){
			return;
		}
		foreach (Item item in remove) {
			inventory.removeItemAmount (item);
		}
	}

	public void RemoveRessource(Item item, int amount){
		if(amount<0){
			return;
		}
		Item i = item.Clone ();
		i.count = amount;
		inventory.removeItemAmount (i);
	}

	public bool HasItem(Item item){
		return inventory.hasAnythingOf (item);
	}
	public bool IsWilderness(){
		if(playerNumber==-1 && this != island.Wilderness){
			Debug.LogError ("NOT WILDERNESS! But -1 playernumber!? ");
		}
		return this == island.Wilderness;
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
		Item i = ti.SellItemAmount (inventory.GetItemWithIDClone (itemID));
		Player myPlayer = PlayerController.Instance.GetPlayer (playerNumber);
		int am = TradeWithShip (i,Mathf.Clamp (amount,0,i.count),ship);
		myPlayer.AddMoney (am * ti.price);
		player.ReduceMoney (am * ti.price);

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
		Item i = ti.BuyItemAmount (inventory.GetItemWithIDClone (itemID));
		Player myPlayer = PlayerController.Instance.GetPlayer (playerNumber);
		int am = TradeFromShip (ship,i,Mathf.Clamp (amount,0,i.count));
		myPlayer.ReduceMoney (am * ti.price);
		player.AddMoney (am * ti.price);

	}
	public int TradeWithShip(Item toTrade,int amount=50, Unit ship = null){
		if(myWarehouse==null || myWarehouse.inRangeUnits.Count==0  || toTrade ==null){
			Debug.Log ("myWarehouse==null || myWarehouse.inRangeUnits.Count==0  || toTrade ==null"); 
			return 0;
		}
		if (tradeUnit == null && ship==null) {
			return inventory.moveItem (myWarehouse.inRangeUnits [0].inventory, toTrade,amount);
		} else if(ship==null){
			return inventory.moveItem (tradeUnit.inventory, toTrade,amount);
		} else {
			return inventory.moveItem (ship.inventory, toTrade,amount);
		}
	}
	public int TradeFromShip(Unit u,Item getTrade,int amount = 50){
		if(getTrade ==null){
			return 0;
		}
		return u.inventory.moveItem (inventory,getTrade,amount);
	}
	public float GetPercentage(Need need){
		if(idToNeed.ContainsKey (need.ID)==false){
			return 0;
		}
		return idToNeed[need.ID].percantageAvailability;
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
	public int GetAmountForThis( Item item, float amount ){
		return inventory.GetAmountForItem (item);
	}
	
	public void AddRoute(Route route){
		if(myRoutes==null){
			myRoutes = new List<Route> (); // i dont get why its null while loading
		}
		this.myRoutes.Add (route);
	}

	public void RemoveRoute(Route route){
		if(myRoutes.Contains (route)){
			myRoutes.Remove (route);
		} 
	}
	public void RemoveStructure(Structure structure, bool returnRessources=false){
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
				Item[] res = structure.BuildingItems;
				for (int i = 0; i < res.Length; i++) {
					res [i].count /= 3; // FIXME do not have this hardcoded! Change it to be chooseable!
				}
				inventory.AddItems (res);
			}
			myStructures.Remove (structure);
			cityBalance -= structure.Maintenancecost;
            cbStructureRemoved?.Invoke(structure);
        } else {
			//this is no error if this is wilderness
			if(structure is Warehouse){
				return;
			}
			Debug.LogError (this.Name + " This structure "+structure.ToString () +" does not belong to this city "); 
		}
		island.allReadyHighlighted = false;

	}
	public float GetHappinessForCitizenLevel(int level){
		return citizienHappiness [level];
	}
	public void RemoveTiles(IEnumerable<Tile> tiles){
		foreach (Tile item in tiles) {
			item.MyCity = null;
			if(item.MyCity!=this){
				MyTiles.Remove (item);
			}
		}
		if(MyTiles.Count==0){
			Destroy ();
		}
	}
	public void Destroy(){
		if(playerNumber==-1){
			return; // this is the wilderness it cant be removed! or destroyed
		}
		if(MyTiles.Count > 0){
			RemoveTiles (MyTiles);
		}
		island.RemoveCity (this);
        cbCityDestroy?.Invoke(this);
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
            cbEventCreated?.Invoke(ge);
        }
	}
	public void OnEventEnded(GameEvent ge){
		//this only gets called in two cases
		//either event is on this island or in one of its cities
		if(ge.IsTarget (island)||ge.IsTarget(this)){
			ge.InfluenceTarget (this, false);
            cbEventEnded?.Invoke(ge);
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
	public bool IsCurrPlayerCity(){
		return playerNumber == PlayerController.currentPlayerNumber;
	}
	public int GetTargetType(){
		return TargetType;
	}
    
    public override string ToString() {
        return Name;
    }
}
