using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class MineStructure : Structure {
	
	public MineStructure(int pid){
		this.ID = pid;
		mustBeBuildOnMountain = true;
		tileWidth = 2;
		tileHeight = 3;
		name = "Mine";
		myBuildingTyp = BuildingTyp.Blocking;
		BuildTyp = BuildTypes.Single;
	}
	protected MineStructure(MineStructure ms){
		myBuildingTyp = ms.myBuildingTyp;
		BuildTyp = ms.BuildTyp;
		mustBeBuildOnMountain = true;
		tileWidth = ms.tileWidth;
		tileHeight = ms.tileHeight;
		name = ms.name;
	}

	public override bool SpecialCheckForBuild (List<Tile> tiles) {
		for (int i = 0; i < tiles.Count; i++) {
			
		}
		return true;
	}

	public override void update (float deltaTime){
		
	}

	public override Structure Clone (){
		return new MineStructure (this);
	}
	public override void OnBuild ()	{
		
	}
	public override void ReadXml (System.Xml.XmlReader reader){
		BaseReadXml (reader);
	}
	public override void WriteXml (System.Xml.XmlWriter writer)	{
		BaseWriteXml (writer);
	}
}
