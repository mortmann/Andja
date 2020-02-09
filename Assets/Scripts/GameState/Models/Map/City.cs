using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

[JsonObject(MemberSerialization.OptIn)]
public class City : IGEventable {

    #region Serialize
    [JsonPropertyAttribute] public bool autoUpgradeHomes;
    [JsonPropertyAttribute] public int playerNumber = 0;
    [JsonPropertyAttribute] public Inventory inventory;
    [JsonPropertyAttribute] public List<Structure> structures;
    [JsonPropertyAttribute] public float useTickTimer;
    [JsonPropertyAttribute] public Dictionary<string, TradeItem> itemIDtoTradeItem;
    [JsonPropertyAttribute] private string _name = "";
    [JsonPropertyAttribute] public Island island;
    [JsonPropertyAttribute] List<PopulationLevel> populationLevels;

    #endregion
    #region RuntimeOrOther
    public string Name {
        get {
            if (this.IsWilderness()) {
                return "Wilderness";
            }
            if (_name.Length == 0) {
                return "City " + island.Cities.IndexOf(this);
            }
            return _name;
        }
        set {
            _name = value;
        }
    }

    public HashSet<Tile> Tiles {
        get {
            return _Tiles;
        }

        set {
            _Tiles = value;
        }
    }

    public int PopulationCount {
        get {
            int sum = 0;
            foreach (PopulationLevel p in populationLevels)
                sum += p.populationCount;
            return sum;
        }
    }

    //TODO: set this to the player that creates this
    public List<HomeStructure> homes;
    private HashSet<Tile> _Tiles;
    public List<Route> routes;
    public Unit tradeUnit;

    public int expanses = 0;
    public int income = 0;
    public int Balance => income - expanses;
    public float useTick;
    public WarehouseStructure warehouse;

    Action<Structure> cbStructureAdded;
    Action<Structure> cbStructureRemoved;
    Action<City> cbCityDestroy;

    Action<Structure> cbRegisterTradeOffer;
    #endregion

    public City(int playerNr, Island island) {
        this.playerNumber = playerNr;
        this.island = island;

        Tiles = new HashSet<Tile>();

        itemIDtoTradeItem = new Dictionary<string, TradeItem>();
        structures = new List<Structure>();
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
        return populationLevels[structureLevel];
    }

    /// <summary>
    /// DO NOT USE! ONLY serialization!
    /// </summary>
    public City() {
        Tiles = new HashSet<Tile>();
    }

    private void Setup() {
        homes = new List<HomeStructure>();
        routes = new List<Route>();
        if (populationLevels == null)
            populationLevels = new List<PopulationLevel>();
        if (playerNumber < 0)
            return;
        foreach (PopulationLevel pl in PrototypController.Instance.GetPopulationLevels(this)) {
            if (populationLevels.Exists(x => x.Level == pl.Level))
                continue;
            if (pl.previousLevel != null)
                pl.previousLevel = populationLevels[pl.previousLevel.Level]; // so when adding new levels to existing links get updated
            populationLevels.Add(pl);
        }
        island.RegisterOnEvent(OnEventCreate, OnEventEnded);
    }

    internal PopulationLevel GetPreviousPopulationLevel(int level) {
        for (int i = level - 1; i >= 0; i--) {
            PopulationLevel p = populationLevels.Find(x => x.Level == level);
            if (p != null)
                return p;
        }
        return null;
    }

    public IEnumerable<Structure> Load(Island island) {
        this.island = island;
        Setup();
        foreach (Structure item in structures) {
            if (item is WarehouseStructure) {
                warehouse = (WarehouseStructure)item;
            }
            else
            if (item is HomeStructure) {
                homes.Add((HomeStructure)item);
            }
        }
        if (IsWilderness() == false) {
            for (int i = populationLevels.Count - 1; i >= 0; i--) {
                if (populationLevels[i].Exists() == false) {
                    populationLevels.Remove(populationLevels[i]);
                    continue;
                }
                populationLevels[i].Load();
            }
            PlayerController.GetPlayer(playerNumber).OnCityCreated(this);
        }

        return structures;
    }

    internal void Update(float deltaTime) {
        expanses = 0;
        for (int i = structures.Count-1; i >= 0; i--) {
            expanses += structures[i].MaintenanceCost;
            structures[i].Update(deltaTime);
        }
        if (playerNumber == -1 || homes.Count == 0) {
            return;
        }
        UpdateNeeds(deltaTime);
        //TODO: check for better spot?
        income = 0;
        foreach (PopulationLevel pl in populationLevels) {
            income += pl.GetTaxIncome(this);
        }
    }
    /// <summary>
    /// USE only for the creation of non player city aka Wilderness
    /// </summary>
    /// <param name="tiles">Tiles.</param>
    /// <param name="island">Island.</param>
    public City(List<Tile> tiles, Island island) {
        List<Tile> temp = new List<Tile>(tiles);
        island.Wilderness = this;
        Tiles = new HashSet<Tile>(tiles);
        for (int i = 0; i < tiles.Count; i++) {
            temp[i].City = null;
        }
        inventory = new Inventory(0);
        this.playerNumber = -1;
        this.island = island;
        island.Wilderness = this;
        structures = new List<Structure>();
    }
    public void AddStructure(Structure str) {
        if (structures.Contains(str)) {
            //happens on loading for loaded stuff
            //so not an actual error anymore
            //			Debug.LogError ("Adding a structure that already belongs to this city.");
            return;
        }
        if (str is HomeStructure) {
            homes.Add((HomeStructure)str);
        }
        if (str is WarehouseStructure) {
            if (warehouse != null && warehouse.buildID != str.buildID) {
                Debug.LogError("There should be only one Warehouse per City! ");
                return;
            }
            warehouse = (WarehouseStructure)str;
        }
        RemoveRessources(str.GetBuildingItems());

        structures.Add(str);

        cbStructureAdded?.Invoke(str);
    }

    private void UpdateNeeds(float deltaTime) {
        useTickTimer -= deltaTime;
        if (useTickTimer <= 0) {
            useTickTimer = useTick;
            foreach (PopulationLevel pop in populationLevels) {
                pop.FullfillNeedsAndCalcHappiness(this);
            }
        }
    }

    public void TriggerAddCallBack(Structure str) {
        cbStructureAdded?.Invoke(str);
    }
    //current wrapper to make sure its valid
    public void RemoveTile(Tile t) {
        //if it doesnt contain it there is an error
        if (Tiles.Contains(t) == false) {
            Debug.LogError("This city does not know that it had this tile! " + t.ToString() + " -> " + Tiles.Count);
            return;
        }
        Tiles.Remove(t);
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
        if (t.Type == TileType.Ocean || Tiles.Contains(t)) {
            return;
        }
        t.City = this;
        if (t.Structure != null) {
            if (structures.Contains(t.Structure) == false) {
                t.Structure.City = this;
                AddStructure(t.Structure);
            }
        }
        island.allReadyHighlighted = false;
        Tiles.Add(t);
        if (IsCurrPlayerCity()) {
            World.Current.OnTileChanged(t);
        }
    }
    public bool GetNeedCriticalForLevel(int structureLevel) {
        return populationLevels[structureLevel].criticalMissingNeed;
    }
    public void AddPeople(int level, int count) {
        if (count < 0) {
            return;
        }
        populationLevels[level].AddPeople(count);
    }
    public void RemovePeople(int level, int count) {
        if (count < 0) {
            return;
        }
        populationLevels[level].RemovePeople(count);
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
    public void SellingTradeItem(string itemID, Player player, Ship ship, int amount = 50) {
        if (itemIDtoTradeItem.ContainsKey(itemID) == false) {
            return;
        }
        TradeItem ti = itemIDtoTradeItem[itemID];
        if (ti.IsSelling == false) {
            Debug.Log("this item is not to buy");
            return;
        }
        Item i = ti.SellItemAmount(inventory.GetItemWithIDClone(itemID));
        Player Player = PlayerController.GetPlayer(playerNumber);
        int am = TradeWithShip(i, Mathf.Clamp(amount, 0, i.count), ship);
        Player.AddMoney(am * ti.price);
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
    public void BuyingTradeItem(string itemID, Player player, Ship ship, int amount = 50) {
        if (itemIDtoTradeItem.ContainsKey(itemID) == false) {
            return;
        }
        TradeItem ti = itemIDtoTradeItem[itemID];
        if (ti.IsBuying == false) {
            Debug.Log("this item is not to sell here");
            return;
        }
        Item i = ti.BuyItemAmount(inventory.GetItemWithIDClone(itemID));
        Player Player = PlayerController.GetPlayer(playerNumber);
        int am = TradeFromShip(ship, i, Mathf.Clamp(amount, 0, i.count));
        Player.ReduceMoney(am * ti.price);
        player.AddMoney(am * ti.price);
    }
    public int TradeWithShip(Item toTrade, int amount = 50, Unit ship = null) {
        if (warehouse == null || warehouse.inRangeUnits.Count == 0 || toTrade == null) {
            Debug.Log("myWarehouse==null || myWarehouse.inRangeUnits.Count==0  || toTrade ==null");
            return 0;
        }
        if (tradeUnit == null && ship == null) {
            return inventory.MoveItem(warehouse.inRangeUnits[0].inventory, toTrade, amount);
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
        if (routes == null) {
            routes = new List<Route>(); // i dont get why its null while loading
        }
        this.routes.Add(route);
    }

    public void RemoveRoute(Route route) {
        if (routes.Contains(route)) {
            routes.Remove(route);
        }
    }
    public void RemoveStructure(Structure structure, bool returnRessources = false) {
        if (structure == null) {
            Debug.Log("null");
            return;
        }
        if (structures.Contains(structure)) {
            if (structure is HomeStructure) {
                homes.Remove((HomeStructure)structure);
            }
            else
            if (structure is WarehouseStructure) {
                warehouse = null;
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
            structures.Remove(structure);
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
        return populationLevels[level].Happiness;
    }
    internal IEnumerable<NeedGroup> GetPopulationALLNeedGroups(int level) {
        return populationLevels[level].AllNeedGroupList;
    }
    public void RemoveTiles(IEnumerable<Tile> tiles) {
        foreach (Tile item in tiles) {
            item.City = null;
            if (item.City != this) {
                Tiles.Remove(item);
                if (IsCurrPlayerCity()) {
                    World.Current.OnTileChanged(item);
                }
            }
        }
        if (Tiles.Count == 0) {
            Destroy();
        }
    }
    public void Destroy() {
        if (playerNumber == -1) {
            return; // this is the wilderness it cant be removed! or destroyed
        }
        if (Tiles.Count > 0) {
            RemoveTiles(Tiles);
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
        return island.Fertilities.Contains(fer);
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
