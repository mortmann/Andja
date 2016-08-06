using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class World : IXmlSerializable{
	
    public Tile[,] tiles { get; protected set; }
    public int Width { get; protected set; }
    public int Height { get; protected set; }
    public Path_TileGraph tileGraph { get; set; }
    public List<Island> islandList { get; protected set; }
    public List<Unit> units { get; protected set; }
	public List<Need> allNeeds;
	public static World current { get; protected set; }
	public Dictionary<Climate,List<Fertility>> allFertilities;
	public Dictionary<int,Fertility> idToFertilities;


    Action<Unit> cbUnitCreated;
	Action<Worker> cbWorkerCreated;
    Action<Tile> cbTileChanged;
	Action<World> cbTileGraphChanged;
    public World(int width = 1000, int height = 1000){
		SetupWorld (width,height);
		for (int x = 30; x < 40; x++) {
			for (int y = 40; y < 60; y++) {
				tiles[x, y].Type = TileType.Dirt;
			}
		}
		for (int x = 60; x < 70; x++) {
			for (int y = 40; y < 60; y++) {
				tiles[x, y].Type = TileType.Dirt;
			}
		}
		CreateUnit(tiles[42, 38]);    
		CreateIsland (31, 41);
		CreateIsland (61, 41);
		tileGraph = new Path_TileGraph(this);

    }
	public World(){
	}
	public void SetupWorld(int Width, int Height){
		current = this;
		this.Width = Width;
		this.Height = Width;
		tiles = new Tile[Width, Height];
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				tiles [x, y] = new Tile (x, y);
			}
		}
		allNeeds = GameObject.FindObjectOfType<BuildController>().allNeeds;
		allFertilities = GameObject.FindObjectOfType<BuildController>().allFertilities;
		idToFertilities= GameObject.FindObjectOfType<BuildController>().idToFertilities;
		tileGraph = new Path_TileGraph(this);
		islandList = new List<Island>();
		units = new List<Unit>();

	}
    internal void update(float deltaTime) {
        foreach(Island i in islandList) {
            i.update(deltaTime);
        }
    }
	internal void fixedupdate(float deltaTime){
		foreach (Unit item in units) {
			item.Update (deltaTime);
		}
	}
	public void CreateIsland(int x, int y){
		Tile t = GetTileAt (x, y);
		if(t.Type == TileType.Water){
			Debug.LogError ("Tried to create island on a water tile at " + t.toString ());
			return;
		}

		float third = (float)Height/3f;
		Climate myClimate =(Climate)Mathf.RoundToInt ( t.Y / third);
		Fertility[] fers = new Fertility[3];
		List<Fertility> climFer = new List<Fertility>(BuildController.Instance.allFertilities [myClimate]);

		for (int i = 0; i < fers.Length; i++) {
			Fertility f = climFer[UnityEngine.Random.Range (0,climFer.Count)];
			climFer.Remove (f);
			fers [i] = f;
		}

		Island island = new Island (t,(Climate)myClimate);
		island.myFertilities = new List<Fertility> (fers);
		islandList.Add (island);

	}


    public Tile GetTileAt(int x,int y){
        if (x >= Width ||y >= Height ) {
            return null;
        }
        if (x < 0 || y < 0) {
            return null;
        }
        return tiles[x, y];
    }
	public bool IsInTileAt(Tile t,float x,float y){
		if (x >= Width ||y >= Height ) {
			return false;
		}
		if (x < 0 || y < 0) {
			return false;
		}
		if (x + 0.5f <= t.X + 0.4f && x + 0.5f >= t.X - 0.4f) {
			if (y + 0.5f <= t.Y + 0.4f && y + 0.5f >= t.Y - 0.4f) {
				return true;
			}
		}
		return false;
	}
    public Tile GetTileAt(float fx, float fy) {
        int x = Mathf.FloorToInt(fx);
        int y = Mathf.FloorToInt(fy);
        if (x >= Width || y >= Height) {
            return null;
        }
        if (x < 0 || y < 0) {
            return null;
        }
        return tiles[x, y];
    }
    public Unit CreateUnit(Tile t) {
		Unit c = new Unit (t);
        units.Add(c);
        if (cbUnitCreated != null)
            cbUnitCreated(c);
        return c;
    }


	public void checkIfInCamera(float lowerX,float lowerY, float upperX,float upperY){
		PlayerController pc = GameObject.FindObjectOfType<PlayerController>();
		for (int i = 0; i < islandList.Count; i++) {
			if (islandList [i].allReadyHighlighted) {
				continue;
			}
			//TODO IS THIS optimal? if not optimise this 
			if (islandList [i].myTiles.Find (x => x.X > lowerX && x.X < upperX && x.Y > lowerY && x.Y < upperY) != null) {
				islandList [i].allReadyHighlighted = true;
				for (int t = 0; t < islandList [i].myTiles.Count; t++) {
					if (islandList [i].myTiles [t].myCity.playerNumber != pc.number) {
						islandList [i].myTiles [t].TileState = TileMark.Dark;
					} else {
						islandList [i].myTiles [t].TileState = TileMark.None;
					}
				}
			}

		}
	}
	public void resetIslandMark(){
		for (int i = 0; i < islandList.Count; i++) {
			if (islandList [i].allReadyHighlighted == false) {
				continue;
			}
			islandList [i].allReadyHighlighted = false;
			for (int t = 0; t < islandList [i].myTiles.Count; t++) {
				islandList [i].myTiles [t].TileState = TileMark.None;
			}
		}
	}
	public Fertility getFertility(int ID){
		return idToFertilities [ID];
	}


	public void invalidateGraph(){
		tileGraph = null;
		tileGraph = new Path_TileGraph (this);
		if (cbTileGraphChanged != null)
			cbTileGraphChanged (this);

	}
	public void CreateWorkerGameObject(Worker worker) {
		if (cbWorkerCreated != null)
			cbWorkerCreated(worker);
	}
	#region callbacks
	public void RegisterTileGraphChanged(Action<World> callbackfunc) {
		cbTileGraphChanged += callbackfunc;
	}

	public void UnregisterTileGraphChanged(Action<World> callbackfunc) {
		cbTileGraphChanged -= callbackfunc;
	}
    public void RegisterTileChanged(Action<Tile> callbackfunc) {
        cbTileChanged += callbackfunc;
    }

    public void UnregisterTileChanged(Action<Tile> callbackfunc) {
        cbTileChanged -= callbackfunc;
    }

    public void RegisterUnitCreated(Action<Unit> callbackfunc) {
        cbUnitCreated += callbackfunc;
    }

    public void UnregisterUnitCreated(Action<Unit> callbackfunc) {
        cbUnitCreated -= callbackfunc;
    }
	public void RegisterWorkerCreated(Action<Worker> callbackfunc) {
		cbWorkerCreated += callbackfunc;
	}

	public void UnregisterWorkerCreated(Action<Worker> callbackfunc) {
		cbWorkerCreated -= callbackfunc;
	}
    // Gets called whenever ANY tile changes
    public void OnTileChanged(Tile t) {
        if (cbTileChanged == null)
            return;

        cbTileChanged(t);
    }
	#endregion
	//////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 						SAVING & LOADING
	/// 
	//////////////////////////////////////////////////////////////////////////////////////
	#region xmlsave
	public XmlSchema GetSchema() {
		return null;
	}

	public void WriteXml(XmlWriter writer) {
		// Save info here
		writer.WriteAttributeString( "Width", Width.ToString() );
		writer.WriteAttributeString( "Height", Height.ToString() );
		writer.WriteAttributeString( "BuildID", BuildController.Instance.buildID.ToString() );
		writer.WriteStartElement("Tiles");
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				if (tiles [x, y].Type != TileType.Water) {
					writer.WriteStartElement ("Tile");
					tiles [x, y].WriteXml (writer);
					writer.WriteEndElement ();
				}
			}
		}
		writer.WriteEndElement();
//
		writer.WriteStartElement("Islands");
		foreach(Island island in islandList) {
			writer.WriteStartElement("Island");
			island.WriteXml(writer);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();

		writer.WriteStartElement("Units");
		foreach(Unit c in units) {
			writer.WriteStartElement("Unit");
			c.WriteXml(writer);
			writer.WriteEndElement();

		}
		writer.WriteEndElement();

	}

	public void ReadXml(XmlReader reader) {
		Debug.Log("World::ReadXml");
		// Load info here

		Width = int.Parse( reader.GetAttribute("Width") );
		Height = int.Parse( reader.GetAttribute("Height") );
		BuildController.Instance.buildID = int.Parse( reader.GetAttribute("BuildID") );
		SetupWorld(Width, Height);
		while(reader.Read()) {
			switch(reader.Name) {
			case "Tiles":
				if(reader.IsStartElement ())
					ReadXml_Tiles(reader);
				break;
			case "Islands":
				if(reader.IsStartElement ())
					ReadXml_Islands(reader);
				break;
			case "Units":
				if(reader.IsStartElement ())
					ReadXml_Units(reader);
				break; 
			}
		}

	}

	void ReadXml_Tiles(XmlReader reader) {
		Debug.Log("ReadXml_Tiles");
		// We are in the "Tiles" element, so read elements until
		// we run out of "Tile" nodes.

		if( reader.ReadToDescendant("Tile") ) {
			// We have at least one tile, so do something with it.
			do {
				int x = int.Parse( reader.GetAttribute("X") );
				int y = int.Parse( reader.GetAttribute("Y") );
//				Debug.Log(x + " " + y);
				tiles[x,y].ReadXml(reader);
			} while ( reader.ReadToNextSibling("Tile") );
		}

	}
	void ReadXml_Islands(XmlReader reader) {
		Debug.Log("ReadXml_Islands");
		if(reader.ReadToDescendant("Island") ) {
			do {
				int x = int.Parse( reader.GetAttribute("StartTile_X") );
				int y = int.Parse( reader.GetAttribute("StartTile_Y") );
				Island i = new Island(GetTileAt (x,y));
				i.ReadXml (reader);
				islandList.Add (i);
			} while( reader.ReadToNextSibling("Island") );
		}
	}
	void ReadXml_Units(XmlReader reader) {
		Debug.Log("ReadXml_Units");
		if(reader.ReadToDescendant("Unit") ) {
			do {
				int x = int.Parse( reader.GetAttribute("currTile_X") );
				int y = int.Parse( reader.GetAttribute("currTile_Y") );
				Unit u = CreateUnit( tiles[x,y] );
				u.ReadXml(reader);
			} while( reader.ReadToNextSibling("Unit") );
		}
	}
	#endregion
}
