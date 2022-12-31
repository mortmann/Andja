using System;
using System.Collections.Generic;

namespace Andja.Model {
    public interface ICity : IGEventable {
        string Name { get; set; }
        HashSet<Tile> Tiles { get; set; }
        int TradeItemCount { get; }
        int PopulationCount { get; }
        List<MarketStructure> MarketStructures { get; set; }
        List<Route> Routes { get; set; }
        Unit TradeUnit { get; set; }
        int Expanses { get; }
        int Income { get; }
        int Balance { get; }
        WarehouseStructure Warehouse { get; set; }
        bool AutoUpgradeHomes { get; set; }
        int PlayerNumber { get; }
        CityInventory Inventory { get; }
        List<Structure> Structures { get; }
        Dictionary<string, TradeItem> ItemIDtoTradeItem { get; }
        IIsland Island { get; }
        int PlayerTradeAmount { get; }
        void SetPlayerTradeAmount(int amount);
        void SetName(string name);
        void SetTaxForPopulationLevel(int structureLevel, float percentage);
        bool AddTradeItem(TradeItem ti);
        void DeleteTradeItem(TradeItem ti);
        PopulationLevel GetPopulationLevel(int structureLevel);
        int GetPopulationLevel();
        PopulationLevel GetPreviousPopulationLevel(int level);
        IEnumerable<Structure> Load(Island island);
        void Update(float deltaTime);
        void CalculateExpanses();
        void CalculateIncome();
        void AddStructure(Structure str);
        void TriggerAddCallBack(Structure str);
        void RemoveTile(Tile t);
        void AddTiles(IEnumerable<Tile> t);
        void AddTiles(HashSet<Tile> tiles);
        void AddTile(Tile t);
        void AddPeople(int level, int count);
        void RemovePeople(int level, int count);
        int GetPopulationCount(int level);
        void RemoveItems(Item[] remove);
        void RemoveItem(Item item, int amount);
        bool HasEnoughOfItems(IEnumerable<Item> items, int times = 1);
        bool HasEnoughOfItem(Item item);
        bool HasAnythingOfItem(Item item);
        bool IsWilderness();
        bool HasOwnerUnlockedAllNeeds(int populationLevel);

        /// <summary>
        /// Ship buys from city means
        /// SELLING IT from perspectiv City
        /// </summary>
        /// <param name="itemID">Item I.</param>
        /// <param name="ship">Ship.</param>
        /// <param name="amount">Amount.</param>
        void SellingTradeItem(string itemID, Ship ship, int amount = 50);

        /// <summary>
        /// Ship sells to city.
        /// City BUYs it.
        /// </summary>
        /// <param name="itemID">Item I.</param>
        /// <param name="ship">Ship.</param>
        /// <param name="amount">Amount.</param>
        void BuyingTradeItem(string itemID, Ship ship, int amount = 50);

        void TradeWithAnyShip(Item item);
        int TradeWithShip(Item toTrade, Func<int> amount, Unit ship);
        int TradeFromShip(Unit u, Item getTrade, int amount = 50);
        bool RemoveTradeItem(Item item);
        bool RemoveTradeItem(string itemID);
        void ChangeTradeItemAmount(Item item);
        void ChangeTradeItemPrice(string id, int price);
        int GetAmountForThis(Item item);
        void AddRoute(Route route);
        void RemoveRoute(Route route);
        void RemoveStructure(Structure structure);
        float GetHappinessForCitizenLevel(int level);
        List<INeedGroup> GetPopulationNeedGroups(int level);
        void RemoveTiles(IEnumerable<Tile> tiles);
        void Destroy();
        void RegisterCityDestroy(Action<ICity> callbackfunc);
        void UnregisterCityDestroy(Action<ICity> callbackfunc);
        void RegisterStructureAdded(Action<Structure> callbackfunc);
        void UnregisterStructureAdded(Action<Structure> callbackfunc);
        void RegisterStructureRemove(Action<Structure> callbackfunc);
        float GetTaxPercentage(int populationLevel);
        void UnregisterStructureRemove(Action<Structure> callbackfunc);
        void RegisterTileRemove(Action<ICity, Tile> callbackfunc);
        void UntegisterTileRemove(Action<ICity, Tile> callbackfunc);
        void RegisterTileAdded(Action<ICity, Tile> callbackfunc);
        bool HasFertility(Fertility fer);
        bool IsCurrentPlayerCity();
        Player GetOwner();
        string ToString();
        float GetPopulationItemUsage(Item item);
        bool HasOwnerEnoughMoney(int nextLevelBuildCost);
        void ReduceTreasureFromOwner(int nextLevelBuildCost);
    }
}