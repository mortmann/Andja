using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;

[JsonObject]
public class PopulationLevelPrototypData : LanguageVariables {
    public List<NeedGroup> needGroupList; //not used as "copy" of the USED in "ticks" just as a reference which Needs gets unlocked with this level!
    public int Level; // cant be negative!
}
[JsonObject(MemberSerialization.OptIn)]
public class PopulationLevel  {
    #region Serialize
    [JsonPropertyAttribute] public int populationCount = 0;
    [JsonPropertyAttribute] public bool criticalMissingNeed = false;
    [JsonPropertyAttribute] public int Level;
    [JsonPropertyAttribute] public List<NeedGroup> NeedGroupList;
    [JsonPropertyAttribute] public PopulationLevel previousLevel;
    #endregion

    #region Runtime
    protected PopulationLevelPrototypData _Data;
    public PopulationLevelPrototypData Data {
        get {
            if (_Data == null) {
                _Data = (PopulationLevelPrototypData)PrototypController.Instance.GetPopulationLevelPrototypDataForLevel(Level);
            }
            return _Data;
        }
    }

    List<NeedGroup> _AllNeedGroupList;
    public List<NeedGroup> AllNeedGroupList {
        get {
            if (_AllNeedGroupList == null)
                _AllNeedGroupList = GetAllPreviousNeedGroups();
            return _AllNeedGroupList;
        }
    }

    public float Happiness { get; internal set; }

    City city;
    #endregion
    public PopulationLevel() {

    }

    public PopulationLevel(int level, City city, PopulationLevel previous) {
        this.Level = level;
        NeedGroupList = Data.needGroupList;
        this.previousLevel = previous;
        this.city = city;
        city.GetOwner().RegisterNeedUnlock(UnlockedNeed);
    }
    public PopulationLevel(PopulationLevel pl) {
        this.Level = pl.Level;
    }
    internal void CalculateHappiness(City city) {
        float fullfilled = 0;
        bool missingNeed = false;
        foreach(NeedGroup group in AllNeedGroupList) {
            group.CalculateFullfillment(city, this);
            fullfilled += group.GetFullfilledPercantage();
            if (group.HasMissingNeed)
                missingNeed = true;
        }
        criticalMissingNeed = missingNeed;
        fullfilled /= NeedGroupList.Count;
        //TODO: make it trend towards the happiness? so it doesnt swing like crazy
    }

    internal void AddPeople(int count) {
        populationCount += count;
    }

    internal void RemovePeople(int count) {
        populationCount -= count;
    }
    public List<NeedGroup> GetAllPreviousNeedGroups() {
        List<NeedGroup> temp = new List<NeedGroup>(NeedGroupList);
        if (previousLevel != null)
            temp.AddRange(previousLevel.GetAllPreviousNeedGroups());
        return temp;
    }
    internal PopulationLevel Clone() {
        return new PopulationLevel(this);
    }

    internal void Load() {
        UpdateNeeds();
        if (previousLevel.Exists() == false)
            previousLevel = city.GetPreviousPopulationLevel(Level);
    }

    private void UpdateNeeds() {
        if (NeedGroupList == null)
            NeedGroupList = new List<NeedGroup>();
        for (int i = 0; i < NeedGroupList.Count; i++) {
            if (Data.needGroupList.Contains(NeedGroupList[i]) == false) {
                NeedGroupList.Remove(NeedGroupList[i]);
            }
        }
        Player player = PlayerController.Instance.GetPlayer(city.playerNumber);
        player.RegisterNeedUnlock(UnlockedNeed);
        foreach (NeedGroup ng in Data.needGroupList) {
            NeedGroup inList = NeedGroupList.Find (x => x.ID == ng.ID);
            if (inList != null) {
                inList.UpdateNeeds(city.GetOwner());
                continue;
            }
            NeedGroupList.Add(ng);
        }
    }

    internal bool Exists() {
        return Data != null;
    }

    private void UnlockedNeed(Need need) {
        if (need.StartLevel != Level)
            return;
        NeedGroup ng = NeedGroupList.Find(x => x.ID == need.Group.ID);
        if(ng == null) {
            Debug.LogError("UnlockedNeed " + need + " doesnt have the right group inside this level" + Level );
            return;
        }
        ng.AddNeed(need.Clone());
    }
}
