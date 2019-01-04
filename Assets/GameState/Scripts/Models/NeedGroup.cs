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

[JsonObject(MemberSerialization.OptIn)]
public class NeedGroup {
    #region Prototype
    protected NeedGroupPrototypData _prototypData;
    public NeedGroupPrototypData Data {
        get {
            if (_prototypData == null) {
                _prototypData = PrototypController.Instance.GetNeedGroupPrototypDataForID(ID);
            }
            return _prototypData;
        }
    }
    public float ImportanceLevel => Data.ImportanceLevel;
    public string Name => Data.Name;
    #endregion
    [JsonPropertyAttribute] public List<Need> Needs;
    [JsonPropertyAttribute] public float LastFullfillmentPercentage;
    #region Runtime
    public bool HasMissingNeed { get; internal set; }
    public readonly int ID;
    public List<Need> CombinedNeeds;
    #endregion

    public NeedGroup(int ID) {
        Needs = new List<Need>();
        this.ID = ID;
        CombinedNeeds = new List<Need>();
    }

    public NeedGroup(NeedGroup needGroup) {
        _prototypData = needGroup.Data;
        Needs = new List<Need>();
        foreach (Need n in needGroup.Needs) {
            Needs.Add(n.Clone());
        }
        CombinedNeeds = new List<Need>();

    }

    public NeedGroup Clone() {
        return new NeedGroup(this);
    }
    public void AddNeeds(IEnumerable<Need> need) {
        Needs.AddRange(need);
    }

    //public float GetFullfilledPercantage() {
    //    float currentValue = 0;
    //    foreach (Need n in Needs) {
    //        currentValue += n.GetCombinedFullfillment();
    //    }
    //    currentValue /= Needs.Count;
    //    currentValue *= ImportanceLevel;
    //    return currentValue;
    //}

    internal void CalculateFullfillment(City city, PopulationLevel populationLevel) {
        float currentValue = 0;
        int number = 0;
        foreach (Need need in Needs) {
            if (need.IsStructureNeed()) {
                continue;
            }
            number++;
            need.CalculateFullfillment(city, populationLevel);
            currentValue += need.GetCombinedFullfillment();
        }
        currentValue = CalculateRealPercantage(currentValue, number);
        LastFullfillmentPercentage = currentValue; // currently not needed! but maybe nice to have
    }

    public void CombineGroup(NeedGroup ng) {
        CombinedNeeds.AddRange(ng.Needs);
    }

    internal void AddNeed(Need need) {
        Needs.Add(need);
        CombinedNeeds.Add(need);
    }

    internal void UpdateNeeds(Player player) {
        foreach(Need need in Needs) {
            if (player.HasUnlockedNeed(need) == false) {
                Needs.Remove(need);
            }
            if (need.Exists() || need.IsStructureNeed()) {
                Needs.Remove(need);
            }
        }
    }

    private float CalculateRealPercantage(float percentage, int number) {
        percentage /= number;
        percentage = percentage * Mathf.Clamp(ImportanceLevel,0.4f,1.6f);
        return percentage;
    }

    internal float GetFullfillmentForHome(HomeBuilding homeBuilding) {
        float currentValue = 0;
        foreach (Need need in Needs) {
            if (need.IsStructureNeed()) {
                currentValue += homeBuilding.StructureNeeds.Find(x => x.ID == need.ID).GetCombinedFullfillment();
            } else {
                currentValue += need.GetCombinedFullfillment();
            }
        }
        return CalculateRealPercantage(currentValue, Needs.Count);
    }
}
