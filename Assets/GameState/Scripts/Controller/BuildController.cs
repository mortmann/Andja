#region License
// ====================================================
// Andja Copyright(C) 2016 Team Mortmann
// ====================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Build state modes.
/// </summary>
public enum BuildStateModes { None, Build, Destroy }

/// <summary>
/// Build controller.
/// </summary>
public class BuildController : MonoBehaviour {
    public static BuildController Instance { get; protected set; }
	protected BuildStateModes _buildState;
	public BuildStateModes BuildState { 
			get { return _buildState; } 
			set { 
				if (_buildState == value) {
					return;
				}
				World.current.resetIslandMark ();
				_buildState = value;
				if (cbBuildStateChange != null)
					cbBuildStateChange (_buildState); 
				}
			}
	public uint buildID = 0;

	public Dictionary<int,Structure>  structurePrototypes {
		get { return PrototypController.Instance.structurePrototypes; }
	}
	public Structure toBuildStructure;

	Action<Structure,bool> cbStructureCreated;
	Action<City> cbCityCreated;
	Action<BuildStateModes> cbBuildStateChange;

	public Dictionary<int, Item> getCopieOfAllItems(){
		return PrototypController.Instance.getCopieOfAllItems();
	}

	public void Awake(){
		if (Instance != null) {
			Debug.LogError("There should never be two world controllers.");
		}
		Instance = this;
		BuildState = BuildStateModes.None;
		buildID = 0;
	}

	public void OnClickSettle(){
		OnClick (6);
	}
	public void DestroyStructureOnTiles( IEnumerable<Tile> tiles, Player destroyPlayer){
		foreach(Tile t in tiles){
			DestroyStructureOnTile (t,destroyPlayer);
		}
	}
	/// <summary>
	/// Works only for current player not for someone else
	/// </summary>
	/// <param name="t">T.</param>
	public void DestroyStructureOnTile(Tile t, Player destroyPlayer){
		if(t.Structure==null){
			return;
		}
		if(t.Structure.playerNumber==destroyPlayer.playerNumber){
			t.Structure.Destroy ();
		}
	}
	public void OnClick(int id) {
		if(structurePrototypes.ContainsKey (id) == false){
			Debug.LogError ("BUTTON has ID that is not a structure prototypes ->o_O<- ");
			return;
		}
		toBuildStructure = structurePrototypes [id].Clone ();
		if(structurePrototypes [id].BuildTyp == BuildTypes.Path){
			MouseController.Instance.mouseState = MouseState.Path;
			MouseController.Instance.structure = toBuildStructure;
		}
		if(structurePrototypes [id].BuildTyp == BuildTypes.Single){
			MouseController.Instance.mouseState = MouseState.Single;
			MouseController.Instance.structure = toBuildStructure;
		}
		if(structurePrototypes [id].BuildTyp == BuildTypes.Drag){
			MouseController.Instance.mouseState = MouseState.Drag;
			MouseController.Instance.structure = toBuildStructure;
		}

		BuildState = BuildStateModes.Build;
    }
	public void BuildOnTile(List<Tile> tiles, bool forEachTileOnce,int playerNumber){
		if (toBuildStructure == null) {
			return;
		}
		BuildOnTile (tiles, forEachTileOnce, toBuildStructure,playerNumber);
	}
	/// <summary>
	/// USED ONLY FOR LOADING
	/// DONT USE THIS FOR ANYTHING ELSE!!
	/// </summary>
	/// <param name="s">S.</param>
	/// <param name="t">T.</param>
	private void BuildOnTile(Structure s , Tile t){
		if(s==null||t==null){
			Debug.LogError ("Something went wrong by loading Structure!");
			return;
		}
		RealBuild (s.GetBuildingTiles (t.X, t.Y), s,-1,true,true);
	}
	public void BuildOnTile(List<Tile> tiles, bool forEachTileOnce, Structure structure,int playerNumber,bool wild=false){
		if(tiles == null || tiles.Count == 0 || WorldController.Instance.IsPaused){
			return;
		}
		if (forEachTileOnce == false) {
			RealBuild (tiles,structure,playerNumber,false,wild);
		} else {
			foreach (Tile tile in tiles) {
				List<Tile> t = new List<Tile> ();
				t.AddRange (structure.GetBuildingTiles (tile.X,tile.Y));
				RealBuild (t,structure,playerNumber,false,wild);
			}
		}
	}
	protected void RealBuild(List<Tile> tiles,Structure s,int playerNumber,bool loading=false,bool wild=false){
		int rotate = s.rotated;
		if (loading == false) {
			s = s.Clone ();
		}
		s.rotated = rotate;

		//if it should be build in wilderniss city
		if(wild){
			s.playerNumber = -1;
			s.buildInWilderniss = true;
		} else {
			//set the player id for check for city
			//has to be changed if someone takes it over
			s.playerNumber = playerNumber;
		}
		//before we need to check if we can build THERE
		//we need to know if there is if we COULD build 
		//it anyway? that means enough ressources and enough Money
		if(loading==false&&wild==false){
			//TODO: Check for Event restricting building from players
			//return;


			//find a city that matches the player 
			//and check for money
			if(playerHasEnoughMoney(s,playerNumber)==false){
				Debug.Log ("not playerHasEnoughMoney"); 
				return;
			}
			//if it need ressources
			if (s.buildingItems != null) {
				foreach (Tile item in tiles) {
					//we can build in wilderniss terrain but we need our own city
					//FIXME how do we do it with warehouses?
					if (s.GetType ()!=typeof(Warehouse) && item.myCity != null && item.myCity.IsWilderness () == false) {
						//WARNING: checking for this twice!
						//this is one is not necasserily needed
						//but it we *need* the city to check for its ressources
						//this saves a lot of cpu but it can be problematic if we want to be able 
						//to build something in enemy-terrain
						if (item.myCity.playerNumber != PlayerController.currentPlayerNumber) {
							Debug.Log ("PlayerController.Instance.number"); 
							return;
						}
						//check for ressources  
						if (item.myCity.myInv.ContainsItemsWithRequiredAmount (s.BuildingItems ()) == false) {
							Debug.Log ("ContainsItemsWithRequiredAmount==null"); 
							return;
						}
						//now we know that there is enough from everthing and it can be build
						//we dont need longer to check a city tile
						//playercontroller will handle the reduction of money/and everything else 
						//related to money - But we need to remove the Ressources
						break;
					}
				}
			}
		} 
	
		//now we know that we COULD build that structure
		//but CAN WE?
		//check to see if the structure can be placed there
		if (s.PlaceStructure (tiles) == false) {
			if(loading){
				Debug.LogError ("PLACING FAILED WHILE LOADING! " + s.buildID);
			}
			return;
		}

		//call all callbacks on structure created
		//FIXME remove this or smth
		if (cbStructureCreated != null) {
			cbStructureCreated (s,loading);
		} 
		if (loading == false) {
			// this is for loading so everything will be placed in order
			s.buildID = buildID;
			buildID++;
		}
		s.RegisterOnDestroyCallback (OnDestroyStructure);
	}
	public void OnDestroyStructure(Structure str){
//		str.City.removeStructure (str);
	}
	public bool playerHasEnoughMoney(Structure s,int playerNumber){
		if(PlayerController.Instance.GetPlayer (playerNumber).balance >= s.buildcost){
			return true;
		}
		return false;
	}
	public void BuildOnTile(int id, List<Tile> tiles,int playerNumber){
		if(structurePrototypes.ContainsKey (id) == false){
			return;
		}
		BuildOnTile (tiles, true, structurePrototypes[id],playerNumber);
	}
	public City CreateCity(Tile t,Warehouse w){
		if(t.myIsland == null){
			Debug.LogError ("CreateCity called not on a island!");
			return null;
		}
		if(t.myCity != null && t.myCity.IsWilderness () ==false){
			Debug.LogError ("CreateCity called not on a t.myCity && t.myCity.IsWilderness () ==false!");
			return null;
		}
		City c = t.myIsland.CreateCity (w.playerNumber);
		// needed for mapimage
		c.addStructure (w);// dont know if this is good ...
		if(cbCityCreated != null) {
			cbCityCreated (c);
		}
		return c; 
	}


	public void PlaceAllLoadedStructure(List<Structure> loadedStructures){
		for (int i = 0; i < loadedStructures.Count; i++) {
//			loadedStructures[i].LoadPrototypData (structurePrototypes[loadedStructures[i].ID]);
			BuildOnTile (loadedStructures[i],loadedStructures[i].BuildTile);
		}
	}
	public void ResetBuild(){
		BuildState = BuildStateModes.None;
		this.toBuildStructure = null;
	}
	public void DestroyToolSelect(){
		ResetBuild ();
		BuildState = BuildStateModes.Destroy;
		MouseController.Instance.mouseState = MouseState.Destroy;
	}
	public void Escape(){
		World.current.resetIslandMark ();
		ResetBuild ();
	}


	public void RegisterStructureCreated(Action<Structure,bool> callbackfunc) {
		cbStructureCreated += callbackfunc;
	}
	public void UnregisterStructureCreated(Action<Structure,bool> callbackfunc) {
		cbStructureCreated -= callbackfunc;
	}
	public void RegisterCityCreated(Action<City> callbackfunc) {
		cbCityCreated += callbackfunc;
	}
	public void UnregisterCityCreated(Action<City> callbackfunc) {
		cbCityCreated -= callbackfunc;
	}
	public void RegisterBuildStateChange(Action<BuildStateModes> callbackfunc) {
		cbBuildStateChange += callbackfunc;
	}
	public void UnregisterBuildStateChange(Action<BuildStateModes> callbackfunc) {
		cbBuildStateChange -= callbackfunc;
	}

}
