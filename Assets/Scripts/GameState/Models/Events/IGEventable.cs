using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class IGEventable {

        /// <summary>
        /// For integer and float modifier
        /// </summary>
        protected Dictionary<string, float> VariablenameToFloat;

        [JsonPropertyAttribute] protected List<Effect> _effects;

        public IReadOnlyList<Effect> Effects => _effects;

        private List<Effect> _UpdateEffectList;

        protected List<Effect> UpdateEffectList {
            get {
                if (_effects == null)
                    return null;
                //for loading -- makes it ez to get them again without doing in load functions
                if (_UpdateEffectList == null) {
                    _UpdateEffectList = new List<Effect>(_effects);
                    _UpdateEffectList.RemoveAll(x => x.IsUpdateChange == false);
                }
                return _UpdateEffectList;
            }
        }

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

        public virtual string GetID() {
            return null;
        } // only needs to get changed WHEN there is diffrent ids

        public bool HasNegativEffect { get; protected set; }

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
                    targets.Add(Target.DamagableStructure);
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

        public abstract void OnEventCreate(GameEvent ge);

        public abstract void OnEventEnded(GameEvent ge);

        public void UpdateEffects(float deltaTime) {
            if (UpdateEffectList == null || UpdateEffectList.Count == 0)
                return;
            for (int i = UpdateEffectList.Count - 1; i >= 0; i--) {
                UpdateEffectList[i].Update(deltaTime, this);
            }
        }

        public void AddEffects(Effect[] effects) {
            if (this._effects == null)
                this._effects = new List<Effect>();
            foreach (Effect effect in effects)
                AddEffect(effect);
        }

        public virtual bool AddEffect(Effect effect) {
            if (TargetGroups.IsTargeted(effect.Targets) == false) {
                return false;
            }
            if (_effects == null)
                _effects = new List<Effect>();
            if (effect.IsUnique && HasEffect(effect)) {
                return false;
            }
            _effects.Add(effect);
            cbEffectChange?.Invoke(this, effect, true);
            if (effect.IsSpecial) {
                AddSpecialEffect(effect);
            }
            else {
                if (VariablenameToFloat == null)
                    VariablenameToFloat = new Dictionary<string, float>();
                //we change a float or integer variable
                if (effect.ModifierType != EffectModifier.Update || effect.ModifierType != EffectModifier.Special) {
                    if (VariablenameToFloat.ContainsKey(effect.NameOfVariable + effect.ModifierType) == false) {
                        VariablenameToFloat.Add(effect.NameOfVariable + effect.ModifierType, 0);
                    }
                    VariablenameToFloat[effect.NameOfVariable + effect.ModifierType] += effect.Change;
                }
            }
            if (effect.IsUpdateChange) {
                if (_UpdateEffectList == null)
                    _UpdateEffectList = new List<Effect>();
                UpdateEffectList.Add(effect);
            }
            if (effect.IsNegativ)
                HasNegativEffect = true;
            return true;
        }

        internal Effect GetEffect(string ID) {
            return _effects.Find(x => x.ID == ID);
        }

        public bool HasEffect(Effect effect) {
            return _effects.Find(x => x.ID == effect.ID) != null;
        }
        public bool HasAnyEffect(params Effect[] effects) {
            return _effects.Exists(x => Array.Exists<Effect>(effects, y => x.ID == y.ID));
        }
        public virtual bool RemoveEffect(Effect effect, bool all = false) {
            if (_effects.Find(e => e.ID == effect.ID) == null) {
                return false;
            }

            if (all)
                _effects.RemoveAll(e => e.ID == effect.ID);
            else
                _effects.Remove(effect);
            UpdateEffectList.RemoveAll(e => e.ID == effect.ID);
            cbEffectChange?.Invoke(this, effect, false);

            if (effect.IsSpecial) {
                RemoveSpecialEffect(effect);
            }
            else {
                //we change a float or integer variable
                if (VariablenameToFloat != null && VariablenameToFloat.ContainsKey(effect.NameOfVariable + effect.ModifierType)) {
                    VariablenameToFloat[effect.NameOfVariable + effect.ModifierType] -= effect.Change;
                }
            }
            if (effect.IsNegativ)
                HasNegativEffect = _effects.Find(x => x.IsNegativ) != null;
            return true;
        }

        /// <summary>
        /// USE this for any variable thats supposed to be able to be modified
        /// </summary>
        /// <param name="name"></param>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        protected float CalculateRealValue(string name, float currentValue, bool clampToZero = true) {
            float value = (GetAdditiveValue(name)) + currentValue * Mathf.Clamp(1 + GetMultiplicative(name),0,100);
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
            return Mathf.RoundToInt(CalculateRealValue(name, (float)currentValue, clampToZero));
        }

        private float GetMultiplicative(string name) {
            if (VariablenameToFloat == null || VariablenameToFloat.ContainsKey(name + EffectModifier.Multiplicative) == false)
                return 0f;
            return VariablenameToFloat[name + EffectModifier.Multiplicative];
        }

        private float GetAdditiveValue(string name) {
            if (VariablenameToFloat == null || VariablenameToFloat.ContainsKey(name + EffectModifier.Additive) == false)
                return 0f;
            return VariablenameToFloat[name + EffectModifier.Additive];
        }
        protected virtual void AddSpecialEffect(Effect effect) {
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
}