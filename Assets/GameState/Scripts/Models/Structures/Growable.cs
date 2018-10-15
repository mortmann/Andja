using UnityEngine;
using System.Collections;
using Newtonsoft.Json;

public class GrowablePrototypeData : OutputPrototypData {
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

    public Fertility Fertility {get{ return GrowableData.fertility; }}
	public int AgeStages {get{ return GrowableData.ageStages; }}

	protected GrowablePrototypeData _growableData;
	public GrowablePrototypeData GrowableData {
		get { if(_growableData==null){
				_growableData = (GrowablePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID (ID);
			}
			return _growableData;
		}
	}

    protected float TimePerStage => (ProduceTime / (float)AgeStages + 1);
    protected const float GrowTickTime = 1f;

    #endregion

    public Growable(int id, GrowablePrototypeData _growableData){
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
		if(Fertility!=null && City.HasFertility (Fertility)==false){
			efficiencyModifier = 0;
		} else {
			//maybe have ground type be factor? stone etc
			efficiencyModifier = 1;
		}
	}
	public override void Update (float deltaTime) {
		if(hasProduced||efficiencyModifier<=0){
			return;
		}
		age += efficiencyModifier * (deltaTime);
		if((age) > currentStage * TimePerStage) {
            currentStage++;
            if (currentStage >= AgeStages) {
                Produce();
                return;
            }
            //Debug.Log ("Stage " + currentStage + " @ Time " + age);
            CallbackChangeIfnotNull ();
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
	
    protected void Produce() {
        hasProduced = true;
        Output[0].count = 1;
        CallbackChangeIfnotNull();
    }

	public void Harvest (){
		Output[0].count = 0;
		currentStage= 0;
		age = 0f;
		CallbackChangeIfnotNull ();
		hasProduced = false;
	}
    #region override
    public override string GetSpriteName() {
        return base.GetSpriteName() + "_" + currentStage;
    }
    public override string ToString() {
        if (BuildTile == null) {
            return SpriteName + "@error";
        }
        return SpriteName + "@ X=" + BuildTile.X + " Y=" + BuildTile.Y + "\n " 
            +"Age: " + age + " Current Stage " + currentStage + " \n" 
            +" HasProduced " + hasProduced;
    }
    #endregion
}
