using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public enum Target {
    World, Player, AllUnit, Ship, LandUnit, Island, City, AllStructure, RoadStructure, NeedStructure, MilitaryStructure, HomeStructure,
    ServiceStructure, GrowableStructure, OutputStructure, MarketStructure, WarehouseStructure, MineStructure,
    FarmStructure, ProductionStructure
}

public abstract class IGEventable {

    /// <summary>
    /// For integer and float modifier
    /// </summary>
    protected Dictionary<string, float> VariablenameToFloat;
    [JsonPropertyAttribute] protected List<Effect> Effects;
    
    /// <summary>
    /// Target, Effect, added=true removed=false 
    /// </summary>
    protected Action<IGEventable, Effect, bool> cbEffectChange;
    protected Action<GameEvent> cbEventCreated;
    protected Action<GameEvent> cbEventEnded;
    private TargetGroup _targetGroup;
    public TargetGroup TargetGroups {
        get {
            if (_targetGroup == null)
                _targetGroup = CalculateTargetGroups();
            return _targetGroup;
        }
    }

    public virtual int GetID() { return 0; } // only needs to get changed WHEN there is diffrent ids

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
        if (this is RoadStructure)
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
        cbEventCreated += create;
        cbEventEnded += ending;
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
        if(effect.IsUnique && HasEffect(effect)) {
            return;
        }
        Effects.Add(effect);
        cbEffectChange?.Invoke(this, effect, true);
        if (effect.IsSpecial) {
            ExecuteSpecialEffect(effect);
        } else {
            if (VariablenameToFloat == null)
                VariablenameToFloat = new Dictionary<string, float>();
            //we change a float or integer variable 
            VariablenameToFloat[effect.NameOfVariable + effect.ModifierType] += effect.Change;
        }
    }

    public bool HasEffect(Effect effect) {
        return Effects.Find(x => x.ID == effect.ID) != null;
    }

    public virtual void RemoveEffect(Effect effect) {
        if (Effects.Find(e => e.ID == effect.ID) == null) {
            return;
        }
        Effects.RemoveAll(e => e.ID == effect.ID);
        cbEffectChange?.Invoke(this, effect, false);

        if (effect.IsSpecial) {
            RemoveSpecialEffect(effect);
        }
        else {
            //we change a float or integer variable 
            if (VariablenameToFloat.ContainsKey(effect.NameOfVariable + effect.ModifierType)) {
                VariablenameToFloat[effect.NameOfVariable + effect.ModifierType] -= effect.Change;
            } else {
                Debug.LogWarning("Tried to remove an Effect that didnt have a value yet.");
            }
        }
    }


    /// <summary>
    /// USE this for any variable thats supposed to be able to be modified
    /// </summary>
    /// <param name="name"></param>
    /// <param name="currentValue"></param>
    /// <returns></returns>
    protected float CalculateRealValue(string name, float currentValue, bool clampToZero = true) {
        float value = (currentValue + GetAdditiveValue(name)) * GetMultiplicative(name);
        if (clampToZero)
            return Mathf.Clamp(value, 0, value);
        return value;
    }
    /// <summary>
    /// USE this for any variable thats supposed to be able to be modified
    /// </summary>
    /// <param name="name"></param>
    /// <param name="currentValue"></param>
    /// <returns></returns>
    protected int CalculateRealValue(string name, int currentValue, bool clampToZero = true) {
        return Mathf.RoundToInt(CalculateRealValue(name,(float)currentValue,clampToZero));
    }
    private float GetMultiplicative(string name) {
        if (VariablenameToFloat.ContainsKey(name + EffectModifier.Multiplicative) == false)
            return 0f;
        return VariablenameToFloat[name + EffectModifier.Multiplicative];
    }

    private float GetAdditiveValue(string name) {
        if (VariablenameToFloat.ContainsKey(name + EffectModifier.Additive) == false)
            return 0f;
        return VariablenameToFloat[name + EffectModifier.Additive];
    }

    protected virtual void ExecuteSpecialEffect(Effect effect) {
        Debug.LogError("Not implemented Add Special Effect " + effect.ID + " for this object: " + this.ToString());
    }
    protected virtual void RemoveSpecialEffect(Effect effect) {
        Debug.LogError("Not implemented Remove Special Effect " + effect.ID + " for this object: " + this.ToString());
    }
    public void RegisterOnEffectChangedCallback(Action<IGEventable, Effect, bool> cb) {
        cbEffectChange += cb;
    }
    public void UnregisterOnEffectChangedCallback(Action<IGEventable, Effect, bool> cb) {
        cbEffectChange -= cb;
    }
}
