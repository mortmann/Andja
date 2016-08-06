using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


public enum TileType { Water, Shore, Dirt, Grass, Stone, Mountain };
public enum TileMark { None, Highlight, Dark, Reset }


public class Tile : IXmlSerializable {
    //Want to have more than one structure in one tile!
    //more than one tree or tree and bench! But for now only one
	protected Structure _structures= null;
	public Structure Structure {
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

	public Island myIsland { get{return _myIsland;} 
		set{ 
			if(value==null){
				Debug.LogError ("setting myisland to NULL is not viable " + value);
				return;
			}
			_myIsland = value;
		}}
	//the 
	private Queue<City> cities;
	protected City _myCity;
	public City myCity { 
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
				Debug.Log ("override");
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
	protected TileMark _tileState;
	public TileMark TileState {
		get { return _tileState;}
		set { 
			if (value == TileMark.Reset) {
				if (Type == TileType.Water) {
					this._tileState = TileMark.None;
					World.current.OnTileChanged (this);
					return;
				}
				this._tileState = _oldTileState;
				World.current.OnTileChanged (this);
				return;
			}
			if(value == _tileState){
				return;
			} else {
				if (_tileState != TileMark.Highlight) {
					_oldTileState = _tileState;
				}
				this._tileState = value;
				World.current.OnTileChanged (this);
			}
		}
	}
    
	private TileType _type = TileType.Water;
    public TileType Type {
        get { return _type; }
        set {
			listOfInRangeNeedBuildings = new List<NeedsBuilding> ();
            _type = value;
        }
    }

	public List<NeedsBuilding> listOfInRangeNeedBuildings { get; protected set; }


    public float MovementCost {
        get {
			if (Type == TileType.Water) {
				if(Structure!=null){
					return float.PositiveInfinity;
				}
				return 1;  
			}
			if (Type == TileType.Mountain) {
				return Mathf.Infinity;  
			}
			if (Structure == null){
				return 1;
			}
			if (Structure.BuildTyp == BuildTypes.Single){
				return float.PositiveInfinity;
			}
			if (Structure.BuildTyp == BuildTypes.Path){
				return 0.25f;
			}
            return 1;
        }
    }
	[XmlAttribute("X")]
	int x;
	[XmlAttribute("Y")]
	int y;
    public int X {
        get {
            return x;
        }
    }

    public int Y {
        get {
            return y;
        }
    }
	public Tile(){}
    public Tile(int x, int y){
        this.x = x;
        this.y = y;
        _type = TileType.Water;
    }

    // The function we callback any time our tile's data changes
	Action<Tile,Structure> cbTileStructureChanged;
    /// <summary>
    /// Register a function to be called back when our tile type changes.
    /// </summary>
	public void RegisterTileStructureChangedCallback(Action<Tile,Structure> callback) {
        cbTileStructureChanged += callback;
    }

    /// <summary>
    /// Unregister a callback.
    /// </summary>
	public void UnregisterTileStructureChangedCallback(Action<Tile,Structure> callback) {
        cbTileStructureChanged -= callback;
    }


    // Tells us if two tiles are adjacent.
    public bool IsNeighbour(Tile tile, bool diagOkay = false) {
        // Check to see if we have a difference of exactly ONE between the two
        // tile coordinates.  Is so, then we are vertical or horizontal neighbours.
        return
            Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 ||  // Check hori/vert adjacency
            (diagOkay && (Mathf.Abs(this.X - tile.X) == 1 && Mathf.Abs(this.Y - tile.Y) == 1)); // Check diag adjacency
            
    }

	public void addNeedStructure(NeedsBuilding ns){
		if(IsBuildType (Type)== false){
			return;
		}
		if (listOfInRangeNeedBuildings == null) {
			listOfInRangeNeedBuildings = new List<NeedsBuilding> ();
		}
		listOfInRangeNeedBuildings.Add (ns);
	}
	public void removeNeedStructure(NeedsBuilding ns){
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
    /// <summary>
    /// Gets the neighbours.
    /// </summary>
    /// <returns>The neighbours.</returns>
    /// <param name="diagOkay">Is diagonal movement okay?.</param>
    public Tile[] GetNeighbours(bool diagOkay = false) {
        Tile[] ns;

        if (diagOkay == false) {
            ns = new Tile[4];   // Tile order: N E S W
        } else {
            ns = new Tile[8];   // Tile order : N E S W NE SE SW NW
        }

        Tile n;

		n = World.current.GetTileAt(X, Y + 1);
    	//NORTH
		ns[0] = n;  // Could be null, but that's okay.
		//WEST
		n = World.current.GetTileAt(X + 1, Y);
        ns[1] = n;  // Could be null, but that's okay.
        //SOUTH
		n = World.current.GetTileAt(X, Y - 1);
        ns[2] = n;  // Could be null, but that's okay.
        //EAST
		n = World.current.GetTileAt(X - 1, Y);
        ns[3] = n;  // Could be null, but that's okay.

        if (diagOkay == true) {
			n = World.current.GetTileAt(X + 1, Y + 1);
            ns[4] = n;  // Could be null, but that's okay.
			n = World.current.GetTileAt(X + 1, Y - 1);
            ns[5] = n;  // Could be null, but that's okay.
			n = World.current.GetTileAt(X - 1, Y - 1);
            ns[6] = n;  // Could be null, but that's okay.
			n = World.current.GetTileAt(X - 1, Y + 1);
            ns[7] = n;  // Could be null, but that's okay.
        }

        return ns;
    }

    public Tile North() {
		return World.current.GetTileAt(X, Y + 1);
    }
    public Tile South() {
		return World.current.GetTileAt(X, Y - 1);
    }
    public Tile East() {
		return World.current.GetTileAt(X + 1, Y);
    }
    public Tile West() {
		return World.current.GetTileAt(X - 1, Y);
    }
	/// <summary>
	/// Checks if Structure can be placed on the tile.
	/// </summary>
	/// <returns><c>true</c>, if tile is buildable, <c>false</c> otherwise.</returns>
	/// <param name="t">Tile to check, canBeBuildOnShore if shore tiles are ok</param>
	public static bool checkTile(Tile t, bool canBeBuildOnShore =false, bool canBeBuildOnMountain =false){
		if(t.Type == TileType.Water){
			return false;
		}
		if(t.Type == TileType.Mountain && canBeBuildOnMountain ==false){
			return false;
		}
		if(t.Type == TileType.Stone){
			return false;
		}
		if(canBeBuildOnShore == false){
			if(t.Type == TileType.Shore){
				return false;
			}
		}
		if(t.Structure != null ) {
			if(t.Structure.canBeBuildOver == false){
				return false;
			}
		}
		return true;
	}

	public static bool IsBuildType(TileType t){
		if( t == TileType.Water){
			return false;
		}
		if( t == TileType.Mountain){
			return false;
		}
		return true;
	}
	/// <summary>
	/// Water doesnt count as unbuildable!
	/// Determines if is unbuildable type the specified t.
	/// </summary>
	/// <returns><c>true</c> if is unbuildable type the specified t; otherwise, <c>false</c>.</returns>
	/// <param name="t">T.</param>
	public static bool IsUnbuildableType(TileType t,TileType toBuildOn){
		if( t == TileType.Mountain && toBuildOn != TileType.Mountain){
			return true;
		}
		if( t == TileType.Water && toBuildOn != TileType.Water){
			return true;
		}
		return false;
	}

    public String toString() {
        return "tile_" + X + "_" + Y;
    }
//	public override bool Equals (object obj){
//		if(obj==null){
//			return false;
//		}
//		return X == ((Tile)obj).X && Y == ((Tile)obj).Y;
//	}
	//////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 						SAVING & LOADING
	/// 
	//////////////////////////////////////////////////////////////////////////////////////
	public XmlSchema GetSchema() {
		return null;
	}

	public void WriteXml(XmlWriter writer) {
		writer.WriteAttributeString( "X", X.ToString() );
		writer.WriteAttributeString( "Y", Y.ToString() );
		writer.WriteAttributeString( "Type", ((int)Type).ToString() );
	}

	public void ReadXml(XmlReader reader) {
		Type = (TileType)int.Parse( reader.GetAttribute("Type") );
	}

}
