using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class MineStructure : ProductionBuilding {

	public string myRessource;
	public override float Efficiency { get { return 100; } }
	public MineStructure(int pid){
		myRessource = "stone";
		this.ID = pid;
		mustBeBuildOnMountain = true;
		tileWidth = 2;
		tileHeight = 3;
		name = "Mine";
		myBuildingTyp = BuildingTyp.Blocking;
		BuildTyp = BuildTypes.Single;
		output = new Item[1];
		output[0] = BuildController.Instance.allItems [3];
		outputStorage = new int[1];
		hasHitbox = true;
		maxOutputStorage = 5;
		produceTime = 15f;
		buildingRange = 0;
	}
	public MineStructure(){
	}
	protected MineStructure(MineStructure ms){
		myBuildingTyp = ms.myBuildingTyp;
		myRessource = ms.myRessource;
		BuildTyp = ms.BuildTyp;
		mustBeBuildOnMountain = true;
		tileWidth = ms.tileWidth;
		tileHeight = ms.tileHeight;
		name = ms.name;
		produceTime = ms.produceTime;
		produceCountdown = produceTime;
		output = ms.output;
		outputStorage = ms.outputStorage;
		hasHitbox = ms.hasHitbox;
		maxOutputStorage = ms.maxOutputStorage;
		buildingRange = ms.buildingRange;
	}


	public override bool SpecialCheckForBuild (List<Tile> tiles) {
		for (int i = 0; i < tiles.Count; i++) {
			if(tiles[i].Type == TileType.Mountain){
				if (BuildTile.myIsland.myRessources [myRessource] <= 0) {
					return false;
				}
			}
		}
		return true;
	}

	public override void update (float deltaTime){
		if (BuildTile.myIsland.myRessources [myRessource] <= 0) {
			return;
		} 
		if (outputStorage[0] >= maxOutputStorage){
			return;
		}

		produceCountdown -= deltaTime;
		if (produceCountdown <= 0) {
			produceCountdown = produceTime;
			output [0].count++;
		}
	}

	public override Structure Clone (){
		return new MineStructure (this);
	}
	public override void OnBuild ()	{
		
	}
	public override void ReadXml (System.Xml.XmlReader reader){
		BaseReadXml (reader);
		base.ReadXml (reader);
	}
	public override void WriteXml (System.Xml.XmlWriter writer)	{
		BaseWriteXml (writer);
		base.WriteXml (writer);
	}
}
