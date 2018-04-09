using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;



[JsonObject(MemberSerialization.OptIn)]
public class TradeRoute {
	int numberOfStops { get {return trades.Count;}}
	[JsonPropertyAttribute] int currentDestination=0;
	[JsonPropertyAttribute] List<Trade> trades;

	public bool isStarted=false;
	public bool Valid{
		get {
			return trades.Count > 1;
		}
	}
	public TradeRoute(){
		trades = new List<Trade> ();
	}
	public TradeRoute(TradeRoute tr){
		this.trades = tr.trades;
		currentDestination = tr.currentDestination;
		isStarted = tr.isStarted;
	}
	public void AddWarehouse(Warehouse w){
		Trade t = new Trade (w.City,null,null);
		trades.Add (t);
	}
	public void SetCityTrade(City city,Item[] getting,Item[] giving){
		Trade t = trades.Find (x => x.city == city);
		if(t!=null){
			t.getting = getting;
			t.giving = giving;
		} else {
			Debug.LogError ("Wat de f SetCityTrade"); 
		}
	}
	public Trade GetCurrentCityTrade(){
		return trades [currentDestination];
	}
	public void RemoveWarehouse(Warehouse w){
		Trade t = GetTradeFor(w.City);
		if(t==null){
			Debug.LogError ("Tried to remove a city that wasnt in here!");
			return; // not in error from somewhere
		}
		if(trades.IndexOf(t)<currentDestination){
			currentDestination--; // smaller then we must remove to be on the same still
		} else 
		if(trades.IndexOf(t)==currentDestination){
			//if its behind the otherone so decrease the destination pointer
			currentDestination--;
			currentDestination = Mathf.Clamp (currentDestination,0,numberOfStops-1);
		}
		trades.Remove (t);

	}

	public int GetLastNumber(){
		return numberOfStops;
	}
	public int GetNumberFor(Warehouse w){
		for (int i = 0; i < trades.Count; i++) {
			if(trades[i].city==w.City){
				return i + 1;
			} 
		}
		return -1;
	}
	public Tile getCurrentDestination(){
		if(trades.Count==0){
			return null;
		}
		if(trades [currentDestination].city.myWarehouse==null){
			return null;
		}
		return trades [currentDestination].city.myWarehouse.getTradeTile ();
	}
	public Tile getNextDestination(){
		//if theres only one destination
		//that means there is no realtraderoute in place
		//so just return
		if(trades.Count<=1){
			return null;
		}

		for (int i = 0; i < numberOfStops; i++) {
			increaseDestination ();
			if(trades [currentDestination].city.myWarehouse!=null){
				isStarted = true;
				return trades [currentDestination].city.myWarehouse.getTradeTile ();
			}
		}
		return null;
	}
	public void increaseDestination(){
		if(isStarted)
			currentDestination = (currentDestination + 1) % trades.Count;
	}
	public bool Contains(City c){
		return GetTradeFor (c) != null;
	}

	public Trade GetNextTrade(City curr , bool r){
		if(Contains (curr)==false){
			Debug.LogError ("GetTradeCurr-currentcity isnt in cities"); 
			return null;
		}
		return trades [currentDestination];
	}
	public Trade GetTradeFor(City c){
		if(Contains (c)==false){
			return null;
		}
		return  trades.Find (x => x.city == c);
	}

	public void doCurrentTrade(Unit u){
		Trade t = GetCurrentCityTrade ();
		City c = t.city;
		Inventory inv=u.inventory;
		//only get some if its needed
		foreach (Item item in t.getting) {
			int needed = item.count - inv.GetTotalAmountFor (item);
			if(needed==0){
				continue;
			}
			c.TradeWithShip (item,needed,u);
		}
		//give as much as possible but max the choosen one
		foreach (Item item in t.giving) {
			c.TradeFromShip (u,item,item.count);
		}

	}

	[JsonObject(MemberSerialization.OptIn)]
//	[JsonConverter(typeof(TradeSerializer))]
	[System.Serializable]
	public class Trade {
		[JsonPropertyAttribute] public City city;
		[JsonPropertyAttribute] public Item[] getting;
		[JsonPropertyAttribute] public Item[] giving;
		public Trade(City c,Item[] getting,Item[] giving){
			city = c;
			this.getting = getting;
			this.giving = giving;
			//		getting = new List<Item> ();
			//		giving = new List<Item> ();
		}
		public Trade(Trade t){
			city = t.city;
			getting = t.getting;
			giving = t.giving;
		}
		public Trade(){
		}
	}

//	public class TradeSerializer : JsonSerializer{
//		override 
////		internal override void SerializeInternal (JsonWriter jsonWriter, object value, System.Type objectType){
////			var trade = value as Trade;
////			jsonWriter.WriteStartObject ();
////			Serialize (jsonWriter,trade.city.myTiles.GetEnumerator().Current);
////			Serialize (jsonWriter,trade.getting);
////			Serialize (jsonWriter,trade.giving);
////			jsonWriter.WriteEndObject ();
////		}
//	}


}
