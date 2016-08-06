using UnityEngine;
using System.Collections;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Growable : Structure {
	float growTime = 5f;
	float age = 0;
	int ageStages = 2;
	public int currentStage= 0;
	public bool hasProduced =false;
	public bool outputClaimed =false;
	Item produceItem;

	public Growable(int id,string name,Item produceItem){
		this.ID = id;
		this.myBuildingTyp = BuildingTyp.Blocking;
		this.BuildTyp = BuildTypes.Drag;
		buildcost = 50;
		tileWidth = 1;
		tileHeight = 1;
		growTime = 100f;
		hasHitbox = false;
		canBeBuildOver = true;
		this.name = name;
		this.produceItem = produceItem;
		canBeBuildOver = true;
	}
	protected Growable(Growable g){
		this.canBeBuildOver = g.canBeBuildOver;
		this.ID = g.ID;
		this.name = g.name;
		this.produceItem = g.produceItem;
		this.tileWidth = g.tileWidth;
		this.tileHeight = g.tileHeight;
		this.buildcost = g.buildcost;
		this.BuildTyp = g.BuildTyp;
		this.rotated = g.rotated;
		this.hasHitbox = g.hasHitbox;
		this.growTime = g.growTime;
		this.canBeBuildOver = g.canBeBuildOver;
		this.canTakeDamage = g.canTakeDamage;

	}
	public override Structure Clone (){
		return new Growable(this);
	}

	public Item getProducedItem(){
		Item p = produceItem.Clone ();
		p.count = 1;
		return p;
	}

	public override void OnBuild(){

	}
	public override void update (float deltaTime) {
		if(hasProduced){
			return;
		}
		if(currentStage==ageStages){
			hasProduced = true;
			callbackIfnotNull ();
			return;
		}
		age += deltaTime;
		if((age/growTime) > 0.33*currentStage){
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
