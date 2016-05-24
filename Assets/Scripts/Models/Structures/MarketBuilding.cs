using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class MarketBuilding : UserStructure {
	public List<Route> myRoutes;
	public List<Structure> RegisteredSturctures;
	public MarketBuilding(int id){
		this.ID = id;
		tileWidth = 4;
		tileHeight = 4;
		name = "market";
		buildcost = 500;
		maintenancecost = 10;
		BuildTyp = BuildTypes.Single;
		myBuildingTyp = BuildingTyp.Blocking;
		buildingRange = 18;
	}
	public MarketBuilding(){
	}
	protected MarketBuilding(MarketBuilding str){
		this.ID = str.ID;
		this.name = str.name;
		this.tileWidth = str.tileWidth;
		this.tileHeight = str.tileHeight;
		this.mustBeBuildOnShore = str.mustBeBuildOnShore;
		this.maintenancecost = str.maintenancecost;
		this.maxNumberOfWorker = str.maxNumberOfWorker;
		this.buildcost = str.buildcost;
		this.BuildTyp = str.BuildTyp;
		this.rotated = str.rotated;
		this.hasHitbox = str.hasHitbox;
		this.buildingRange = str.buildingRange;
	}
	public override Structure Clone (){
		return new MarketBuilding(this);
	}
	public override void update (float deltaTime){
		base.update_Worker (deltaTime);
	}
	public override void OnBuild(){
		myWorker = new List<Worker> ();
		RegisteredSturctures = new List<Structure> ();
		myRoutes = GetMyRoutes ();
		jobsToDo = new List<ProductionBuilding> ();
		// add all the tiles to the city it was build in
		Tile t = myBuildingTiles [0];
		this.city = t.myCity;
		//dostuff thats happen when build
		for (int w = 0; w < buildingRange; w++) {
			city.addTile (t);
			Tile tn = t.North ();
			for (int h = 1; h < buildingRange; h++) {
				city.addTile (tn);
				tn.North ();
			}
		}
		foreach(Tile rangeTile in myRangeTiles){
			if(rangeTile.structures == null){
				continue;
			}
			if(rangeTile.structures is ProductionBuilding){
				if (RegisteredSturctures.Contains (rangeTile.structures) == false) {
					((ProductionBuilding)rangeTile.structures).RegisterOutputChanged (OnOutputChangedStructure);
					RegisteredSturctures.Add (rangeTile.structures);
				}
			}
		}
		BuildController.Instance.RegisterStructureCreated (OnStructureBuild);
	}
	public void OnOutputChangedStructure(Structure str){
		if(str is ProductionBuilding == false){
			return;
		}
		foreach (Route item in ((ProductionBuilding)str).GetMyRoutes()) {
			if (myRoutes.Contains (item)) {
				foreach (Tile tile in str.neighbourTiles) {
					if(tile.structures is Road == false){
						continue;
					}
					if(myRoutes.Contains(((Road)tile.structures).Route) == false){
						continue;
					}
					if (((ProductionBuilding)str).outputClaimed == false) {
						jobsToDo.Add ((ProductionBuilding)str);
					}
					return;
				}
			}
		}
	}

	public void OnStructureBuild(Structure structure){
		if(this == structure){
			return;
		}
		if(structure.myBuildingTyp == BuildingTyp.Production){
			foreach (Tile item in structure.myBuildingTiles) {
				if(myRangeTiles.Contains (item)){
					break;
				}
			}
			((ProductionBuilding)structure).RegisterOutputChanged (OnOutputChangedStructure);
		}
		if (structure.myBuildingTyp == BuildingTyp.Pathfinding) {
			if(neighbourTiles.Contains (structure.myBuildingTiles[0])){
				Route r = ((Road)structure).Route;
				if (myRoutes.Contains (r) == false) {
					myRoutes.Add (r);
				}
			}
		}
	}

	public override void WriteXml (XmlWriter writer){
		writer.WriteAttributeString("Name", name ); //change this to id
		writer.WriteAttributeString("BuildingTile_X", myBuildingTiles[0].X.ToString () );
		writer.WriteAttributeString("BuildingTile_Y", myBuildingTiles[0].Y.ToString () );
		writer.WriteElementString("Rotated", rotated.ToString());
		if (myWorker != null) {
			writer.WriteStartElement ("Workers");
			foreach (Worker w in myWorker) {
				writer.WriteStartElement ("Worker");
				w.WriteXml (writer);
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}
		
	}
	public override void ReadXml(XmlReader reader) {
		rotated = int.Parse( reader.ReadElementString("Rotated") );
		if(reader.ReadToDescendant("Workers") ) {
			do {
				Worker w = new Worker(this);
				w.ReadXml (reader);
				myWorker.Add (w);
			} while( reader.ReadToNextSibling("Worker") );
		}
	}
}
