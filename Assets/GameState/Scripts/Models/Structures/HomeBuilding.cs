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
				_homeData = (HomePrototypeData)PrototypController.Instance.GetPrototypDataForID (ID);
			}
			return _homeData;
		}
	}

	public int maxLivingSpaces {get{ return HomeData.maxLivingSpaces;}}
	public float increaseSpeed {get{ return HomeData.increaseSpeed;}}
	public float decreaseSpeed {get{ return HomeData.decreaseSpeed;}}
	public bool canUpgrade;
	#endregion


	public HomeBuilding(int pid, HomePrototypeData proto){
		this.ID = pid;
		this._homeData = proto;
	}
	protected HomeBuilding(HomeBuilding b){
		BaseCopyData (b);
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
	}

	public override void update (float deltaTime) {
		if(City==null||City.playerNumber==-1){
			//here the people are very unhappy and will leave veryfast
			return;
		}
		float allPercentage = 0;
		int count = 0;
		bool percCritical=false;
		foreach (Need n in City.allNeeds.Keys) {
			Player pc = PlayerController.Instance.GetPlayer (playerNumber);
			if (n.startLevel <= buildingLevel && n.popCount <= pc.maxPopulationCount) {
				if (n.structure == null) {
					allPercentage += City.allNeeds [n];
					if(City.allNeeds [n] < 0.4f){
						percCritical=true;
					}
				} else {
					if(isInRangeOf (n.structure)){
						allPercentage += 1;
					}
				}
				count++;
			}
		}
		allPercentage /= count; 
		if (allPercentage < 0.4f && percCritical) {
			decTimer -= deltaTime;
			incTimer += deltaTime;
			incTimer = Mathf.Clamp (incTimer, 0, increaseSpeed);

			if (decTimer <= 0) {
				people--;
			}
		} else
		if (allPercentage > 0.4f && allPercentage < 0.85f) {
			incTimer += deltaTime;
			incTimer = Mathf.Clamp (incTimer, 0, increaseSpeed);
			decTimer += deltaTime;
			decTimer = Mathf.Clamp (decTimer, 0, decreaseSpeed);
		} else 
		if (allPercentage > 0.85f) {
			incTimer -= deltaTime;
			decTimer += deltaTime;
			decTimer = Mathf.Clamp (decTimer, 0, decreaseSpeed);
			if (incTimer <= 0) {
				people++;
			}
		}
		people = Mathf.Clamp (people, 0, maxLivingSpaces);
	}
	public void OnTStructureChange(Tile t, Structure old){
		if(old is Road == false || t.Structure is Road == false){
			return;
		}
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

	public void UpgradeHouse(){
		if(canUpgrade==false){
			return;
		}
		buildingLevel += 1;
//		Homedata.maxLivingSpaces *= 2; // TODO load this in from somewhere
		canUpgrade = false;
	}
	public void DowngradeHouse(){
		buildingLevel -= 1;
//		Homedata.maxLivingSpaces /= 2; // TODO load this in from somewhere

	}
}
