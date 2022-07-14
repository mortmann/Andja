using System;
using System.Collections.Generic;

namespace Andja.Model {
    public interface ICity {
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
        IReadOnlyList<Effect> Effects { get; }
        TargetGroup TargetGroups { get; }
        bool HasNegativEffect { get; }
        bool AutoUpgradeHomes { get; set; }
        int PlayerNumber { get; }
        CityInventory Inventory { get; }
        List<Structure> Structures { get; }
        Dictionary<string, TradeItem> itemIDtoTradeItem { get; }
        Island Island { get; }
        int PlayerTradeAmount { get; }
        void SetPlayerTradeAmount(int amount);
        void SetName(string name);
        void SetTaxForPopulationLevel(int structureLevel, float percantage);
        bool AddTradeItem(TradeItem ti);
        void DeleteTradeItem(TradeItem ti);
        bool HasAnythingOfItems(Item[] buildingItems);
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

        /// <summary>
        /// Ship buys from city means
        /// SELLING IT from perspectiv City
        /// </summary>
        /// <param name="itemID">Item I.</param>
        /// <param name="unitPlayer">Player.</param>
        /// <param name="ship">Ship.</param>
        /// <param name="amount">Amount.</param>
        void SellingTradeItem(string itemID, Player unitPlayer, Ship ship, int amount = 50);

        /// <summary>
        /// Ship sells to city.
        /// City BUYs it.
        /// </summary>
        /// <param name="itemID">Item I.</param>
        /// <param name="player">Player.</param>
        /// <param name="ship">Ship.</param>
        /// <param name="amount">Amount.</param>
        void BuyingTradeItem(string itemID, Player player, Ship ship, int amount = 50);

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
        List<NeedGroup> GetPopulationNeedGroups(int level);
        void RemoveTiles(IEnumerable<Tile> tiles);
        void Destroy();
        void RegisterCityDestroy(Action<ICity> callbackfunc);
        void UnregisterCityDestroy(Action<ICity> callbackfunc);
        void RegisterStructureAdded(Action<Structure> callbackfunc);
        void UnregisterStructureAdded(Action<Structure> callbackfunc);
        void RegisterStructureRemove(Action<Structure> callbackfunc);
        void UnregisterStructureRemove(Action<Structure> callbackfunc);
        void RegisterTileRemove(Action<ICity, Tile> callbackfunc);
        void UntegisterTileRemove(Action<ICity, Tile> callbackfunc);
        void RegisterTileAdded(Action<ICity, Tile> callbackfunc);
        void UnregisterTileAdded(Action<ICity, Tile> callbackfunc);
        void OnEventCreate(GameEvent ge);
        void OnEventEnded(GameEvent ge);
        bool HasFertility(Fertility fer);
        int GetPlayerNumber();
        bool IsCurrPlayerCity();
        Player GetOwner();
        string ToString();
        float GetPopulationItemUsage(Item item);
        string GetID();
        void RegisterOnEvent(Action<GameEvent> create, Action<GameEvent> ending);
        void UpdateEffects(float deltaTime);
        void AddEffects(Effect[] effects);
        bool AddEffect(Effect effect);
        bool HasEffect(string effectID);
        bool HasEffect(Effect effect);
        bool HasAnyEffect(params Effect[] effects);
        bool RemoveEffect(Effect effect, bool all = false);
        void RegisterOnEffectChangedCallback(Action<IGEventable, Effect, bool> cb);
        void UnregisterOnEffectChangedCallback(Action<IGEventable, Effect, bool> cb);
    }
}