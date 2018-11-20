using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class HomePrototypeData : StructurePrototypeData {
	public int maxLivingSpaces;
	public float increaseTime;
	public float decreaseTime;
}


[JsonObject(MemberSerialization.OptIn)]
public class HomeBuilding : TargetStructure {
    enum CitizienMoods { Mad, Neutral, Happy }
    #region Serialize
    [JsonPropertyAttribute] public int people;
	[JsonPropertyAttribute] public int buildingLevel;
	[JsonPropertyAttribute] public float decTimer;
	[JsonPropertyAttribute] public float incTimer;

	#endregion
	#region RuntimeOrOther
	protected HomePrototypeData _homeData;
	public HomePrototypeData HomeData {
		get { if(_homeData==null){
				_homeData = (HomePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID (ID);
			}
			return _homeData;
		}
	}
    CitizienMoods currentMood;
    //Should Probably not be a list but lazy fast solution for now
    public List<Need> StructureNeeds;
    //List containing the groups which contains those needs this structure has
    public List<NeedGroup> NeedGroups;

    public int MaxLivingSpaces {get{ return HomeData.maxLivingSpaces; }}
	public float IncreaseTime {get{ return HomeData.increaseTime; }}
	public float DecreaseTime {get{ return HomeData.decreaseTime; }}
	bool canUpgrade;
	#endregion


	public HomeBuilding(int pid, HomePrototypeData proto){
		this.ID = pid;
		this._homeData = proto;
		people = 1;
	}
	protected HomeBuilding(HomeBuilding b){
		BaseCopyData (b);
		people = 1;
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public HomeBuilding(){}


	public override Structure Clone (){
		return new HomeBuilding (this);
	}

	public override void OnBuild(){
		foreach (Tile t in neighbourTiles) {
			t.RegisterTileOldNewStructureChangedCallback (OnTStructureChange);
		}
        foreach(Tile t in myBuildingTiles) {
            ((LandTile)t).RegisterOnNeedStructureChange(OnNeedsBuildingChange);
            foreach(NeedsBuilding ns in t.GetListOfInRangeCityNeedBuildings()) {
                OnNeedsBuildingChange(t, ns, true);
            }
        }
        StructureNeeds = new List<Need>();
        AddStructureNeeds(City.GetOwner().GetCopyStructureNeeds(buildingLevel));
        AddNeedsToGroup(City.GetPopulationALLNeedGroups(buildingLevel));
        City.GetOwner().RegisterStructureNeedUnlock(OnNeedUnlock);
        City.GetPopulationLevel(buildingLevel).RegisterNeedUnlock(OnNeedUnlock);
        City.AddPeople (buildingLevel,people);
	}

    private void AddNeedsToGroup(IEnumerable<NeedGroup> list) {
        //this is gonna be ugly! But for now lazy implementation
        //until i think about something better.
        foreach(NeedGroup ng in list) {
            NeedGroup inList = NeedGroups.Find(x => x.ID == ng.ID);
            if(inList == null) {
                inList = ng.Clone();
                NeedGroups.Add(inList);
            }
            inList.CombineGroup(ng);
        }
    }

    private void AddStructureNeeds(List<Need> list) {
        foreach(Need n in list) {
            OnNeedUnlock(n);
        }
    }

    private void OnNeedUnlock(Need need) {
        if(need.StartLevel > buildingLevel) {
            return;
        }
        if (need.IsStructureNeed()) {
            need = need.Clone();
            StructureNeeds.Add(need);
        }
        NeedGroup ng = NeedGroups.Find(x => x.ID == need.Group.ID);
        if (ng == null) {
            ng = (new NeedGroup(need.Group.ID));
            NeedGroups.Add(ng);
        }
        ng.AddNeed(need);
    }

    private void OnNeedsBuildingChange(Tile tile, NeedsBuilding type, bool add) {
        foreach (Need ng in StructureNeeds) {
            if (ng.Structure.ID == type.ID) {
                ng.SetStructureFullfilled(false);
            }
        }
    }

    public override void Update (float deltaTime) {
		if(City==null||City.playerNumber==-1){
			//here the people are very unhappy and will leave veryfast
			return;
		}
        OpenExtraUI();
        float summedFullfillment = 0f;
        foreach(NeedGroup ng in NeedGroups) {
            summedFullfillment += ng.LastFullfillmentPercentage;
        }

        float percentage = summedFullfillment /= NeedGroups.Count;
        
        if(percentage > 0.9f) {
            currentMood = CitizienMoods.Happy;
        } else 
        if(percentage > 0.5) {
            currentMood = CitizienMoods.Neutral;
        }
        else {
            currentMood = CitizienMoods.Mad;
        }

        UpdatePeopleChange(deltaTime);

  //      int count = structureNeeds.Count;
  //      if (count > 0) {

        //          if (allPercentage < 0.4f || percCritical) {
        //	decTimer += deltaTime;
        //	incTimer -= deltaTime;
        //	incTimer = Mathf.Clamp (incTimer, 0, IncreaseTime);
        //	if (decTimer >= DecreaseTime) {
        //		TryToDecreasePeople();
        //		decTimer = 0;
        //	}
        //} 
        //else if (allPercentage > 0.4f && allPercentage < 0.85f) {
        //	incTimer -= deltaTime;
        //	incTimer = Mathf.Clamp (incTimer, 0, IncreaseTime);
        //	decTimer -= deltaTime;
        //	decTimer = Mathf.Clamp (decTimer, 0, DecreaseTime);
        //}  
        //else if (allPercentage > 0.85f) {
        //	incTimer += deltaTime;
        //	decTimer -= deltaTime;
        //	decTimer = Mathf.Clamp (decTimer, 0, DecreaseTime);
        //	if (incTimer >= IncreaseTime) {
        //		incTimer = 0;
        //		if(people==MaxLivingSpaces && City.GetOwner().HasUnlockedAllNeeds(buildingLevel)){
        //			canUpgrade = true;
        //                  OpenExtraUI();
        //		}
        //		TryToIncreasePeople ();
        //	}
        //}
    }

    private void TryToIncreasePeople(){
		if(people>=MaxLivingSpaces){
			return;
		}
		people++;
		City.AddPeople (buildingLevel,1);
	}
	private void TryToDecreasePeople(){
		if(people<=0){
			return;
		}
		people--;
		City.RemovePeople (buildingLevel,1);
	}
	public void OnTStructureChange(Structure now, Structure old){
		if(old is Road == false || now is Road == false){
			return;
		}
	}
	public bool IsStructureNeedFullfilled(Need need){
		if(need.IsItemNeed()){
			Debug.LogError ("wrong need got called here! " + need.ID);
			return false;
		}
		return IsInRangeOf (need.Structure);
	}
	public bool IsInRangeOf(NeedsBuilding str){
		if(str==null){
			return false;
		}
		List<NeedsBuilding> strs = new List<NeedsBuilding> ();
		foreach (Tile item in myBuildingTiles) {
			if(item.GetListOfInRangeCityNeedBuildings()==null){
				continue;
			}
			strs.AddRange (item.GetListOfInRangeCityNeedBuildings());
		}
		if(strs.Count==0){
			return false;
		}
		return strs.Contains (str);
	}
	protected override void OnCityChange (City old, City newOne) {
		old.RemovePeople (buildingLevel, people);
		newOne.AddPeople (buildingLevel, people);
        NeedGroups.Clear();
        StructureNeeds.Clear();
        foreach(Need n in newOne.GetOwner().GetALLUnlockedStructureNeedsTill(buildingLevel)) {
            OnNeedUnlock(n);
        }
        AddNeedsToGroup(newOne.GetPopulationALLNeedGroups(buildingLevel));

    }

    protected void UpdatePeopleChange(float deltaTime) {
        switch (currentMood) {
            case CitizienMoods.Mad:
                incTimer = Mathf.Clamp(incTimer - deltaTime, 0, IncreaseTime);
                decTimer = Mathf.Clamp(decTimer + deltaTime, 0, DecreaseTime);
                break;
            case CitizienMoods.Neutral:
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

    protected override void OnDestroy () {
		City.RemovePeople (buildingLevel,people);
	}
	public void UpgradeHouse(){
		if(canUpgrade==false&&IsMaxLevel()){
			return;
		}
		ID += 1; // we need to change to the other House type
		buildingLevel += 1;
		_homeData = (HomePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID (ID);
		cbStructureChanged (this);

        List<Need> needs = City.GetOwner().GetCopyStructureNeeds(buildingLevel);
        foreach(Need n in needs) {
            if (IsInRangeOf(n.Structure)) {
                n.SetStructureFullfilled(true);
            } else {
                n.SetStructureFullfilled(false);
            }
        }
        StructureNeeds.AddRange(needs);
//		Homedata.maxLivingSpaces *= 2; // TODO load this in from somewhere
		canUpgrade = false;
	}
	public void DowngradeHouse(){
		buildingLevel -= 1;
//		Homedata.maxLivingSpaces /= 2; // TODO load this in from somewhere

	}
	public bool BuildingCanBeUpgraded(){
		return IsMaxLevel () == false && canUpgrade;
	}
	public bool IsMaxLevel(){
		return ID == 23; //TODO Change this to smth flexibel not static check
	}
}
