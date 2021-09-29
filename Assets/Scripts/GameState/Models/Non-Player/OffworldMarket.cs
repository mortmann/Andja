using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {
    /// <summary>
    /// Offworld market.
    /// Only ships can trade with it
    /// </summary>

    [JsonObject(MemberSerialization.OptIn)]
    public class OffworldMarket {
        [JsonPropertyAttribute] public Dictionary<string, Price> itemIDtoPrice;
        [JsonPropertyAttribute] private float demandChangeTimer = 5f;
        public OffworldMarket() {
            //Read the prices for selling/buying from a seperate file in savegame
            //are these prices randomly generated? if so were do we get the lower
            //and upper bounds for it do we sell/buy limitless or is there a cooldown
            //on how much per timeunit? is it the same for all the items or are they gonna
            //differ in some way

            itemIDtoPrice = new Dictionary<string, Price>();
            //_____TEMPORARY?_____________
            //get all the diffrent
            Dictionary<string, Item> temp = BuildController.Instance.GetCopieOfAllItems();
            foreach (string id in temp.Keys) {
                itemIDtoPrice.Add(id, new Price(50, 50)); //eg Random.Range (10,20)
                                                          //itemIDtoPrice[id].DemandChange += Random.Range(-10, 10);
            }
            demandChangeTimer = GameData.DemandChangeTime;
            
            //if(Application.isEditor) {
            //    Price tempP = new Price(50, 50);
            //    for (int i = 0; i <= 50; i++) {
            //        tempP.DemandChange++;
            //        Debug.Log("Parable " + i + ": " + tempP.CubicBuy + " " + tempP.CubicSell);
            //    }
            //    Price tempa = new Price(50, 50);
            //    for (int i = 0; i <= 50; i++) {
            //        tempa.DemandChange--;
            //    }
            //}
        }

        public void SellItemToOffWorldMarket(Item item, Player player) {
            if (itemIDtoPrice.ContainsKey(item.ID) == false) {
                return;
            }
            int count = item.count;
            item.count = 0;
            int money = 0;
            for (int i = 1; i <= count; i++) {
                itemIDtoPrice[item.ID].DemandChange -= i;
                money += Mathf.RoundToInt(itemIDtoPrice[item.ID].Sell);
            }
            player.AddToTreasure(money);
        }

        public Item BuyItemToOffWorldMarket(Item item, int amount, Player player) {
            if (itemIDtoPrice.ContainsKey(item.ID) == false) {
                return null;
            }
            item.count = amount;
            int money = 0;
            for (int i = 1; i <= amount; i++) {
                itemIDtoPrice[item.ID].DemandChange += i;
                money += Mathf.RoundToInt(itemIDtoPrice[item.ID].Buy);
            }
            player.ReduceTreasure(money);
            return item;
        }

        internal int GetSellPrice(string item_id) {
            return itemIDtoPrice[item_id].Sell;
        }

        internal int GetBuyPrice(string item_id) {
            return itemIDtoPrice[item_id].Buy;
        }

        public void Update(float deltaTime) {
            //update price so they go back to the equilibriums demand
            if (demandChangeTimer > 0) {
                demandChangeTimer -= deltaTime;
                return;
            }
            foreach (Price p in itemIDtoPrice.Values) {
                p.ChangeDemand();
            }
            demandChangeTimer = GameData.DemandChangeTime;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Price {

            //TODO: rework this for better pricing stuff
            public static float BuyMultiplier = 1.05f;

            public int CubicBuy {
                get {
                    float x = Demand;
                    return Mathf.RoundToInt(-Mathf.Pow(x * 0.05f, 3) - 0.25f * x + StartPrice * 1.1f);
                }
            }
            public int CubicSell {
                get {
                    float x = Demand;
                    return Mathf.RoundToInt(-Mathf.Pow(x * 0.05f, 3) - 0.25f * x + StartPrice);
                }
            }

            public int Buy => Mathf.RoundToInt(CubicBuy); // 2  + parableBuy / 2
            public int Sell => Mathf.RoundToInt(CubicSell); // 2 + parableSell / 2
            public float LinearBuy => Mathf.Clamp(BuyMultiplier * Demand * (demandB / normalDemand), 10, int.MaxValue);
            public float LinearSell => Mathf.Clamp(Demand * (demandB / normalDemand), 1, int.MaxValue);
            public float Demand => normalDemand + DemandChange;

            [JsonPropertyAttribute] public int DemandChange; // what was bought and sold

            public int StartPrice = 50;
            //this is gonna be read in 
            public int normalDemand = 0;
            private float demandA = 0.02f;
            private readonly float demandB = 50;

            public Price(float startPrice, int startDemand) {
                demandA = (startPrice / (startDemand * startDemand));
                demandB = startPrice;
                //demandM = startPrice / startDemand;
                //normalDemand = startDemand;
            }
            public Price() {
                demandA = 0.02f;
                demandB = 50;
                normalDemand = 50;
            }

            public void ChangeDemand() {
                if (DemandChange > 0)
                    DemandChange -= 1;
                if (DemandChange < 0)
                    DemandChange += 1;
            }
        }
    }
}