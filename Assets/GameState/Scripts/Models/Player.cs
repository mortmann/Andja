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

	public HashSet<Need>[] lockedNeeds { get; protected set;}
	public HashSet<Need> unlockedItemNeeds { get; protected set;}
	public HashSet<Need>[] unlockedStructureNeeds { get; protected set;}

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
		unlockedStructureNeeds = new HashSet<Need>[City.citizienLevels];
		unlockedItemNeeds = new HashSet<Need> ();

		for (int i = 0; i < City.citizienLevels; i++) {
			lockedNeeds [i] = new HashSet<Need> ();
			unlockedStructureNeeds [i] = new HashSet<Need> ();
		}
		foreach(Need n in PrototypController.Instance.getCopieOfAllNeeds ()){
			lockedNeeds [n.startLevel].Add (n);
		}
		for(int i = 0; i <= maxPopulationLevel; i++){
			int count = maxPopulationCount;
			if(i<maxPopulationLevel){
				count = int.MaxValue;
			}
			needUnlockCheck (i, count);
		}
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
		HashSet<Need> toRemove = new HashSet<Need> ();
		foreach(Need need in lockedNeeds[level]){
			if(need.startLevel!=level){
				return;
			}
			if(need.popCount>count){
				return;
			}
			toRemove.Add (need);
			if(need.IsItemNeed()){
				unlockedItemNeeds.Add (need);
			} else {
				unlockedStructureNeeds[need.startLevel].Add (need);
			}
			if(cbNeedUnlocked!=null)
				cbNeedUnlocked (need);
		}	
		foreach(Need need in toRemove){
			lockedNeeds [level].Remove (need);
		}
	}
	public HashSet<Need> getUnlockedStructureNeeds(int level){
		return unlockedStructureNeeds [level];
	}
	public bool hasUnlockedAllNeeds(int level){
		return lockedNeeds [level].Count == 0;
	}
	public bool hasUnlockedNeed(Need n){
		if (n == null) {
			Debug.Log ("??? Need is null!");
			return false;
		}
		if (lockedNeeds [n.startLevel] == null) {
			Debug.Log ("??? lockedNeeds is null!");
			return false;
		}
		return lockedNeeds [n.startLevel].Contains (n)==false;
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
