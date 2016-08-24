using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
/// <summary>
/// /*Tile type.*/
/// Ocean = Water outside of Islands -> have no own GameObjects
/// Shore = Water/Land(Sand or smth) at the borders island -> normally it can be build(only onshore can build here)
/// Water = Eg Sea or River inside islands
/// Dirt = Not as good as Grass but can be build on by everything
/// Grass = Normal Tile for forest and stuff
/// Stone = its rocky like a big rock -> cant build here
/// Desert = nothing grows here
/// Steppe = Exotic goods like it here
/// Jungle = Exotic goods Love it here
/// Mountain = you cant build anything here except mines(andso)
/// </summary>
public enum TileType { Ocean, Shore, Water, Dirt, Grass, Stone, Desert, Steppe, Jungle, Mountain };
public enum TileMark { None, Highlight, Dark, Reset }

public class Tile : IXmlSerializable {

	[XmlAttribute("X")]
	protected int x;
	[XmlAttribute("Y")]
	protected int y;
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
	public virtual string SpriteName {
		get { return null; }
		set {
		}
	}

    public Path_TileGraph pathfinding;

	public Vector3 vector { get {return new Vector3 (x, y, 0);} }
	public Tile(){}
	public Tile(int x, int y){
		this.x = x;
		this.y = y;
		_type = TileType.Ocean;
	}
	protected TileType _type = TileType.Ocean;
	public TileType Type {
		get { return _type; }
		set {
			_type = value;
		}
	}


	public float MovementCost {
		get {
			if (Type == TileType.Ocean) {
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
			if (Structure.myBuildingTyp != BuildingTyp.Pathfinding){
				return float.PositiveInfinity;
			}
			if (Structure.myBuildingTyp == BuildingTyp.Pathfinding){
				return 0.25f;
			}
			return 1;
		}
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
	/// <returns><c>true</c>, if tile is buildable, <c>false</c> otherwise.</returns>
	/// <param name="t">Tile to check, canBeBuildOnShore if shore tiles are ok</param>
	public virtual bool checkTile(Tile t, bool canBeBuildOnShore =false, bool canBeBuildOnMountain =false){
		if(t.Type == TileType.Ocean){
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
		if( t == TileType.Ocean){
			return false;
		}
		if( t == TileType.Mountain){
			return false;
		}
		if( t == TileType.Stone){
			return false;
		}
		if( t == TileType.Shore){
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
		if( t == TileType.Ocean && toBuildOn != TileType.Ocean){
			return true;
		}
		return false;
	}



	//Want to have more than one structure in one tile!
	//more than one tree or tree and bench! But for now only one
	public virtual Structure Structure {
		get{
			return null;
		} 
		set {
		}
	}
	public virtual Island myIsland { get { return null; } set { } }
	public virtual City myCity { 
		get{
			return null;
		} 
		set {
		} 
	}
	public virtual TileMark TileState {
		get { return TileMark.None;}
		set { 
		}
	}
	/// <summary>
	/// Register a function to be called back when our tile type changes.
	/// </summary>
	public virtual void RegisterTileStructureChangedCallback(Action<Tile,Structure> callback) {
	}

	/// <summary>
	/// Unregister a callback.
	/// </summary>
	public virtual void UnregisterTileStructureChangedCallback(Action<Tile,Structure> callback) {
	}
	public virtual void addNeedStructure(NeedsBuilding ns){
	}
	public virtual void removeNeedStructure(NeedsBuilding ns){
	}
	public virtual List<NeedsBuilding> getListOfInRangeNeedBuildings(){
		return null;
	}
	public String toString() {
		return "tile_" + X + "_" + Y+"-("+Type+")";
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
