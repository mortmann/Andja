using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class MarketPrototypData : OutputPrototypData {
	public float takeOverStartGoal = 100;
}



[JsonObject(MemberSerialization.OptIn)]
public class MarketBuilding : OutputStructure {

	#region Serialize

	[JsonPropertyAttribute] public int level=1;
	[JsonPropertyAttribute] public float takenOverState = 0;

	#endregion
	#region RuntimeOrOther

	public List<Structure> RegisteredSturctures;
	public List<Structure> OutputMarkedSturctures;

	public float takeOverStartGoal {get{ return MarketData.maxOutputStorage;}}

	protected MarketPrototypData _marketData;
	public MarketPrototypData  MarketData {
		get { if(_marketData==null){
				_marketData = (MarketPrototypData)PrototypController.Instance.GetPrototypDataForID (ID);
			}
			return _marketData;
		}
	}
	#endregion

	public MarketBuilding(int id,MarketPrototypData  MarketData){
		this.ID = id;
		_marketData = MarketData;
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public MarketBuilding(){
	}
	protected MarketBuilding(MarketBuilding str){
		BaseCopyData (str);
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
			if(rangeTile.myCity!=City){
				continue;
			}
			OnStructureAdded (rangeTile.Structure);
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
				if (((OutputStructure)str).outputClaimed == false) {
					jobsToDo.Add ((OutputStructure)str,null);
				}
				if(OutputMarkedSturctures.Contains (str)){
					OutputMarkedSturctures.Remove (str);
				}
					return;
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
		base.OnDestroy ();
		List<Tile> h = new List<Tile> (myBuildingTiles);
		h.AddRange (myRangeTiles); 
		City.removeTiles (h);
	} 


	public void OnStructureAdded(Structure structure){
		if(structure == null){
			return;
		}
		if(this == structure){
			return;
		}
		if(structure.City!=City){
			return;
		}
		if(structure is OutputStructure){
			if(((OutputStructure)structure).forMarketplace==false){
				return;
			}
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
	public void TakeOverMarketBuilding(float deltaTime,int playerNumber, float speed = 1){
		takenOverState += deltaTime * speed;
		if(takeOverStartGoal<=takenOverState){
			if(myBuildingTiles[0].myIsland!=null){
				City c = myBuildingTiles [0].myIsland.myCities.Find (x => x.playerNumber == playerNumber);
				if(c!=null){
					OnDestroy ();
					City = c;
					OnBuild ();
				} else {
					Health = 0; //???? is this good?
				}
			}
		}
	}

}
