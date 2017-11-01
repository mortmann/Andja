using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

[JsonObject(MemberSerialization.OptIn)]
public class Player : IGEventable {
	#region Not Serialized
	public const int TargetType = 1;
	Action<GameEvent> cbEventCreated;
	Action<GameEvent> cbEventEnded;
	List<City> myCities;

	HashSet<Need>[] lockedNeeds;
	HashSet<Need> unlockedItemNeeds;
	HashSet<Need>[] unlockedStructureNeeds;

	private int _change;
	public int change { get { return _change;} 
		protected set { _change = value;}
	} //should be calculated after reload anyway

	Action<int,int> cbMaxPopulationMLCountChange;
	Action<Need> cbNeedUnlocked;

	#endregion 
	#region Serialized
	[JsonPropertyAttribute]
	private int _balance;
	public int balance { get { return _balance;} 
		protected set { _balance = value;}
	} 
	// because only the new level popcount is interesting
	// needs to be saved because you can lose pop due
	// war or death and only the highest ever matters here 
	[JsonPropertyAttribute]
	private int _maxPopulationLevel;
	public int maxPopulationLevel { 
		get {return _maxPopulationLevel;} 
		set { 
			if(maxPopulationLevel<value){
				Debug.Log ("value < maxPopulationLevel");
				return;
			}
			_maxPopulationLevel = value;
			_maxPopulationCount = 0; 
			if(cbMaxPopulationMLCountChange!=null)
				cbMaxPopulationMLCountChange (maxPopulationLevel,_maxPopulationCount);
		}
	} 									  
	[JsonPropertyAttribute]
	private int _maxPopulationCount;
	public int maxPopulationCount { 
		get {return _maxPopulationCount;} 
		set { 
			if(value<_maxPopulationCount){
				Debug.Log ("value < _maxPopulationCount");
				return;
			} 
			_maxPopulationCount = value;
			if(cbMaxPopulationMLCountChange!=null)
				cbMaxPopulationMLCountChange (maxPopulationLevel,_maxPopulationCount);
		}
	} 
	[JsonPropertyAttribute]
	public int playerNumber;
	#endregion 

	public Player(){
		Setup ();
	}

	public Player(int number){
		playerNumber = number;
		maxPopulationCount = 0;
		maxPopulationLevel = 0;
		change = 0;
		balance = 50000;
		Setup ();
	}
	private void Setup(){
		myCities = new List<City> ();
		lockedNeeds = new HashSet<Need>[City.citizienLevels];
		foreach(Need n in PrototypController.Instance.getCopieOfAllNeeds ()){
			if(lockedNeeds[n.startLevel]==null){
				lockedNeeds [n.startLevel] = new HashSet<Need> ();
			}
			lockedNeeds [n.startLevel].Add (n);
		}
		unlockedItemNeeds = new HashSet<Need> ();
		unlockedStructureNeeds = new HashSet<Need>[City.citizienLevels];

		RegisterMaxPopulationCountChange (needUnlockCheck);

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
	private void needUnlockCheck(int level, int count){
		//TODO Replace this with a less intense check
		foreach(Need need in lockedNeeds[level]){
			if(need.startLevel!=level){
				return;
			}
			if(need.popCount<count){
				return;
			}
			lockedNeeds[level].Remove (need);
			if(need.IsItemNeed()){
				unlockedItemNeeds.Add (need);
			} else {
				if(unlockedStructureNeeds[need.startLevel]==null){
					unlockedStructureNeeds [need.startLevel] = new HashSet<Need> ();
				}
				unlockedStructureNeeds[need.startLevel].Add (need);
			}
			cbNeedUnlocked (need);
		}	
	}
	public HashSet<Need> getUnlockedStructureNeeds(int level){
		return unlockedStructureNeeds [level];
	}
	public bool hasUnlockedAllNeeds(int level){
		return lockedNeeds [level].Count == 0;
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
	public void UnregisterNeedUnlock(Action<Need> callbackfunc) {
		cbNeedUnlocked -= callbackfunc;
	}
	public void RegisterNeedUnlock(Action<Need> callbackfunc) {
		cbNeedUnlocked += callbackfunc;
	}
	/// <summary>
	/// Registers the population count change.
	/// First is the max POP-LEVEL the player has
	/// Second is the max POP-Count in that level he has
	/// </summary>
	/// <param name="callbackfunc">Callbackfunc.</param>
	public void RegisterMaxPopulationCountChange(Action<int,int> callbackfunc) {
		cbMaxPopulationMLCountChange += callbackfunc;
	}
	public void UnregisterMaxPopulationCountChange(Action<int,int> callbackfunc) {
		cbMaxPopulationMLCountChange -= callbackfunc;
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
