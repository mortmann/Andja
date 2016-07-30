using UnityEngine;
using System.Collections.Generic;

public class Trade {
	public City city;
	public List<Item> getting;
	public List<Item> giving;
	public Trade(Warehouse w){
		city = w.City;
		getting = new List<Item> ();
		giving = new List<Item> ();
	}
}
public class TradeRoute {
	int numberOfStops=0;
	int currentDestination=0;
	List<City> cities;
	Dictionary<City,Trade> cityToTrade;

	public TradeRoute(){
		cities = new List<City> ();
		cityToTrade = new Dictionary<City, Trade> ();
	}

	public void AddWarehouse(Warehouse w){
		numberOfStops++;
		Trade t = new Trade (w);
		cities.Add (w.City);
		cityToTrade.Add (w.City,t);
	}

	public void RemoveWarehouse(Warehouse w){
		numberOfStops--;
		cityToTrade.Remove (w.City);
	}

	public int GetLastNumber(){
		return numberOfStops;
	}
	public int GetNumberFor(Warehouse w){
		for (int i = 0; i < cities.Count; i++) {
			if(cities[i].myWarehouse==w||w.City==cities[i]){
				return i + 1;
			} else {
				return -1;
			}
		}
		return -1;
	}
	public Tile getCurrentDestination(){
		if(cities [currentDestination].myWarehouse==null){
			return null;
		}
		return cities [currentDestination].myWarehouse.getTradeTile ();
	}
	public Tile getNextDestination(){
		for (int i = 0; i < numberOfStops; i++) {
			currentDestination = (currentDestination + 1) % cities.Count;
			if(cities [currentDestination].myWarehouse!=null){
				return cities [currentDestination].myWarehouse.getTradeTile ();
			}
		}
		return null;
	}
	public bool Contains(Warehouse w){
		return cityToTrade.ContainsKey (w.City);
	}
}
