using UnityEngine;
using System.Collections;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Growable : OutputStructure {
	float growTime = 5f;
	float age = 0;
	public int ageStages = 2;
	public int currentStage= 0;
	public bool hasProduced =false;
	public bool outputClaimed =false;
	public Fertility fer;



	public Growable(int id,string name,Item produceItem,Fertility fer = null){
		forMarketplace = false;
		maxNumberOfWorker = 0;
		output = new Item[]{produceItem};
		maxOutputStorage = 1;
		this.ID = id;
		this.fer = fer;
		this.myBuildingTyp = BuildingTyp.Free;
		this.BuildTyp = BuildTypes.Drag;
		buildcost = 50;
		tileWidth = 1;
		tileHeight = 1;
		growTime = 100f;
		hasHitbox = false;
		canBeBuildOver = true;
		this.name = name;
		canBeBuildOver = true;
	}
	protected Growable(Growable g){
		this.canBeBuildOver = g.canBeBuildOver;
		this.ID = g.ID;
		this.name = g.name;
		this.output = g.output;
		this.tileWidth = g.tileWidth;
		this.tileHeight = g.tileHeight;
		this.buildcost = g.buildcost;
		this.BuildTyp = g.BuildTyp;
		this.rotated = g.rotated;
		this.hasHitbox = g.hasHitbox;
		this.growTime = g.growTime;
		this.canBeBuildOver = g.canBeBuildOver;
		this.canTakeDamage = g.canTakeDamage;
		this.fer = g.fer;
		this.forMarketplace = g.forMarketplace;
	}
	public override Structure Clone (){
		return new Growable(this);
	}
	//got replaced with get output
//	public Item getProducedItem(){
//		Item p = produceItem.Clone ();
//		p.count = 1;
//		return p;
//	}

	public override void OnBuild(){
		if(fer!=null && City.HasFertility (fer)==false){
			efficiencyModifier = 0;
		} else {
			//maybe have ground type be factor? stone etc
			efficiencyModifier = 1;
		}
	}
	public override void update (float deltaTime) {
		if(hasProduced||efficiencyModifier==0){
			return;
		}
		if(currentStage==ageStages){
			hasProduced = true;
			output[0].count=1;
			callbackIfnotNull ();
			return;
		}
		age += efficiencyModifier*(deltaTime/growTime);
		if((age) > 0.33*currentStage){
			if(Random.Range (0,100) <99){
				return;
			}
			if(currentStage>=ageStages){
				return;
			}
			currentStage++;
			callbackIfnotNull ();
		}
	}

	public void Reset (){
		output[0].count = 0;
		currentStage= 0;
		age = 0f;
		callbackIfnotNull ();
		hasProduced = false;
	}

	public override void WriteXml (XmlWriter writer){
		BaseWriteXml (writer);
		writer.WriteAttributeString("OutputClaimed", outputClaimed.ToString () );
		writer.WriteAttributeString("CurrentStage",currentStage.ToString());
		writer.WriteAttributeString("Age",age.ToString());
	}
	public override void ReadXml (XmlReader reader)	{
		BaseReadXml (reader);
		currentStage = int.Parse( reader.GetAttribute("CurrentStage") );
		age = float.Parse( reader.GetAttribute("Age") );
		outputClaimed = bool.Parse (reader.GetAttribute("OutputClaimed"));

	}
}
