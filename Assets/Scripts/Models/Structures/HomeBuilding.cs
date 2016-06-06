using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class HomeBuilding : Structure{
	public PlayerController pc;
	public int people;
	public int maxLivingSpaces;
	public float increaseSpeed;
	public float decreaseSpeed;
	public bool canUpgrade;
	public int buildingLevel;
	public float decTimer;
	public float incTimer;

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
		hasHitbox = true;
	}
	protected HomeBuilding(HomeBuilding b){
		this.ID = b.ID;
		this.people = b.people;
		this.tileWidth = b.tileWidth;
		this.tileHeight = b.tileHeight;
		this.rotated = b.rotated;
		this.maintenancecost = b.maintenancecost;
		this.hasHitbox = b.hasHitbox;
		this.name = b.name;
		this.BuildTyp = b.BuildTyp;
		this.buildcost = b.buildcost;
		this.buildingLevel = 1;
		this.maintenancecost = 0;
		this.maxLivingSpaces = b.maxLivingSpaces;
		this.increaseSpeed = b.increaseSpeed;
		this.decreaseSpeed = b.decreaseSpeed;
	}
	public override Structure Clone (){
		return new HomeBuilding (this);
	}

	public override void OnBuild(){
		pc = GameObject.FindObjectOfType<PlayerController> ();
		foreach (Tile t in neighbourTiles) {
			t.RegisterTileStructureChangedCallback (OnTStructureChange);
		}
	}

	public override void update (float deltaTime) {
		float allPercentage = 0;
		int count = 0;
		foreach (Need n in city.allNeeds.Keys) {
			if (n.startLevel <= buildingLevel && n.popCount <= pc.maxPopulationCount) {
				allPercentage += city.allNeeds [n];
				count++;
			}
		}
		allPercentage /= count; 
		if (allPercentage < 0.2f) {
			decTimer -= deltaTime;
			incTimer += deltaTime;
			incTimer = Mathf.Clamp (incTimer, 0, increaseSpeed);

			if (decTimer <= 0) {
				people--;
			}
		}
		if (allPercentage > 0.2f && allPercentage < 0.85f) {
			incTimer += deltaTime;
			incTimer = Mathf.Clamp (incTimer, 0, increaseSpeed);
			decTimer += deltaTime;
			decTimer = Mathf.Clamp (decTimer, 0, decreaseSpeed);
		}
		if (allPercentage > 0.85f) {
			incTimer -= deltaTime;
			decTimer += deltaTime;
			decTimer = Mathf.Clamp (decTimer, 0, decreaseSpeed);
			if (incTimer <= 0) {
				people++;
			}
		}
	}
	public void OnTStructureChange(Tile t, Structure old){
		if(old is Road == false || t.Structure is Road == false){
			return;
		}
	}
	public override void WriteXml (XmlWriter writer)	{
		BaseWriteXml (writer);
		writer.WriteAttributeString("People", people.ToString());
		writer.WriteAttributeString("BuildingLevel", buildingLevel.ToString());

	}
	public override void ReadXml (XmlReader reader) {
		BaseReadXml (reader);
		people = int.Parse( reader.GetAttribute("People") );
		buildingLevel = int.Parse( reader.GetAttribute("BuildingLevel") );
	}
}
