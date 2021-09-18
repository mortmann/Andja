using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {

    public enum Trade { Buy, Sell }

    [JsonObject(MemberSerialization.OptIn)]
    public class TradeItem {
        [JsonPropertyAttribute] public string ItemId;
        [JsonPropertyAttribute] public int count;
        [JsonPropertyAttribute] public int price;
        [JsonPropertyAttribute] public Trade trade;

        public bool IsSelling {
            set {
                if (value)
                    trade = Trade.Sell;
                else
                    IsBuying = true;
            }
            get {
                return trade == Trade.Sell;
            }
        }

        public bool IsBuying {
            set {
                if (value)
                    trade = Trade.Buy;
                else
                    IsSelling = true;
            }
            get {
                return trade == Trade.Buy;
            }
        }

        public TradeItem(string ItemId, int count, int price, Trade trade) {
            this.ItemId = ItemId;
            this.count = count;
            this.price = price;
            this.trade = trade; // will set it correctly
        }

        /// <summary>
        /// DO NOT USE IT
        /// </summary>
        public TradeItem() { }

        public Item SellItemAmount(Item inINV) {
            if (IsSelling == false) {
                Debug.Log("Wrong function call - This item is not to sell here");
                return null;
            }
            //SELLING ONLY works IF
            //The item amount IN inventory is SMALLER(!)
            //than the count in tradeitem
            Item i = inINV.CloneWithCount();
            //		  WANTS    - HAS = CAN SELL HERE
            i.count = i.count - count;
            return i;
        }

        public Item BuyItemAmount(Item inINV) {
            if (IsBuying == false) {
                Debug.Log("Wrong function call - This item is not to buy here");
                return null;
            }
            //BUYING ONLY works IF
            //The item amount IN inventory is BIGGER
            //than the count in tradeitem
            Item i = inINV.CloneWithCount();
            //ti.count = 25
            //i.count = 30
            // most selling is 5
            //		  HAS     - REMAINING = you can buy here
            i.count = count - i.count;
            return i;
        }
    }
}