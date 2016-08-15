using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class MarketBuilding : OutputStructure {
	public List<Structure> RegisteredSturctures;
	public List<Structure> OutputMarkedSturctures;
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
		this.canTakeDamage = true;

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
		this.hasHitbox = str.hasHitbox;
		this.canTakeDamage = str.canTakeDamage;

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
		OutputMarkedSturctures = new List<Structure> ();
		jobsToDo = new Dictionary<OutputStructure, Item[]> ();
		// add all the tiles to the city it was build in
		//dostuff thats happen when build
		City.addTiles (myRangeTiles);
		foreach(Tile rangeTile in myRangeTiles){
			if(rangeTile.Structure == null){
				continue;
			}
			if(rangeTile.Structure is OutputStructure){
				if (RegisteredSturctures.Contains (rangeTile.Structure) == false) {
					((OutputStructure)rangeTile.Structure).RegisterOutputChanged (OnOutputChangedStructure);
					RegisteredSturctures.Add (rangeTile.Structure);
				}
			}
		}
		City.RegisterStructureAdded (OnStructureAdded);
	}
	public void OnOutputChangedStructure(Structure str){
		if(str is OutputStructure == false){
			return;
		}
		bool hasOutput = false;
		for (int i = 0; i < ((OutputStructure)str).output.Length; i++) {
			if(((OutputStructure)str).output[i].count > 0){
				hasOutput = true;
				break;
			}
		}
		if(hasOutput == false){
			if(OutputMarkedSturctures.Contains (str)){
				OutputMarkedSturctures.Remove (str);
			}
			return;
		}
		if(jobsToDo.ContainsKey ((OutputStructure)str)){
			jobsToDo.Remove ((OutputStructure)str);
		}
		List<Route> myRoutes = GetMyRoutes ();
		//get the roads around the structure
		foreach (Route item in ((OutputStructure)str).GetMyRoutes()) {
			//if one of them is in my roads
			if (myRoutes.Contains (item)) {
				//if we are here we can get there through atleast 1 road
//				foreach (Tile tile in str.neighbourTiles) {
//					if(tile.Structure is Road == false){
//						continue;
//					}
//					if(myRoutes.Contains(((Road)tile.Structure).Route) == false){
//						continue;
//					}
					if (((OutputStructure)str).outputClaimed == false) {
						jobsToDo.Add ((OutputStructure)str,null);
					}
				if(OutputMarkedSturctures.Contains (str)){
					OutputMarkedSturctures.Remove (str);
				}
					return;
//				}
			}
		}
		//if were here there is noconnection between here and a the structure
		//so remember it for the case it gets connected to it.
		if(OutputMarkedSturctures.Contains (str)){
			return;
		}
		OutputMarkedSturctures.Add (str);
	}
	protected override void OnDestroy (){
		List<Tile> h = new List<Tile> (myBuildingTiles);
		h.AddRange (myRangeTiles); 
		City.removeTiles (h);
		foreach (Worker item in myWorker) {
			item.Destroy ();
		}
	} 


	public void OnStructureAdded(Structure structure){
		if(this == structure){
			return;
		}
		if(structure is OutputStructure){
			foreach (Tile item in structure.myBuildingTiles) {
				if(myRangeTiles.Contains (item)){
					((OutputStructure)structure).RegisterOutputChanged (OnOutputChangedStructure);
					break;
				}
			}
		}
		//IF THIS is a pathfinding structure check for new road
		//if true added that to the myroads

		if (structure.myBuildingTyp == BuildingTyp.Pathfinding) {
			List<Route> myRoutes = GetMyRoutes ();
			if(neighbourTiles.Contains (structure.myBuildingTiles[0])){
				if (myRoutes.Contains (((Road)structure).Route) == false) {
					myRoutes.Add (((Road)structure).Route);
				}
			}
			for (int i = 0; i < OutputMarkedSturctures.Count; i++) {
				foreach (Route item in ((OutputStructure)OutputMarkedSturctures[i]).GetMyRoutes ()) {
					if(myRoutes.Contains (item)){
						OnOutputChangedStructure(OutputMarkedSturctures[i]);				
						break;//breaks only the innerloop eg the routes loop
					}
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
