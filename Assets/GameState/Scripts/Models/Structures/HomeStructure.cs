using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class HomePrototypeData : StructurePrototypeData {
    public int maxLivingSpaces;
    public int previouseMaxLivingSpaces = 0; // lower bound -> decreasing level
    public float increaseTime;
    public float decreaseTime;
}


[JsonObject(MemberSerialization.OptIn)]
public class HomeStructure : TargetStructure {
    enum CitizienMoods { Mad, Neutral, Happy }
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
    CitizienMoods currentMood;
    //Should Probably not be a list but lazy fast solution for now
    public List<Need> StructureNeeds;
    //List containing the groups which contains those needs this structure has
    public List<NeedGroup> NeedGroups;
    public int PreviouseMaxLivingSpaces { get { return HomeData.previouseMaxLivingSpaces; } }
    public int MaxLivingSpaces { get { return HomeData.maxLivingSpaces; } }

    public float IncreaseTime { get { return CalculateRealValue("IncreaseTime", HomeData.increaseTime); } }
    public float DecreaseTime { get { return CalculateRealValue("decreaseTime", HomeData.decreaseTime); } }

    public bool CanUpgrade => MaxLivingSpaces == people // is full
                            && currentMood == CitizienMoods.Happy // still wants more people
                            && IsMaxLevel() // if there is smth to be upgraded to
                            && CanBeUpgraded // set through xml prototype file 
                            && City.HasEnoughOfItems(UpgradeItems) // city has enough items to build
                            && City.GetOwner().HasEnoughMoney(UpgradeCost); // player has enough money
    #endregion


    public HomeStructure(int pid, HomePrototypeData proto) {
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
        foreach (Tile t in neighbourTiles) {
            t.RegisterTileOldNewStructureChangedCallback(OnTStructureChange);
        }
        StructureNeeds = new List<Need>();
        SetNeedGroups(City.GetPopulationALLNeedGroups(StructureLevel));
        AddStructureNeeds(City.GetOwner().GetCopyStructureNeeds(StructureLevel));

        City.GetOwner().RegisterStructureNeedUnlock(OnNeedUnlock);
        City.GetPopulationLevel(StructureLevel).RegisterNeedUnlock(OnNeedUnlock);
        City.AddPeople(StructureLevel, people);
        foreach (Tile t in myStructureTiles) {
            ((LandTile)t).RegisterOnNeedStructureChange(OnNeedStructureChange);
            List<NeedStructure> needsStructures = t.GetListOfInRangeCityNeedStructures();
            if (needsStructures == null)
                continue;
            foreach (NeedStructure ns in needsStructures) {
                OnNeedStructureChange(t, ns, true);
            }
        }
    }

    private void SetNeedGroups(IEnumerable<NeedGroup> list) {
        NeedGroups = new List<NeedGroup>(list);
    }
    private void AddStructureNeeds(List<Need> list) {
        foreach (Need n in list) {
            OnNeedUnlock(n);
        }
    }

    private void OnNeedUnlock(Need need) {
        if (need.StartLevel > StructureLevel) {
            return;
        }
        if (need.IsStructureNeed()) {
            need = need.Clone();
            StructureNeeds.Add(need);
        }
    }

    private void OnNeedStructureChange(Tile tile, NeedStructure type, bool add) {
        foreach (Need ng in StructureNeeds) {
            if (ng.IsSatisifiedThroughStructure(type)) {
                ng.SetStructureFullfilled(false);
            }
        }
    }

    public override void Update(float deltaTime) {
        base.Update(deltaTime);

        if (City == null || City.IsWilderness()) {
            //here the people are very unhappy and will leave veryfast
            currentMood = CitizienMoods.Mad;
            return;
        }
        float summedFullfillment = 0f;
        float summedImportance = 0;
        foreach (NeedGroup ng in NeedGroups) {
            summedFullfillment += ng.GetFullfillmentForHome(this);
            summedImportance += ng.ImportanceLevel;
        }
        float percentage = summedFullfillment / summedImportance;
        if (percentage > 0.9f) {
            currentMood = CitizienMoods.Happy;
        }
        else
        if (percentage > 0.5) {
            currentMood = CitizienMoods.Neutral;
        }
        else {
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
            OpenExtraUI();
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
    public void OnTStructureChange(Structure now, Structure old) {
        if (old is RoadStructure == false || now is RoadStructure == false) {
            return;
        }
    }
    public bool IsStructureNeedFullfilled(Need need) {
        if (need.IsItemNeed()) {
            Debug.LogError("wrong need got called here! " + need.ID);
            return false;
        }

        foreach (NeedStructure s in need.Structures) {
            if (IsInRangeOf(s)) {
                return true;
            }
        }
        return false;
    }
    public bool IsInRangeOf(NeedStructure str) {
        if (str == null) {
            return false;
        }
        List<NeedStructure> strs = new List<NeedStructure>();
        foreach (Tile item in myStructureTiles) {
            if (item.GetListOfInRangeCityNeedStructures() == null) {
                continue;
            }
            strs.AddRange(item.GetListOfInRangeCityNeedStructures());
        }
        if (strs.Count == 0) {
            return false;
        }
        return strs.Contains(str);
    }
    protected override void OnCityChange(City old, City newOne) {
        old.RemovePeople(StructureLevel, people);
        newOne.AddPeople(StructureLevel, people);
        NeedGroups.Clear();
        StructureNeeds.Clear();
        foreach (Need n in newOne.GetOwner().GetALLUnlockedStructureNeedsTill(StructureLevel)) {
            OnNeedUnlock(n);
        }
        SetNeedGroups(newOne.GetPopulationALLNeedGroups(StructureLevel));
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
        _homeData = null;
        City.RemoveRessources(BuildingItems);
        City.GetOwner().ReduceMoney(Buildcost);
        cbStructureChanged(this);

        List<Need> needs = City.GetOwner().GetCopyStructureNeeds(StructureLevel);
        foreach (Need n in needs) {
            if (IsStructureNeedFullfilled(n)) {
                n.SetStructureFullfilled(true);
            }
            else {
                n.SetStructureFullfilled(false);
            }
        }
        SetNeedGroups(City.GetPopulationALLNeedGroups(StructureLevel));
        StructureNeeds.AddRange(needs);
    }
    public void DowngradeHouse() {
        //Remove Structure Needs of the old level
        List<Need> needs = City.GetOwner().GetCopyStructureNeeds(StructureLevel);
        foreach (Need n in needs) {
            StructureNeeds.Remove(StructureNeeds.Find(x => x.ID == n.ID));
        }
        ID = PrototypController.Instance.GetStructureIDForTypeNeighbourStructureLevel(GetType(), StructureLevel, false);
        _homeData = null;
        cbStructureChanged(this);

        SetNeedGroups(City.GetPopulationALLNeedGroups(StructureLevel));
        StructureNeeds.AddRange(needs);
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
