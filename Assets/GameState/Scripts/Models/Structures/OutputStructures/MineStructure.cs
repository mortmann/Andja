﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class MineStructure : OutputStructure {
	#region Serialize
	#endregion
	#region RuntimeOrOther

	public string myRessource;

	public override float Efficiency { get { 
			if(BuildTile.myIsland.myRessources [myRessource] ==0){
				return 0;
			}
			return 100; 
	} }

	#endregion

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

		hasHitbox = true;
		maxOutputStorage = 5;
		produceTime = 15f;
		buildingRange = 0;
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public MineStructure(){
	}
	protected MineStructure(MineStructure ms){
		LoadPrototypData (ms);
	}
	public override void LoadPrototypData(Structure s){
		MineStructure ms = s as MineStructure;
		if(ms == null){
			Debug.LogError ("ERROR - Prototyp was wrong");
			return;
		}
		CopyData (ms);
	}
	private void CopyData(MineStructure ms){
		OutputCopyData (ms);
		myRessource = ms.myRessource;

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
