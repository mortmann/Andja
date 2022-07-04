using System;
using System.Collections.Generic;

namespace Andja.Model {
    public interface ICity {
        HashSet<Tile> Tiles { get; set; }
        int TradeItemCount { get; }

        void AddPeople(int level, int count);
        void AddRoute(Route route);
        void AddStructure(Structure str);
        void AddTile(Tile t);
        void AddTiles(HashSet<Tile> tiles);
        void AddTiles(IEnumerable<Tile> t);
        void BuyingTradeItem(string itemID, Player player, Ship ship, int amount = 50);
        void ChangeTradeItemAmount(Item item);
        void ChangeTradeItemPrice(string id, int price);
        void Destroy();
        int GetAmountForThis(Item item);
        float GetHappinessForCitizenLevel(int level);
        Player GetOwner();
        int GetPlayerNumber();
        int GetPopulationCount(int level);
        int GetPopulationLevel();
        bool HasAnythingOfItem(Item item);
        bool HasEnoughOfItem(Item item);
        bool HasEnoughOfItems(IEnumerable<Item> items, int times = 1);
        bool HasFertility(Fertility fer);
        bool IsCurrPlayerCity();
        bool IsWilderness();
        void RegisterTileAdded(Action<City, Tile> callbackfunc);
        void RegisterTileRemove(Action<City, Tile> callbackfunc);
        void RemovePeople(int level, int count);
        void RemoveItem(Item item, int amount);
        void RemoveItems(Item[] remove);
        void RemoveRoute(Route route);
        void RemoveStructure(Structure structure);
        void RemoveTile(Tile t);
        void RemoveTiles(IEnumerable<Tile> tiles);
        bool RemoveTradeItem(Item item);
        bool RemoveTradeItem(string itemID);
        void SellingTradeItem(string itemID, Player unitPlayer, Ship ship, int amount = 50);
        int TradeFromShip(Unit u, Item getTrade, int amount = 50);
        void TradeWithAnyShip(Item item);
        int TradeWithShip(Item toTrade, Func<int> amount, Unit ship);
    }
}