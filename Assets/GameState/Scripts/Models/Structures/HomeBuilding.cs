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

    //Should Probably not be a list but lazy fast solution for now
    public List<Need> structureNeeds;

    public int MaxLivingSpaces {get{ return HomeData.maxLivingSpaces;}}
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
        structureNeeds = new List<Need>();
        AddStructureNeeds(City.GetOwner().GetCopyStructureNeeds(buildingLevel));

        City.GetOwner().RegisterStructureNeedUnlock(OnStructureNeedUnlock);
        City.AddPeople (buildingLevel,people);
	}

    private void AddStructureNeeds(List<Need> list) {
        structureNeeds.AddRange(list);

    }

    private void OnStructureNeedUnlock(Need obj) {
        if(obj.StartLevel > buildingLevel) {
            return;
        }
        structureNeeds.Add(obj);
    }

    private void OnNeedsBuildingChange(Tile tile, NeedsBuilding type, bool add) {
        foreach (Need ng in structureNeeds) {
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
        float allPercentage = City.GetHappinessForCitizenLevel(buildingLevel);
		float structurePercentage = 0;

		bool percCritical = City.GetNeedCriticalForLevel(buildingLevel);

        int count = structureNeeds.Count;
        if (count > 0) {
            foreach (Need n in structureNeeds) {
                structurePercentage += n.GetFullfiment(buildingLevel);
            }
            structurePercentage /= count;
            allPercentage += structurePercentage;
			allPercentage /= 2;
		}

		if (allPercentage < 0.4f || percCritical) {
			decTimer += deltaTime;
			incTimer -= deltaTime;
			incTimer = Mathf.Clamp (incTimer, 0, IncreaseTime);
			if (decTimer >= DecreaseTime) {
				TryToDecreasePeople();
				decTimer = 0;
			}
		} 
		else if (allPercentage > 0.4f && allPercentage < 0.85f) {
			incTimer -= deltaTime;
			incTimer = Mathf.Clamp (incTimer, 0, IncreaseTime);
			decTimer -= deltaTime;
			decTimer = Mathf.Clamp (decTimer, 0, DecreaseTime);
		}  
		else if (allPercentage > 0.85f) {
			incTimer += deltaTime;
			decTimer -= deltaTime;
			decTimer = Mathf.Clamp (decTimer, 0, DecreaseTime);
			if (incTimer >= IncreaseTime) {
				incTimer = 0;
				if(people==MaxLivingSpaces && City.GetOwner().HasUnlockedAllNeeds(buildingLevel)){
					canUpgrade = true;
                    OpenExtraUI();
				}
				TryToIncreasePeople ();
			}
		}
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
        structureNeeds.AddRange(needs);
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
