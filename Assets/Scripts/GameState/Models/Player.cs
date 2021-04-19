﻿using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Linq;

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
    public HashSet<String>[] UnlockedItemNeeds { get; protected set; }
    public HashSet<String>[] UnlockedStructureNeeds { get; protected set; }
    public HashSet<String> UnlockedStructures { get; protected set; }
    public HashSet<String> UnlockedUnits { get; protected set; }

    public HashSet<Structure> AllStructures;
    public HashSet<Unit> Units;
    public HashSet<Ship> Ships;
    public Action<Player> cbHasLost;
    public List<City> Cities;
    public bool HasLost => _hasLost;
    PlayerPrototypeData PlayerPrototypeData => PrototypController.CurrentPlayerPrototypData;

    private int MaximumDebt => PlayerPrototypeData.maximumDebt; // if we want a maximum debt where you still can buy things

    private int _treasuryChange;
    public bool IsHuman => _IsHuman;
    /// <summary>
    /// How the Balance CHANGES foreach Tick that happens
    /// </summary>
	public int TreasuryChange {
        get { return CalculateRealValue(nameof(TreasuryChange), _treasuryChange); }
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
    Action<IEnumerable<Structure>> cbStructuresUnlocked;
    Action<IEnumerable<Unit>> cbUnitsUnlocked;
    Action<Structure> cbNewStructure;
    Action<Structure> cbLostStructure;
    Action<City> cbCityCreated;
    Action<City> cbCityDestroy;

    #endregion
    #region Serialized
    [JsonPropertyAttribute]
    private string _name;
    public string Name => _name ?? "Number " + Number; //FOR NOW
    [JsonPropertyAttribute]
    private bool _IsHuman;
    [JsonPropertyAttribute]
    private int _treasuryBalance;
    [JsonPropertyAttribute]
    public Statistics Statistics { get; protected set; }
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
        }
    }

    internal List<Need> GetCopyStructureNeeds(int level) {
        List<Need> list = new List<Need>();
        foreach (String n in UnlockedStructureNeeds[level]) {
            list.Add(new Need(n));
        }
        return list;
    }

    internal void RegisterStructureNeedUnlock(Action<Need> onStructureNeedUnlock) {
        cbStructureNeedUnlocked += onStructureNeedUnlock;
    }

    internal bool HasStructureUnlocked(string iD) {
        return UnlockedStructures.Contains(iD);
    }

    internal bool IsCurrent() {
        return PlayerController.currentPlayerNumber == Number;
    }

    internal void RegisterStructuresUnlock(Action<IEnumerable<Structure>> onStructuresUnlock) {
        cbStructuresUnlocked += onStructuresUnlock;
    }
    internal void UnregisterStructuresUnlock(Action<IEnumerable<Structure>> onStructuresUnlock) {
        cbStructuresUnlocked -= onStructuresUnlock;
    }
    internal void RegisterUnitsUnlock(Action<IEnumerable<Unit>> onUnitsUnlock) {
        cbUnitsUnlocked += onUnitsUnlock;
    }
    internal void UnregisterStructureNeedUnlock(Action<Need> onStructureNeedUnlock) {
        cbStructureNeedUnlocked -= onStructureNeedUnlock;
    }
    [JsonPropertyAttribute]
    private int[] _maxPopulationCounts;
    public int[] MaxPopulationCounts {
        get { return _maxPopulationCounts; }
        protected set { _maxPopulationCounts = value; }
    }
    public int MaxPopulationCount {
        get { return MaxPopulationCounts[MaxPopulationLevel]; }
    }
    [JsonPropertyAttribute]
    public List<TradeRoute> TradeRoutes { get; protected set; }
    public int CurrentPopulationLevel { get; internal set; }

    [JsonPropertyAttribute]
    public int Number;
    [JsonPropertyAttribute]
    protected bool _hasLost;
    #endregion

    public Player() {
        Setup();
    }

    public Player(int number, bool isHuman, int startingTreasure) {
        this._IsHuman = isHuman;
        Number = number;
        MaxPopulationLevel = 0;
        TreasuryChange = 0;
        TreasuryBalance = startingTreasure;
        if (isHuman) {
            _name = "itsMeAnTotallyHumanHuman";
            Statistics = new Statistics(Number);
        }
        Setup();
    }
    private void Setup() {
        Cities = new List<City>();
        Ships = new HashSet<Ship>();
        UnlockedStructureNeeds = new HashSet<String>[PrototypController.Instance.NumberOfPopulationLevels];
        UnlockedItemNeeds = new HashSet<String>[PrototypController.Instance.NumberOfPopulationLevels];
        UnlockedStructures = new HashSet<String>();
        UnlockedUnits = new HashSet<String>();
        TradeRoutes = new List<TradeRoute>();
        AllStructures = new HashSet<Structure>();
        Units = new HashSet<Unit>();
        for (int i = 0; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
            UnlockedStructureNeeds[i] = new HashSet<String>();
            UnlockedItemNeeds[i] = new HashSet<String>();
        }
        MaxPopulationCounts = new int[PrototypController.Instance.NumberOfPopulationLevels];
        RegisterMaxPopulationCountChange(UnlockCheck);
        UnlockCheck(0, 0);
    }

    public void CalculateBalance() {
        LastTreasuryChange = TreasuryChange;
        TreasuryChange = 0;
        for (int i = 0; i < Cities.Count; i++) {
            LastTreasuryChange += Cities[i].Balance;
        }
    }

    internal IEnumerable<Island> GetIslandList() {
        HashSet<Island> islands = new HashSet<Island>();
        foreach (City c in Cities)
            islands.Add(c.Island);
        return islands;
    }

    internal IEnumerable<Unit> GetLandUnits() {
        List<Unit> units = new List<Unit>(Units);
        return units.OfType<Unit>();
    }

    internal IEnumerable<Ship> GetShipUnits() {
        return Ships; 
    }

    internal void Load() {
        for (int i = 0; i <= MaxPopulationLevel; i++) {
            int maxCount = MaxPopulationCounts[i];
            foreach(int count in PrototypController.Instance.LevelCountToUnlocks[i].Keys) {
                if (maxCount < count)
                    break;
                UnlockCheck(i, count);
            }
        }
        CalculateBalance();
    }

    public void UpdateBalance(float partialPayAmount) {
        CalculateBalance();
        TreasuryBalance += Mathf.RoundToInt( LastTreasuryChange / partialPayAmount );

    }

    public void UpdateMaxPopulationCount(int level, int count) {
        if(MaxPopulationCounts[level] == 0 && level > MaxPopulationLevel) {
            MaxPopulationLevel = level;
        }
        if(MaxPopulationCounts[level] < count) {
            MaxPopulationCounts[level] = count;
            cbMaxPopulationMLCountChange?.Invoke(MaxPopulationLevel, MaxPopulationCounts[MaxPopulationLevel]);
        }
    }
    public int GetCurrentPopulation(int level) {
        int value = 0;
        foreach (City item in Cities) {
            value += item.GetPopulationCount(level);
        }
        return value;
    }
    private void UnlockCheck(int level, int count) {
        Unlocks unlock = PrototypController.Instance.GetUnlocksFor(level, count);
        if (unlock == null)
            return;
        foreach (Need n in unlock.needs) {
            if (n.IsItemNeed()) {
                cbNeedUnlocked?.Invoke(n);
            }
            else {
                cbStructureNeedUnlocked?.Invoke(n);
            }
            for (int i = n.StartLevel; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
                if (n.IsItemNeed()) {
                    UnlockedItemNeeds[i].Add(n.ID);
                }
                else {
                    UnlockedStructureNeeds[i].Add(n.ID);
                }
            }
        }
        if (unlock.structures.Count > 0) {
            cbStructuresUnlocked?.Invoke(unlock.structures);
            foreach (Structure s in unlock.structures) {
                UnlockedStructures.Add(s.ID);
            }
        }
        if (unlock.units.Count > 0) {
            cbUnitsUnlocked?.Invoke(unlock.units);
            foreach (Unit u in unlock.units) {
                UnlockedStructures.Add(u.ID);
            }
        }
    }
    public HashSet<String> GetUnlockedStructureNeeds(int level) {
        return UnlockedStructureNeeds[level];
    }
    public HashSet<String> GetALLUnlockedStructureNeedsTill(int level) {
        HashSet<String> needs = new HashSet<String>();
        for (int i = 0; i <= level; i++) {
            needs.UnionWith(UnlockedStructureNeeds[i]);
        }
        return needs;
    }
    public bool HasUnlockedAllNeeds(int level) {
        return UnlockedItemNeeds[level].Count + UnlockedStructureNeeds[level].Count == PrototypController.Instance.GetNeedCountLevel(level);
    }

    public bool HasNeedUnlocked(Need need) {
        if (need.IsItemNeed())
            return UnlockedItemNeeds[need.StartLevel].Contains(need.ID);
        return UnlockedStructureNeeds[need.StartLevel].Contains(need.ID);
    }
    public void AddTradeRoute(TradeRoute route) {
        if (TradeRoutes == null)
            TradeRoutes = new List<TradeRoute>();
        TradeRoutes.Add(route);
    }
    public bool RemoveTradeRoute(TradeRoute route) {
        route.Destroy();
        return TradeRoutes.Remove(route);
    }
    public void ReduceTreasure(int money) {
        if (money < 0) {
            return;
        }
        TreasuryBalance -= money;
        CheckIfLost();
    }
    public void AddToTreasure(int money) {
        if (money < 0) {
            return;
        }
        TreasuryBalance += money;
    }
    public void ReduceTreasureChange(int amount) {
        if (amount < 0) {
            return;
        }
        TreasuryChange -= amount;
    }
    public void AddTreasureChange(int amount) {
        if (amount < 0) {
            return;
        }
        TreasuryChange += amount;
    }
    public void OnCityCreated(City city) {
        if (city.PlayerNumber != Number)
            return;
        Cities.Add(city);
        city.RegisterStructureAdded(OnStructureAdded);
        city.RegisterCityDestroy(OnCityDestroy);
        cbCityCreated?.Invoke(city);
    }
    public void OnCityDestroy(City city) {
        city.UnregisterStructureAdded(OnStructureAdded);
        Cities.Remove(city);
        cbCityDestroy?.Invoke(city);
        CheckIfLost();
    }

    private void CheckIfLost() {
        if (TreasuryBalance < PlayerPrototypeData.maximumDebt)
            _hasLost = true;
        if (Cities.Count == 0 && Ships.Count == 0)
            _hasLost = true;
        if(_hasLost)
            cbHasLost?.Invoke(this);
    }

    public void OnStructureAdded(Structure structure) {
        structure.RegisterOnDestroyCallback(OnStructureLost);
        AllStructures.Add(structure);
        cbNewStructure?.Invoke(structure);
    }
    public void OnStructureLost(Structure structure, IWarfare destroyer) {
        //dosmth
        structure.UnregisterOnDestroyCallback(OnStructureLost);
        AllStructures.Remove(structure);
        cbLostStructure?.Invoke(structure);
    }
    public void OnUnitCreated(Unit unit) {
        if (unit.playerNumber != Number)
            return;
        unit.RegisterOnDestroyCallback(OnUnitDestroy);
        Units.Add(unit);
        if (unit.IsShip)
            Ships.Add((Ship)unit);
    }
    public void OnUnitDestroy(Unit unit, IWarfare warfare) {
        //dosmth
        unit.UnregisterOnDestroyCallback(OnUnitDestroy);
        Units.Remove(unit);
        if (unit.IsShip)
            Ships.Remove((Ship)unit);
        CheckIfLost();
    }
    public void UnregisterNeedUnlock(Action<Need> callbackfunc) {
        cbNeedUnlocked -= callbackfunc;
    }
    public void RegisterNeedUnlock(Action<Need> callbackfunc) {
        cbNeedUnlocked += callbackfunc;
    }
    public void UnregisterHasLost(Action<Player> callbackfunc) {
        cbHasLost -= callbackfunc;
    }

    public void RegisterHasLost(Action<Player> callbackfunc) {
        cbHasLost += callbackfunc;
    }

    public void UnregisterNewStructure(Action<Structure> callbackfunc) {
        cbNewStructure -= callbackfunc;
    }
    public void RegisterNewStructure(Action<Structure> callbackfunc) {
        cbNewStructure += callbackfunc;
    }
    public void UnregisterLostStructure(Action<Structure> callbackfunc) {
        cbLostStructure -= callbackfunc;
    }
    public void RegisterLostStructure(Action<Structure> callbackfunc) {
        cbLostStructure += callbackfunc;
    }
    public void UnregisterCityDestroy(Action<City> callbackfunc) {
        cbCityDestroy -= callbackfunc;
    }
    public void RegisterCityDestroy(Action<City> callbackfunc) {
        cbCityDestroy += callbackfunc;
    }
    public void UnregisterCityCreated(Action<City> callbackfunc) {
        cbCityCreated -= callbackfunc;
    }
    public void RegisterCityCreated(Action<City> callbackfunc) {
        cbCityCreated += callbackfunc;
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

