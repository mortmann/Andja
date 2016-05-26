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

    Action<Unit> cbUnitCreated;
	Action<Worker> cbWorkerCreated;
    Action<Tile> cbTileChanged;

    //get { return height; } protected set { height = value;}

    public World(int width = 1000, int height = 1000){
        this.Width = width;
        this.Height = height;
        tiles = new Tile[width, height];
        for (int x = 0; x < width; x++) {
            for (int y= 0; y < height ; y++) {
                tiles[x, y] = new Tile(this, x, y);
                if (x > 40 && x < 60) {
                    if (y > 40 && y < 60) {
                        tiles[x, y].Type = TileType.Dirt;
                    }
                }
            }
        }
        
//		LoadPrototypsNeedsFromXML ();

        tileGraph = new Path_TileGraph(this);
        islandList = new List<Island>();
        units = new List<Unit>();
//        CreateUnit(tiles[30, 30]);    
        islandList.Add(new Island(tiles[41, 41]));
		islandList [0].CreateCity ();
		foreach(Tile t in islandList [0].myTiles){
			islandList [0].myCities [0].addTile (t);
		}
    }
	public World(){
	}
	public void SetupWorld(int Width, int Height){
//		LoadPrototypsNeedsFromXML ();
		tiles = new Tile[Width, Height];
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				tiles [x, y] = new Tile (this, x, y);
			}
		}

		this.Width = Width;
		this.Height = Width;
		tileGraph = new Path_TileGraph(this);
		islandList = new List<Island>();
		units = new List<Unit>();
	}
    internal void update(float deltaTime) {
        foreach(Island i in islandList) {
            i.update(deltaTime);
        }
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
		GameObject go = GameObject.Instantiate((GameObject)Resources.Load ("Prefabs/ship",typeof(GameObject)));
//        Unit c = go.AddComponent<Unit>();
		Unit c = go.GetComponent<Unit> ();
        units.Add(c);
        if (cbUnitCreated != null)
            cbUnitCreated(c);
        return c;
    }

	public void LoadPrototypsNeedsFromXML(){
		allNeeds = new List<Need> ();
		XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
		TextAsset ta = ((TextAsset)Resources.Load("XMLs/needs", typeof(TextAsset)));
		xmlDoc.LoadXml(ta.text); // load the file.
		foreach(XmlElement node in xmlDoc.SelectNodes("Needs/Need")){
			Need need = new Need ();
			need.id = int.Parse(node.SelectSingleNode("ID").InnerText);
			need.startLevel = int.Parse(node.SelectSingleNode("Level").InnerText);
			need.name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			need.structure = BuildController.Instance.structurePrototypes [int.Parse(node.SelectSingleNode("Structure").InnerText)];
			need.item = BuildController.Instance.allItems [int.Parse(node.SelectSingleNode("Item").InnerText)];
			allNeeds.Add (need);
		}
	}


	public void CreateWorkerGameObject(Worker worker) {
		if (cbWorkerCreated != null)
			cbWorkerCreated(worker);
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

	//////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 						SAVING & LOADING
	/// 
	//////////////////////////////////////////////////////////////////////////////////////

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
//
//		writer.WriteStartElement("Units");
//		foreach(Unit c in units) {
//			writer.WriteStartElement("Character");
//			c.WriteXml(writer);
//			writer.WriteEndElement();
//
//		}
//		writer.WriteEndElement();
//
		/*		writer.WriteStartElement("Width");
		writer.WriteValue(Width);
		writer.WriteEndElement();
*/


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
				ReadXml_Tiles(reader);
				break;
			case "Islands":
				ReadXml_Islands(reader);
				break;
//			case "Units":
//				ReadXml_Units(reader);
//				break; 
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
				Unit c = CreateUnit( tiles[x,y] );
				c.ReadXml(reader);
			} while( reader.ReadToNextSibling("Unit") );
		}
	}

}
