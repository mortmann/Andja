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

    #region Runtime
    public bool HasMissingNeed { get; internal set; }
    public readonly int ID;
    public List<Need> CombinedNeeds;
    #endregion

    public NeedGroup(int ID) {
        Needs = new List<Need>();
        this.ID = ID;
    }

    public NeedGroup(NeedGroup needGroup) {
        _prototypData = needGroup.Data;
        Needs = new List<Need>();
        foreach (Need n in needGroup.Needs) {
            Needs.Add(n.Clone());
        }
    }

    public NeedGroup Clone() {
        return new NeedGroup(this);
    }
    public void AddNeeds(IEnumerable<Need> need) {
        Needs.AddRange(need);
    }

    public float GetFullfilledPercantage() {
        float currentValue = 0;
        foreach (Need n in Needs) {
            currentValue += n.GetCombinedFullfillment();
        }
        currentValue /= Needs.Count;
        currentValue *= ImportanceLevel;
        return currentValue;
    }

    internal void CalculateFullfillment(City city, PopulationLevel populationLevel) {
        foreach(Need need in Needs) {
            need.CalculateFullfillment(city, populationLevel);
        }
    }

    public void CombineGroup() {

    }

    internal void AddNeed(Need need) {
        Needs.Add(need);
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
}
