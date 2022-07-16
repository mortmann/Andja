using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    [JsonObject]
    public class NeedGroupPrototypeData : LanguageVariables {
        public string ID;
        public float importanceLevel;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class NeedGroup : INeedGroup {
        private readonly float _fulfillmentLimit = 0;

        #region Prototype

        protected NeedGroupPrototypeData prototypeData;

        public NeedGroupPrototypeData Data => prototypeData ??= PrototypController.Instance.GetNeedGroupPrototypDataForID(ID);

        public float ImportanceLevel => Data.importanceLevel;
        public string Name => Data.Name;

        #endregion Prototype

        [JsonPropertyAttribute] public string ID { get; protected set; }
        [JsonPropertyAttribute] public List<Need> Needs { get; protected set; }
        [JsonPropertyAttribute] public float LastFulfillmentPercentage { get; protected set; }

        #region Runtime
        public List<Need> CombinedNeeds { get; protected set; }
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
            prototypeData = needGroup.Data;
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

        public void CalculateFulfillment(ICity city, PopulationLevel populationLevel) {
            float currentValue = 0;
            int number = 0;
            foreach (Need need in Needs) {
                if (need.IsStructureNeed()) {
                    continue;
                }
                number++;
                need.CalculateFulfillment(city, populationLevel);
                currentValue += need.GetCombinedFulfillment();
            }
            currentValue = CalculateRealPercentage(currentValue, number);
            LastFulfillmentPercentage = currentValue; // currently not needed! but maybe nice to have
        }

        public void CombineGroup(NeedGroup ng) {
            CombinedNeeds.AddRange(ng.Needs);
        }

        public void AddNeed(Need need) {
            Needs.Add(need);
            CombinedNeeds.Add(need);
        }

        public void UpdateNeeds(Player player) {
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

        private float CalculateRealPercentage(float percentage, int number) {
            if (number == 0)
                return 1;
            percentage /= number;
            percentage *= Mathf.Clamp(ImportanceLevel, 0.4f, 1.6f);
            return percentage;
        }

        public Tuple<float, bool> GetFulfillmentForHome(HomeStructure homeStructure) {
            float currentValue = 0;
            bool missing = false;
            foreach (Need need in Needs) {
                if (need.IsStructureNeed()) {
                    bool structureFulfilled = homeStructure.IsStructureNeedFulfilled(need);
                    missing &= structureFulfilled;
                    currentValue += structureFulfilled ? 1 : 0;
                }
                else {
                    float Fulfilled = need.GetFulfillment(homeStructure.PopulationLevel);
                    missing |= Fulfilled < _fulfillmentLimit;
                    currentValue += Fulfilled;
                }
            }
            return new Tuple<float, bool>(CalculateRealPercentage(currentValue, Needs.Count), missing);
        }

        public bool HasNeed(Need need) {
            return Needs.Contains(need);
        }

        public bool IsUnlocked() {
            return Needs.Count > 0;
        }
    }
}