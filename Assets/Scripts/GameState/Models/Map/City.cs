using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class City : IGEventable {

    #region Serialize
    [JsonPropertyAttribute] public bool AutoUpgradeHomes;
    [JsonPropertyAttribute] public int playerNumber = 0;
    [JsonPropertyAttribute] public Inventory inventory;
    [JsonPropertyAttribute] public List<Structure> myStructures;
    [JsonPropertyAttribute] public float useTickTimer;
    /// <summary>
    /// ITEM which is to trade
    /// bool = true -> SELL
    /// 	 = false -> BUY
    /// </summary>
    [JsonPropertyAttribute] public Dictionary<string, TradeItem> itemIDtoTradeItem;
    [JsonPropertyAttribute] private string _name = "";

    [JsonPropertyAttribute] public Island island;

    [JsonPropertyAttribute] List<PopulationLevel> PopulationLevels;

    #endregion
    #region RuntimeOrOther
    public string Name {
        get {
            if (this.IsWilderness()) {
                return "Wilderness";
            }
            if (_name.Length == 0) {
                return "City " + island.myCities.IndexOf(this);
            }
            return _name;
        }
        set {
            _name = value;
        }
    }

    public HashSet<Tile> MyTiles {
        get {
            return _myTiles;
        }

        set {
            _myTiles = value;
        }
    }

    public int PopulationCount {
        get {
            int sum = 0;
            foreach (PopulationLevel p in PopulationLevels)
                sum += p.populationCount;
            return sum;
        }
    }

    //TODO: set this to the player that creates this
    public List<HomeStructure> myHomes;
    private HashSet<Tile> _myTiles;
    public List<Route> myRoutes;
    public Unit tradeUnit;

    public int Expanses = 0;
    public int Income = 0;
    public int Balance => Income - Expanses;
    public float useTick;
    public WarehouseStructure myWarehouse;

    Action<Structure> cbStructureAdded;
    Action<Structure> cbStructureRemoved;
    Action<City> cbCityDestroy;

    Action<Structure> cbRegisterTradeOffer;
    #endregion

    public City(int playerNr, Island island) {
        this.playerNumber = playerNr;
        this.island = island;

        MyTiles = new HashSet<Tile>();

        itemIDtoTradeItem = new Dictionary<string, TradeItem>();
        myStructures = new List<Structure>();
        inventory = new CityInventory();
        _name = "<City>" + UnityEngine.Random.Range(0, 1000);

        Setup();
        useTick = 15f;

        //		useTickTimer = useTick;
    }

    internal bool HasAnythingOfItems(Item[] buildingItems) {
        foreach (Item i in buildingItems) {
            if (HasAnythingOfItem(i) == false)
                return false;
        }
        return true;
    }

    internal PopulationLevel GetPopulationLevel(int structureLevel) {
        return PopulationLevels[structureLevel];
    }

    /// <summary>
    /// DO NOT USE! ONLY serialization!
    /// </summary>
    public City() {
        MyTiles = new HashSet<Tile>();
    }

    private void Setup() {
        myHomes = new List<HomeStructure>();
        myRoutes = new List<Route>();
        if (PopulationLevels == null)
            PopulationLevels = new List<PopulationLevel>();
        if (playerNumber < 0)
            return;
        foreach (PopulationLevel pl in PrototypController.Instance.GetPopulationLevels(this)) {
            if (PopulationLevels.Exists(x => x.Level == pl.Level))
                continue;
            if (pl.previousLevel != null)
                pl.previousLevel = PopulationLevels[pl.previousLevel.Level]; // so when adding new levels to existing links get updated
            PopulationLevels.Add(pl);
        }
        island.RegisterOnEvent(OnEventCreate, OnEventEnded);
    }

    internal PopulationLevel GetPreviousPopulationLevel(int level) {
        for (int i = level - 1; i >= 0; i--) {
            PopulationLevel p = PopulationLevels.Find(x => x.Level == level);
            if (p != null)
                return p;
        }
        return null;
    }

    public IEnumerable<Structure> Load(Island island) {
        this.island = island;
        Setup();
        foreach (Structure item in myStructures) {
            if (item is WarehouseStructure) {
                myWarehouse = (WarehouseStructure)item;
            }
            else
            if (item is HomeStructure) {
                myHomes.Add((HomeStructure)item);
            }
        }
        if (IsWilderness() == false) {
            for (int i = PopulationLevels.Count - 1; i >= 0; i--) {
                if (PopulationLevels[i].Exists() == false) {
                    PopulationLevels.Remove(PopulationLevels[i]);
                    continue;
                }
                PopulationLevels[i].Load();
            }
            PlayerController.GetPlayer(playerNumber).OnCityCreated(this);
        }

        return myStructures;
    }

    internal void Update(float deltaTime) {
        Expanses = 0;
        for (int i = myStructures.Count-1; i >= 0; i--) {
            Expanses += myStructures[i].MaintenanceCost;
            myStructures[i].Update(deltaTime);
        }
        if (playerNumber == -1 || myHomes.Count == 0) {
            return;
        }
        UpdateNeeds(deltaTime);
        //TODO: check for better spot?
        Income = 0;
        foreach (PopulationLevel pl in PopulationLevels) {
            Income += pl.GetTaxIncome(this);
        }
    }
    /// <summary>
    /// USE only for the creation of non player city aka Wilderness
    /// </summary>
    /// <param name="tiles">Tiles.</param>
    /// <param name="island">Island.</param>
    public City(List<Tile> tiles, Island island) {
        //		tiles.ForEach (x => x.myCity = this);
        List<Tile> temp = new List<Tile>(tiles);
        island.Wilderness = this;
        MyTiles = new HashSet<Tile>(tiles);
        for (int i = 0; i < tiles.Count; i++) {
            temp[i].MyCity = null;
        }
        inventory = new Inventory(0);
        this.playerNumber = -1;
        this.island = island;
        island.Wilderness = this;
        myStructures = new List<Structure>();
    }
    public void AddStructure(Structure str) {
        if (myStructures.Contains(str)) {
            //happens on loading for loaded stuff
            //so not an actual error anymore
            //			Debug.LogError ("Adding a structure that already belongs to this city.");
            return;
        }
        if (str is HomeStructure) {
            myHomes.Add((HomeStructure)str);
        }
        if (str is WarehouseStructure) {
            if (myWarehouse != null && myWarehouse.buildID != str.buildID) {
                Debug.LogError("There should be only one Warehouse per City! ");
                return;
            }
            myWarehouse = (WarehouseStructure)str;
        }
        RemoveRessources(str.GetBuildingItems());

        myStructures.Add(str);

        cbStructureAdded?.Invoke(str);
    }

    private void UpdateNeeds(float deltaTime) {
        useTickTimer -= deltaTime;
        if (useTickTimer <= 0) {
            useTickTimer = useTick;
            CalculateNeeds();
        }
    }
    public void CalculateNeeds() {
        foreach (PopulationLevel pop in PopulationLevels) {
            pop.FullfillNeedsAndCalcHappiness(this);
        }
        //foreach(Need need in itemNeeds){
        //	need.TryToConsumThisIn (this,citizienCount);
        //	levelCount [need.StartLevel]++;
        //	sumOfPerc [need.StartLevel] += need.percantageAvailability;
        //	if(need.percantageAvailability<0.4f){
        //		criticalAvaibilityNeed [need.StartLevel] = true;
        //	}
        //}
        //float[] percentagesPerLevel = new float[citizienLevels];
        //float percantageSummed=0;
        //for (int i = 0; i < citizienLevels; i++) {
        //	percentagesPerLevel [i] = sumOfPerc [i] / levelCount [i];
        //	for (int s = 0; s <= i; s++) { 
        //		percantageSummed = percentagesPerLevel [s]; 
        //	}
        //	citizienHappiness [i] = percantageSummed / (i+1);
        //}
    }

    public void TriggerAddCallBack(Structure str) {
        cbStructureAdded?.Invoke(str);
    }
    //current wrapper to make sure its valid
    public void RemoveTile(Tile t) {
        //if it doesnt contain it there is an error
        if (MyTiles.Contains(t) == false) {
            Debug.LogError("This city does not know that it had this tile! " + t.ToString() + " -> " + MyTiles.Count);
            return;
        }
        MyTiles.Remove(t);
        island.allReadyHighlighted = false;
    }
    public void AddTiles(IEnumerable<Tile> t) {
        AddTiles(new HashSet<Tile>(t));
    }

    public void AddTiles(HashSet<Tile> t) {
        // does not really needs it because tiles witout island reject cities
        //but it is a secondary security that this does not happen
        if (t == null) {
            return;
        }
        t.RemoveWhere(x => x == null || x.Type == TileType.Ocean);
        List<Tile> tiles = new List<Tile>(t);
        for (int i = 0; i < tiles.Count; i++) {
            AddTile(tiles[i]);
        }
        island.allReadyHighlighted = false;
    }
    public void AddTile(Tile t) {
        if (t.Type == TileType.Ocean || MyTiles.Contains(t)) {
            return;
        }
        t.MyCity = this;
        if (t.Structure != null) {
            if (myStructures.Contains(t.Structure) == false) {
                t.Structure.City = this;
                AddStructure(t.Structure);
            }
        }
        island.allReadyHighlighted = false;
        MyTiles.Add(t);
        if (IsCurrPlayerCity()) {
            World.Current.OnTileChanged(t);
        }
    }
    public bool GetNeedCriticalForLevel(int structureLevel) {
        return PopulationLevels[structureLevel].criticalMissingNeed;
    }
    public void AddPeople(int level, int count) {
        if (count < 0) {
            return;
        }
        PopulationLevels[level].AddPeople(count);
    }
    public void RemovePeople(int level, int count) {
        if (count < 0) {
            return;
        }
        PopulationLevels[level].RemovePeople(count);
    }

    public void RemoveRessources(Item[] remove) {
        if (remove == null) {
            return;
        }
        foreach (Item item in remove) {
            inventory.RemoveItemAmount(item);
        }
    }

    public void RemoveRessource(Item item, int amount) {
        if (amount < 0) {
            return;
        }
        Item i = item.Clone();
        i.count = amount;
        inventory.RemoveItemAmount(i);
    }
    public bool HasEnoughOfItems(IEnumerable<Item> item) {
        return inventory.HasEnoughOfItems(item);
    }
    public bool HasEnoughOfItem(Item item) {
        return inventory.HasEnoughOfItem(item);
    }
    public bool HasAnythingOfItem(Item item) {
        return inventory.HasAnythingOf(item);
    }
    public bool IsWilderness() {
        if (playerNumber == -1 && this != island.Wilderness) {
            Debug.LogError("NOT WILDERNESS! But -1 playernumber!? ");
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
    public void BuyFromCity(string itemID, Player player, Ship ship, int amount = 50) {
        if (itemIDtoTradeItem.ContainsKey(itemID) == false) {
            return;
        }
        //true = BUY
        TradeItem ti = itemIDtoTradeItem[itemID];
        if (ti.selling == false) {
            Debug.Log("this item is not to buy");
            return;
        }
        Item i = ti.SellItemAmount(inventory.GetItemWithIDClone(itemID));
        Player myPlayer = PlayerController.GetPlayer(playerNumber);
        int am = TradeWithShip(i, Mathf.Clamp(amount, 0, i.count), ship);
        myPlayer.AddMoney(am * ti.price);
        player.ReduceMoney(am * ti.price);

    }
    /// <summary>
    /// Ship sells to city.
    /// City BUYs it.
    /// </summary>
    /// <param name="itemID">Item I.</param>
    /// <param name="player">Player.</param>
    /// <param name="ship">Ship.</param>
    /// <param name="amount">Amount.</param>
    public void SellToCity(string itemID, Player player, Ship ship, int amount = 50) {
        if (itemIDtoTradeItem.ContainsKey(itemID) == false) {
            return;
        }
        TradeItem ti = itemIDtoTradeItem[itemID];
        //TRUE = sell
        if (ti.selling == true) {
            Debug.Log("this item is not to sell here");
            return;
        }
        Item i = ti.BuyItemAmount(inventory.GetItemWithIDClone(itemID));
        Player myPlayer = PlayerController.GetPlayer(playerNumber);
        int am = TradeFromShip(ship, i, Mathf.Clamp(amount, 0, i.count));
        myPlayer.ReduceMoney(am * ti.price);
        player.AddMoney(am * ti.price);
    }
    public int TradeWithShip(Item toTrade, int amount = 50, Unit ship = null) {
        if (myWarehouse == null || myWarehouse.inRangeUnits.Count == 0 || toTrade == null) {
            Debug.Log("myWarehouse==null || myWarehouse.inRangeUnits.Count==0  || toTrade ==null");
            return 0;
        }
        if (tradeUnit == null && ship == null) {
            return inventory.MoveItem(myWarehouse.inRangeUnits[0].inventory, toTrade, amount);
        }
        else if (ship == null) {
            return inventory.MoveItem(tradeUnit.inventory, toTrade, amount);
        }
        else {
            return inventory.MoveItem(ship.inventory, toTrade, amount);
        }
    }
    public int TradeFromShip(Unit u, Item getTrade, int amount = 50) {
        if (getTrade == null) {
            return 0;
        }
        return u.inventory.MoveItem(inventory, getTrade, amount);
    }

    public void RemoveTradeItem(Item item) {
        itemIDtoTradeItem.Remove(item.ID);
    }
    public void ChangeTradeItemAmount(Item item) {
        itemIDtoTradeItem[item.ID].count = item.count;
    }
    public void ChangeTradeItemPrice(string id, int price) {
        itemIDtoTradeItem[id].price = price;
    }
    public int GetAmountForThis(Item item, float amount) {
        return inventory.GetAmountForItem(item);
    }

    public void AddRoute(Route route) {
        if (myRoutes == null) {
            myRoutes = new List<Route>(); // i dont get why its null while loading
        }
        this.myRoutes.Add(route);
    }

    public void RemoveRoute(Route route) {
        if (myRoutes.Contains(route)) {
            myRoutes.Remove(route);
        }
    }
    public void RemoveStructure(Structure structure, bool returnRessources = false) {
        if (structure == null) {
            Debug.Log("null");
            return;
        }
        if (myStructures.Contains(structure)) {
            if (structure is HomeStructure) {
                myHomes.Remove((HomeStructure)structure);
            }
            else
            if (structure is WarehouseStructure) {
                myWarehouse = null;
            }
            //if were geting some of the ressources back
            //when we destroy it -> should be a setting 
            if (returnRessources) {
                Item[] res = structure.BuildingItems;
                for (int i = 0; i < res.Length; i++) {
                    res[i].count /= 3; // FIXME do not have this hardcoded! Change it to be chooseable!
                }
                inventory.AddItems(res);
            }
            myStructures.Remove(structure);
            cbStructureRemoved?.Invoke(structure);
        }
        else {
            //this is no error if this is wilderness
            if (structure is WarehouseStructure) {
                return;
            }
            Debug.LogError(this.Name + " This structure " + structure.ToString() + " does not belong to this city ");
        }
        island.allReadyHighlighted = false;

    }
    public float GetHappinessForCitizenLevel(int level) {
        return PopulationLevels[level].Happiness;
    }
    internal IEnumerable<NeedGroup> GetPopulationALLNeedGroups(int level) {
        return PopulationLevels[level].AllNeedGroupList;
    }
    public void RemoveTiles(IEnumerable<Tile> tiles) {
        foreach (Tile item in tiles) {
            item.MyCity = null;
            if (item.MyCity != this) {
                MyTiles.Remove(item);
                if (IsCurrPlayerCity()) {
                    World.Current.OnTileChanged(item);
                }
            }
        }
        if (MyTiles.Count == 0) {
            Destroy();
        }
    }
    public void Destroy() {
        if (playerNumber == -1) {
            return; // this is the wilderness it cant be removed! or destroyed
        }
        if (MyTiles.Count > 0) {
            RemoveTiles(MyTiles);
        }
        island.RemoveCity(this);
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
    #region igeventable
    public override void OnEventCreate(GameEvent ge) {
        if (ge.target is City && ge.target != this)
            return;
        //this only gets called in two cases
        //either event is on this island or in one of its cities
        if (ge.IsTarget(this)) {
            ge.EffectTarget(this, true);
            cbEventCreated?.Invoke(ge);
        } else {
            cbEventCreated?.Invoke(ge);
        }
    }
    public override void OnEventEnded(GameEvent ge) {
        if (ge.target is City && ge.target != this)
            return;
        //this only gets called in two cases
        //either event is on this island or in one of its cities
        if (ge.IsTarget(this)) {
            ge.EffectTarget(this, false);
            cbEventEnded?.Invoke(ge);
        } else {
            cbEventEnded?.Invoke(ge);
        }
    }
    public bool HasFertility(Fertility fer) {
        //this is here so we could make it 
        //That cities can have additional fertirilies as the island
        //for now its an easier way to get the information
        return island.myFertilities.Contains(fer);
    }
    public override int GetPlayerNumber() {
        return playerNumber;
    }
    #endregion

    public bool IsCurrPlayerCity() {
        return playerNumber == PlayerController.currentPlayerNumber;
    }

    public Player GetOwner() {
        return PlayerController.GetPlayer(playerNumber);
    }
    public override string ToString() {
        return Name;
    }

}
