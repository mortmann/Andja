using UnityEngine;
using System.Collections;

public class Growable : Structure {
	float growTime = 5f;
	float age = 0;
	int ageStages = 2;
	public int currentStage= 0;
	public bool hasProduced =false;
	Item produceItem ;

	public Growable(string name,Item produceItem){
		this.myBuildingTyp = BuildingTyp.Blocking;
		this.BuildTyp = BuildTypes.Drag;
		buildcost = 50;
		tileWidth = 1;
		tileHeight = 1;
		growTime = 100f;
		hasHitbox = false;
		this.name = name;
		this.produceItem = produceItem;
		canBeBuildOver = true;
	}
	protected Growable(Growable g){
		this.name = g.name;
		this.produceItem = g.produceItem;
		this.tileWidth = g.tileWidth;
		this.tileHeight = g.tileHeight;
		this.buildcost = g.buildcost;
		this.BuildTyp = g.BuildTyp;
		this.rotated = g.rotated;
		this.hasHitbox = g.hasHitbox;
		this.growTime = g.growTime;
		this.canBeBuildOver = true;
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
			if(currentStage == ageStages){
				hasProduced = true;
			}
			callbackIfnotNull ();
		}
	}

	public void Reset (){
		growTime = 100f;
		currentStage= 0;
		callbackIfnotNull ();
		hasProduced = false;
	}
}
