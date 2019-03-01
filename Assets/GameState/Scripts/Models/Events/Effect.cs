using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;

public enum EffectTypes { Integer, Float, Special }
public enum EffectModifier { Additive, Multiplicative, Update, Special }
public enum EffectUpdateChanges { None, Health }
public enum EffectClassification { Negativ, Neutral, Positiv }

public class EffectPrototypeData : LanguageVariables {

    public string nameOfVariable; // what does it change
    public float change; // how it changes the Variable? -- for update this will per *second* 
    public TargetGroup targets; // what it can target
    public EffectTypes addType;
    public EffectModifier modifierType;
    public EffectUpdateChanges updateChange;
    public EffectClassification classification;
    public bool unique;

}
[JsonObject(MemberSerialization.OptIn)]
public class Effect {

    public int ID;
    public InfluenceTyp InfluenceTyp { protected set; get; }
    public InfluenceRange InfluenceRange { protected set; get; }
    public EffectTypes AddType => EffectPrototypData.addType;
    public EffectModifier ModifierType => EffectPrototypData.modifierType;
    public bool IsUnique => EffectPrototypData.unique;
    public EffectUpdateChanges UpdateChange => EffectPrototypData.updateChange;
    public EffectClassification Classification => EffectPrototypData.classification;
    public TargetGroup Targets => EffectPrototypData.targets;
    public string NameOfVariable => EffectPrototypData.nameOfVariable;
    public float Change => EffectPrototypData.change;
    //Some special function will be called for it 
    //so it isnt very flexible and must be either precoded or we need to add support for lua
    public bool IsSpecial => AddType == EffectTypes.Special || ModifierType == EffectModifier.Special;
    public bool IsUpdateChange => ModifierType == EffectModifier.Update && UpdateChange != EffectUpdateChanges.None;
    public bool IsNegativ => EffectClassification.Negativ == Classification;

    protected EffectPrototypeData _effectPrototypData;
    public EffectPrototypeData EffectPrototypData {
        get {
            if (_effectPrototypData == null) {
                _effectPrototypData = (EffectPrototypeData)PrototypController.Instance.GetEffectPrototypDataForID(ID);
            }
            return _effectPrototypData;
        }
    }


    [JsonPropertyAttribute] public bool Serialize; 

    public Effect() {

    }
    public Effect(int ID) {
        this.ID = ID;
    }

    public void Update(float deltaTime, IGEventable target) {
        if (IsUpdateChange == false) {
            return;
        }
        if(target is Structure) {
            switch (UpdateChange) {
                case EffectUpdateChanges.Health:
                    ((Structure)target).ReduceHealth(Change * deltaTime);
                    break;
            }
        } else
        if (target is Unit) {
            switch (UpdateChange) {
                case EffectUpdateChanges.Health:
                    ((Unit)target).ReduceHealth(Change * deltaTime);
                    break;
            }
        }
    }

    //for Serializing if it should be saved -- not needed for structure effects etc
    public bool ShouldSerializeEffect() {
        return Serialize;
    }
}