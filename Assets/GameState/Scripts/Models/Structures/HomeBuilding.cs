using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class HomePrototypeData : StructurePrototypeData {
	public int maxLivingSpaces;
	public float increaseSpeed;
	public float decreaseSpeed;
}


[JsonObject(MemberSerialization.OptIn)]
public class HomeBuilding : Structure {
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

	public int MaxLivingSpaces {get{ return HomeData.maxLivingSpaces;}}
	public float IncreaseSpeed {get{ return HomeData.increaseSpeed;}}
	public float DecreaseSpeed {get{ return HomeData.decreaseSpeed;}}
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
        //doing it in "structure on build" already so if no problems remove it
		//HashSet<Tile> neighbourTiles = new HashSet<Tile> ();
		//foreach (Tile item in myBuildingTiles) {
		//	foreach(Tile nbt in item.GetNeighbours()){
		//		if (myBuildingTiles.Contains (nbt) == false) {
		//			neighbourTiles.Add (nbt);
		//		}
		//	}
		//}
		foreach (Tile t in neighbourTiles) {
			t.RegisterTileOldNewStructureChangedCallback (OnTStructureChange);
		}
		City.AddPeople (buildingLevel,people);

	}

	public override void Update (float deltaTime) {
		if(City==null||City.playerNumber==-1){
			//here the people are very unhappy and will leave veryfast
			return;
		}
		float allPercentage = 0;
		float structurePercentage = 0;
		int count = 0;
		bool percCritical = City.GetNeedCriticalForLevel(buildingLevel);
		Player myPlayer = PlayerController.Instance.GetPlayer (PlayerNumber);
		foreach (Need n in myPlayer.GetUnlockedStructureNeeds(buildingLevel)) {
			Player pc = PlayerController.Instance.GetPlayer (PlayerNumber);
			if (n.StartLevel <= buildingLevel && n.PopCount <= pc.MaxPopulationCount) {
				if(IsInRangeOf (n.Structure)){
					structurePercentage += 1;
				}
				count++;
			}
		}
		if (count == 0) {
			allPercentage = City.GetHappinessForCitizenLevel (buildingLevel);
		} else {
			allPercentage = City.GetHappinessForCitizenLevel (buildingLevel) + structurePercentage;
			structurePercentage /= count; 
			allPercentage /= 2;
		}

		if (allPercentage < 0.4f || percCritical) {
			decTimer += deltaTime;
			incTimer -= deltaTime;
			incTimer = Mathf.Clamp (incTimer, 0, IncreaseSpeed);
			if (decTimer >= DecreaseSpeed) {
				TryToDecreasePeople();
				decTimer = 0;
			}
		} 
		else if (allPercentage > 0.4f && allPercentage < 0.85f) {
			incTimer -= deltaTime;
			incTimer = Mathf.Clamp (incTimer, 0, IncreaseSpeed);
			decTimer -= deltaTime;
			decTimer = Mathf.Clamp (decTimer, 0, DecreaseSpeed);
		}  
		else if (allPercentage > 0.85f) {
			incTimer += deltaTime;
			decTimer -= deltaTime;
			decTimer = Mathf.Clamp (decTimer, 0, DecreaseSpeed);
			if (incTimer >= IncreaseSpeed) {
				incTimer = 0;
				if(people==MaxLivingSpaces && myPlayer.HasUnlockedAllNeeds(buildingLevel)){
					canUpgrade = true;
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
