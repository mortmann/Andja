using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public enum Climate {Cold,Middle,Warm};

public class Island : IXmlSerializable,IGEventable{
    
	public Path_TileGraph tileGraphIslandTiles { get; protected set; }
	public Path_TileGraph tileGraphAroundIslandTiles { get; protected set; }

    public List<Tile> myTiles;
    public List<City> myCities;

	public Climate myClimate;
	public List<Fertility> myFertilities;
	public Dictionary<string,int> myRessources;

	public Vector2 min;
	public Vector2 max;
	public City wilderniss;
	public bool allReadyHighlighted;

	Action<GameEvent> cbEventCreated;
	Action<GameEvent> cbEventEnded;


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
        tileGraphIslandTiles = new Path_TileGraph(this);
		tileGraphAroundIslandTiles = new Path_TileGraph (min, max);

		allReadyHighlighted = false;

		World.current.RegisterOnEvent (OnEventCreated,OnEventEnded);
    }
    protected void IslandFloodFill(Tile tile) {
        if (tile == null) {
            // We are trying to flood fill off the map, so just return
            // without doing anything.
            return;
        }
        if (tile.Type == TileType.Ocean) {
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


            if (t.Type != TileType.Ocean && t.myIsland != this) {
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
		myCities.Add (new City(myTiles,this)); 
		wilderniss = myCities [0];
//		BuildController.Instance.BuildOnTile (myTiles,true,BuildController.Instance.structurePrototypes[3],true);
    }

    public void update(float deltaTime) {
		for (int i = 0; i < myCities.Count; i++) {
			myCities[i].update(deltaTime);
        }
    }
	public void AddStructure(Structure str){
		allReadyHighlighted = false;
		if(str.City == wilderniss){
//			Debug.LogWarning ("adding to wilderniss wanted?");
		}
		str.City.addStructure (str);
	}
	public City CreateCity(int playerNumber) {
		allReadyHighlighted = false;
		City c = new City(playerNumber,this,World.current.allNeeds);
		myCities.Add (c);
        return c;
    }
	public void RemoveCity(City c) {
		myCities.Remove (c);
	}

	public void RegisterOnEvent(Action<GameEvent> create,Action<GameEvent> ending){
		cbEventCreated += create;
		cbEventEnded += ending;
	}
	public void OnEventCreated(GameEvent ge){
		OnEvent (ge,cbEventCreated);
	}
	void OnEvent(GameEvent ge, Action<GameEvent> ac){
		if(ge.target is Island){
			if(ge.target == this){
				if(ac!=null){
					ac (ge);
				}
			}
			return;
		} else {
			if(ac!=null){
				ac (ge);
			}
			return;	
		}
//		if(ge.target is City){
//			ac (ge);
//			return;
//		} 
//		if(ge.target is Structure) {
//			if(ac!=null){
//				ac (ge);
//			}
//		}
//		if(ge.target is Player){
//			for (int i = 0; i < myCities.Count; i++) {
//				if(ge.target.GetPlayerNumber ()!=myCities[i].playerNumber){
//					continue;
//				}
//				ac (ge);
//			}
//			return;
//		}
	}
	public void OnEventEnded(GameEvent ge){
		OnEvent (ge,cbEventEnded);
	}
	public int GetPlayerNumber(){
		return -1;
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
		writer.WriteStartElement("fertilities");
		foreach(Fertility fer in myFertilities){
			writer.WriteStartElement("fertility");
			writer.WriteAttributeString ("ID",fer.ID.ToString ());
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
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
		reader.ReadToFollowing ("fertilities");
		List<int> ferIDs = new List<int>();
		while (reader.Read ()) {
			if(reader.IsStartElement ("fertility")==false){
				Debug.Log (reader.Name ); 
				if(reader.Name == "fertilities"){
					break;
				}
				continue;
			}	
			ferIDs.Add (int.Parse (reader.GetAttribute ("ID")));

		}
		foreach (int item in ferIDs) {
			myFertilities.Add (World.current.getFertility(item)); 
			Debug.Log (World.current.getFertility(item).name); 
		}
		myCities = new List<City> ();
		wilderniss = null;
		reader.ReadToFollowing ("Cities");
		if (reader.ReadToDescendant ("City")) {
			//Workaround for readnextsibling
			//why ever it is not working here
			//read as long as it reads cities
			//if it reads city start the do more
			do {
				if(reader.IsStartElement ("City")==false){
					Debug.Log (reader.Name ); 
					if(reader.Name == "Cities"){
						return;
					}
					continue;
				}				 

				int playerNumber = int.Parse (reader.GetAttribute ("Player"));

				City c = null;
				if (playerNumber == -1) {
					c = new City (this.myTiles,this);
				} else {
					c = new City (playerNumber, this, World.current.allNeeds);	
				}
//				c.ReadXml (reader);
				myCities.Add (c);

				 
			} while(reader.Read ());
		}
	}
}
