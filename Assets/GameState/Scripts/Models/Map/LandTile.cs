using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class LandTile : Tile {
	//Want to have more than one structure in one tile!
	//more than one tree or tree and bench! But for now only one
	protected Structure _structures= null;
	public override Structure Structure {
		get{
			return _structures;
		} 
		set {
			if(_structures!=null&&_structures == value){
				//				Debug.Log ("Tile.structure! Why does the structure add itself again to the tile?");
				return;
			}
			if(_structures != null && _structures.canBeBuildOver && value!=null){
				_structures.Destroy ();
			} 
			Structure oldStructure = _structures;
			_structures = value;
			if (cbTileStructureChanged != null) {
				cbTileStructureChanged (this,oldStructure);
			} 
		}
	}
	protected Island _myIsland;

	public override Island myIsland { get{return _myIsland;} 
		set{ 
			if(value==null){
				Debug.LogError ("setting myisland to NULL is not viable " + value);
				return;
			}
			_myIsland = value;
		}}
	protected string _spriteName;
	public override string SpriteName {
		get { return _spriteName; }
		set {
			_spriteName = value;
		}
	}

	private Queue<City> cities;
	protected City _myCity;
	public override City myCity { 
		get{
			return _myCity;
		} 
		set {
			if(myIsland==null){
				return;
			}
			//if the tile gets unclaimed by the current owner of this
			//either wilderniss or other player
			if (value == null) {
				if(cities!=null&&cities.Count>0){
					//if this has more than one city claiming it 
					//its gonna go add them to a queue and giving it 
					//in that order the right to own it
					City c = cities.Dequeue ();
					_myCity = value;
					c.addTile (this);
					return;
				}
				myIsland.wilderniss.addTile (this);
				_myCity = myIsland.wilderniss;
				return;
			} 
			//warns about double wilderniss
			//can be removed for performance if 
			//necessary but it helps for development
			if(_myCity!=null &&_myCity.playerNumber==-1 && value.playerNumber==-1){
				_myCity = value;
				return;
			}
			//remembers the order of the cities that have a claim 
			//on that tile -- Maybe do a check if the city
			//that currently owns has a another claim onit?
			if (_myCity!=null && _myCity.IsWilderness ()==false){
				if(cities==null){
					cities = new Queue<City> ();
				}
				cities.Enqueue (value);
				return;
			}
			//if the current city is not null remove this from it
			//FIXME is there a performance problem here? ifso fix it
			if(_myCity!=null){
				_myCity.RemoveTile(this);
			}
			_myCity = value;
		} 
	}

	protected TileMark _oldTileState;
	private TileMark OldTileState {
		get { return _oldTileState; }
		set {
			if(value==TileMark.Highlight){
				
			} else {
				_oldTileState = value;
			}

		}
	}
	protected TileMark _tileState;
	public override TileMark TileState {
		get { 
			return _tileState;
		}
		set { 
			if (value == TileMark.Reset) {
				_tileState = _oldTileState;
				World.current.OnTileChanged (this);
				return;
			}
			if(value == _tileState){
				return;
			} else {
				_oldTileState = _tileState;
				this._tileState = value;
				World.current.OnTileChanged (this);
			}
		}
	}

	public List<NeedsBuilding> listOfInRangeNeedBuildings { get; protected set; }

	public LandTile(){}
	public LandTile(int x, int y){
		this.x = x;
		this.y = y;
		_type = TileType.Ocean; 
	} 

	// The function we callback any time our tile's data changes
	Action<Tile,Structure> cbTileStructureChanged;
	/// <summary>
	/// Register a function to be called back when our tile type changes.
	/// </summary>
	public override void RegisterTileStructureChangedCallback(Action<Tile,Structure> callback) {
		cbTileStructureChanged += callback;
	}

	/// <summary>
	/// Unregister a callback.
	/// </summary>
	public override void UnregisterTileStructureChangedCallback(Action<Tile,Structure> callback) {
		cbTileStructureChanged -= callback;
	}

	public override void addNeedStructure(NeedsBuilding ns){
		if(IsBuildType (Type)== false){
			return;
		}
		if (listOfInRangeNeedBuildings == null) {
			listOfInRangeNeedBuildings = new List<NeedsBuilding> ();
		}
		listOfInRangeNeedBuildings.Add (ns);
	}
	public override void removeNeedStructure(NeedsBuilding ns){
		if(IsBuildType (Type)== false){
			return;
		}
		if (listOfInRangeNeedBuildings == null) {
			return;
		}
		if (listOfInRangeNeedBuildings.Contains (ns) == false) {
			return;
		}
		listOfInRangeNeedBuildings.Remove (ns);
	}

	public override List<NeedsBuilding > getListOfInRangeNeedBuildings (){
		return listOfInRangeNeedBuildings;
	}
}
