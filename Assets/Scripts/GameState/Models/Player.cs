﻿using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class PlayerPrototypeData {
    //This will contain stuff from the difficulty settings 
    //like maximumdebt
    public int maximumDebt = 0;
    public float BalanceFullTime = 60f;
    public float BalanceTicksTime = 4f;


}

[JsonObject(MemberSerialization.OptIn)]
public class Player : IGEventable {
    #region Not Serialized
    internal bool HasEnoughMoney(int buildCost) {
        return TreasuryBalance + MaximumDebt > buildCost;
    }
    public HashSet<Need>[] LockedNeeds { get; protected set; }
    public HashSet<Need> UnlockedItemNeeds { get; protected set; }
    public HashSet<Need>[] UnlockedStructureNeeds { get; protected set; }
    public HashSet<Structure> AllStructures;
    public HashSet<Unit> AllUnits;
    public List<City> myCities;
    public bool HasLost => TreasuryBalance < MaximumDebt;
    PlayerPrototypeData PlayerPrototypeData => PrototypController.CurrentPlayerPrototypData;

    private int MaximumDebt => PlayerPrototypeData.maximumDebt; // if we want a maximum debt where you still can buy things

    private int _treasuryChange;
    /// <summary>
    /// How the Balance CHANGES foreach Tick that happens
    /// </summary>
	public int TreasuryChange {
        get { return CalculateRealValue("change", _treasuryChange); }
        protected set { _treasuryChange = value; }
    } //should be calculated after reload anyway

    [JsonPropertyAttribute]
    private int _lastTreasuryChange;
    public int LastTreasuryChange {
        get { return _lastTreasuryChange; }
        protected set { _lastTreasuryChange = value; }
    }

    Action<int, int> cbMaxPopulationMLCountChange;
    Action<Need> cbNeedUnlocked;
    Action<Need> cbStructureNeedUnlocked;



    #endregion
    #region Serialized
    [JsonPropertyAttribute]
    private int _treasuryBalance;
    /// <summary>
    /// How much Money you have to spend
    /// </summary>
	public int TreasuryBalance {
        get { return _treasuryBalance; }
        protected set { _treasuryBalance = value; }
    }
    // because only the new level popcount is interesting
    // needs to be saved because you can lose pop due
    // war or death and only the highest ever matters here 
    [JsonPropertyAttribute]
    private int _maxPopulationLevel;
    public int MaxPopulationLevel {
        get { return _maxPopulationLevel; }
        set {
            if (MaxPopulationLevel < value) {
                Debug.Log("value < maxPopulationLevel");
                return;
            }
            _maxPopulationLevel = value;
            _maxPopulationCount = 0;
            cbMaxPopulationMLCountChange?.Invoke(MaxPopulationLevel, _maxPopulationCount);
        }
    }

    internal List<Need> GetCopyStructureNeeds(int level) {
        List<Need> list = new List<Need>();
        foreach (Need n in UnlockedStructureNeeds[level]) {
            list.Add(n.Clone());
        }
        return list;
    }

    internal void RegisterStructureNeedUnlock(Action<Need> onStructureNeedUnlock) {
        cbStructureNeedUnlocked += onStructureNeedUnlock;
    }
    internal void UnregisterStructureNeedUnlock(Action<Need> onStructureNeedUnlock) {
        cbStructureNeedUnlocked -= onStructureNeedUnlock;
    }
    [JsonPropertyAttribute]
    private int _maxPopulationCount;
    public int MaxPopulationCount {
        get { return _maxPopulationCount; }
        set {
            if (value < _maxPopulationCount) {
                Debug.Log("value < _maxPopulationCount");
                return;
            }
            _maxPopulationCount = value;
            cbMaxPopulationMLCountChange?.Invoke(MaxPopulationLevel, _maxPopulationCount);
        }
    }
    [JsonPropertyAttribute]
    public List<TradeRoute> MyTradeRoutes { get; protected set; }

    [JsonPropertyAttribute]
    public int Number;
    #endregion

    public Player() {
        Setup();
    }

    public Player(int number) {
        Number = number;
        MaxPopulationCount = 0;
        MaxPopulationLevel = 0;
        TreasuryChange = 0;
        TreasuryBalance = 50000;
        Setup();
    }
    private void Setup() {
        myCities = new List<City>();
        LockedNeeds = new HashSet<Need>[PrototypController.Instance.NumberOfPopulationLevels];
        UnlockedStructureNeeds = new HashSet<Need>[PrototypController.Instance.NumberOfPopulationLevels];
        UnlockedItemNeeds = new HashSet<Need>();
        MyTradeRoutes = new List<TradeRoute>();
        AllStructures = new HashSet<Structure>();
        AllUnits = new HashSet<Unit>();
        for (int i = 0; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
            LockedNeeds[i] = new HashSet<Need>();
            UnlockedStructureNeeds[i] = new HashSet<Need>();
        }
        foreach (Need n in PrototypController.Instance.GetCopieOfAllNeeds()) {
            LockedNeeds[n.StartLevel].Add(n);
        }
        for (int i = 0; i <= MaxPopulationLevel; i++) {
            int count = MaxPopulationCount;
            if (i < MaxPopulationLevel) {
                count = int.MaxValue;
            }
            NeedUnlockCheck(i, count);
        }
        RegisterMaxPopulationCountChange(NeedUnlockCheck);
        CalculateBalance();
        BuildController.Instance.RegisterCityCreated(OnCityCreated);
        //World.Current.RegisterUnitCreated(OnUnitCreated);
    }

    private void CalculateBalance() {
        LastTreasuryChange = TreasuryChange;
        TreasuryChange = 0;
        for (int i = 0; i < myCities.Count; i++) {
            LastTreasuryChange += myCities[i].Balance;
        }
    }

    internal IEnumerable<Island> GetIslandList() {
        HashSet<Island> islands = new HashSet<Island>();
        foreach (City c in myCities)
            islands.Add(c.island);
        return islands;
    }

    internal IEnumerable<Unit> GetLandUnits() {
        HashSet<Unit> units = new HashSet<Unit>(AllUnits);
        units.RemoveWhere(x => x.IsShip);
        return units;
    }

    internal IEnumerable<Ship> GetShipUnits() {
        HashSet<Unit> units = new HashSet<Unit>(AllUnits);
        units.RemoveWhere(x => x.IsShip == false);
        return (IEnumerable<Ship>)units; //should be safe cause removing non ship
    }

    internal void Load() {
        //Setup();
    }

    public void UpdateBalance(float partialPayAmount) {
        CalculateBalance();
        TreasuryBalance += Mathf.RoundToInt( LastTreasuryChange / partialPayAmount );

        if (TreasuryBalance < -1000000) {
            // game over !
        }
    }
    private void NeedUnlockCheck(int level, int count) {
        //TODO Replace this with a less intense check
        HashSet<Need> toRemove = new HashSet<Need>();
        foreach (Need need in LockedNeeds[level]) {
            if (need.StartLevel != level) {
                return;
            }
            if (need.PopCount > count) {
                return;
            }
            toRemove.Add(need);
            if (need.IsItemNeed()) {
                UnlockedItemNeeds.Add(need);
            }
            else {
                cbStructureNeedUnlocked?.Invoke(need);
                UnlockedStructureNeeds[need.StartLevel].Add(need);
            }
            cbNeedUnlocked?.Invoke(need);
        }
        foreach (Need need in toRemove) {
            LockedNeeds[level].Remove(need);
        }
    }
    public HashSet<Need> GetUnlockedStructureNeeds(int level) {
        return UnlockedStructureNeeds[level];
    }
    public HashSet<Need> GetALLUnlockedStructureNeedsTill(int level) {
        HashSet<Need> needs = new HashSet<Need>();
        for (int i = 0; i <= level; i++) {
            needs.UnionWith(UnlockedStructureNeeds[i]);
        }
        return needs;
    }
    public bool HasUnlockedAllNeeds(int level) {
        return LockedNeeds[level].Count == 0;
    }
    public bool HasUnlockedNeed(Need n) {
        if (n == null) {
            Debug.Log("??? Need is null!");
            return false;
        }
        if (LockedNeeds[n.StartLevel] == null) {
            Debug.Log("??? lockedNeeds is null!");
            return false;
        }
        //either StartLevel is smaller so unlocked
        return n.StartLevel < MaxPopulationLevel 
            //is equal so count matters too
            || n.StartLevel == MaxPopulationLevel && n.PopCount <= MaxPopulationCount; // LockedNeeds[n.StartLevel].Contains(n) == false;
    }
    public bool HasNeedUnlocked(Need need) {
        if (need.IsItemNeed())
            return UnlockedItemNeeds.Contains(need);
        return UnlockedStructureNeeds[need.StartLevel].Contains(need);
    }
    public void AddTradeRoute(TradeRoute route) {
        if (MyTradeRoutes == null)
            MyTradeRoutes = new List<TradeRoute>();
        MyTradeRoutes.Add(route);
    }
    public bool RemoveTradeRoute(TradeRoute route) {
        route.Destroy();
        return MyTradeRoutes.Remove(route);
    }
    public void ReduceMoney(int money) {
        if (money < 0) {
            return;
        }
        TreasuryBalance -= money;
    }
    public void AddMoney(int money) {
        if (money < 0) {
            return;
        }
        TreasuryBalance += money;
    }
    public void ReduceChange(int amount) {
        if (amount < 0) {
            return;
        }
        TreasuryChange -= amount;
    }
    public void AddChange(int amount) {
        if (amount < 0) {
            return;
        }
        TreasuryChange += amount;
    }
    public void OnCityCreated(City city) {
        if (city.playerNumber != Number)
            return;
        myCities.Add(city);
        city.RegisterStructureAdded(OnStructureCreated);
        city.RegisterCityDestroy(OnCityDestroy);
    }
    public void OnCityDestroy(City city) {
        city.UnregisterStructureAdded(OnStructureCreated);
        myCities.Remove(city);
    }

    public void OnStructureCreated(Structure structure) {
        ReduceMoney(structure.BuildCost);
        structure.RegisterOnDestroyCallback(OnStructureDestroy);
        AllStructures.Add(structure);
    }
    public void OnStructureDestroy(Structure structure) {
        //dosmth
        structure.UnregisterOnDestroyCallback(OnStructureDestroy);
        AllStructures.Remove(structure);

    }
    public void OnUnitCreated(Unit unit) {
        if (unit.playerNumber != Number)
            return;
        //ReduceMoney(unit.BuildCost); -- will be removed when starting to build one -- not when finished
        unit.RegisterOnDestroyCallback(OnUnitDestroy);
        AllUnits.Add(unit);
    }
    public void OnUnitDestroy(Unit unit) {
        //dosmth
        unit.UnregisterOnDestroyCallback(OnUnitDestroy);
        AllUnits.Remove(unit);
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
    public void RegisterMaxPopulationCountChange(Action<int, int> callbackfunc) {
        cbMaxPopulationMLCountChange += callbackfunc;
    }
    public void UnregisterMaxPopulationCountChange(Action<int, int> callbackfunc) {
        cbMaxPopulationMLCountChange -= callbackfunc;
    }

    #region igeventable
    public override void OnEventCreate(GameEvent ge) {

    }
    public override void OnEventEnded(GameEvent ge) {

    }
    public override int GetPlayerNumber() {
        return Number;
    }
    #endregion
}