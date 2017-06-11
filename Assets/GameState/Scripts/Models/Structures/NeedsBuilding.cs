using UnityEngine;
using System.Collections;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class NeedsBuilding : Structure {
	#region Serialize
	#endregion
	#region RuntimeOrOther
	#endregion
	public NeedsBuilding (int pid){
		this.ID = pid;
		tileWidth = 2;
		tileHeight = 2;
		BuildTyp = BuildTypes.Single;
		myBuildingTyp =	BuildingTyp.Blocking;
		name = "NeedsBuilding";
		this.maintenancecost = 100;
	}
	public NeedsBuilding (NeedsBuilding b){
		this.ID = b.ID;
		this.tileWidth = b.tileWidth;
		this.tileHeight = b.tileHeight;
		this.rotated = b.rotated;
		this.maintenancecost = b.maintenancecost;
		this.hasHitbox = b.hasHitbox;
		this.name = b.name;
		this.BuildTyp = b.BuildTyp;
		this.buildcost = b.buildcost;
		this.maintenancecost = b.maintenancecost;
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public NeedsBuilding(){}
	public override Structure Clone (){
		return new NeedsBuilding (this);
	}
	public override void OnBuild ()	{
		foreach (Tile t in myRangeTiles) {
			t.addNeedStructure (this);
		}
	}
	public override void OnClick (){
		
	}
	public override void OnClickClose (){
	}
	public override void update (float deltaTime){
	}

}
