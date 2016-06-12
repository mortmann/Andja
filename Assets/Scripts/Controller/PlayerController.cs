using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {
	public int number { get; protected set;}
	List<City> myCities;
	public int change { get; protected set;}
	public int balance { get; protected set;}
	public int maxPopulationCount { get; set;}
	public int maxPopulationLevel { get; set;}
	float balanceTicks;
	float tickTimer;
	public static PlayerController Instance { get; protected set; }

	// Use this for initialization
	void Awake () {
		if (Instance != null) {
			Debug.LogError("There should never be two mouse controllers.");
		}
		Instance = this;
		number = 0;
		maxPopulationCount = 0;
		maxPopulationLevel = 0;
		myCities = new List<City> ();
		change = -10;
		balance = 0;
		balanceTicks = 5f;
		tickTimer = balanceTicks;
		GameObject.FindObjectOfType<BuildController>().RegisterCityCreated (OnCityCreated);
		GameObject.FindObjectOfType<BuildController>().RegisterStructureCreated (OnStructureCreated);
	}
	
	// Update is called once per frame
	void Update () {

		tickTimer -= Time.deltaTime;
		if(tickTimer<=0){
			int citychange=0;
			for (int i = 0; i < myCities.Count; i++) {
				citychange += myCities[i].cityBalance;
			}
			tickTimer = balanceTicks;
			balance += change+citychange;

		}
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
	}
}
