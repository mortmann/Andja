using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System;

public class HomePrototypeData : StructurePrototypeData {
    public int maxLivingSpaces;
    public int previouseMaxLivingSpaces = 0; // lower bound -> decreasing level
    public float increaseTime;
    public float decreaseTime;
}


[JsonObject(MemberSerialization.OptIn)]
public class HomeStructure : TargetStructure {
    public enum CitizienMoods { Mad, Neutral, Happy }
    #region Serialize
    [JsonPropertyAttribute] public int people;
    [JsonPropertyAttribute] public float decTimer;
    [JsonPropertyAttribute] public float incTimer;
    [JsonPropertyAttribute] public bool isAbandoned;
    #endregion
    #region RuntimeOrOther
    protected HomePrototypeData _homeData;
    public HomePrototypeData HomeData {
        get {
            if (_homeData == null) {
                _homeData = (HomePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            }
            return _homeData;
        }
    }
    public CitizienMoods currentMood { get; protected set; }
    List<NeedStructure> needStructures;
    public int PreviouseMaxLivingSpaces { get { return HomeData.previouseMaxLivingSpaces; } }
    public int MaxLivingSpaces { get { return HomeData.maxLivingSpaces; } }

    public float IncreaseTime { get { return CalculateRealValue(nameof(HomeData.increaseTime), HomeData.increaseTime); } }
    public float DecreaseTime { get { return CalculateRealValue(nameof(HomeData.decreaseTime), HomeData.decreaseTime); } }

    public bool CanUpgrade => MaxLivingSpaces == people // is full
                            && currentMood == CitizienMoods.Happy // still wants more people
                            && IsMaxLevel() // if there is smth to be upgraded to
                            && CanBeUpgraded // set through xml prototype file 
                            && City.HasEnoughOfItems(UpgradeItems) // city has enough items to build
                            && City.GetOwner().HasEnoughMoney(UpgradeCost)
                            && City.GetOwner().HasUnlockedAllNeeds(StructureLevel); // player has enough money

    internal List<NeedGroup> GetNeedGroups() {
        return City.GetPopulationNeedGroups(StructureLevel);
    }
    #endregion


    public HomeStructure(string pid, HomePrototypeData proto) {
        this.ID = pid;
        this._homeData = proto;
        people = 1;
    }
    protected HomeStructure(HomeStructure b) {
        BaseCopyData(b);
        people = 1;
    }
    /// <summary>
    /// DO NOT USE
    /// </summary>
    public HomeStructure() { }


    public override Structure Clone() {
        return new HomeStructure(this);
    }

    public override void OnBuild() {
        needStructures = new List<NeedStructure>();
        if (City.IsWilderness() == false) {
            OnCityChange(this, null, City);
        }
        foreach (Tile t in Tiles) {
            ((LandTile)t).RegisterOnNeedStructureChange(OnNeedStructureChange);
            List<NeedStructure> needsStructures = t.GetListOfInRangeCityNeedStructures();
            if (needsStructures == null)
                continue;
            foreach (NeedStructure ns in needsStructures) {
                OnNeedStructureChange(t, ns, true);
            }
        }
        RegisterOnOwnerChange(OnCityChange);
    }

    internal float GetTaxPercantage() {
        return City.GetPopulationLevel(StructureLevel).taxPercantage;
    }

    private void OnNeedStructureChange(Tile tile, NeedStructure type, bool add) {
        if(add) {
            needStructures.Add(type);
        } else {
            needStructures.Remove(type);
        }
    }

    public override void OnUpdate(float deltaTime) {
        if (City == null || City.IsWilderness()) {
            //here the people are very unhappy and will leave veryfast
            CloseExtraUI();
            currentMood = CitizienMoods.Mad;
            return;
        }
        float summedFullfillment = 0f;
        float summedImportance = 0;
        foreach (NeedGroup ng in GetNeedGroups()) {
            if (ng.IsUnlocked() == false)
                continue;
            summedFullfillment += ng.GetFullfillmentForHome(this);
            summedImportance += ng.ImportanceLevel;
        }
        float percentage = summedFullfillment / summedImportance;

        //Tax can offset some unhappines from missing stuff
        //this needs to be balanced tho
        //for now just 1:1 % from 1.5 to 0.5 happiness offset 
        float tax = Mathf.Clamp(City.GetPopulationLevel(PopulationLevel).taxPercantage, 0.5f, 1.5f);
        percentage += 2f- Mathf.Clamp(tax * tax, 0.5f, 2);
        percentage /= 2;
        if (percentage > 0.9f) {
            currentMood = CitizienMoods.Happy;
        }
        else
        if (percentage > 0.5) {
            CloseExtraUI();
            currentMood = CitizienMoods.Neutral;
        }
        else {
            CloseExtraUI();
            currentMood = CitizienMoods.Mad;
        }
        if(HasNegativEffect)
            currentMood = CitizienMoods.Mad;
        UpdatePeopleChange(deltaTime);
    }

    private void TryToIncreasePeople() {
        if (people >= MaxLivingSpaces) {
            return;
        }
        if (isAbandoned == true) {
            isAbandoned = false;
        }
        people++;
        City.AddPeople(StructureLevel, 1);
        if (currentMood == CitizienMoods.Happy && people == MaxLivingSpaces) {
            if(CanBeUpgraded) {
                OpenExtraUI();
                TryToUpgrade();
            }
        }
    }

    private void TryToDecreasePeople() {
        if (people <= 0) {
            isAbandoned = true;
            return;
        }
        people--;
        City.RemovePeople(StructureLevel, 1);
        if (people < PreviouseMaxLivingSpaces)
            DowngradeHouse();
    }

    private void TryToUpgrade() {
        if(City.AutoUpgradeHomes == false) {
            return;
        }
        //TODO: check for performance impact
        // if bad change to boolean in city that gets non frequent set
        if (City.HasEnoughOfItems(UpgradeItems) == false) {
            return;
        }
        if (City.GetOwner().HasEnoughMoney(UpgradeCost) == false) {
            return;
        }
        UpgradeHouse();
    }
    public override void OpenExtraUI() {
        if(CanBeUpgraded)
            base.OpenExtraUI();
    }
    public override void CloseExtraUI() {
        if (CanBeUpgraded == false)
            base.CloseExtraUI();
    }

    public bool IsStructureNeedFullfilled(Need need) {
        if (need.HasToReachPerRoad) {
            if (GetRoutes().Count == 0)
                return false;
            need.IsSatisifiedThroughStructure(needStructures.Where((x) => x.City == City && x.GetRoutes().Overlaps(GetRoutes())).ToList());
        }
        return need.IsSatisifiedThroughStructure(needStructures.Where((x)=> x.City == City).ToList());
    }

    protected void OnCityChange(Structure str, City old, City newOne) {
        if(old != null && old.IsWilderness() == false) {
            old.RemovePeople(StructureLevel, people);
        }
        if (newOne.IsWilderness() == false) {
            newOne.AddPeople(StructureLevel, people);
        }
    }

    protected void UpdatePeopleChange(float deltaTime) {
        switch (currentMood) {
            case CitizienMoods.Mad:
                if (isAbandoned == true)
                    return;
                incTimer = Mathf.Clamp(incTimer - deltaTime, 0, IncreaseTime);
                decTimer = Mathf.Clamp(decTimer + deltaTime, 0, DecreaseTime);
                break;
            case CitizienMoods.Neutral:
                if (isAbandoned == true)
                    return;
                incTimer = Mathf.Clamp(incTimer - deltaTime, 0, IncreaseTime);
                decTimer = Mathf.Clamp(decTimer - deltaTime, 0, DecreaseTime);
                break;
            case CitizienMoods.Happy:
                incTimer = Mathf.Clamp(incTimer + deltaTime, 0, IncreaseTime);
                decTimer = Mathf.Clamp(decTimer - deltaTime, 0, DecreaseTime);
                break;
        }
        if (incTimer >= IncreaseTime) {
            TryToIncreasePeople();
            incTimer = 0f;
        }
        if (decTimer >= DecreaseTime) {
            TryToDecreasePeople();
            decTimer = 0f;
        }
    }

    protected override void OnDestroy() {
        City.RemovePeople(StructureLevel, people);
    }
    public void UpgradeHouse() {
        if (CanUpgrade == false && IsMaxLevel()) {
            return;
        }
        CloseExtraUI();
        ID = PrototypController.Instance.GetStructureIDForTypeNeighbourStructureLevel(GetType(), StructureLevel, true);
        City.RemovePeople(StructureLevel, people);
        City.RemoveResources(UpgradeItems);
        City.GetOwner().ReduceTreasure(UpgradeCost);
        _homeData = null;
        _prototypData = null;
        City.AddPeople(StructureLevel, people);
        cbStructureChanged(this);
    }
    public void DowngradeHouse() {
        ID = PrototypController.Instance.GetStructureIDForTypeNeighbourStructureLevel(GetType(), StructureLevel, false);
        City.RemovePeople(StructureLevel, people);
        _homeData = null;
        _prototypData = null;
        City.AddPeople(StructureLevel, people);
        cbStructureChanged(this);
    }

    public bool IsMaxLevel() {
        return PrototypController.Instance.GetMaxStructureLevelForStructureType(GetType()) == StructureLevel;
    }

    protected override void AddSpecialEffect(Effect effect) {
        base.AddSpecialEffect(effect);
        
    }
    protected override void RemoveSpecialEffect(Effect effect) {
        base.RemoveSpecialEffect(effect);

    }
}
