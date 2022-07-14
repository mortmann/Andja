using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    [JsonObject]
    public class NeedGroupPrototypData : LanguageVariables {
        public string ID;
        public float importanceLevel;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class NeedGroup {
        private readonly float FullfillmentLimit = 0;

        #region Prototype

        protected NeedGroupPrototypData _prototypData;

        public NeedGroupPrototypData Data {
            get {
                if (_prototypData == null) {
                    _prototypData = PrototypController.Instance.GetNeedGroupPrototypDataForID(ID);
                }
                return _prototypData;
            }
        }

        public float ImportanceLevel => Data.importanceLevel;
        public string Name => Data.Name;

        #endregion Prototype

        [JsonPropertyAttribute] public string ID;
        [JsonPropertyAttribute] public List<Need> Needs;
        [JsonPropertyAttribute] public float LastFullfillmentPercentage;

        #region Runtime
        public List<Need> CombinedNeeds;
        #endregion Runtime

        public NeedGroup() {
            CombinedNeeds = new List<Need>();
        }

        public NeedGroup(string ID) {
            Needs = new List<Need>();
            this.ID = ID;
            CombinedNeeds = new List<Need>();
        }

        public NeedGroup(NeedGroup needGroup) {
            ID = needGroup.ID;
            _prototypData = needGroup.Data;
            Needs = new List<Need>();
            foreach (Need n in needGroup.Needs) {
                Needs.Add(n.Clone());
            }
            CombinedNeeds = new List<Need>();
        }

        public NeedGroup Clone() {
            return new NeedGroup(this);
        }

        public void AddNeeds(IEnumerable<Need> need) {
            Needs.AddRange(need);
        }

        internal void CalculateFullfillment(ICity city, PopulationLevel populationLevel) {
            float currentValue = 0;
            int number = 0;
            foreach (Need need in Needs) {
                if (need.IsStructureNeed()) {
                    continue;
                }
                number++;
                need.CalculateFullfillment(city, populationLevel);
                currentValue += need.GetCombinedFullfillment();
            }
            currentValue = CalculateRealPercantage(currentValue, number);
            LastFullfillmentPercentage = currentValue; // currently not needed! but maybe nice to have
        }

        public void CombineGroup(NeedGroup ng) {
            CombinedNeeds.AddRange(ng.Needs);
        }

        internal void AddNeed(Need need) {
            Needs.Add(need);
            CombinedNeeds.Add(need);
        }

        internal void UpdateNeeds(Player player) {
            List<Need> currNeeds = new List<Need>(Needs);
            foreach (Need need in currNeeds) {
                if (player.HasNeedUnlocked(need) == false) {
                    Needs.Remove(need);
                }
                if (need.Exists() == false || need.IsStructureNeed()) {
                    Needs.Remove(need);
                }
            }
        }

        private float CalculateRealPercantage(float percentage, int number) {
            if (number == 0)
                return 1;
            percentage /= number;
            percentage *= Mathf.Clamp(ImportanceLevel, 0.4f, 1.6f);
            return percentage;
        }

        internal Tuple<float, bool> GetFullfillmentForHome(HomeStructure homeStructure) {
            float currentValue = 0;
            bool missing = false;
            foreach (Need need in Needs) {
                if (need.IsStructureNeed()) {
                    bool structureFullfilled = homeStructure.IsStructureNeedFullfilled(need);
                    missing &= structureFullfilled;
                    currentValue += structureFullfilled ? 1 : 0;
                }
                else {
                    float fullfilled = need.GetFullfiment(homeStructure.PopulationLevel);
                    missing |= fullfilled < FullfillmentLimit;
                    currentValue += fullfilled;
                }
            }
            return new Tuple<float, bool>(CalculateRealPercantage(currentValue, Needs.Count), missing);
        }

        internal bool HasNeed(Need need) {
            return Needs.Contains(need);
        }

        internal bool IsUnlocked() {
            return Needs.Count > 0;
        }
    }
}