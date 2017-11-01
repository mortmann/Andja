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

	public int maxLivingSpaces {get{ return HomeData.maxLivingSpaces;}}
	public float increaseSpeed {get{ return HomeData.increaseSpeed;}}
	public float decreaseSpeed {get{ return HomeData.decreaseSpeed;}}
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
		HashSet<Tile> neighbourTiles = new HashSet<Tile> ();
		foreach (Tile item in myBuildingTiles) {
			foreach(Tile nbt in item.GetNeighbours()){
				if (myBuildingTiles.Contains (nbt) == false) {
					neighbourTiles.Add (nbt);
				}
			}
		}
		foreach (Tile t in neighbourTiles) {
			t.RegisterTileStructureChangedCallback (OnTStructureChange);
		}
		City.addPeople (buildingLevel,people);

	}

	public override void update (float deltaTime) {
		if(City==null||City.playerNumber==-1){
			//here the people are very unhappy and will leave veryfast
			return;
		}
		float allPercentage = 0;
		float structurePercentage = 0;
		int count = 0;
		bool percCritical = City.getNeedCriticalForLevel(buildingLevel);
		Player myPlayer = PlayerController.Instance.GetPlayer (playerNumber);
		foreach (Need n in myPlayer.getUnlockedStructureNeeds(buildingLevel)) {
			Player pc = PlayerController.Instance.GetPlayer (playerNumber);
			if (n.startLevel <= buildingLevel && n.popCount <= pc.maxPopulationCount) {
				if(isInRangeOf (n.structure)){
					structurePercentage += 1;
				}
				count++;
			}
		}
		if (count == 0) {
			allPercentage = City.getHappinessForCitizenLevel (buildingLevel);
		} else {
			allPercentage = City.getHappinessForCitizenLevel (buildingLevel) + structurePercentage;
			structurePercentage /= count; 
			allPercentage /= 2;
		}

		if (allPercentage < 0.4f || percCritical) {
			decTimer += deltaTime;
			incTimer -= deltaTime;
			incTimer = Mathf.Clamp (incTimer, 0, increaseSpeed);
			if (decTimer >= decreaseSpeed) {
				TryToDecreasePeople();
				decTimer = 0;
			}
		} 
		else if (allPercentage > 0.4f && allPercentage < 0.85f) {
			incTimer -= deltaTime;
			incTimer = Mathf.Clamp (incTimer, 0, increaseSpeed);
			decTimer -= deltaTime;
			decTimer = Mathf.Clamp (decTimer, 0, decreaseSpeed);
		}  
		else if (allPercentage > 0.85f) {
			incTimer += deltaTime;
			decTimer -= deltaTime;
			decTimer = Mathf.Clamp (decTimer, 0, decreaseSpeed);
			if (incTimer >= increaseSpeed) {
				incTimer = 0;
				if(people==maxLivingSpaces && myPlayer.hasUnlockedAllNeeds(buildingLevel)){
					canUpgrade = true;
				}
				TryToIncreasePeople ();
			}
		}
	}

	private void TryToIncreasePeople(){
		if(people>=maxLivingSpaces){
			return;
		}
		people++;
		City.addPeople (buildingLevel,1);
	}
	private void TryToDecreasePeople(){
		if(people<=0){
			return;
		}
		people--;
		City.removePeople (buildingLevel,1);
	}
	public void OnTStructureChange(Structure now, Structure old){
		if(old is Road == false || now is Road == false){
			return;
		}
	}
	public bool isStructureNeedFullfilled(Need need){
		if(need.IsItemNeed()){
			Debug.LogError ("wrong need got called here! " + need.ID);
			return false;
		}
		return isInRangeOf (need.structure);
	}
	public bool isInRangeOf(NeedsBuilding str){
		if(str==null){
			return false;
		}
		List<NeedsBuilding> strs = new List<NeedsBuilding> ();
		foreach (Tile item in myBuildingTiles) {
			if(item.getListOfInRangeNeedBuildings()==null){
				continue;
			}
			strs.AddRange (item.getListOfInRangeNeedBuildings());
		}
		if(strs.Count==0){
			return false;
		}
		return strs.Contains (str);
	}
	protected override void OnCityChange (City old, City newOne) {
		old.removePeople (buildingLevel, people);
		newOne.addPeople (buildingLevel, people);
	}
	protected override void OnDestroy () {
		City.removePeople (buildingLevel,people);
	}
	public void UpgradeHouse(){
		if(canUpgrade==false&&IsMaxLevel()){
			return;
		}
		ID += 1; // we need to change to the other House type
		buildingLevel += 1;

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
