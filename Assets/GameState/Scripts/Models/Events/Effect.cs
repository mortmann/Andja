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

    public bool canSpread;
    public float spreadProbability;
    public int spreadTileRange = 1;
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

    public bool CanSpread => EffectPrototypData.canSpread;
    public int SpreadTileRange => EffectPrototypData.spreadTileRange;
    public float SpreadProbability => EffectPrototypData.spreadProbability;

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
    public bool Serialize = true; 

    public Effect() {

    }
    public Effect(int ID) {
        this.ID = ID;
    }

    public void Update(float deltaTime, IGEventable target) {
        if (IsUpdateChange) {
            CalculateUpdateChange(deltaTime, target);
        }
        if (CanSpread) {
            CalculateSpread(deltaTime, target);
        }
    }

    private void CalculateSpread(float deltaTime, IGEventable target) {
        //we need some kind increased probability over time that it spread
        //if it happens it will need to check for a valid target 
        //if valid is found it needs to add itself as new effect to that target

        IGEventable newTarget = GetValidTarget(target);
        if (newTarget == null)
            return;
        newTarget.AddEffect(new Effect(ID));
    }

    private IGEventable GetValidTarget(IGEventable target) {
        if(target is Structure) {
            List<Structure> strs = ((Structure)target).GetNeighbourStructuresInRange(SpreadTileRange);
            strs.RemoveAll(x=> Targets.IsTargeted(x.TargetGroups) == false);
            //now we have a list we can effect 
            //maybe smth more complex but for now just random
            return strs[UnityEngine.Random.Range(0, strs.Count)];
        }
        Debug.LogError("CheckForValidTarget has not been implemented for " + target.GetType());
        return null;
    }

    private void CalculateUpdateChange(float deltaTime, IGEventable target) {
        if (target is Structure) {
            switch (UpdateChange) {
                case EffectUpdateChanges.Health:
                    ((Structure)target).ReduceHealth(Change * deltaTime);
                    break;
            }
        }
        else
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