using UnityEngine;
using System.Collections;

public class TradeItem {
	public int ItemId;
	public int count;
	public int price;
	public bool selling;
	public TradeItem(int ItemId, int count, int price, bool selling){
		this.ItemId = ItemId;
		this.count = count;
		this.price = price;
		this.selling = selling;
	}

	public Item SellItemAmount(Item inINV){
		if(selling==false){
			Debug.Log ("Wrong function call - This item is not to sell here");
			return null;
		}
		//SELLING ONLY works IF
		//The item amount IN inventory is SMALLER(!) 
		//than the count in tradeitem
		Item i = inINV.CloneWithCount ();
		//		  WANTS    - HAS = CAN SELL HERE 
		i.count = i.count - count;
		return i;
	}
	public Item BuyItemAmount(Item inINV){
		if(selling==true){
			Debug.Log ("Wrong function call - This item is not to buy here");
			return null;
		}	
		//BUYING ONLY works IF
		//The item amount IN inventory is BIGGER 
		//than the count in tradeitem
		Item i = inINV.CloneWithCount ();
		//ti.count = 25
		//i.count = 30
		// most selling is 5
		//		  HAS     - REMAINING = you can buy here 
		i.count = count - i.count;
		Debug.Log (count + "-" + i.count); 

		return i;
	}
}
