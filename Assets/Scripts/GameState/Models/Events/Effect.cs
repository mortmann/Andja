using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public enum EffectTypes { Integer, Float, Special }

    public enum EffectModifier { Additive, Multiplicative, Update, Special }

    public enum EffectUpdateChanges { None, Health }

    public enum EffectClassification { Negative, Neutral, Positive }

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

        public string uiSpriteName;
        public string onMapSpriteName;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Effect : IEffect {

        public InfluenceTyp InfluenceTyp { protected set; get; }
        public InfluenceRange InfluenceRange { protected set; get; }
        public EffectTypes AddType => EffectPrototypeData.addType;
        public EffectModifier ModifierType => EffectPrototypeData.modifierType;
        public bool IsUnique => EffectPrototypeData.unique;
        public EffectUpdateChanges UpdateChange => EffectPrototypeData.updateChange;
        public EffectClassification Classification => EffectPrototypeData.classification;
        public TargetGroup Targets => EffectPrototypeData.targets;
        public string NameOfVariable => EffectPrototypeData.nameOfVariable;
        public float Change => EffectPrototypeData.change;
        public string UiSpriteName => EffectPrototypeData.uiSpriteName;
        public string OnMapSpriteName => EffectPrototypeData.onMapSpriteName;
        public bool CanSpread => EffectPrototypeData.canSpread;
        public int SpreadTileRange => EffectPrototypeData.spreadTileRange;
        public float SpreadProbability => EffectPrototypeData.spreadProbability;
        public string Name => EffectPrototypeData.Name;
        public string Description => EffectPrototypeData.Description;
        public string HoverOver => EffectPrototypeData.HoverOver;

        //Some special function will be called for it
        //so it isnt very flexible and must be either precoded or we need to add support for lua
        public bool IsSpecial => AddType == EffectTypes.Special || ModifierType == EffectModifier.Special;

        public bool IsUpdateChange => ModifierType == EffectModifier.Update && UpdateChange != EffectUpdateChanges.None;
        public bool IsNegative => EffectClassification.Negative == Classification;

        protected EffectPrototypeData effectPrototypeData;

        public EffectPrototypeData EffectPrototypeData => effectPrototypeData ??= (EffectPrototypeData)PrototypController.Instance.GetEffectPrototypDataForID(ID);

        public bool Serialize = true;

        [JsonPropertyAttribute] public string ID { get; protected set; }
        [JsonPropertyAttribute] public float WorkAmount = 0; // THIS is used for servicestructure workers -- for example when removing this effect
        [JsonPropertyAttribute] public float SpreadTick = GameData.EffectTickTime;
        public Effect() {
        }

        public Effect(string ID) {
            this.ID = ID;
        }
        public Effect(string ID, EffectPrototypeData data) : this(ID) {
            effectPrototypeData = data;
        }

        public Effect(IEffect e) {
            this.ID = e.ID;
        }

        public void Update(float deltaTime, GEventable target) {
            if (IsUpdateChange) {
                CalculateUpdateChange(deltaTime, target);
            }
            if (CanSpread) {
                CalculateSpread(deltaTime, target);
            }
        }

        private void CalculateSpread(float deltaTime, GEventable target) {
            //we need some kind increased probability over time that it spread
            //if it happens it will need to check for a valid target
            //if valid is found it needs to add itself as new effect to that target
            SpreadTick -= deltaTime;
            if (SpreadTick > 0) {
                return;
            }
            SpreadTick = GameData.EffectTickTime;
            if (Random.Range(0f, 1f) > SpreadProbability - SpreadProbability * WorkAmount) {
                return;
            }
            GEventable newTarget = GetValidTarget(target);
            if (newTarget == null)
                return;
            newTarget.AddEffect(new Effect(ID));
        }

        private GEventable GetValidTarget(GEventable target) {
            if (target is Structure structure) {
                List<Structure> strs = structure.GetNeighbourStructuresInTileDistance(SpreadTileRange);
                strs.RemoveAll(x => Targets.IsTargeted(x.TargetGroups) == false);
                //now we have a list we can effect
                //maybe smth more complex but for now just random
                return strs[Random.Range(0, strs.Count)];
            }
            Debug.LogError("CheckForValidTarget has not been implemented for " + target.GetType());
            return null;
        }

        private void CalculateUpdateChange(float deltaTime, GEventable target) {
            if (target is Structure structure) {
                switch (UpdateChange) {
                    case EffectUpdateChanges.Health:
                        structure.ChangeHealth(Change * deltaTime);
                        break;
                }
            }
            else
            if (target is Unit unit) {
                switch (UpdateChange) {
                    case EffectUpdateChanges.Health:
                        unit.ChangeHealth(Change * deltaTime);
                        break;
                }
            }
        }

        //for Serializing if it should be saved -- not needed for structure effects etc
        public bool ShouldSerializeEffect() {
            return Serialize;
        }
    }
}