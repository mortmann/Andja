using UnityEngine;
using System.Collections.Generic;

public class Farm : UserStructure {
	public string growable;
	public int growableReadyCount;
	public int OnRegisterCallbacks;
	Queue<Structure> workingGrowables;

	public override float Efficiency{
		get {
			return Mathf.Round(((float)OnRegisterCallbacks / (float)myRangeTiles.Count)*1000)/10f;
		}
	}
	public Farm(int id, string name,float produceTime, Item produce, int tileWidth, int tileHeight, int buildcost, int maintance ){
		this.ID = id;
		this.name = name;
		this.tileWidth = tileWidth;
		this.tileHeight = tileHeight;
		this.buildcost = buildcost;
		this.maintenancecost = maintance;
		this.output = new Item[1];
		this.output [0] = produce;
		this.produceTime = produceTime;
		myBuildingTyp = BuildingTyp.Blocking;
		BuildTyp = BuildTypes.Single;
		hasHitbox = true;
	}
	protected Farm(Farm f){
		this.ID = f.ID;
		this.name = f.name;
		this.tileWidth = f.tileWidth;
		this.tileHeight = f.tileHeight;
		this.buildcost = f.buildcost;
		this.maintenancecost = f.maintenancecost;
		this.output = f.output;
		this.produceTime = f.produceTime;
		this.produceCountdown = f.produceTime;
		myBuildingTyp = f.myBuildingTyp;
		BuildTyp = f.BuildTyp;
		hasHitbox = f.hasHitbox;
	}
	public override Structure Clone ()	{
		return new Farm (this);
	}

	public override void OnBuild ()	{
		growable = "tree";
		workingGrowables = new Queue<Structure> ();
		if(growable == null | growable == ""){
			return;
		}
		GameObject.FindObjectOfType<BuildController> ().BuildOnTile (3, new List<Tile>(myRangeTiles));
		//farm has it needs plant if it can 
		foreach (Tile rangeTile in myRangeTiles) {
			if(rangeTile.Structure != null){
				if(rangeTile.Structure.name.Contains (growable)){
					rangeTile.Structure.RegisterOnChangedCallback (OnGrowableChanged);	
					OnRegisterCallbacks++;
					if(((Growable)rangeTile.Structure).hasProduced == true){
						growableReadyCount ++;
						workingGrowables.Enqueue (rangeTile.Structure);
					}
				}
			}
		}
		foreach(Tile rangeTile in myRangeTiles){
			rangeTile.RegisterTileStructureChangedCallback (OnTileStructureChange);
		}
	}
	public override void update (float deltaTime){
		if(growableReadyCount==0){
			return;
		}
		if(output[0].count >= maxOutputStorage){
			return;
		}

		produceCountdown -= deltaTime;
		if (produceCountdown <= 0) {
			produceCountdown = deltaTime;
			if (growable != null) {
				Growable g = (Growable)workingGrowables.Dequeue ();
				output[0].count++;
				growableReadyCount--;
				((Growable)g).Reset ();
			}
			if (cbOutputChange != null) {
				cbOutputChange (this);
			}
		}
	}
	public void OnGrowableChanged(Structure str){
		if(str is Growable == false){
			return;
		}
		if(str.name != growable){
			return;
		}
		if(((Growable)str).hasProduced == false){
			return;
		}
		workingGrowables.Enqueue (str);
		growableReadyCount ++;
		// send worker todo this job
		// not important right now
	}
	public void OnTileStructureChange(Tile t, Structure old){
		if(old != null && old.name == growable){
			OnRegisterCallbacks--;
		}
		if(t.Structure == null){
			return;
		}
		if(t.Structure.name == growable){
			OnRegisterCallbacks++;
			t.Structure.RegisterOnChangedCallback (OnGrowableChanged);	
		}
	}
	public override void ReadXml (System.Xml.XmlReader reader)	{
		base.BaseReadXml (reader);
		ReadUserXml (reader);
	}
	public override void WriteXml (System.Xml.XmlWriter writer)	{
		BaseWriteXml (writer);
		WriteUserXml (writer);
	}
}
