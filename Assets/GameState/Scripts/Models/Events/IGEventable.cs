using UnityEngine;
using System;
using System.Collections.Generic;

public enum Target {
    World, Player, AllUnit, Ship, LandUnit, Island, City, AllStructure, Road, NeedStructure, MilitaryStructure, HomeStructure,
    ServiceStructure, GrowableStructure, OutputStructure, MarketStructure, WarehouseStructure, MineStructure,
    FarmStructure, ProductionStructure
}

public abstract class IGEventable {

    /// <summary>
    /// For integer and float modifier
    /// </summary>
    protected Dictionary<string, float> variablenameToFloat;

    protected Action<GameEvent> cbEventCreated;
    protected Action<GameEvent> cbEventEnded;
    protected TargetGroup _targetGroup;
    public TargetGroup TargetGroups {
        get {
            if (_targetGroup == null)
                _targetGroup = CalculateTargetGroups();
            return _targetGroup;
        }
    }
    /// <summary>
    /// TODO: think about ways to make it better
    /// 
    /// </summary>
    /// <returns></returns>
    private TargetGroup CalculateTargetGroups() {
        List<Target> targets = new List<Target>();
        if (this is World)
            targets.Add(Target.World);
        if (this is Player)
            targets.Add(Target.Player);
        if (this is Unit)
            targets.Add(Target.AllUnit);
        if (this is Unit && this is Ship == false)
            targets.Add(Target.LandUnit);
        if (this is Ship)
            targets.Add(Target.Ship);
        if (this is Island)
            targets.Add(Target.Island);
        if (this is City)
            targets.Add(Target.City);
        if (this is Structure)
            targets.Add(Target.AllStructure);
        if (this is Road)
            targets.Add(Target.HomeStructure);
        if (this is NeedStructure)
            targets.Add(Target.NeedStructure);
        if (this is MilitaryStructure)
            targets.Add(Target.MilitaryStructure);
        if (this is ServiceStructure)
            targets.Add(Target.ServiceStructure);
        if (this is GrowableStructure)
            targets.Add(Target.GrowableStructure);
        if (this is OutputStructure)
            targets.Add(Target.OutputStructure);
        if (this is MarketStructure)
            targets.Add(Target.MarketStructure);
        if (this is WarehouseStructure)
            targets.Add(Target.WarehouseStructure);
        if (this is MineStructure)
            targets.Add(Target.MineStructure);
        if (this is FarmStructure )
            targets.Add(Target.FarmStructure);
        if (this is ProductionStructure)
            targets.Add(Target.ProductionStructure);
        return new TargetGroup(targets);
    }

    public void RegisterOnEvent(Action<GameEvent> create, Action<GameEvent> ending) {

    }
    public virtual int GetPlayerNumber() {
        return -1;
    }

    public abstract void OnEventCreate(GameEvent ge);
    public abstract void OnEventEnded(GameEvent ge);


    public void AddEffects(Effect[] effects) {
        foreach (Effect effect in effects)
            AddEffect(effect);
    }

    public virtual void AddEffect(Effect effect) {
        if(TargetGroups.IsTargeted(effect.Targets) == false) {
            return;
        }
        Debug.Log("No implementation for effect " + effect.ID);

        if(effect.IsSpecial) {
            //ExecuteSpecialEffect(effect);
        } else {
            if (variablenameToFloat == null)
                variablenameToFloat = new Dictionary<string, float>();
            //we change a float or integer variable 
            float change = 0f;
            float.TryParse(effect.Change, out change);
            variablenameToFloat[effect.NameOfVariable + effect.ModifierType] += change;
        }
    }
    /// <summary>
    /// USE this for any variable thats supposed to be able to be modified
    /// </summary>
    /// <param name="name"></param>
    /// <param name="currentValue"></param>
    /// <returns></returns>
    protected float CalculateRealValue(string name, float currentValue) {
        return (currentValue + GetAdditiveValue(name)) * GetMultiplicative(name);
    }

    private float GetMultiplicative(string name) {
        if (variablenameToFloat.ContainsKey(name + EffectModifier.Multiplicative) == false)
            return 0f;
        return variablenameToFloat[name + EffectModifier.Multiplicative];
    }

    private float GetAdditiveValue(string name) {
        if (variablenameToFloat.ContainsKey(name + EffectModifier.Additive) == false)
            return 0f;
        return variablenameToFloat[name + EffectModifier.Additive];
    }

    //protected abstract void ExecuteSpecialEffect(Effect effect);
}
