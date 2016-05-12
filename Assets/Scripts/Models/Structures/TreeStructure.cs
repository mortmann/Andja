using UnityEngine;
using System.Collections;

public class TreeStructure : Structure {

	float growTime = 5f;
	float age = 0;
	int ageStages = 3;
	int currentStage= 1;
	public TreeStructure(string name){
		this.myBuildingTyp = BuildingTyp.Blocking;
		buildcost = 50;
		tileWidth = 1;
		tileHeight = 1;
		hasHitbox = true;
		this.name = name;
	}
	protected TreeStructure(TreeStructure ts){
		this.name = ts.name;
		this.tileWidth = ts.tileWidth;
		this.tileHeight = ts.tileHeight;
		this.buildcost = ts.buildcost;
		this.BuildTyp = ts.BuildTyp;
		this.rotated = ts.rotated;
		this.hasHitbox = ts.hasHitbox;
		this.growTime = ts.growTime;
	}
	public override Structure Clone (){
		return new TreeStructure(this);
	}
	public override void OnBuild(){

	}
	public override void update (float deltaTime) {
		if(age>growTime){
			return;
		}
		age += deltaTime;
		if((age/growTime) > 0.33*currentStage){
			if(currentStage>=ageStages){
				return;
			}
			currentStage++;
			callbackIfnotNull ();
		}
		base.update (deltaTime);
	}
}
