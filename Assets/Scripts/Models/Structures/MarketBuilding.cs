using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class MarketBuilding : UserStructure {
	public List<Route> myRoutes;
	public List<Structure> RegisteredSturctures;
	public MarketBuilding(int id){
		
		hasHitbox = true;
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
		this.hasHitbox = hasHitbox;
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
		jobsToDo = new Dictionary<UserStructure, Item[]> ();
		// add all the tiles to the city it was build in
		//dostuff thats happen when build
		City.addTiles (myRangeTiles);
		foreach(Tile rangeTile in myRangeTiles){
			if(rangeTile.Structure == null){
				continue;
			}
			if(rangeTile.Structure is UserStructure){
				if (RegisteredSturctures.Contains (rangeTile.Structure) == false) {
					((UserStructure)rangeTile.Structure).RegisterOutputChanged (OnOutputChangedStructure);
					RegisteredSturctures.Add (rangeTile.Structure);
				}
			}
		}
		City.RegisterStructureAdded (OnStructureAdded);
	}
	public void OnOutputChangedStructure(Structure str){
		if(str is UserStructure == false){
			return;
		}
		bool hasOutput = false;
		for (int i = 0; i < ((UserStructure)str).output.Length; i++) {
			if(((UserStructure)str).output[i].count > 0){
				hasOutput = true;
				break;
			}
		}
		if(hasOutput == false){
			return;
		}
		if(jobsToDo.ContainsKey ((UserStructure)str)){
			jobsToDo.Remove ((UserStructure)str);
		}
		foreach (Route item in ((UserStructure)str).GetMyRoutes()) {
			if (myRoutes.Contains (item)) {
				foreach (Tile tile in str.neighbourTiles) {
					if(tile.Structure is Road == false){
						continue;
					}
					if(myRoutes.Contains(((Road)tile.Structure).Route) == false){
						continue;
					}
					if (((UserStructure)str).outputClaimed == false) {
						jobsToDo.Add ((UserStructure)str,null);
					}
					return;
				}
			}
		}
	}

	public void OnStructureAdded(Structure structure){
		if(this == structure){
			return;
		}
		if(structure.myBuildingTyp == BuildingTyp.Production){
			foreach (Tile item in structure.myBuildingTiles) {
				if(myRangeTiles.Contains (item)){
					((UserStructure)structure).RegisterOutputChanged (OnOutputChangedStructure);
					break;
				}
			}
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

	public override Item[] getOutput(Item[] getItems,int[] maxAmounts){
		Item[] temp = new Item[getItems.Length];
		for (int i = 0; i < getItems.Length; i++) {
			if(City.myInv.GetAmountForItem (getItems[i]) == 0){
				continue;
			}	
			temp [i] = City.myInv.getItemWithMaxAmount (getItems [i], maxAmounts [i]);
		}
		return temp;
	}

	public override void WriteXml (XmlWriter writer){
		BaseWriteXml (writer);
		WriteUserXml (writer);
	}
	public override void ReadXml(XmlReader reader) {
		BaseReadXml (reader);
		ReadUserXml(reader);
	}
}
