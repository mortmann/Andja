using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


public enum TileType { Water, Shore, Dirt, Grass, Stone, Mountain };


public class Tile : IXmlSerializable {
    //Want to have more than one structure in one tile!
    //more than one tree or tree and bench! But for now only one
	protected Structure _structures= null;
	public Structure structures {
		get{
			return _structures;
		} 
		set {
			if(_structures == value){
				Debug.Log ("Tile.structures! Why does the structure add itself again to the tile?");
				return;
			}
			if(_structures != null){
				_structures.Destroy ();
			}
			Structure oldStructure = _structures;
			_structures = value;
			if (cbTileStructureChanged != null) {
				cbTileStructureChanged (this,oldStructure);
			} 
		}
	}

    public Island myIsland { get; set; }
	protected City _myCity;
	public City myCity { 
		get{
			return _myCity;
		} 
		set { 
			if (value == null) {
				if (_myCity !=null){
					 
				}
			} 
			if (_myCity !=null){
				return;
			}
			_myCity = value;
		} 
	}
	protected bool _isHighlighted;
	public bool IsHighlighted {
		get { return _isHighlighted;}
		set { 
			if(value == _isHighlighted){
				return;
			} else {
				this._isHighlighted = value;
				World.current.OnTileChanged (this);
			}
		}
	}
    private TileType _type = TileType.Water;
    public TileType Type {
        get { return _type; }
        set {
            _type = value;
        }
    }

    const float baseTileMovementCost = 1;

    public float movementCost {
        get {
			if (Type == TileType.Water) {
				return 1;  
			}
			if (Type == TileType.Mountain) {
				return Mathf.Infinity;  
			}
			if (structures == null){
				return baseTileMovementCost;
			}
			if (structures.BuildTyp == BuildTypes.Single){
				return float.PositiveInfinity;
			}
			if (structures.BuildTyp == BuildTypes.Path){
				return 0.25f;
			}
            return 1;
        }
    }

	int x;
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
	/// <returns><c>true</c>, if tile was checked, <c>false</c> otherwise.</returns>
	/// <param name="t">Tile to check, canBeBuildOnShore if shore tiles are ok</param>
	public static bool checkTile(Tile t, bool canBeBuildOnShore =false){
		if(t.Type == TileType.Water){
			return false;
		}
		if(t.Type == TileType.Mountain){
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
		if(t.structures != null ) {
			if(t.structures.canBeBuildOver == false){
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
