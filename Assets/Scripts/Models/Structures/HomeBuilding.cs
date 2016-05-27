using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class HomeBuilding : Structure{

	public int people;
	public int maxLivingSpaces;
	public float increaseSpeed;
	public float decreaseSpeed;
	public bool canUpgrade;
	public int buildingLevel;

	public HomeBuilding(){
		canUpgrade = false;
		tileWidth = 2;
		tileHeight = 2;
		BuildTyp = BuildTypes.Drag;
		myBuildingTyp =	BuildingTyp.Blocking;
		people = 1;
		maxLivingSpaces = 8;
		buildingLevel = 0;
		name = "Home";
	}
	protected HomeBuilding(HomeBuilding b){
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
	}
	public override Structure Clone (){
		return new HomeBuilding (this);
	}

	public override void OnBuild(){
		
	}

	public override void update (float deltaTime) {
	
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
