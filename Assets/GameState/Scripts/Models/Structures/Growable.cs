using UnityEngine;
using System.Collections;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Growable : OutputStructure {
	

	#region Serialize

	[JsonPropertyAttribute] float age = 0;
	[JsonPropertyAttribute] public int currentStage= 0;
	[JsonPropertyAttribute] public bool hasProduced =false;

	#endregion
	#region RuntimeOrOther

	float growTime = 5f;
	public Fertility fer;
	public int ageStages = 2;

	#endregion

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
		CopyData (g);
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public Growable(){}

	public override Structure Clone (){
		return new Growable(this);
	}

	public override void LoadPrototypData(Structure s){
		Growable g = s as Growable;
		if(g == null){
			Debug.LogError ("ERROR - Prototyp was wrong");
			return;
		}
		CopyData (s);
	}
	private void CopyData(Growable g){
		BaseCopyData (g);
		OutputCopyData (g);
		growTime = g.growTime;
		fer = g.fer;
		ageStages = g.ageStages;

//		this.canBeBuildOver = g.canBeBuildOver;
//		this.ageStages = g.ageStages;
//		this.ID = g.ID;
//		this.name = g.name;
//		this.output = g.output;
//		this.tileWidth = g.tileWidth;
//		this.tileHeight = g.tileHeight;
//		this.buildcost = g.buildcost;
//		this.BuildTyp = g.BuildTyp;
//		this.rotated = g.rotated;
//		this.hasHitbox = g.hasHitbox;
//		this.growTime = g.growTime;
//		this.canBeBuildOver = g.canBeBuildOver;
//		this.canTakeDamage = g.canTakeDamage;
//		this.fer = g.fer;
//		this.forMarketplace = g.forMarketplace;
	}

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
		
}
