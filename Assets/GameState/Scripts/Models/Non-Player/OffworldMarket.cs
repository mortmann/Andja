using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Offworld market.
/// Only ships can trade with it
/// </summary>

[JsonObject(MemberSerialization.OptIn)]
public class OffworldMarket {
	[JsonPropertyAttribute] public Dictionary<int,int> itemIDtoSellPrice;
	[JsonPropertyAttribute] public Dictionary<int,int> itemIDtoBuyPrice;

	// Use this for initialization
	public OffworldMarket (bool n=true) {
		//Read the prices for selling/buying from a seperate file in savegame
		//are these prices randomly generated? if so were do we get the lower 
		//and upper bounds for it do we sell/buy limitless or is there a cooldown 
		//on how much per timeunit? is it the same for all the items or are they gonna
		//differ in some way

		//ID to SELL Prices dictionary 
		itemIDtoSellPrice = new Dictionary<int, int> ();
		//ID to BUY Prices dictionary 
		itemIDtoBuyPrice = new Dictionary<int, int> ();
		//_____TEMPORARY?_____________
		//get all the diffrent
		Dictionary<int,Item> temp = BuildController.Instance.getCopieOfAllItems ();
		foreach (int id in temp.Keys) {
			//temporary everything cost 10
			itemIDtoSellPrice.Add (id,10); //eg Random.Range (10,20)
			itemIDtoBuyPrice.Add (id,10); //eg Random.Range (10,20)
		}
	}
	public OffworldMarket(){
	}

	public void SellItemToOffWorldMarket(Item item, Player player){
		if(itemIDtoSellPrice.ContainsKey (item.ID )== false){
			return; 
		}
		int count = item.count;
		item.count = 0;
		player.AddMoney (Mathf.RoundToInt (count * itemIDtoSellPrice [item.ID]));
	}
	public Item BuyItemToOffWorldMarket(Item item, int amount, Player player){
		if(itemIDtoSellPrice.ContainsKey (item.ID )== false){
			return null; 
		}
		item.count = amount;
		player.ReduceMoney (Mathf.RoundToInt (amount * itemIDtoSellPrice [item.ID]));
		return item;
	}
}
