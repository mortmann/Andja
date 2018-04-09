﻿using UnityEngine;
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

	public HashSet<Need>[] LockedNeeds { get; protected set;}
	public HashSet<Need> UnlockedItemNeeds { get; protected set;}
	public HashSet<Need>[] UnlockedStructureNeeds { get; protected set;}

	private int _change;
	public int Change { get { return _change;} 
		protected set { _change = value;}
	} //should be calculated after reload anyway

	Action<int,int> cbMaxPopulationMLCountChange;
	Action<Need> cbNeedUnlocked;

	#endregion 
	#region Serialized
	[JsonPropertyAttribute]
	private int _balance;
	public int Balance { get { return _balance;} 
		protected set { _balance = value;}
	} 
	// because only the new level popcount is interesting
	// needs to be saved because you can lose pop due
	// war or death and only the highest ever matters here 
	[JsonPropertyAttribute]
	private int _maxPopulationLevel;
	public int MaxPopulationLevel { 
		get {return _maxPopulationLevel;} 
		set { 
			if(MaxPopulationLevel<value){
				Debug.Log ("value < maxPopulationLevel");
				return;
			}
			_maxPopulationLevel = value;
			_maxPopulationCount = 0;
            cbMaxPopulationMLCountChange?.Invoke(MaxPopulationLevel, _maxPopulationCount);
        }
	} 									  
	[JsonPropertyAttribute]
	private int _maxPopulationCount;
	public int MaxPopulationCount { 
		get {return _maxPopulationCount;} 
		set { 
			if(value<_maxPopulationCount){
				Debug.Log ("value < _maxPopulationCount");
				return;
			} 
			_maxPopulationCount = value;
            cbMaxPopulationMLCountChange?.Invoke(MaxPopulationLevel, _maxPopulationCount);
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
		MaxPopulationCount = 0;
		MaxPopulationLevel = 0;
		Change = 0;
		Balance = 50000;
		Setup ();
	}
	private void Setup(){
		myCities = new List<City> ();
		LockedNeeds = new HashSet<Need>[City.citizienLevels];
		UnlockedStructureNeeds = new HashSet<Need>[City.citizienLevels];
		UnlockedItemNeeds = new HashSet<Need> ();

		for (int i = 0; i < City.citizienLevels; i++) {
			LockedNeeds [i] = new HashSet<Need> ();
			UnlockedStructureNeeds [i] = new HashSet<Need> ();
		}
		foreach(Need n in PrototypController.Instance.getCopieOfAllNeeds ()){
			LockedNeeds [n.StartLevel].Add (n);
		}
		for(int i = 0; i <= MaxPopulationLevel; i++){
			int count = MaxPopulationCount;
			if(i<MaxPopulationLevel){
				count = int.MaxValue;
			}
			NeedUnlockCheck (i, count);
		}
		RegisterMaxPopulationCountChange (NeedUnlockCheck);

	}
	public void Update () {

		int citychange=0;
		for (int i = 0; i < myCities.Count; i++) {
			citychange += myCities[i].cityBalance;
		}

		Balance += Change+citychange;

		if(Balance < -1000000){
			// game over !
		}
	}
	private void NeedUnlockCheck(int level, int count){
		//TODO Replace this with a less intense check
		HashSet<Need> toRemove = new HashSet<Need> ();
		foreach(Need need in LockedNeeds[level]){
			if(need.StartLevel!=level){
				return;
			}
			if(need.PopCount>count){
				return;
			}
			toRemove.Add (need);
			if(need.IsItemNeed()){
				UnlockedItemNeeds.Add (need);
			} else {
				UnlockedStructureNeeds[need.StartLevel].Add (need);
			}
            cbNeedUnlocked?.Invoke(need);
        }	
		foreach(Need need in toRemove){
			LockedNeeds [level].Remove (need);
		}
	}
	public HashSet<Need> GetUnlockedStructureNeeds(int level){
		return UnlockedStructureNeeds [level];
	}
	public bool HasUnlockedAllNeeds(int level){
		return LockedNeeds [level].Count == 0;
	}
	public bool HasUnlockedNeed(Need n){
		if (n == null) {
			Debug.Log ("??? Need is null!");
			return false;
		}
		if (LockedNeeds [n.StartLevel] == null) {
			Debug.Log ("??? lockedNeeds is null!");
			return false;
		}
		return LockedNeeds [n.StartLevel].Contains (n)==false;
	}

	public void ReduceMoney(int money) {
		if (money < 0) {
			return;	
		}
		Balance -= money;
	}
	public void AddMoney(int money) {
		if (money < 0) {
			return;	
		}
		Balance += money;
	}
	public void ReduceChange(int amount) {
		if (amount < 0) {
			return;	
		}
		Change -= amount;
	}
	public void AddChange(int amount) {
		if (amount < 0) {
			return;	
		}
		Change += amount;
	}
	public void OnCityCreated(City city){
		myCities.Add (city);

	}
	public void OnStructureCreated(Structure structure){
		ReduceMoney (structure.buildcost);
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
