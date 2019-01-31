using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum EffectTypes { Integer, Float, Special }
public enum EffectModifier { Additive, Multiplicative, Special }

public class EffectPrototypeData : LanguageVariables {

    public string nameOfVariable; // what does it change
    public float change; // how it changes the Variable?
    public TargetGroup targets; // what it can target
    public EffectTypes addType;
    public EffectModifier modifierType;
    public bool unique;

}

public class Effect {

    public int ID;

    public InfluenceTyp InfluenceTyp { protected set; get; }
    public InfluenceRange InfluenceRange { protected set; get; }
    public EffectTypes AddType => EffectPrototypData.addType;
    public EffectModifier ModifierType => EffectPrototypData.modifierType;
    public bool IsUnique => EffectPrototypData.unique;

    protected EffectPrototypeData _effectPrototypData;

    public TargetGroup Targets => EffectPrototypData.targets;
    public string NameOfVariable => EffectPrototypData.nameOfVariable;
    public float Change => EffectPrototypData.change;

    //Some special function will be called for it 
    //so it isnt very flexible and must be either precoded or we need to add support for lua
    public bool IsSpecial => AddType == EffectTypes.Special || ModifierType == EffectModifier.Special;

    public EffectPrototypeData EffectPrototypData {
        get {
            if (_effectPrototypData == null) {
                _effectPrototypData = (EffectPrototypeData)PrototypController.Instance.GetEffectPrototypDataForID(ID);
            }
            return _effectPrototypData;
        }
    }
    public Effect() {

    }
    public Effect(int ID) {
        this.ID = ID;
    }
    public void ApplyEffect(IGEventable target) {
        
    }

}