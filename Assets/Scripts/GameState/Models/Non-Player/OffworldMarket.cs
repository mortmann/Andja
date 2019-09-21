using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Offworld market.
/// Only ships can trade with it
/// </summary>

[JsonObject(MemberSerialization.OptIn)]
public class OffworldMarket {
    [JsonPropertyAttribute] public Dictionary<string, Price> itemIDtoPrice;
    [JsonPropertyAttribute] float demandChangeTimer = 5f;
    float DemandChangeTime = 30f;
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
            itemIDtoPrice.Add(id, new Price(50,50)); //eg Random.Range (10,20)
            //itemIDtoPrice[id].DemandChange += Random.Range(-10, 10);
        }
        //float amount=0;
        //Price tempP = new Price(50, 50);
        //for (int i = 0; i <= 10; i++) {
        //    tempP.DemandChange += i;
        //    Debug.Log(tempP.Buy);
        //    amount += tempP.Buy;

        //}
        //Debug.Log("Buyw " + amount);

    }

    public void SellItemToOffWorldMarket(Item item, Player player) {
        if (itemIDtoPrice.ContainsKey(item.ID) == false) {
            return;
        }
        int count = item.count;
        item.count = 0;
        int money = 0;
        for(int i =1;i<=count; i++) {
            itemIDtoPrice[item.ID].DemandChange -= i;
            money += Mathf.RoundToInt(itemIDtoPrice[item.ID].Sell);
        }
        player.AddMoney(money);
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
        player.ReduceMoney(money);
        return item;
    }
    public void Update(float deltaTime){
        //update price so they go back to the equilibriums demand
        if (demandChangeTimer > 0) {
            demandChangeTimer -= deltaTime;
            return;
        }
        foreach(Price p in itemIDtoPrice.Values) {
            p.ChangeDemand();
        }
        demandChangeTimer = DemandChangeTime;
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class Price {
        //TODO: rework this for better pricing stuff
        public static float BuyMultiplier = 1.05f;
        public float ParableBuy => Mathf.Clamp(Mathf.RoundToInt( (demandA * BuyMultiplier ) * (Demand * Demand) ), 10,int.MaxValue);
        public float ParableSell => Mathf.Clamp(Mathf.RoundToInt(demandA * (Demand * Demand) ), 1, int.MaxValue);
        public int Buy => Mathf.RoundToInt(LinearBuy );/// 2  + parableBuy / 2
        public int Sell => Mathf.RoundToInt(LinearSell); // / 2 + parableSell / 2
        public float LinearBuy => Mathf.Clamp(BuyMultiplier * Demand * (demandB / normalDemand), 10, int.MaxValue);
        public float LinearSell => Mathf.Clamp(Demand * (demandB / normalDemand), 1, int.MaxValue);

        [JsonPropertyAttribute] public int DemandChange; // what was bought and sold
        public float Demand => normalDemand + DemandChange*0.1f;
        public int normalDemand;

        
        //float demandM;
        float demandA;
        readonly float demandB;
        public Price(float startPrice, int startDemand) {
            demandA = (startPrice / (startDemand * startDemand));
            demandB = startPrice;
            //demandM = startPrice / startDemand;
            normalDemand = startDemand;
        }
        public void ChangeDemand() {
            if (DemandChange > 0)
                DemandChange -= 1;
            if (DemandChange < 0)
                DemandChange += 1;
        }
    }
}
