using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;

[JsonObject]
public class NeedGroupPrototypData : LanguageVariables {
    public int ID;
    public float ImportanceLevel;

}

public class NeedGroup {
    #region Prototype
    public NeedGroup Data;
    public float ImportanceLevel => Data.ImportanceLevel;

    public bool HasMissingNeed { get; internal set; }
    #endregion
    #region Runtime

    List<Need> GroupNeeds;

    #endregion

    public NeedGroup() {
        GroupNeeds = new List<Need>();
    }

    public void AddNeed(Need need) {
        GroupNeeds.Add(need);
    }

    public float GetFullfilledPercantage() {
        float currentValue = 0;
        foreach (Need n in GroupNeeds) {
            currentValue += n.GetCombinedFullfillment();
        }
        currentValue /= GroupNeeds.Count;
        currentValue *= ImportanceLevel;
        return currentValue;
    }

    internal void CalculateFullfillment(City city, PopulationLevel populationLevel) {
        foreach(Need need in GroupNeeds) {
            need.CalculateFullfillment(city, populationLevel);
        }
    }
}
