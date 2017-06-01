using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;


[Serializable]
public class Player : IGEventable {
	[NonSerialized]
	public const int TargetType = 1;

	Action<GameEvent> cbEventCreated;
	Action<GameEvent> cbEventEnded;
	List<City> myCities;

	[NonSerialized]
	private int _change;
	public int change { get { return _change;} 
		protected set { _change = value;}
	} //should be calculated after reload anyway
	[SerializeField]
	private int _balance;
	public int balance { get { return _balance;} 
		protected set { _balance = value;}
	} 
	// because only the new level popcount is interesting
	// needs to be saved because you can lose pop due
	// war or death and only the highest ever matters here 
	[SerializeField]
	private int _maxPopulationLevel;
	public int maxPopulationLevel { 
		get {return _maxPopulationLevel;} 
		set { 
			if(maxPopulationLevel<value){
				return;
			}
			_maxPopulationLevel = value;
			maxPopulationCount = 0; 
		}
	} 									  
	[SerializeField]
	private int _maxPopulationCount;
	public int maxPopulationCount { 
		get {return _maxPopulationCount;} 
		set { 
			_maxPopulationCount = value;
		}
	} 
	[SerializeField]
	public int playerNumber;

	public Player(){
	}

	public Player(int number){
		playerNumber = number;
		maxPopulationCount = 0;
		maxPopulationLevel = 0;
		myCities = new List<City> ();
		change = -10;
		balance = 50000;
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

	#region save
		public void SaveIGE(XmlWriter writer){
			writer.WriteAttributeString("TargetType", TargetType +"" );
			writer.WriteAttributeString("PlayerNumber", playerNumber +"" );
		}
	#endregion


}
