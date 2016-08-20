using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class EditorIsland  : IXmlSerializable {

	public static EditorIsland current;
	public EditorTile[,] tiles;
	public int width;
	public int height;
	public Dictionary<EditorTile,int[]> structures;
	public EditorIsland(){}
	public Climate myClimate;
	public EditorIsland(int width,int height, Climate myClimate){
		current = this;
		SetupWorld (width, height,myClimate);
		structures = new Dictionary<EditorTile, int[]> ();
	}
	public void SetupWorld(int width,int height, Climate myClimate){
		this.width = width;
		this.height = height;
		this.myClimate = myClimate;
		tiles = new EditorTile[width,height];
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				tiles [x, y] = new EditorTile (x, y);
				tiles [x, y].RegisterTileChangedCallback (OnTileChange);
			}
		}

	}
	public void OnTileChange(EditorTile et){
		EditorTileSpriteController.Instance.OnTileChanged (et);
	}
	public EditorTile GetTileAt(int x, int y){
		if (x >= width ||y >= height ) {
			return null;
		}
		if (x < 0 || y < 0) {
			return null;
		}
		return tiles [x, y];
	}
	public void AddStructure(int id,int stage,EditorTile tile){
		int[] temp = { id, stage };
		structures.Add (tile,temp); 
	}
	public void RemoveStructure(EditorTile tile){
		structures.Remove (tile);
	}
	public XmlSchema GetSchema() {
		return null;
	}

	public void WriteXml(XmlWriter writer) {
		// Save info here
		writer.WriteAttributeString( "EditorWidth", width.ToString() );
		writer.WriteAttributeString( "EditorHeight", height.ToString() );

		Vector2 min = new Vector2 (float.MaxValue,float.MaxValue);
		Vector2 max = new Vector2 (0,0);
		foreach (EditorTile item in tiles) {
			if(item.Type!=TileType.Ocean){
				if(min.x>item.X){
					min.x = item.X;
				}
				if(min.y>item.Y){
					min.y = item.Y;
				}
				if(max.x<item.X){
					max.x = item.X;
				}
				if(max.y<item.Y){
					max.y = item.Y;
				}
			}
		}
		Debug.Log (min + " " + max); 
		if(max.magnitude>0){
			writer.WriteAttributeString( "GameWidth", (0).ToString() );
			writer.WriteAttributeString( "GameHeight", (0).ToString() );
		} else {
			writer.WriteAttributeString( "GameWidth", (max.x-min.x +1).ToString() );
			writer.WriteAttributeString( "GameHeight", (max.y-min.y +1).ToString() );
		}
		writer.WriteAttributeString( "Climate", ((int)myClimate).ToString() );
		writer.WriteStartElement("Tiles");
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if (tiles [x, y].Type != TileType.Ocean) {
					writer.WriteStartElement ("Tile");
					tiles [x, y].WriteXml (writer);
					writer.WriteEndElement ();
				}
			}
		}
		writer.WriteEndElement();

		writer.WriteStartElement("Structures");
		foreach(EditorTile str in structures.Keys) {
			writer.WriteStartElement("Structure");
				writer.WriteAttributeString ("X",str.X.ToString ());
				writer.WriteAttributeString ("Y",str.Y.ToString ());
				writer.WriteAttributeString ("ID",structures[str][0].ToString ());
				writer.WriteAttributeString ("CurrentStage",structures[str][1].ToString ());
			writer.WriteEndElement();
		}
		writer.WriteEndElement();

	}

	public void ReadXml(XmlReader reader) {
		Debug.Log("World::ReadXml");
		// Load info here

		width = int.Parse( reader.GetAttribute("EditorWidth") );
		height = int.Parse( reader.GetAttribute("EditorHeight") );
		myClimate = (Climate)int.Parse( reader.GetAttribute("Climate") );
		SetupWorld(width, height,myClimate);
		while(reader.Read()) {
			switch (reader.Name) {
			case "Tiles":
				if (reader.IsStartElement ())
					ReadXml_Tiles (reader);
				break;
			case "Structures":
				if (reader.IsStartElement ())
					ReadXml_Structure (reader);
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
				tiles[x,y] = new EditorTile(x,y); //save only landtiles
				tiles[x,y].ReadXml(reader);
			} while ( reader.ReadToNextSibling("Tile") );
		}

	}
	void ReadXml_Structure(XmlReader reader) {
		structures = new Dictionary<EditorTile, int[]> ();
		Debug.Log("ReadXml_Tiles");
		if( reader.ReadToDescendant("Structure") ) {
			do {
				int x = int.Parse( reader.GetAttribute("X") );
				int y = int.Parse( reader.GetAttribute("Y") );
				EditorTile t = GetTileAt (x,y);
				int[] temp = new int[2] ;
				temp[0]= int.Parse( reader.GetAttribute("ID") );
				temp[1] = int.Parse( reader.GetAttribute("CurrentStage") );
				structures.Add(t,temp);
			} while ( reader.ReadToNextSibling("Structure") );
		}

	}
}



