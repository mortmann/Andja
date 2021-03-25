using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

public class FertilityPrototypeData : LanguageVariables, IWeighted {
    public string ID;
    public Climate[] climates;
    public float percentageOfIslands;
    [Ignore] public HashSet<string> ItemsDependentOnThis = new HashSet<string>();
    [Ignore] public int UnlockLevel;
    [Ignore] public int UnlockPopulationCount;

    public float GetStartWeight() {
        return percentageOfIslands;
    }
    public float GetCurrentWeight(int maximumSelect) {
        return Mathf.Clamp(percentageOfIslands - generated / maximumSelect, 0.01f, 1);
    }
    int generated;
    public float Select(int maximumSelect) {
        float old = GetCurrentWeight(maximumSelect);
        generated++;
        return old - GetCurrentWeight(maximumSelect);
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class Fertility : IComparable<Fertility>, IEqualityComparer<Fertility> {
    [JsonPropertyAttribute] public string ID;
    
    protected FertilityPrototypeData _prototypData;
    public FertilityPrototypeData Data {
        get {
            if (_prototypData == null) {
                _prototypData = PrototypController.Instance.GetFertilityPrototypDataForID(ID);
            }
            return _prototypData;
        }
    }
    public string Name {
        get { return Data.Name; }
    }
    public Climate[] Climates {
        get { return Data.climates; }
    }
    public Fertility() {

    }

    public Fertility(string ID, FertilityPrototypeData fpd) {
        this.ID = ID;
        this._prototypData = fpd;
    }

    #region IComparable implementation
    public int CompareTo(Fertility other) {
        return ID.CompareTo(other.ID);
    }
    #endregion

    #region IEqualityComparer implementation

    public bool Equals(Fertility x, Fertility y) {
        return x.ID == y.ID;
    }
    public int GetHashCode(Fertility obj) {
        return GetHashCode();
    }
    #endregion
    public override bool Equals(object obj) {
        Fertility f = obj as Fertility;
        if (f == null)
            return false;
        return f.ID == ID;
    }
    public override int GetHashCode() {
        return base.GetHashCode();
    }
}

public class FertilityCost {
    public List<Fertility> fertilities;
    public FertilityCost(params Fertility[] required) {
        fertilities = new List<Fertility>(required);
    } 
    public bool Fullfills(Fertility fert) {
        return fertilities.Contains(fert);
    }
}