using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public enum BuildTypes {Drag, Path, Single};
public enum BuildingTyp {Production, Pathfinding, Blocking};
public enum Direction {N, E, S, W};

public abstract class Structure : IXmlSerializable {
	//prototype id
	public int ID;
	//player id
	public int playerID;
	//build id -- when it was build
	public int buildID;

	public string name;
	public City city;
    public bool isWalkable { get; protected set; }
	public bool hasHitbox { get; protected set; }

	public int buildingRange = 3;
	public int PopulationLevel = 0;
	public int PopulationCount = 0;

	public int rotated = 0; 
	public bool canRotate = true;

	public bool canBeBuildOver = false;

	public Direction front = Direction.E; 

	private int _tileWidth;
	public int tileWidth {
		get { 
			if (rotated == 0 || rotated == 180) {
				return _tileWidth;
			}  
			if (rotated == 90 || rotated == 270) {
				return _tileHeight;
			} 
			// should never come to this if its an error
			Debug.LogError ("Structure was rotated out of angle bounds: " + rotated);
			return 0;
		}
		protected set { _tileWidth = value;}
	}

	public Tile BuildTile { get { return myBuildingTiles [0]; }}

	private int _tileHeight; 
	public int tileHeight {
		get { 
			if (rotated == 0 || rotated == 180) {
				return _tileHeight;
			} 
			if (rotated == 90 || rotated == 270) {
				return _tileWidth;
			} 
			// should never come to this if its an error
			Debug.LogError ("Structure was rotated out of angle bounds: " + rotated);
			return 0;
		}
		protected set { _tileHeight = value;}
	}

	public List<Tile> myBuildingTiles;
	public List<Tile> neighbourTiles;
	public List<Tile> myRangeTiles;
	public List<Tile> myPrototypeTiles;

    Action<Structure> cbStructureChanged;
	Action<Structure> cbStructureDestroy;

    public bool canStartBurning;
	public bool mustBeBuildOnShore= false;
	public bool mustBeBuildOnMountain= false;

	public int maintenancecost;
	public int buildcost;
	public BuildTypes BuildTyp;
	public BuildingTyp myBuildingTyp = BuildingTyp.Blocking;
	public string connectOrientation;

	public abstract Structure Clone ();

    public GameObject getGameObject() {
        GameObject go = new GameObject();
		if (hasHitbox) {
			BoxCollider2D b = go.AddComponent<BoxCollider2D> ();
			b.size = new Vector2 (tileWidth, tileHeight);
		}
        return go;
    }

	public virtual void update (float deltaTime){
    }


	public void callbackIfnotNull(){
		if(cbStructureChanged != null)
			cbStructureChanged (this);
	}
    public void RegisterOnChangedCallback(Action<Structure> cb) {
        cbStructureChanged += cb;
    }
    public void UnregisterOnChangedCallback(Action<Structure> cb) {
        cbStructureChanged -= cb;
    }
	public void RegisterOnDestroyCallback(Action<Structure> cb) {
		cbStructureDestroy += cb;
	}
	public void UnregisterOnDestroyCallback(Action<Structure> cb) {
		cbStructureDestroy -= cb;
	}
	public bool PlaceStructure(List<Tile> tiles){
		myBuildingTiles = new List<Tile> ();
		//test if the place is buildable
		// if it has to be on land
		if (mustBeBuildOnShore == false && mustBeBuildOnMountain == false) {
			if (PlaceOnLand (tiles) == false) {
				return false;
			}
		}
		// if it has to be on mountain
		if (mustBeBuildOnMountain == true && mustBeBuildOnShore == false) {
			if (PlaceOnMountain (tiles) == false) {
				return false;
			}
		} 
		//if it has to be on shore 
		if (mustBeBuildOnShore == true && mustBeBuildOnMountain == false) {
			if (PlaceOnShore (tiles) == false) {
				return false;
			}
		}
		//special check for some structures 
		if (SpecialCheckForBuild (tiles) == false) {
			return false;
		}

		//if we are here we can build this and
		//set the tiles to the this structure -> claim the tiles!
		neighbourTiles = new List<Tile>();
		foreach (Tile mt in myBuildingTiles) {
			mt.structures = this;
			foreach(Tile nbt in mt.GetNeighbours()){
				if (myBuildingTiles.Contains (nbt) == false) {
					neighbourTiles.Add (nbt);
				}
			}
		}

		//it searches all the tiles it has in its reach!
		GetInRangeTiles (myBuildingTiles[0]);
		myPrototypeTiles = null;
		// do on place structure stuff here!
		OnBuild ();
		return true;
	}

	protected bool PlaceOnLand(List<Tile> tiles){
		if (tileWidth == 1 && tileHeight == 1) {
			if(tiles[0].structures != null && tiles [0].structures.canBeBuildOver){
				if(tiles [0].structures.name == this.name){
					return false;
				}
			}
			if (correctSpotOnLand (tiles) == false) {
				return false;
			}
			myBuildingTiles.Add (tiles [0]);
		} else {
			if(correctSpotOnLand(tiles)){
				myBuildingTiles.AddRange (tiles);
			} else {
				return false;
			}
		}
		return true;
	}
	protected bool PlaceOnMountain(List<Tile> tiles){
		Debug.Log ("PlaceOnMountain");
		if (tileWidth == 1 && tileHeight == 1) {
			if (tiles [0].structures != null && tiles [0].structures.canBeBuildOver) {
				if (tiles [0].structures.name == this.name) {
					return false;
				}
			}
			if (correctSpotOnMountain  (tiles) == false) {
				return false;
			}
			myBuildingTiles.Add (tiles [0]);
		} else {
			if (correctSpotOnMountain (tiles)) {
				myBuildingTiles.AddRange (tiles);
			} else {
				return false;
			}
		}
		return true;
	}
	protected bool PlaceOnShore(List<Tile> tiles){
		if (tileWidth == 1 && tileHeight == 1) {
			if(tiles [0].structures.canBeBuildOver){
				if(tiles [0].structures.name == this.name){
					return false;
				}
			}
			if (correctSpotOnShore (tiles) == false) {
				return false;
			}
			myBuildingTiles.Add (tiles[0]);
		} 
		else {
			if(correctSpotOnShore(tiles)){
				myBuildingTiles.AddRange (tiles);
			} else {
				return false;
			}
		}
		return true;
	}

	public virtual bool SpecialCheckForBuild(List<Tile> tiles){
		return true;
	}


	public List<Tile> GetBuildingTiles(float x , float y){
		List<Tile> tiles = new List<Tile> ();
		for (int w = 0; w < tileWidth; w++) {
			tiles.Add ( World.current.GetTileAt(x + w, y));
			for (int h = 1; h < tileHeight; h++) {
				tiles.Add (World.current.GetTileAt (x + w, y + h));
			}
		}
		return tiles;
	}
	private void CalculatePrototypTiles(){
		if(buildingRange == 0){
			return;
		}
		List<Tile> temp = new List<Tile> ();
		float x;
		float y;
		//get the tile at bottom left to create a "prototype circle"
		Tile firstTile = WorldController.Instance.world.GetTileAt (0 + buildingRange+tileWidth/2,0 + buildingRange+tileHeight/2);
		Vector2 center = new Vector2 (firstTile.X, firstTile.Y);
		if (tileWidth > 1) {
			center.x += 0.5f + ((float)tileWidth) / 2f - 1;
		}
		if (tileHeight > 1) {
			center.y += 0.5f + ((float)tileHeight) / 2f - 1;
		}
		World w = WorldController.Instance.world;
		float radius = this.buildingRange + 1.5f;
		for (float a = 0; a < 360; a += 0.5f) {
			x = center.x + radius * Mathf.Cos (a);
			y = center.y + radius * Mathf.Sin (a);
			//			GameObject go = new GameObject ();
			//			go.transform.position = new Vector3 (x, y);
			//			go.AddComponent<SpriteRenderer> ().sprite = Resources.Load<Sprite> ("Debug");
			x = Mathf.RoundToInt (x);
			y = Mathf.RoundToInt (y);
			for (int i = 0; i < buildingRange; i++) {
				Tile circleTile = w.GetTileAt (x, y);
				if (temp.Contains (circleTile) == false) {
					temp.Add (circleTile);
				}
			}
		}
		myPrototypeTiles = new List<Tile> ();
		//like flood fill the inner circle
		Queue<Tile> tilesToCheck = new Queue<Tile> ();
		tilesToCheck.Enqueue (firstTile.South ());
		while (tilesToCheck.Count > 0) {
			Tile t = tilesToCheck.Dequeue ();
			if (temp.Contains (t) == false && myPrototypeTiles.Contains (t) == false) {
				myPrototypeTiles.Add (t);
				Tile[] ns = t.GetNeighbours (false);
				foreach (Tile t2 in ns) {
					tilesToCheck.Enqueue (t2);
				}
			}
		}
		//remove the tile where the building is standing
		foreach (Tile item in GetBuildingTiles (firstTile.X,firstTile.Y)) {
			myPrototypeTiles.Remove (item);
		}
	}
	public List<Tile> GetInRangeTiles (Tile firstTile) {
		if(myPrototypeTiles == null){
			CalculatePrototypTiles ();
		}
		if(firstTile==null){
			return null;
		}
		if(buildingRange == 0){
			return null;
		}

		World w = WorldController.Instance.world;
		myRangeTiles = new List<Tile> ();
		float width = firstTile.X-buildingRange - tileWidth / 2;
		float height = firstTile.Y-buildingRange - tileHeight / 2;
		foreach(Tile t in myPrototypeTiles){
			myRangeTiles.Add (w.GetTileAt (t.X +width,t.Y+height));			
		}
		//remove the tile where the building is standing
		return myRangeTiles;
	}
	

	public abstract void OnBuild();
	public bool correctSpotOnLand(List<Tile> tiles){
		foreach(Tile t in tiles){
			if (Tile.checkTile (t) == false) {
				return false;
			}
		}
		return true;
	}
	public bool correctSpotOnMountain(List<Tile> tiles){
		return correctSpotForOn (tiles,TileType.Mountain);
	}
	public bool correctSpotOnShore(List<Tile> tiles){
		return correctSpotForOn (tiles,TileType.Water);
	}
	public bool correctSpotForOn(List<Tile> tiles, TileType tt){
		int other = 0;
		int land  = 0;
		Tile[] otherTiles = new Tile[Mathf.Max (tileWidth,tileHeight)];
		foreach (Tile t in tiles) {
			if(t == null){
				return false;
			}
			if (t.Type == tt) {
				other++;
				if ((tileWidth) < other && (tileHeight) < other ) {
					return false;
				}
				otherTiles [other-1] = t;
			}
			if (Tile.IsBuildType (t.Type)) {
				land++;
				if ((tileWidth)*2 < land && (tileHeight)*2 < land ) {
					return false;
				}
			}
			if (Tile.IsUnbuildableType (t.Type,tt)) {
				return false;
			}
		}
		if (otherTiles [0] == null) {
			return false;
		}
		Tile temp = otherTiles [0];
		for (int i = 1; i < otherTiles.Length; i++) {
			if(otherTiles [i] == null){
				return false;
			}
			if (temp.IsNeighbour (otherTiles [i]) == false) {
				return false;
			} 
			temp = otherTiles [i];
		}
		return true;
	}

	public void RotatedStructure(){
		if(canRotate == false) {
			return;
		}
		rotated += 90;
		if (rotated == 360) {
			rotated = 0;
		}
	}

	public List<Tile> roadsAroundStructure(){
		List<Tile> roads = new List<Tile>();
		foreach (Tile item in myBuildingTiles) {
			foreach (Tile n in item.GetNeighbours ()) {
				if(n.structures != null ){
					if (n.structures is Road) {
						roads.Add (n);
					}
				}
			}
		}
		return roads;
	}

	public void Destroy(){
		if (city != null) {
			city.removeStructure (this);
		}
		if(cbStructureDestroy!=null)
			cbStructureDestroy(this);
	}

	//////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 						SAVING & LOADING
	/// 
	//////////////////////////////////////////////////////////////////////////////////////
	public XmlSchema GetSchema() {
		return null;
	}
	public abstract void WriteXml (XmlWriter writer);
	public abstract void ReadXml (XmlReader reader);

	public void BaseWriteXml(XmlWriter writer){
		writer.WriteAttributeString ("BuildID", buildID.ToString ()); 
		writer.WriteAttributeString ("ID", ID.ToString ()); //change this to id
		writer.WriteAttributeString ("BuildingTile_X", myBuildingTiles [0].X.ToString ());
		writer.WriteAttributeString ("BuildingTile_Y", myBuildingTiles [0].Y.ToString ());
		writer.WriteAttributeString("Rotated", rotated.ToString());
	}
	public void BaseReadXml(XmlReader reader){
		rotated = int.Parse( reader.GetAttribute("Rotated") );
		buildID = int.Parse( reader.GetAttribute("BuildID") );
	}
}

