using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable All

namespace Andja.Model {
    public interface IGEventable {
        IReadOnlyList<Effect> Effects { get; }
        TargetGroup TargetGroups { get; }
        bool HasNegativeEffect { get; }
        string GetID();
        void RegisterOnEvent(Action<GameEvent> create, Action<GameEvent> ending);
        int GetPlayerNumber();
        void OnEventCreate(GameEvent ge);
        void OnEventEnded(GameEvent ge);
        void UpdateEffects(float deltaTime);
        void AddEffects(Effect[] effects);
        bool AddEffect(Effect effect);
        Effect GetEffect(string ID);
        bool HasEffect(string effectID);
        bool HasEffect(Effect effect);
        bool HasAnyEffect(params Effect[] effects);
        bool RemoveEffect(Effect effect, bool all = false);
        void RegisterOnEffectChangedCallback(Action<GEventable, Effect, bool> cb);
        void UnregisterOnEffectChangedCallback(Action<GEventable, Effect, bool> cb);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class GEventable : IGEventable {

        /// <summary>
        /// For integer and float modifier
        /// </summary>
        protected Dictionary<string, float> VariableNameToFloat;

        [JsonPropertyAttribute] protected List<Effect> effects;

        public IReadOnlyList<Effect> Effects => effects;

        private List<Effect> _updateEffectList;

        protected List<Effect> UpdateEffectList {
            get {
                if (effects == null)
                    return null;
                //for loading -- makes it ez to get them again without doing in load functions
                if (_updateEffectList != null) return _updateEffectList;
                _updateEffectList = new List<Effect>(effects);
                _updateEffectList.RemoveAll(x => x.IsUpdateChange == false);
                return _updateEffectList;
            }
        }

        /// <summary>
        /// Target, Effect, added=true removed=false
        /// </summary>
        protected Action<GEventable, Effect, bool> cbEffectChange;

        protected Action<GameEvent> cbEventCreated;
        protected Action<GameEvent> cbEventEnded;
        private TargetGroup _targetGroup;

        public TargetGroup TargetGroups => _targetGroup ??= CalculateTargetGroups();

        public virtual string GetID() {
            return null;
        } // only needs to get changed WHEN there is diffrent ids

        public bool HasNegativeEffect { get; protected set; }

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
            if (this is Structure str) {
                targets.Add(Target.AllStructure);
                if (str.CanTakeDamage)
                    targets.Add(Target.DamageableStructure);
                if (str.CanStartBurning)
                    targets.Add(Target.BurnableStructure);
            }
            if (this is HomeStructure)
                targets.Add(Target.HomeStructure);
            if (this is RoadStructure)
                targets.Add(Target.RoadStructure);
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
            if (this is FarmStructure)
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

        public virtual void OnEventCreate(GameEvent ge) {
            if (ge.IsTarget(this)) {
                ge.EffectTarget(this, true);
            }
            cbEventCreated?.Invoke(ge);
        }

        public virtual void OnEventEnded(GameEvent ge) {
            if (ge.IsTarget(this)) {
                ge.EffectTarget(this, false);
            }
            cbEventEnded?.Invoke(ge);
        }

        public void UpdateEffects(float deltaTime) {
            if (UpdateEffectList == null || UpdateEffectList.Count == 0)
                return;
            for (int i = UpdateEffectList.Count - 1; i >= 0; i--) {
                UpdateEffectList[i].Update(deltaTime, this);
            }
        }

        public void AddEffects(Effect[] effects) {
            foreach (Effect effect in effects)
                AddEffect(effect);
        }

        public virtual bool AddEffect(Effect effect) {
            if (TargetGroups.IsTargeted(effect.Targets) == false) {
                return false;
            }
            effects ??= new List<Effect>();
            if (effect.IsUnique && HasEffect(effect)) {
                return false;
            }
            effects.Add(effect);
            cbEffectChange?.Invoke(this, effect, true);
            if (effect.IsSpecial) {
                AddSpecialEffect(effect);
            }
            else {
                VariableNameToFloat ??= new Dictionary<string, float>();
                //we change a float or integer variable
                if (effect.ModifierType != EffectModifier.Update || effect.ModifierType != EffectModifier.Special) {
                    if (VariableNameToFloat.ContainsKey(effect.NameOfVariable + effect.ModifierType) == false) {
                        VariableNameToFloat.Add(effect.NameOfVariable + effect.ModifierType, 0);
                    }
                    VariableNameToFloat[effect.NameOfVariable + effect.ModifierType] += effect.Change;
                }
            }
            if (effect.IsUpdateChange) {
                _updateEffectList ??= new List<Effect>();
                UpdateEffectList.Add(effect);
            }
            if (effect.IsNegative)
                HasNegativeEffect = true;
            return true;
        }

        public Effect GetEffect(string ID) {
            return effects.Find(x => x.ID == ID);
        }
        public bool HasEffect(string effectID) {
            return effects.Exists(x => x.ID == effectID);
        }
        public bool HasEffect(Effect effect) {
            return HasEffect(effect.ID);
        }
        public bool HasAnyEffect(params Effect[] effects) {
            return this.effects.Exists(x => Array.Exists<Effect>(effects, y => x.ID == y.ID));
        }
        public virtual bool RemoveEffect(Effect effect, bool all = false) {
            if (effects.Find(e => e.ID == effect.ID) == null) {
                return false;
            }
            if (all) {
                effects.RemoveAll(e => e.ID == effect.ID);
                UpdateEffectList.RemoveAll(e => e.ID == effect.ID);
            }
            else {
                effects.RemoveAt(effects.FindIndex(e => e.ID == effect.ID));
                int updateIndex = effects.FindIndex(e => e.ID == effect.ID);
                if(updateIndex > 0)
                    UpdateEffectList.RemoveAt(updateIndex);
            }
            cbEffectChange?.Invoke(this, effect, false);
            if (effect.IsSpecial) {
                RemoveSpecialEffect(effect);
            }
            else {
                //we change a float or integer variable
                if (VariableNameToFloat != null && VariableNameToFloat.ContainsKey(effect.NameOfVariable + effect.ModifierType)) {
                    VariableNameToFloat[effect.NameOfVariable + effect.ModifierType] -= effect.Change;
                }
            }
            if (effect.IsNegative)
                HasNegativeEffect = effects.Exists(x => x.IsNegative);
            return true;
        }

        /// <summary>
        /// USE this for any variable thats supposed to be able to be modified
        /// </summary>
        /// <param name="name"></param>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        public float CalculateRealValue(string name, float currentValue, bool clampToZero = true) {
            float value = (GetAdditiveValue(name)) + currentValue * Mathf.Clamp(1 + GetMultiplicative(name),0,100);
            if (clampToZero)
                return Mathf.Clamp(value, 0, value);
            return value;
        }
        /// <summary>
        /// USE this for any variable thats supposed to be able to be modified clamp ben
        /// </summary>
        /// <param name="name"></param>
        /// <param name="currentValues"></param>
        /// <param name="min"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        protected float[] CalculateRealValue(string name, float[] currentValues, bool clampToZero = true) {
            float[] realValues = new float[currentValues.Length];
            for (int i = 0; i < currentValues.Length; i++) {
                realValues[i] = CalculateRealValue(name, currentValues[i], clampToZero);
            }
            return realValues;
        }
        /// <summary>
        /// USE this for any variable thats supposed to be able to be modified
        /// </summary>
        /// <param name="name"></param>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        protected int CalculateRealValue(string name, int currentValue, bool clampToZero = true) {
            return Mathf.RoundToInt(CalculateRealValue(name, (float)currentValue, clampToZero));
        }

        private float GetMultiplicative(string name) {
            if (VariableNameToFloat == null || VariableNameToFloat.ContainsKey(name + EffectModifier.Multiplicative) == false)
                return 0f;
            return VariableNameToFloat[name + EffectModifier.Multiplicative];
        }

        private float GetAdditiveValue(string name) {
            if (VariableNameToFloat == null || VariableNameToFloat.ContainsKey(name + EffectModifier.Additive) == false)
                return 0f;
            return VariableNameToFloat[name + EffectModifier.Additive];
        }
        protected virtual void AddSpecialEffect(Effect effect) {
            Debug.LogError("Not implemented Add Special Effect " + effect.ID + " for this object: " + this.ToString());
        }

        protected virtual void RemoveSpecialEffect(Effect effect) {
            Debug.LogError("Not implemented Remove Special Effect " + effect.ID + " for this object: " + this.ToString());
        }

        public void RegisterOnEffectChangedCallback(Action<GEventable, Effect, bool> cb) {
            cbEffectChange += cb;
        }

        public void UnregisterOnEffectChangedCallback(Action<GEventable, Effect, bool> cb) {
            cbEffectChange -= cb;
        }
    }
}