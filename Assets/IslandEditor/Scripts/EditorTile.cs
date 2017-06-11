using UnityEngine;
using System.Collections;
using System;

public class EditorTile {
	protected int _x;
	protected int _y;
	public int X {
		get {
			return _x;
		}
	}
	public int Y {
		get {
			return _y;
		}
	}
	protected TileType _type = TileType.Ocean;
	public TileType Type {
		get { return _type; }
		set {
			_type = value;

		}
	}
	protected string _spriteName;
	public string SpriteName {
		get { return _spriteName; }
		set {
			_spriteName = value;
			if (cbTileTypeChange != null)
				cbTileTypeChange (this);
		}
	}

	Action<EditorTile> cbTileTypeChange;

	public EditorTile(int x, int y){
		_x = x;
		_y = y;
		Type = TileType.Ocean;
	}
	/// <summary>
	/// Register a function to be called back when our tile type changes.
	/// </summary>
	public  void RegisterTileChangedCallback(Action<EditorTile> callback) {
		cbTileTypeChange += callback;
	}

	/// <summary>
	/// Unregister a callback.
	/// </summary>
	public void UnregisterTileChangedCallback(Action<EditorTile> callback) {
		cbTileTypeChange -= callback;
	}

	/// <summary>
	/// Gets the neighbours.
	/// </summary>
	/// <returns>The neighbours.</returns>
	/// <param name="diagOkay">Is diagonal movement okay?.</param>
	public EditorTile[] GetNeighbours(bool diagOkay = false) {
		EditorTile[] ns;

		if (diagOkay == false) {
			ns = new EditorTile[4];   // Tile order: N E S W
		} else {
			ns = new EditorTile[8];   // Tile order : N E S W NE SE SW NW
		}

		EditorTile n;

		n = EditorIsland.current.GetTileAt(X, Y + 1);
		//NORTH
		ns[0] = n;  // Could be null, but that's okay.
		//WEST
		n = EditorIsland.current.GetTileAt(X + 1, Y);
		ns[1] = n;  // Could be null, but that's okay.
		//SOUTH
		n = EditorIsland.current.GetTileAt(X, Y - 1);
		ns[2] = n;  // Could be null, but that's okay.
		//EAST
		n = EditorIsland.current.GetTileAt(X - 1, Y);
		ns[3] = n;  // Could be null, but that's okay.

		if (diagOkay == true) {
			n = EditorIsland.current.GetTileAt(X + 1, Y + 1);
			ns[4] = n;  // Could be null, but that's okay.
			n = EditorIsland.current.GetTileAt(X + 1, Y - 1);
			ns[5] = n;  // Could be null, but that's okay.
			n = EditorIsland.current.GetTileAt(X - 1, Y - 1);
			ns[6] = n;  // Could be null, but that's okay.
			n = EditorIsland.current.GetTileAt(X - 1, Y + 1);
			ns[7] = n;  // Could be null, but that's okay.
		}

		return ns;
	}
		
}
