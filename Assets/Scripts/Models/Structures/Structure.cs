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

public abstract class Structure : IXmlSerializable {
	public int ID;
	public int playerID;
	public string name;
	public City city;
    public bool isWalkable { get; protected set; }
	public bool hasHitbox { get; protected set; }

	public int buildingRange = 3;

	public int rotated = 0; 
	public bool canRotate = true;

	public bool canBeBuildOver = false;

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
	public bool mustBeBuildOnShore;
	public int maintenancecost;
	public int buildcost;
	public BuildTypes BuildTyp;
	public BuildingTyp myBuildingTyp = BuildingTyp.Blocking;
	public string connectOrientation;


	public Structure(string name, int buildcost = 50){
		this.name = name;
		this.tileWidth = 1;
		this.tileHeight = 1;
		maintenancecost = 0;
		buildcost = 25;
		BuildTyp = BuildTypes.Path;
		myBuildingTyp = BuildingTyp.Pathfinding;
		buildingRange = 0;
	}

    public Structure() {
    }
    protected Structure(Structure str){
		this.name = str.name;
		this.tileWidth = str.tileWidth;
		this.tileHeight = str.tileHeight;
		this.mustBeBuildOnShore = str.mustBeBuildOnShore;
		this.maintenancecost = str.maintenancecost;
		this.buildcost = str.buildcost;
		this.BuildTyp = str.BuildTyp;
		this.rotated = str.rotated;
		this.hasHitbox = str.hasHitbox;
		this.buildingRange = str.buildingRange;
	}

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
		// TODO add check for Mines eg buildings on mountains!
			// if it has to be on water
		if (mustBeBuildOnShore == false) {
			if (tileWidth == 1 && tileHeight == 1) {
				if(tiles[0].structures != null && tiles [0].structures.canBeBuildOver){
					if(tiles [0].structures.name == this.name){
						return false;
					}
				}
				if (Tile.checkTile (tiles [0]) == false) {
					return false;
				}
				myBuildingTiles.Add (tiles [0]);
			} else {
				if(correctSpot(tiles)){
					myBuildingTiles.AddRange (tiles);
				} else {
					return false;
				}
			}
		} else {
			//if it has to be on land 
			if (tileWidth == 1 && tileHeight == 1) {
				if(tiles [0].structures.canBeBuildOver){
					if(tiles [0].structures.name == this.name){
						return false;
					}
				}
				if (Tile.checkTile (tiles[0],mustBeBuildOnShore) == false) {
					return false;
				}
				myBuildingTiles.Add (tiles[0]);
			} 
			else {
				if(correctSpotForOnShore(tiles)){
					myBuildingTiles.AddRange (tiles);
				} else {
					return false;
				}
			}
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
	public List<Tile> GetBuildingTiles(float x , float y){
		List<Tile> tiles = new List<Tile> ();
		for (int w = 0; w < tileWidth; w++) {
			tiles.Add ( WorldController.Instance.world.GetTileAt(x + w, y));
			for (int h = 1; h < tileHeight; h++) {
				tiles.Add (WorldController.Instance.world.GetTileAt (x + w, y + h));
			}
		}
		return tiles;
	}
	private void CalculatePrototypTiles(){
		List<Tile> temp = new List<Tile> ();
		float x;
		float y;
		//get the tile at bottom left to create a "prototype circle"
		Tile firstTile = WorldController.Instance.world.GetTileAt (0 + buildingRange+tileWidth/2,0 + buildingRange+tileHeight/2);
		Vector2 center = new Vector2 (firstTile.X, firstTile.Y);
		if (tileWidth > 1) {
			center.x += 0.5f + ((float)tileWidth) / 2 - 1;
		}
		if (tileHeight > 1) {
			center.y += 0.5f + ((float)tileHeight) / 2 - 1;
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
		if(buildingRange == 0){
			return;
		}
		myPrototypeTiles = new List<Tile> ();

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
	public bool correctSpot(List<Tile> tiles){
		foreach(Tile t in tiles){
			if (Tile.checkTile (t) == false) {
				return false;
			}
		}
		return true;
		// old code maybe be useful later
//			Tile t = tiles[0];
//			myTiles.Add (tiles[0]);
//			for (int w = 0; w < tileWidth; w++) {
//				if (Tile.checkTile (t) == false) {
//					return false;
//				}
//				Tile tn = t;
//				for (int h = 0; h < tileHeight - 1; h++) {
//					tn = tn.North ();
//					if (Tile.checkTile (tn) == false) {
//						return false;
//					}
//					myTiles.Add (tn);
//				}
//				myTiles.Add (t);
//				t = t.East ();
//			}
	}
	public bool correctSpotForOnShore(List<Tile> tiles){
		int water = 0;
		int land  = 0;
		Tile[] waterTiles = new Tile[Mathf.Max (tileWidth,tileHeight)];
		foreach (Tile t in tiles) {
			if(t == null){
				return false;
			}
			if (t.Type == TileType.Water) {
				water++;
				if ((tileWidth) < water && (tileHeight) < water ) {
					return false;
				}
				waterTiles [water-1] = t;
			}
			if (Tile.IsBuildType (t.Type)) {
				land++;
				if ((tileWidth)*2 < land && (tileHeight)*2 < land ) {
					return false;
				}
			}
		}
		Tile temp = waterTiles [0];
		for (int i = 1; i < waterTiles.Length; i++) {
			if (temp.IsNeighbour (waterTiles [i]) == false) {
				return false;
			} 
			temp = waterTiles [i];
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


	//			Queue<Tile> temp = new Queue<Tile> ();
	//			int times = ((this.buildingRange*2)) + _tileWidth;
	//			times = times * times;
	//			times -= myBuildingTiles.Count;
	//			times /= 4;
	//			for (int i = 0; i < temp.Count; i++) {
	//
	//				Debug.Log (times);
	//				foreach (Tile n in temp.Dequeue().GetNeighbours (diag)) {
	//					if(temp.Contains (n)==false){
	//						temp.Enqueue (n);
	//						times--;
	//						if (times <= (4 * myBuildingTiles.Count + 4)/4) {
	//							Debug.Log ("times " + times);
	//							diag = false;
	//						}
	//						if (times == 0) {
	//							break;
	//						}
	//					}	
	//				}
	//
	//			}
	//			foreach (Tile t in myBuildingTiles) {
	//				temp.Enqueue(t);
	//			}
	////			bool diag = true;
	//FIXME need to change this its to a more optimized solution
	//			for (int i = 0; i < buildingRange; i++) {
	//				for (int a = 0; a < myBuildingTiles.Count; a++) {
	//					Tile t = temp.Dequeue ();		
	//					foreach (Tile n in t.GetNeighbours (diag)) {
	//						if (temp.Contains (n) == false) {
	//							myRangeTiles.Add (n);
	//							temp.Enqueue (n);
	//						}
	//					}
	//				}
	//			}
	//			myRangeTiles = new List<Tile>(temp);


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
	public abstract void WriteXml(XmlWriter writer);
	public abstract void ReadXml (XmlReader reader);
}

