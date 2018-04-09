using UnityEngine;
using System.Collections;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class NeedsBuilding : Structure {
	#region Serialize
	#endregion
	#region RuntimeOrOther
	#endregion
	public NeedsBuilding (int pid, StructurePrototypeData spd){
		this.ID = pid;
		this._prototypData = spd;
	}
	public NeedsBuilding (int pid){
		this.ID = pid;
	}
	public NeedsBuilding (NeedsBuilding b){
		BaseCopyData (b);
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
			t.AddNeedStructure (this);
		}
	}
	public override void OnClick (){
		
	}
	public override void OnClickClose (){
	}
	public override void update (float deltaTime){
	}

}
