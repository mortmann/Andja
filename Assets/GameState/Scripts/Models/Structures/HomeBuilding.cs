using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class HomeBuilding : Structure {
	#region Serialize

	[JsonPropertyAttribute] public int people;
	[JsonPropertyAttribute] public int buildingLevel;
	[JsonPropertyAttribute] public float decTimer;
	[JsonPropertyAttribute] public float incTimer;

	#endregion
	#region RuntimeOrOther

	public int maxLivingSpaces;
	public float increaseSpeed;
	public float decreaseSpeed;
	public bool canUpgrade=false;

	#endregion


	public HomeBuilding(int pid){
		this.ID = pid;
		canUpgrade = false;
		tileWidth = 2;
		tileHeight = 2;
		BuildTyp = BuildTypes.Drag;
		myBuildingTyp =	BuildingTyp.Blocking;
		people = 1;
		maxLivingSpaces = 8;
		buildingLevel = 0;
		name = "Home";
		increaseSpeed = 3;
		decreaseSpeed = 2;
		this.buildingRange = 0;
		hasHitbox = true;
		this.canTakeDamage = true;
		this.maintenancecost = 0;

	}
	protected HomeBuilding(HomeBuilding b){
//		this.buildingRange = b.buildingRange;
//		this.ID = b.ID;
//		this.people = b.people;
//		this.tileWidth = b.tileWidth;
//		this.tileHeight = b.tileHeight;
//		this.rotated = b.rotated;
//		this.maintenancecost = b.maintenancecost;
//		this.hasHitbox = b.hasHitbox;
//		this.name = b.name;
//		this.BuildTyp = b.BuildTyp;
//		this.buildcost = b.buildcost;
		BaseCopyData (b);
		this.maxLivingSpaces = b.maxLivingSpaces;
		this.increaseSpeed = b.increaseSpeed;
		this.decreaseSpeed = b.decreaseSpeed;
		this.canTakeDamage = b.canTakeDamage;
		this.buildingLevel = b.buildingLevel;
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public HomeBuilding(){}

	public override void LoadPrototypData (Structure prototyp) {
		HomeBuilding home = prototyp as HomeBuilding;
		if(prototyp == null){
			Debug.LogError ("ERROR - Prototyp was wrong");
			return;
		}
		BaseCopyData (home);
		maxLivingSpaces = home.maxLivingSpaces;
		increaseSpeed = home.increaseSpeed;
		decreaseSpeed = home.decreaseSpeed;
	}

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
		maxLivingSpaces *= 2; // TODO load this in from somewhere
		canUpgrade = false;
	}
	public void DowngradeHouse(){
		buildingLevel -= 1;
		maxLivingSpaces /= 2; // TODO load this in from somewhere

	}
}
