using UnityEngine;
using System.Collections.Generic;

public class Trade {
	public City city;
	public Item[] getting;
	public Item[] giving;
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
}
public class TradeRoute {
	int numberOfStops=0;
	int currentDestination=0;
	List<City> cities;
	Dictionary<City,Trade> cityToTrade;
	public bool isStarted=false;
	public bool Valid{
		get {
			return cities.Count > 1;
		}
	}
	public TradeRoute(){
		cities = new List<City> ();
		cityToTrade = new Dictionary<City, Trade> ();
	}
	public TradeRoute(TradeRoute tr){
		this.cities = tr.cities;
		cityToTrade = new Dictionary<City, Trade> ();
		foreach(City c in tr.cityToTrade.Keys){
			cityToTrade.Add (c,new Trade(tr.cityToTrade[c]));
		}
		numberOfStops = tr.numberOfStops;
		currentDestination = tr.currentDestination;
		isStarted = tr.isStarted;

	}
	public void AddWarehouse(Warehouse w){
		numberOfStops++;
		Trade t = new Trade (w.City,null,null);
		cities.Add (w.City);
		cityToTrade.Add (w.City,t);
	}
	public void SetCityTrade(City city,Item[] getting,Item[] giving){
		if(cityToTrade.ContainsKey (city)){
			cityToTrade [city].getting = getting;
			cityToTrade [city].giving = giving;
		} else {
			Debug.LogError ("Wat de f SetCityTrade"); 
		}
	}
	public Trade GetCurrentCityTrade(){
		return cityToTrade [cities[currentDestination]];
	}
	public void RemoveWarehouse(Warehouse w){
		
		numberOfStops--;
		City c=null;
		int pos = 0;
		if (numberOfStops > 1) {
			c = cities [currentDestination];
			pos = cities.IndexOf (c);
		}
		cities.Remove (w.City);
		cityToTrade.Remove (w.City);
		//if the currentdestination gets removed let the pointer
		//but clamp it if needed
		if(c!=null&&c.myWarehouse==w){
			currentDestination = Mathf.Clamp (currentDestination, 0, numberOfStops - 1);
		} else {
			//if its behind the otherone so decrease the destination pointer
			if(currentDestination>pos){
				currentDestination--;
			} 
		}
	}

	public int GetLastNumber(){
		return numberOfStops;
	}
	public int GetNumberFor(Warehouse w){
		for (int i = 0; i < cities.Count; i++) {
			if(cities[i].myWarehouse==w||w.City==cities[i]){
				return i + 1;
			} 
		}
		return -1;
	}
	public Tile getCurrentDestination(){
		if(cities.Count==0){
			return null;
		}
		if(cities [currentDestination].myWarehouse==null){
			return null;
		}
		return cities [currentDestination].myWarehouse.getTradeTile ();
	}
	public Tile getNextDestination(){
		//if theres only one destination
		//that means there is no realtraderoute in place
		//so just return
		if(cities.Count<=1){
			return null;
		}

		for (int i = 0; i < numberOfStops; i++) {
			increaseDestination ();
			if(cities [currentDestination].myWarehouse!=null){
				isStarted = true;
				return cities [currentDestination].myWarehouse.getTradeTile ();
			}
		}
		return null;
	}
	public void increaseDestination(){
		if(isStarted)
			currentDestination = (currentDestination + 1) % cities.Count;
	}
	public bool Contains(City c){
		return cityToTrade.ContainsKey (c);
	}

	public Trade GetNextTrade(City curr , bool r){
		if(cities.Contains (curr)==false){
			Debug.LogError ("GetTradeCurr-currentcity isnt in cities"); 
			return null;
		}
		int i = cities.IndexOf (curr);
		if(r){
			i = (i + 1) % cities.Count;
		} else {
			i = (i - 1) % cities.Count;
		}
		return cityToTrade [cities [i]];
	}
	public Trade GetTradeFor(City c){
		if(cities.Contains (c)==false){
			return null;
		}
		return cityToTrade [c];
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
			c.tradeWithShip (item,needed,u);
		}
		//give as much as possible but max the choosen one
		foreach (Item item in t.giving) {
			c.tradeFromShip (u,item,item.count);
		}

	}


}
