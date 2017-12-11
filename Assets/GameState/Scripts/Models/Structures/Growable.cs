using UnityEngine;
using System.Collections;
using Newtonsoft.Json;

public class GrowablePrototypData : OutputPrototypData {
	public float growTime = 5f;
	public Fertility fertility;
	public int ageStages = 2;

}

[JsonObject(MemberSerialization.OptIn)]
public class Growable : OutputStructure {
	

	#region Serialize

	[JsonPropertyAttribute] float age = 0;
	[JsonPropertyAttribute] public int currentStage = 0;
	[JsonPropertyAttribute] public bool hasProduced = false;

	#endregion
	#region RuntimeOrOther

	float growTime {get{ return GrowableData.growTime; }}
	public Fertility fer {get{ return GrowableData.fertility; }}
	public int ageStages {get{ return GrowableData.ageStages; }}

	protected GrowablePrototypData _growableData;
	public GrowablePrototypData GrowableData {
		get { if(_growableData==null){
				_growableData = (GrowablePrototypData)PrototypController.Instance.GetStructurePrototypDataForID (ID);
			}
			return _growableData;
		}
	}
	#endregion

	public Growable(int id, GrowablePrototypData _growableData){
		this.ID = id;
		this._growableData = _growableData;
	}
	protected Growable(Growable g){
		BaseCopyData (g);
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public Growable(){}

	public override Structure Clone (){
		return new Growable(this);
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
	public override bool SpecialCheckForBuild (System.Collections.Generic.List<Tile> tiles){
		//this should be only ever 1 but for whateverreason it is not it still checks and doesnt really matter anyway
		foreach(Tile t in tiles){
			if(t.Structure==null){
				continue;
			}
			if(t.Structure.ID == ID){
				return false;
			}
		}
		return true;
	}
	public override string GetSpriteName (){
		return base.GetSpriteName () + "_" + currentStage;
	}
	public void Reset (){
		output[0].count = 0;
		currentStage= 0;
		age = 0f;
		callbackIfnotNull ();
		hasProduced = false;
	}
		
}
