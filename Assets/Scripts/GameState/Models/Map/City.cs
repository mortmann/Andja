using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class City : GEventable, ICity {

        #region Serialize

        [JsonPropertyAttribute] public bool AutoUpgradeHomes { get; set; }
        [JsonPropertyAttribute] public int PlayerNumber { get; protected set; }
        [JsonPropertyAttribute] public CityInventory Inventory { get; protected set; }
        [JsonPropertyAttribute] public List<Structure> Structures { get; protected set; }
        [JsonPropertyAttribute] public Dictionary<string, TradeItem> ItemIDtoTradeItem { get; protected set; }
        [JsonPropertyAttribute] public IIsland Island { get; set; }
        [JsonPropertyAttribute] public int PlayerTradeAmount { get; protected set; }
        [JsonPropertyAttribute] public List<PopulationLevel> PopulationLevels { get; protected set; }
        [JsonPropertyAttribute] public float UseTickTimer { get; protected set; }
        [JsonPropertyAttribute] private string _name = "";

        #endregion Serialize

        #region RuntimeOrOther

        public string Name {
            get {
                if (this.IsWilderness()) {
                    return "Wilderness";
                }
                if (_name.Length == 0) {
                    return "City " + Island.Cities.IndexOf(this);
                }
                return _name;
            }
            set => _name = value;
        }

        public void SetPlayerTradeAmount(int amount) {
            PlayerTradeAmount = amount;
        }
        public void SetName(string name) {
            Name = name;
        }

        public HashSet<Tile> Tiles { get; set; }

        public int TradeItemCount => Warehouse?.TradeItemCount ?? 0;

        public int PopulationCount => PopulationLevels.Sum(p => p.PopulationCount);

        protected List<IHomeStructure> Homes;
        public List<MarketStructure> MarketStructures { get; set; }
        public List<Route> Routes { get; set; }
        public Unit TradeUnit { get; set; }

        public int Expanses { get; protected set; }
        public int Income { get; protected set; }
        public int Balance => Income - Expanses;
        public static float UseTick => 60f;
        public WarehouseStructure Warehouse { get; set; }

        private Action<Structure> _cbStructureAdded;
        private Action<Structure> _cbStructureRemoved;
        private Action<ICity> _cbCityDestroy;
        private Action<ICity, Tile> _cbTileAdded;
        private Action<ICity, Tile> _cbTileRemoved;

        #endregion RuntimeOrOther
        /// <summary>
        /// DO NOT USE! ONLY serialization!
        /// </summary>
        public City() {
            Structures = new List<Structure>();
            Tiles = new HashSet<Tile>();
            Routes = new List<Route>();
        }
        public City(int playerNr, IIsland island) : this() {
            this.PlayerNumber = playerNr;
            this.Island = island;
            _name = "<City> " + UnityEngine.Random.Range(0, 1000);
            ItemIDtoTradeItem = new Dictionary<string, TradeItem>();
            Structures = new List<Structure>();
            Tiles = new HashSet<Tile>();
            Routes = new List<Route>();
            Inventory = new CityInventory(42);
            UseTickTimer = UseTick;
            Setup();
        }

        public void SetTaxForPopulationLevel(int structureLevel, float percentage) {
            if (IsWilderness())
                return;
            PopulationLevels[structureLevel].SetTaxPercentage(percentage);
        }

        public bool AddTradeItem(TradeItem ti) {
            if (ItemIDtoTradeItem.ContainsKey(ti.ItemId)) {
                Debug.LogError("Tried to add Trade Item that exists");
                return false;
            }
            if (Warehouse.TradeItemCount <= ItemIDtoTradeItem.Count) {
                return false;
            }
            ItemIDtoTradeItem.Add(ti.ItemId, ti);
            return true;
        }

        public void DeleteTradeItem(TradeItem ti) {
            if (ItemIDtoTradeItem.ContainsKey(ti.ItemId) == false) {
                Debug.LogError("Tried to remove Trade Item that doesnt exist");
                return;
            }
            ItemIDtoTradeItem.Remove(ti.ItemId);
        }

        public PopulationLevel GetPopulationLevel(int structureLevel) {
            return PopulationLevels[structureLevel];
        }

        private void Setup() {
            if (PlayerNumber < 0)
                return;
            Routes = new List<Route>();
            Homes = new List<IHomeStructure>();
            MarketStructures = new List<MarketStructure>();
            PopulationLevels ??= new List<PopulationLevel>();
            foreach (PopulationLevel pl in PrototypController.Instance.GetPopulationLevels(this)) {
                if (PopulationLevels.Exists(x => x.Level == pl.Level))
                    continue;
                if (pl.previousLevel != null)
                    pl.previousLevel = PopulationLevels[pl.previousLevel.Level]; // so when adding new levels to existing links get updated
                PopulationLevels.Add(pl);
            }
            Island.RegisterOnEvent(OnEventCreate, OnEventEnded);
        }

        public PopulationLevel GetPreviousPopulationLevel(int level) {
            for (int i = level - 1; i >= 0; i--) {
                PopulationLevel p = PopulationLevels.Find(x => x.Level == level);
                if (p != null)
                    return p;
            }
            return null;
        }

        public IEnumerable<Structure> Load(Island island) {
            this.Island = island;
            Setup();
            Inventory?.Load();
            foreach (Structure item in Structures) {
                switch (item)
                {
                    case null:
                        Debug.LogError("Missing structure?");
                        continue;
                    case MarketStructure marketStructure:
                        MarketStructures.Add(marketStructure);
                        if (marketStructure is WarehouseStructure warehouse)
                            Warehouse = warehouse;
                        break;
                    case HomeStructure home:
                        Homes.Add(home);
                        break;
                }
                item.City = this;
                item.Load();
            }

            if (IsWilderness()) return Structures;
            for (int i = PopulationLevels.Count - 1; i >= 0; i--) {
                if (PopulationLevels[i].Exists() == false) {
                    PopulationLevels.Remove(PopulationLevels[i]);
                    continue;
                }
                PopulationLevels[i].Load(this);
            }
            PlayerController.Instance.GetPlayer(PlayerNumber).OnCityCreated(this);
            return Structures;
        }

        public void Update(float deltaTime) {
            for (int i = Structures.Count - 1; i >= 0; i--) {
                Structures[i].Update(deltaTime);
            }
            if (PlayerNumber == -1) {
                return;
            }
            if (Homes.Count > 0) {
                UpdateNeeds(deltaTime);
                CalculateIncome();
            }
            //TODO: check for better spot?
            CalculateExpanses();
        }

        public void CalculateExpanses() {
            Expanses = 0;
            for (int i = Structures.Count - 1; i >= 0; i--) {
                Expanses += Structures[i].UpkeepCost;
            }
        }

        public void CalculateIncome() {
            Income = 0;
            foreach (PopulationLevel pl in PopulationLevels) {
                Income += pl.GetTaxIncome();
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
            this.PlayerNumber = GameData.WorldNumber;
            this.Island = island;
            Structures = new List<Structure>();
            for (int i = 0; i < tiles.Count; i++) {
                temp[i].City = null;
            }
        }

        public void AddStructure(Structure str) {
            if (Structures.Contains(str)) {
                //happens on loading for loaded stuff
                //so not an actual error anymore
                //			Debug.LogError ("Adding a structure that already belongs to this city.");
                return;
            }
            switch (str)
            {
                case HomeStructure home:
                    Homes?.Add(home);
                    break;
                case MarketStructure m:
                    MarketStructures?.Add(m);
                    if (str is WarehouseStructure warehouse) {
                        if (Warehouse != null && Warehouse.BuildID != str.BuildID) {
                            Debug.LogError("There should be only one Warehouse per City! ");
                            return;
                        }
                        Warehouse = warehouse;
                    }
                    break;
            }
            Structures.Add(str);
            _cbStructureAdded?.Invoke(str);
        }

        private void UpdateNeeds(float deltaTime) {
            UseTickTimer -= deltaTime;
            if (UseTickTimer > 0) return;
            UseTickTimer = UseTick;
            foreach (PopulationLevel pop in PopulationLevels) {
                pop.FulfillNeedsAndCalcHappiness();
            }
        }

        public void TriggerAddCallBack(Structure str) {
            _cbStructureAdded?.Invoke(str);
        }

        public virtual void RemoveTile(Tile t) {
            if (Tiles.Contains(t) == false) {
                Debug.LogError("This city does not know that it had this tile! " + t.ToString() + " -> " + Tiles.Count);
                return;
            }
            Tiles.Remove(t);
            Island.AlreadyHighlighted = false;
            _cbTileRemoved?.Invoke(this, t);
            if (Tiles.Count == 0) {
                Destroy();
            }
        }

        public void AddTiles(IEnumerable<Tile> t) {
            AddTiles(new HashSet<Tile>(t));
        }

        public void AddTiles(HashSet<Tile> tiles) {
            tiles.RemoveWhere(x => x == null || x.Type == TileType.Ocean);
            foreach (Tile t in tiles) {
                AddTile(t);
            }
            foreach (Tile t in tiles) {
                AIController.UpdateCityCurrentSpaceValue(this, t);
            }
            Island.AlreadyHighlighted = false;
        }

        public void AddTile(Tile t) {
            if (t.Type == TileType.Ocean || Tiles.Contains(t)) {
                return;
            }
            t.City = this;
            if (t.Structure != null) {
                if (Structures.Contains(t.Structure) == false) {
                    t.Structure.City = this;
                    AddStructure(t.Structure);
                }
            }
            Island.AlreadyHighlighted = false;
            Tiles.Add(t);
            if (IsCurrentPlayerCity()) {
                World.Current.OnTileChanged(t);
            }
            _cbTileAdded?.Invoke(this, t);
        }

        public void AddPeople(int level, int count) {
            if (IsWilderness())
                return;
            if (GetPopulationLevel(level).PopulationCount == 0)
                TempHomeUpgradeFixFulfillNeedsAndCalcHappiness(level);
            PopulationLevels[level].AddPeople(count);
        }

        public void RemovePeople(int level, int count) {
            if (IsWilderness())
                return;
            PopulationLevels[level].RemovePeople(count);
        }

        public int GetPopulationCount(int level) {
            return PopulationLevels[level].PopulationCount;
        }

        public int GetPopulationLevel() {
            foreach (PopulationLevel level in PopulationLevels) {
                if (level.PopulationCount == 0) {
                    return level.previousLevel?.Level ?? 0;
                }
            }
            return 0;
        }

        public void RemoveItems(Item[] remove) {
            Inventory.RemoveItemsAmount(remove);
        }

        public void RemoveItem(Item item, int amount) {
            Inventory.RemoveItemAmount(item, amount);
        }

        public bool HasEnoughOfItems(IEnumerable<Item> items, int times = 1) {
            return Inventory.HasEnoughOfItems(items, times);
        }

        public bool HasEnoughOfItem(Item item) {
            return Inventory.HasEnoughOfItem(item);
        }

        public bool HasAnythingOfItem(Item item) {
            return Inventory.HasAnythingOf(item);
        }

        public bool IsWilderness() {
            if (PlayerNumber == GameData.WorldNumber && this != Island.Wilderness) {
                Debug.LogError("NOT WILDERNESS! But -1 playernumber!? ");
            }
            return this == Island.Wilderness;
        }

        /// <summary>
        /// Ship buys from city means
        /// SELLING IT from perspective of the City
        /// </summary>
        /// <param name="itemID">Item I.</param>
        /// <param name="ship">Ship.</param>
        /// <param name="amount">Amount.</param>
        public void SellingTradeItem(string itemID, Ship ship, int amount = 50) {
            if (ItemIDtoTradeItem.ContainsKey(itemID) == false) {
                return;
            }
            TradeItem ti = ItemIDtoTradeItem[itemID];
            if (ti.IsSelling == false) {
                Debug.Log("this item is not to buy");
                return;
            }
            Item i = ti.SellItemAmount(Inventory.GetItemWithMaxAmount(new Item(itemID), amount));
            int am = TradeWithShip(i, () => Mathf.Clamp(amount, 0, i.count), ship);
            GetOwner().AddToTreasure(am * ti.price);
            ship.GetOwner()?.ReduceTreasure(am * ti.price);
        }

        /// <summary>
        /// Ship sells to city.
        /// City BUYs it.
        /// </summary>
        /// <param name="itemID">Item I.</param>
        /// <param name="ship">Ship.</param>
        /// <param name="amount">Amount.</param>
        public void BuyingTradeItem(string itemID, Ship ship, int amount = 50) {
            if (ItemIDtoTradeItem.ContainsKey(itemID) == false) {
                return;
            }
            TradeItem ti = ItemIDtoTradeItem[itemID];
            if (ti.IsBuying == false) {
                Debug.Log("this item is not to sell here");
                return;
            }
            Item i = ti.BuyItemAmount(Inventory.GetItemWithMaxAmount(new Item(itemID), amount));
            int am = TradeFromShip(ship, i, Mathf.Clamp(amount, 0, i.count));
            GetOwner().ReduceTreasure(am * ti.price);
            ship.GetOwner()?.AddToTreasure(am * ti.price);
        }

        public void TradeWithAnyShip(Item item) {
            if (!(TradeUnit is Ship ship)) {
                ship = Warehouse.InRangeUnits.Find(x => x.PlayerNumber == PlayerNumber) as Ship;
            }
            if (ship == null)
                return;
            TradeWithShip(item, () => PlayerTradeAmount, ship);
        }

        public int TradeWithShip(Item toTrade, Func<int> amount, Unit ship) {
            if (Warehouse == null || Warehouse.InRangeUnits.Count == 0 || toTrade == null || ship == null) {
                return 0;
            }
            return Inventory.MoveItem(ship.Inventory, toTrade, amount());
        }

        public int TradeFromShip(Unit u, Item getTrade, int amount = 50) {
            return getTrade == null ? 0 : u.Inventory.MoveItem(Inventory, getTrade, amount);
        }

        public bool RemoveTradeItem(Item item) {
            return RemoveTradeItem(item.ID);
        }
        public bool RemoveTradeItem(string itemID) {
            return ItemIDtoTradeItem.Remove(itemID);
        }
        public void ChangeTradeItemAmount(TradeItem item) {
            ItemIDtoTradeItem[item.ItemId].count = item.count;
        }

        public void ChangeTradeItemPrice(string id, int price) {
            ItemIDtoTradeItem[id].price = price;
        }

        public int GetAmountForThis(Item item) {
            return Inventory.GetAmountFor(item);
        }

        public void AddRoute(Route route) {
            if (Routes.Contains(route) == false)
                Routes.Add(route);
        }

        public void RemoveRoute(Route route) {
            Routes.Remove(route);
        }

        public virtual void RemoveStructure(Structure structure) {
            if (structure == null) {
                Debug.Log("null");
                return;
            }
            if (Structures.Contains(structure)) {
                switch (structure) {
                    case HomeStructure homeStructure:
                        Homes.Remove(homeStructure);
                        break;
                    case WarehouseStructure _:
                        Warehouse = null;
                        break;
                    case MarketStructure m:
                        MarketStructures.Remove(m);
                        break;
                }
                Structures.Remove(structure);
                _cbStructureRemoved?.Invoke(structure);
            }
            else {
                //this is no error if this is wilderness
                if (structure is WarehouseStructure) {
                    return;
                }
                Debug.LogError(this.Name + " This structure " + structure.ToString() + " does not belong to this city ");
            }
            Island.AlreadyHighlighted = false;
        }

        public float GetHappinessForCitizenLevel(int level) {
            return PopulationLevels[level].Happiness;
        }

        public List<INeedGroup> GetPopulationNeedGroups(int level) {
            return PopulationLevels[level].AllNeedGroupList;
        }

        public void RemoveTiles(IEnumerable<Tile> tiles) {
            foreach (Tile item in tiles) {
                RemoveTile(item);
            }
        }

        public void Destroy() {
            if (PlayerNumber == -1) {
                return; // this is the wilderness it cant be removed! or destroyed
            }
            if(Tiles.Count > 0) {
                Debug.LogError("Remove Tiles first before destroying City");
            }
            Island.RemoveCity(this);
            _cbCityDestroy?.Invoke(this);
        }

        public void RegisterCityDestroy(Action<ICity> callbackfunc) {
            _cbCityDestroy += callbackfunc;
        }

        public void UnregisterCityDestroy(Action<ICity> callbackfunc) {
            _cbCityDestroy -= callbackfunc;
        }

        public void RegisterStructureAdded(Action<Structure> callbackfunc) {
            _cbStructureAdded += callbackfunc;
        }

        public void UnregisterStructureAdded(Action<Structure> callbackfunc) {
            _cbStructureAdded -= callbackfunc;
        }

        public void RegisterStructureRemove(Action<Structure> callbackfunc) {
            _cbStructureRemoved += callbackfunc;
        }

        public void UnregisterStructureRemove(Action<Structure> callbackfunc) {
            _cbStructureRemoved -= callbackfunc;
        }
        public void RegisterTileRemove(Action<ICity, Tile> callbackfunc) {
            _cbTileRemoved += callbackfunc;
        }

        public void UntegisterTileRemove(Action<ICity, Tile> callbackfunc) {
            _cbTileRemoved -= callbackfunc;
        }
        public void RegisterTileAdded(Action<ICity, Tile> callbackfunc) {
            _cbTileAdded += callbackfunc;
        }

        public void UnregisterTileAdded(Action<ICity, Tile> callbackfunc) {
            _cbTileAdded -= callbackfunc;
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
            }
            else {
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
            }
            else {
                cbEventEnded?.Invoke(ge);
            }
        }

        public bool HasFertility(Fertility fer) {
            //this is here so we could make it
            //That cities can have additional fertirilies as the island
            //for now its an easier way to get the information
            return Island.Fertilities.Contains(fer);
        }

        public override int GetPlayerNumber() {
            return PlayerNumber;
        }

        #endregion igeventable

        public bool IsCurrentPlayerCity() {
            return PlayerNumber == PlayerController.currentPlayerNumber;
        }

        public IPlayer GetOwner() {
            return PlayerController.Instance.GetPlayer(PlayerNumber);
        }

        public override string ToString() {
            return Name;
        }

        public float GetPopulationItemUsage(Item item) {
            if (PopulationCount == 0)
                return 0;
            return PopulationLevels.Sum(level => item.Data.TotalUsagePerLevel[level.Level] * level.PopulationCount);
        }

        public bool HasOwnerEnoughMoney(int buildCost) {
            return GetOwner().HasEnoughMoney(buildCost);
        }

        public void ReduceTreasureFromOwner(int buildCost) {
            GetOwner().ReduceTreasure(buildCost);
        }

        private void TempHomeUpgradeFixFulfillNeedsAndCalcHappiness(int level) {
            GetPopulationLevel(level).FulfillNeedsAndCalcHappiness();
        }

        public bool HasOwnerUnlockedAllNeeds(int populationLevel) {
            return GetOwner().HasUnlockedAllNeeds(populationLevel);
        }

        public float GetTaxPercentage(int populationLevel) {
            return GetPopulationLevel(populationLevel).taxPercentage;
        }
    }
}