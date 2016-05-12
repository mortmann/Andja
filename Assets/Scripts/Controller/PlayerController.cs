﻿using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {

	List<City> myCities;
	public int change { get; protected set;}
	public int balance { get; protected set;}
	float balanceTicks;
	float tickTimer;

	// Use this for initialization
	void Start () {
		myCities = new List<City> ();
		change = -10;
		balance = 50000;
		balanceTicks = 5f;
		tickTimer = balanceTicks;
		GameObject.FindObjectOfType<BuildController>().RegisterCityCreated (OnCityCreated);
		GameObject.FindObjectOfType<BuildController>().RegisterStructureCreated (OnStructureCreated);
	}
	
	// Update is called once per frame
	void Update () {

		tickTimer -= Time.deltaTime;
		if(tickTimer<=0){
			tickTimer = balanceTicks;
			balance += change;

		}
		if(balance < 1000000){
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
		reduceChange(structure.maintenancecost);
		reduceMoney (structure.buildcost);

	}
}
