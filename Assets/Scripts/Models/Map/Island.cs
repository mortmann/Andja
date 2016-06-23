using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public enum Climate {Cold,Middle,Warm};

public class Island : IXmlSerializable{
    
	public Path_TileGraph tileGraph { get; protected set; }

    public List<Tile> myTiles;
    public List<City> myCities;

	public Climate myClimate;
	public List<Fertility> myFertilities;
	public Dictionary<string,int> myRessources;

	public Vector2 min;
	public Vector2 max;
	public City wilderniss;
	public bool allReadyHighlighted;

    //TODO: get a tile to start with!
	public Island(Tile startTile, Climate climate = Climate.Middle) {
		myFertilities = new List<Fertility> ();


		myRessources = new Dictionary<string, int> ();
		myRessources ["stone"] = int.MaxValue;
        myTiles = new List<Tile>();
        myTiles.Add(startTile);
        myCities = new List<City>();
        startTile.myIsland = this;
        foreach (Tile t in startTile.GetNeighbours()) {
            IslandFloodFill(t);
        }
        tileGraph = new Path_TileGraph(this);
		allReadyHighlighted = false;
    }
    protected void IslandFloodFill(Tile tile) {
        if (tile == null) {
            // We are trying to flood fill off the map, so just return
            // without doing anything.
            return;
        }
        if (tile.Type == TileType.Water) {
            // Water is the border of every island :>
            return;
        }
        if(tile.myIsland == this) {
            // already in there
            return;
        }
		min = new Vector2 (tile.X, tile.Y);
		max = new Vector2 (tile.X, tile.Y);
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);
        while (tilesToCheck.Count > 0) {
			
            Tile t = tilesToCheck.Dequeue();
			if (min.x > t.X) {
				min.x = t.X;
			}
			if (min.y > t.Y) {
				min.y= t.Y;
			}
			if (max.x < t.X) {
				max.x = t.X;
			}
			if (max.y < t.Y) {
				max.y = t.Y;
			}


            if (t.Type != TileType.Water && t.myIsland != this) {
                myTiles.Add(t);
                t.myIsland = this;
                Tile[] ns = t.GetNeighbours();
                foreach (Tile t2 in ns) {
                    tilesToCheck.Enqueue(t2);
                }
            }
        }

		//city that contains all the structures like trees that doesnt belong to any player
		//so it has the playernumber -1 -> needs to be checked for when buildings are placed
		//have a function like is notplayer city
		//it does not need NEEDs
		myCities.Add (new City(-1,this,null,myTiles)); 
		wilderniss = myCities [0];
    }

    public void update(float deltaTime) {
		for (int i = 0; i < myCities.Count; i++) {
			myCities[i].update(deltaTime);
        }
    }
	public void AddStructure(Structure str){
		allReadyHighlighted = false;
		if(str.city == wilderniss){
			Debug.LogWarning ("adding to wilderniss wanted?");
		}
		str.city.addStructure (str);
	}
    public City CreateCity() {
		allReadyHighlighted = false;
		City c = new City(PlayerController.Instance.number,this,World.current.allNeeds);
		myCities.Add (c);
        return c;
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
		writer.WriteAttributeString("StartTile_X",myTiles[0].X.ToString ());
		writer.WriteAttributeString("StartTile_Y",myTiles[0].Y.ToString ());
		writer.WriteAttributeString ("Climate",((int)myClimate).ToString ());

		writer.WriteStartElement("Cities");
		foreach (City c in myCities) {
			writer.WriteStartElement("City");
			c.WriteXml(writer);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}

	public void ReadXml(XmlReader reader) {
		myClimate = (Climate)int.Parse(reader.GetAttribute ("Climate"));
		if (reader.ReadToDescendant ("City")) {
			do {
				int playerNumber = int.Parse( reader.GetAttribute("Player") );
				City c = new City (playerNumber,this, World.current.allNeeds);
				c.ReadXml (reader);
				myCities.Add (c);
			} while(reader.ReadToNextSibling ("City"));
		}
	}

}
