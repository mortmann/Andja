using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;

public class NeedPrototypeData : LanguageVariables {
    public Item item;
    public NeedStructure[] structures;
    public NeedGroupPrototypData group; // only for the typ of the group needed!
    public float[] UsageAmounts;
    public int startLevel;
    public int startPopulationCount;
    public bool hasToReachPerRoad;
    [Ignore] public Dictionary<Produce, int[]> produceForPeople; 
}
/// <summary>
/// each need needs to be in a group with similar needs
/// those group have a priority level and each member has a "priority in its group"
/// how important it is for its owngroup fullfillment
/// </summary>

[JsonObject(MemberSerialization.OptIn)]
public class Need {
    protected NeedPrototypeData _prototypData;
    public NeedPrototypeData Data {
        get {
            if (_prototypData == null) {
                _prototypData = PrototypController.Instance.GetNeedPrototypDataForID(ID);
            }
            return _prototypData;
        }
    }
    public NeedGroupPrototypData Group {
        get { return Data.group; }
    }
    public string Name {
        get { return Data.Name; }
    }
    public Item Item {
        get { return Data.item; }
    }
    public NeedStructure[] Structures {
        get { return Data.structures; }
    }

    public float[] Uses {
        get { return Data.UsageAmounts; }
    }
    public int StartLevel {
        get { return Data.startLevel; }
    }
    public bool HasToReachPerRoad => Data.hasToReachPerRoad;
    public int StartPopulationCount {
        get { return Data.startPopulationCount; }
    }
    [JsonPropertyAttribute]
    public string ID;
    [JsonPropertyAttribute]
    public float[] lastNeededNotConsumed;
    [JsonPropertyAttribute]
    public float[] percantageAvailability;
    [JsonPropertyAttribute]
    public float notUsedOfTon = 0;
    
    public Need(string id, NeedPrototypeData npd) : this() {
        this.ID = id;
        this._prototypData = npd;
    }
    [JsonConstructor]
    public Need() {
        if(lastNeededNotConsumed == null) {
            lastNeededNotConsumed = new float[PrototypController.Instance.NumberOfPopulationLevels];
            percantageAvailability = new float[PrototypController.Instance.NumberOfPopulationLevels];
        } else {
            if(lastNeededNotConsumed.Length < PrototypController.Instance.NumberOfPopulationLevels) {
                Array.Resize(ref lastNeededNotConsumed, PrototypController.Instance.NumberOfPopulationLevels);
                Array.Resize(ref percantageAvailability, PrototypController.Instance.NumberOfPopulationLevels);
            }
        }
    }
    public Need Clone() {
        return new Need(this);
    }
    protected Need(Need other) : this() {
        this.ID = other.ID;
    }

    public Need(string id) {
        this.ID = id;
    }

    public void CalculateFullfillment(City city, PopulationLevel level) {
        TryToConsumThisIn(city, level.populationCount, level.Level);
    }

    public void TryToConsumThisIn(City city, int people, int level) {
        if(people == 0) {
            return;
        }
        if (Item == null) {
            //this does not require any item -> it needs a structure
            percantageAvailability[level] = 0;
            return;
        }
        float neededConsumAmount = 0;
        // how much do we need to consum?
        neededConsumAmount += Uses[level] * ((float)people);
        if (neededConsumAmount <= 0) {
            //we dont need anything to consum so no need to go anyfurther
            percantageAvailability[level] = 0;
            return;
        }
        //IF it has still enough no need to calculate more
        if (notUsedOfTon >= neededConsumAmount) {
            notUsedOfTon -= neededConsumAmount;
            percantageAvailability[level] = 1;
            return;
        }
        //so just remove from needed what we have
        neededConsumAmount -= notUsedOfTon;
        notUsedOfTon = 0;
        //now we have a amount that still needs to be fullfilled by the cities inventory

        float availableAmount = city.GetAmountForThis(Item);
        //either we need to get 1 ton or as much as we need
        neededConsumAmount = Mathf.CeilToInt(neededConsumAmount);
        //now how much do we have in the city
        //if we have none?
        if (availableAmount == 0) {
            //we can just set the lastneeded to current needed
            lastNeededNotConsumed[level] = neededConsumAmount;
            percantageAvailability[level] = 0;
            return;
        }

        // how much to we consum of the avaible?
        float usedAmount = Mathf.Clamp(availableAmount, 0, neededConsumAmount);
        //now remove that amount of items
        if(usedAmount>neededConsumAmount)
            notUsedOfTon = usedAmount - neededConsumAmount;

        city.RemoveResource(Item, Mathf.CeilToInt(usedAmount));

        //minimum is 1 because if 0 -> ERROR due dividing through 0
        //calculate the percantage of availability
        percantageAvailability[level] = Mathf.RoundToInt(100 * (usedAmount / neededConsumAmount)) / 100;
        Debug.Log("NEED " + ID + " -> " + level + " -> " + percantageAvailability[level]);
    }

    internal bool IsSatisifiedThroughStructure(List<NeedStructure> strs) {
        return Array.Exists(Structures, x => strs.Exists(y => y.ID == x.ID));
    }

    internal void SetStructureFullfilled(bool fullfilled) {
        if (IsItemNeed())
            return;
        if (fullfilled) {
            Array.ForEach(percantageAvailability, x => x = 1);
        }
        else {
            Array.ForEach(percantageAvailability, x => x = 0);
        }
    }

    internal float GetFullfiment(int populationLevel) {
        return percantageAvailability[populationLevel];
    }
    internal float GetCombinedFullfillment() {
        float combined = 0f;
        foreach (float ff in percantageAvailability)
            combined += ff;
        combined /= percantageAvailability.Length;
        return combined;
    }

    public bool IsItemNeed() {
        return Item != null;
    }
    public bool IsStructureNeed() {
        return Structures != null;
    }

    public bool Exists() {
        return Data != null;
    }
    public override bool Equals(object obj) {
        Need item = obj as Need;
        if (item == null) {
            return false;
        }
        return this.ID.Equals(item.ID);
    }
    public override int GetHashCode() {
        return this.ID.GetHashCode();
    }
}
