using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;

[JsonObject]
public class PopulationLevelPrototypData : LanguageVariables {
    public List<NeedGroup> needGroupList;
    public int Level;
}
[JsonObject(MemberSerialization.OptIn)]
public class PopulationLevel  {
    #region Serialize
    [JsonPropertyAttribute] public int populationCount = 0;
    [JsonPropertyAttribute] public bool criticalMissingNeed = false;
    [JsonPropertyAttribute] public int Level;
    #endregion
    #region Runtime
    protected PopulationLevelPrototypData _Data;
    public PopulationLevelPrototypData Data {
        get {
            //if (_homeData == null) {
            //    _homeData = (HomePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            //}
            return _Data;
        }
    }
    List<NeedGroup> NeedGroupList => Data.needGroupList;

    public float Happiness { get; internal set; }

    City city;
    #endregion
    public PopulationLevel() {

    }

    internal void CalculateHappiness(City city) {
        float fullfilled = 0;
        bool missingNeed = false;
        foreach(NeedGroup group in NeedGroupList) {
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
}
