using UnityEngine;
using System.Collections.Generic;
using System;
public class Player : IGEventable {
	private static int TargetType = 1;

	Action<GameEvent> cbEventCreated;
	Action<GameEvent> cbEventEnded;
	List<City> myCities;
	public int change { get; protected set;}
	public int balance { get; protected set;}
	public int maxPopulationCount { get; set;}
	public int maxPopulationLevel { get; set;}
	public List<int> playersAtWarWith;
	public int playerNumber;
	public Player(int number){
		playerNumber = number;
		maxPopulationCount = 0;
		maxPopulationLevel = 0;
		myCities = new List<City> ();
		change = -10;
		balance = 50000;
		playersAtWarWith = new List<int> ();
		//TODO remove this
		playersAtWarWith.Add (2); 
		playersAtWarWith.Add (0); 

	}
	public void Update () {

		int citychange=0;
		for (int i = 0; i < myCities.Count; i++) {
			citychange += myCities[i].cityBalance;
		}

		balance += change+citychange;

		if(balance < -1000000){
			// game over !
		}
	}

	public void reduceMoney(int money) {
		if (money < 0) {
			return;	
		}
		balance -= money;
	}
	public void addMoney(int money) {
		if (money < 0) {
			return;	
		}
		balance += money;
	}
	public void reduceChange(int amount) {
		if (amount < 0) {
			return;	
		}
		change -= amount;
	}
	public void addChange(int amount) {
		if (amount < 0) {
			return;	
		}
		change += amount;
	}
	public void OnCityCreated(City city){
		myCities.Add (city);

	}
	public void OnStructureCreated(Structure structure){
		reduceMoney (structure.buildcost);
		structure.RegisterOnDestroyCallback (OnStructureDestroy);
	}
	public void OnStructureDestroy(Structure structure){
		//dosmth
	}
	public void RegisterOnEvent(Action<GameEvent> create,Action<GameEvent> ending){
		
	}
	public int GetPlayerNumber(){
		return playerNumber;
	}
	public int GetTargetType(){
		return TargetType;
	}
}
