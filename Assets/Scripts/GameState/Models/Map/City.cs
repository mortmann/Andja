using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class City : IGEventable {

        #region Serialize

        [JsonPropertyAttribute] public bool AutoUpgradeHomes;
        [JsonPropertyAttribute] public int PlayerNumber = 0;
        [JsonPropertyAttribute] public Inventory Inventory;
        [JsonPropertyAttribute] public List<Structure> Structures;
        [JsonPropertyAttribute] public float useTickTimer;
        [JsonPropertyAttribute] public Dictionary<string, TradeItem> itemIDtoTradeItem;
        [JsonPropertyAttribute] private string _Name = "";
        [JsonPropertyAttribute] public Island Island;
        [JsonPropertyAttribute] private List<PopulationLevel> PopulationLevels;
        [JsonPropertyAttribute] public int PlayerTradeAmount = 50;
        #endregion Serialize

        #region RuntimeOrOther

        public string Name {
            get {
                if (this.IsWilderness()) {
                    return "Wilderness";
                }
                if (_Name.Length == 0) {
                    return "City " + Island.Cities.IndexOf(this);
                }
                return _Name;
            }
            set {
                _Name = value;
            }
        }

        internal void SetPlayerTradeAmount(int amount) {
            PlayerTradeAmount = amount;
        }
        internal void SetName(string name) {
            Name = name;
        }

        public HashSet<Tile> Tiles {
            get {
                return _Tiles;
            }

            set {
                _Tiles = value;
            }
        }

        public int TradeItemCount {
            get {
                if (warehouse == null)
                    return 0;
                return warehouse.TradeItemCount;
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

        public List<HomeStructure> homes;
        private HashSet<Tile> _Tiles;
        public List<Route> Routes;
        public Unit tradeUnit;

        public int expanses = 0;
        public int income = 0;
        public int Balance => income - expanses;
        public static float useTick => 60f;
        public WarehouseStructure warehouse;

        private Action<Structure> cbStructureAdded;
        private Action<Structure> cbStructureRemoved;
        private Action<City> cbCityDestroy;
        private Action<Structure> cbRegisterTradeOffer;

        #endregion RuntimeOrOther
        /// <summary>
        /// DO NOT USE! ONLY serialization!
        /// </summary>
        public City() {
            Structures = new List<Structure>();
            Tiles = new HashSet<Tile>();
            Routes = new List<Route>();
        }
        public City(int playerNr, Island island) : this() {
            this.PlayerNumber = playerNr;
            this.Island = island;
            _Name = "<City> " + UnityEngine.Random.Range(0, 1000);
            itemIDtoTradeItem = new Dictionary<string, TradeItem>();
            Structures = new List<Structure>();
            Tiles = new HashSet<Tile>();
            Routes = new List<Route>();
            Inventory = new CityInventory();
            Setup();
        }

        internal void SetTaxForPopulationLevel(int structureLevel, float percantage) {
            if (IsWilderness())
                return;
            PopulationLevels[structureLevel].SetTaxPercantage(percantage);
        }

        internal void AddTradeItem(TradeItem ti) {
            if (itemIDtoTradeItem.ContainsKey(ti.ItemId)) {
                Debug.LogError("Tried to add Trade Item that exists");
                return;
            }
            itemIDtoTradeItem.Add(ti.ItemId, ti);
        }

        internal void DeleteTradeItem(TradeItem ti) {
            if (itemIDtoTradeItem.ContainsKey(ti.ItemId) == false) {
                Debug.LogError("Tried to remove Trade Item that doesnt exist");
                return;
            }
            itemIDtoTradeItem.Remove(ti.ItemId);
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

        private void Setup() {
            if (PlayerNumber < 0)
                return;
            Routes = new List<Route>();
            homes = new List<HomeStructure>();
            if (PopulationLevels == null)
                PopulationLevels = new List<PopulationLevel>();
            foreach (PopulationLevel pl in PrototypController.Instance.GetPopulationLevels(this)) {
                if (PopulationLevels.Exists(x => x.Level == pl.Level))
                    continue;
                if (pl.previousLevel != null)
                    pl.previousLevel = PopulationLevels[pl.previousLevel.Level]; // so when adding new levels to existing links get updated
                PopulationLevels.Add(pl);
            }
            Island.RegisterOnEvent(OnEventCreate, OnEventEnded);
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
            this.Island = island;
            Setup();
            foreach (Structure item in Structures) {
                if(item == null) {
                    Debug.LogError("Missing structure?");
                    continue;
                }
                if (item is WarehouseStructure) {
                    warehouse = (WarehouseStructure)item;
                }
                else
                if (item is HomeStructure) {
                    homes.Add((HomeStructure)item);
                }
                item.City = this;
                item.Load();
            }
            if (IsWilderness() == false) {
                for (int i = PopulationLevels.Count - 1; i >= 0; i--) {
                    if (PopulationLevels[i].Exists() == false) {
                        PopulationLevels.Remove(PopulationLevels[i]);
                        continue;
                    }
                    PopulationLevels[i].Load(this);
                }
                PlayerController.GetPlayer(PlayerNumber).OnCityCreated(this);
            }
            return Structures;
        }

        internal void Update(float deltaTime) {
            for (int i = Structures.Count - 1; i >= 0; i--) {
                Structures[i].Update(deltaTime);
            }
            if (PlayerNumber == -1 || homes.Count == 0) {
                return;
            }
            UpdateNeeds(deltaTime);
            //TODO: check for better spot?
            CalculateExpanses();
            CalculateIncome();
        }

        public void CalculateExpanses() {
            expanses = 0;
            for (int i = Structures.Count - 1; i >= 0; i--) {
                expanses += Structures[i].UpkeepCost;
            }
        }

        public void CalculateIncome() {
            income = 0;
            foreach (PopulationLevel pl in PopulationLevels) {
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
            if (str is HomeStructure) {
                homes?.Add((HomeStructure)str);
            }
            if (str is WarehouseStructure) {
                if (warehouse != null && warehouse.buildID != str.buildID) {
                    Debug.LogError("There should be only one Warehouse per City! ");
                    return;
                }
                warehouse = (WarehouseStructure)str;
            }
            Structures.Add(str);
            cbStructureAdded?.Invoke(str);
        }

        private void UpdateNeeds(float deltaTime) {
            useTickTimer -= deltaTime;
            if (useTickTimer <= 0) {
                useTickTimer = useTick;
                foreach (PopulationLevel pop in PopulationLevels) {
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
            Island.allReadyHighlighted = false;
            if (Tiles.Count == 0) {
                Destroy();
            }
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
            Island.allReadyHighlighted = false;
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
            Island.allReadyHighlighted = false;
            Tiles.Add(t);
            if (IsCurrPlayerCity()) {
                World.Current.OnTileChanged(t);
            }
        }

        public bool GetNeedCriticalForLevel(int structureLevel) {
            return PopulationLevels[structureLevel].criticalMissingNeed;
        }

        public void AddPeople(int level, int count) {
            if (IsWilderness())
                return;
            if (count < 0) {
                return;
            }
            PopulationLevels[level].AddPeople(count);
        }

        public void RemovePeople(int level, int count) {
            if (IsWilderness())
                return;
            if (count < 0) {
                return;
            }
            PopulationLevels[level].RemovePeople(count);
        }

        public int GetPopulationCount(int level) {
            return PopulationLevels[level].populationCount;
        }

        public int GetPopulationLevel() {
            foreach (PopulationLevel level in PopulationLevels) {
                if (level.populationCount == 0) {
                    return level.previousLevel?.Level ?? 0;
                }
            }
            return 0;
        }

        public void RemoveResources(Item[] remove) {
            if (remove == null) {
                return;
            }
            foreach (Item item in remove) {
                Inventory.RemoveItemAmount(item);
            }
        }

        public void RemoveResource(Item item, int amount) {
            if (amount < 0) {
                return;
            }
            Item i = item.Clone();
            i.count = amount;
            Inventory.RemoveItemAmount(i);
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
        /// SELLING IT from perspectiv City
        /// </summary>
        /// <param name="itemID">Item I.</param>
        /// <param name="unitPlayer">Player.</param>
        /// <param name="ship">Ship.</param>
        /// <param name="amount">Amount.</param>
        public void SellingTradeItem(string itemID, Player unitPlayer, Ship ship, int amount = 50) {
            if (itemIDtoTradeItem.ContainsKey(itemID) == false) {
                return;
            }
            TradeItem ti = itemIDtoTradeItem[itemID];
            if (ti.IsSelling == false) {
                Debug.Log("this item is not to buy");
                return;
            }
            Item i = ti.SellItemAmount(Inventory.GetItemClone(itemID));
            Player CityPlayer = PlayerController.GetPlayer(PlayerNumber);
            int am = TradeWithShip(i, ()=>Mathf.Clamp(amount, 0, i.count), ship);
            CityPlayer.AddToTreasure(am * ti.price);
            unitPlayer?.ReduceTreasure(am * ti.price);
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
            Item i = ti.BuyItemAmount(Inventory.GetItemClone(itemID));
            Player Player = PlayerController.GetPlayer(PlayerNumber);
            int am = TradeFromShip(ship, i, Mathf.Clamp(amount, 0, i.count));
            Player.ReduceTreasure(am * ti.price);
            player?.AddToTreasure(am * ti.price);
        }

        public void TradeWithAnyShip (Item item) {
            Ship ship = tradeUnit as Ship;
            if (ship == null) {
                ship = warehouse.inRangeUnits.Find(x => x.playerNumber == PlayerNumber) as Ship;
            }
            if (ship == null)
                return;
            TradeWithShip(item, () => PlayerTradeAmount, ship);
        }

        public int TradeWithShip(Item toTrade, Func<int> amount, Unit ship) {
            if (warehouse == null || warehouse.inRangeUnits.Count == 0 || toTrade == null || ship == null) {
                return 0;
            }
            return Inventory.MoveItem(ship.inventory, toTrade, amount());
        }

        public int TradeFromShip(Unit u, Item getTrade, int amount = 50) {
            if (getTrade == null) {
                return 0;
            }
            return u.inventory.MoveItem(Inventory, getTrade, amount);
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

        public int GetAmountForThis(Item item) {
            return Inventory.GetAmountForItem(item);
        }

        public void AddRoute(Route route) {
            if (Routes.Contains(route) == false)
                Routes.Add(route);
        }

        public void RemoveRoute(Route route) {
            Routes.Remove(route);
        }

        public void RemoveStructure(Structure structure) {
            if (structure == null) {
                Debug.Log("null");
                return;
            }
            if (Structures.Contains(structure)) {
                if (structure is HomeStructure) {
                    homes.Remove((HomeStructure)structure);
                }
                else
                if (structure is WarehouseStructure) {
                    warehouse = null;
                }
                Structures.Remove(structure);
                cbStructureRemoved?.Invoke(structure);
            }
            else {
                //this is no error if this is wilderness
                if (structure is WarehouseStructure) {
                    return;
                }
                Debug.LogError(this.Name + " This structure " + structure.ToString() + " does not belong to this city ");
            }
            Island.allReadyHighlighted = false;
        }

        public float GetHappinessForCitizenLevel(int level) {
            return PopulationLevels[level].Happiness;
        }

        internal List<NeedGroup> GetPopulationNeedGroups(int level) {
            return PopulationLevels[level].NeedGroupList;
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
            if (PlayerNumber == -1) {
                return; // this is the wilderness it cant be removed! or destroyed
            }
            if (Tiles.Count > 0) {
                RemoveTiles(Tiles);
            }
            Island.RemoveCity(this);
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

        public bool IsCurrPlayerCity() {
            return PlayerNumber == PlayerController.currentPlayerNumber;
        }

        public Player GetOwner() {
            return PlayerController.GetPlayer(PlayerNumber);
        }

        public override string ToString() {
            return Name;
        }
    }
}