using UnityEngine;
using System.Collections;
using System;


public enum TileType { Water, Shore, Dirt, Grass, Stone, Mountain };


public class Tile {
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
			_structures = value;
			if (cbTileChanged != null) {
				cbTileChanged (this);
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

    public World world;

    private TileType _type = TileType.Water;
    public TileType Type {
        get { return _type; }
        set {
            TileType oldType = _type;
            _type = value;
            // Call the callback and let things know we've changed.

            if (cbTileChanged != null && oldType != _type) {
                cbTileChanged(this);
            }
        }
    }

    const float baseTileMovementCost = 1;

    public float movementCost {
        get {
			if (Type == TileType.Water) {
				return 1;  
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

    public Tile(World world, int x, int y){
        this.world = world;
        this.x = x;
        this.y = y;
        _type = TileType.Water;
    }

    // The function we callback any time our tile's data changes
    Action<Tile> cbTileChanged;
    /// <summary>
    /// Register a function to be called back when our tile type changes.
    /// </summary>
    public void RegisterTileStructureChangedCallback(Action<Tile> callback) {
        cbTileChanged += callback;
    }

    /// <summary>
    /// Unregister a callback.
    /// </summary>
    public void UnregisterTileStructureChangedCallback(Action<Tile> callback) {
        cbTileChanged -= callback;
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

        n = world.GetTileAt(X, Y + 1);
    	//NORTH
		ns[0] = n;  // Could be null, but that's okay.
		//WEST
        n = world.GetTileAt(X + 1, Y);
        ns[1] = n;  // Could be null, but that's okay.
        //SOUTH
		n = world.GetTileAt(X, Y - 1);
        ns[2] = n;  // Could be null, but that's okay.
        //EAST
		n = world.GetTileAt(X - 1, Y);
        ns[3] = n;  // Could be null, but that's okay.

        if (diagOkay == true) {
            n = world.GetTileAt(X + 1, Y + 1);
            ns[4] = n;  // Could be null, but that's okay.
            n = world.GetTileAt(X + 1, Y - 1);
            ns[5] = n;  // Could be null, but that's okay.
            n = world.GetTileAt(X - 1, Y - 1);
            ns[6] = n;  // Could be null, but that's okay.
            n = world.GetTileAt(X - 1, Y + 1);
            ns[7] = n;  // Could be null, but that's okay.
        }

        return ns;
    }

    public Tile North() {
        return world.GetTileAt(X, Y + 1);
    }
    public Tile South() {
        return world.GetTileAt(X, Y - 1);
    }
    public Tile East() {
        return world.GetTileAt(X + 1, Y);
    }
    public Tile West() {
        return world.GetTileAt(X - 1, Y);
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



    public String toString() {
        return "tile_" + X + "_" + Y;
    }
}
