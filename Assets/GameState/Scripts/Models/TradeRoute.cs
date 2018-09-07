using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;



[JsonObject(MemberSerialization.OptIn)]
public class TradeRoute {
	int NumberOfStops { get {return Trades.Count;}}
	[JsonPropertyAttribute] int currentDestination=0;
	[JsonPropertyAttribute] public List<Trade> Trades { get; protected set; }

	public bool isStarted=false;
	public bool Valid{
		get {
			return Trades.Count > 1;
		}
	}
	public TradeRoute(){
		Trades = new List<Trade> ();
	}
   
    public TradeRoute(TradeRoute tr){
		this.Trades = tr.Trades;
	}
	public void AddWarehouse(Warehouse w){
		Trade t = new Trade (w.City,null,null);
		Trades.Add (t);
	}
	public void SetCityTrade(City city,Item[] getting,Item[] giving){
		Trade t = Trades.Find (x => x.city == city);
		if(t!=null){
			t.getting = getting;
			t.giving = giving;
		}
  //      else {
		//	Debug.LogError ("Wat de f SetCityTrade"); 
		//}
	}
	public Trade GetCurrentCityTrade(){
		return Trades [currentDestination];
	}
	public void RemoveWarehouse(Warehouse w){
		Trade t = GetTradeFor(w.City);
		if(t==null){
			Debug.LogError ("Tried to remove a city that wasnt in here!");
			return; // not in error from somewhere
		}
		if(Trades.IndexOf(t)<currentDestination){
			currentDestination--; // smaller then we must remove to be on the same still
		} else 
		if(Trades.IndexOf(t)==currentDestination){
			//if its behind the otherone so decrease the destination pointer
			currentDestination--;
			currentDestination = Mathf.Clamp (currentDestination,0,NumberOfStops-1);
		}
		Trades.Remove (t);

	}

	public int GetLastNumber(){
		return NumberOfStops;
	}
	public int GetNumberFor(Warehouse w){
		for (int i = 0; i < Trades.Count; i++) {
			if(Trades[i].city==w.City){
				return i + 1;
			} 
		}
		return -1;
	}
	public Tile GetCurrentDestination(){
		if(Trades.Count==0){
			return null;
		}
		if(Trades [currentDestination].city.myWarehouse==null){
			return null;
		}
		return Trades [currentDestination].city.myWarehouse.GetTradeTile ();
	}
	public Tile GetNextDestination(){
		//if theres only one destination
		//that means there is no realtraderoute in place
		//so just return
		if(Trades.Count<=1){
			return null;
		}

		for (int i = 0; i < NumberOfStops; i++) {
			IncreaseDestination ();
			if(Trades [currentDestination].city.myWarehouse!=null){
				isStarted = true;
				return Trades [currentDestination].city.myWarehouse.GetTradeTile ();
			}
		}
		return null;
	}
	public void IncreaseDestination(){
		if(isStarted)
			currentDestination = (currentDestination + 1) % Trades.Count;
	}
	public bool Contains(City c){
		return GetTradeFor (c) != null;
	}

	public Trade GetNextTrade(City curr , bool r){
		if(Contains (curr)==false){
			Debug.LogError ("GetTradeCurr-currentcity isnt in cities"); 
			return null;
		}
		return Trades [currentDestination];
	}
	public Trade GetTradeFor(City c){
		return  Trades.Find (x => x.city == c);
	}

	public void DoCurrentTrade(Unit u){
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
}
