﻿using UnityEngine;
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
			if(BuildTile.myIsland.myRessources [myRessource] ==0){
				return 0;
			}
			return 100; 
	} }
	
	protected MinePrototypData _mineData;
	public MinePrototypData MineData {
		get { if(_mineData==null){
				_mineData = (MinePrototypData)PrototypController.Instance.GetPrototypDataForID (ID);
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
		if (output[0].count >= maxOutputStorage){
			return;
		}

		produceCountdown -= deltaTime;
		if (produceCountdown <= 0) {
			produceCountdown = produceTime;
			output [0].count++;

			if (cbOutputChange != null) {
				cbOutputChange (this);
			}
		}
	}

	public override Structure Clone (){
		return new MineStructure (this);
	}
	public override void OnBuild ()	{
		
	}

}
