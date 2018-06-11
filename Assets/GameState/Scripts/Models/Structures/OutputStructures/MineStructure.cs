using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class MinePrototypData : OutputPrototypData {
	
	public string myRessource;

}

[JsonObject(MemberSerialization.OptIn)]
public class MineStructure : OutputStructure {
	#region Serialize
	#endregion
	#region RuntimeOrOther

	public string myRessource {get{ return MineData.myRessource;}}

	public override float Efficiency { get { 
			if(BuildTile.MyIsland.myRessources [myRessource] ==0){
				return 0;
			}
			return 100; 
	} }
	
	protected MinePrototypData _mineData;
	public MinePrototypData MineData {
		get { if(_mineData==null){
				_mineData = (MinePrototypData)PrototypController.Instance.GetStructurePrototypDataForID (ID);
			}
			return _mineData;
		}
	}
	#endregion

	public MineStructure(int pid,MinePrototypData MineData){
		this.ID = pid;
		_mineData = MineData;
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public MineStructure(){
	}
	protected MineStructure(MineStructure ms){
		OutputCopyData (ms);
	}

	public override bool SpecialCheckForBuild (List<Tile> tiles) {
		for (int i = 0; i < tiles.Count; i++) {
			if(tiles[i].Type == TileType.Mountain){
				if (BuildTile.MyIsland.myRessources [myRessource] <= 0) {
					return false;
				}
			}
		}
		return true;
	}

	public override void Update (float deltaTime){
		if (BuildTile.MyIsland.myRessources [myRessource] <= 0) {
			return;
		} 
		if (output[0].count >= maxOutputStorage){
			return;
		}

		produceCountdown += deltaTime;
		if(produceCountdown >= produceTime) {
			produceCountdown = 0;
			output [0].count++;

            cbOutputChange?.Invoke(this);
        }
	}

	public override Structure Clone (){
		return new MineStructure (this);
	}
	public override void OnBuild ()	{
		
	}

}
