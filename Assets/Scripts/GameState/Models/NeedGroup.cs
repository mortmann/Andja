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
        private readonly float _fulfillmentLimit = 0.05f;

        #region Prototype

        protected NeedGroupPrototypeData prototypeData;

        public NeedGroupPrototypeData Data => prototypeData ??= PrototypController.Instance.GetNeedGroupPrototypDataForID(ID);

        public float ImportanceLevel => Data.importanceLevel;
        public string Name => Data.Name;

        #endregion Prototype

        [JsonPropertyAttribute] public string ID { get; protected set; }
        [JsonPropertyAttribute] public List<INeed> Needs { get; protected set; }
        [JsonPropertyAttribute] public float LastFulfillmentPercentage { get; protected set; }

        #region Runtime
        public List<INeed> CombinedNeeds { get; protected set; }
        #endregion Runtime

        public NeedGroup() {
            CombinedNeeds = new List<INeed>();
        }

        public NeedGroup(string ID) {
            Needs = new List<INeed>();
            this.ID = ID;
            CombinedNeeds = new List<INeed>();
        }

        public NeedGroup(NeedGroup needGroup) {
            ID = needGroup.ID;
            prototypeData = needGroup.Data;
            Needs = new List<INeed>();
            foreach (INeed n in needGroup.Needs) {
                Needs.Add(n.Clone());
            }
            CombinedNeeds = new List<INeed>();
        }

        public INeedGroup Clone() {
            return new NeedGroup(this);
        }
        public INeedGroup CloneEmptyList() {
            return new NeedGroup(ID);
        }

        public void AddNeeds(IEnumerable<INeed> INeed) {
            Needs.AddRange(INeed);
        }

        public void CalculateFulfillment(ICity city, IPopulationLevel populationLevel) {
            float currentValue = 0;
            int number = 0;
            foreach (INeed Need in Needs) {
                if (Need.IsStructureNeed()) {
                    continue;
                }
                number++;
                Need.CalculateFulfillment(city, populationLevel);
                currentValue += Need.GetCombinedFulfillment();
            }
            currentValue = CalculateRealPercentage(currentValue, number);
            LastFulfillmentPercentage = currentValue; // currently not needed! but maybe nice to have
        }

        public void CombineGroup(NeedGroup ng) {
            CombinedNeeds.AddRange(ng.Needs);
        }

        public void AddNeed(INeed need) {
            Needs.Add(need);
            CombinedNeeds.Add(need);
        }

        public void UpdateNeeds(IPlayer player) {
            List<INeed> currNeeds = new List<INeed>(Needs);
            foreach (INeed need in currNeeds) {
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

        public Tuple<float, bool> GetFulfillmentForHome(IHomeStructure homeStructure) {
            float currentValue = 0;
            bool missing = false;
            foreach (INeed INeed in Needs) {
                if (INeed.IsStructureNeed()) {
                    bool structureFulfilled = homeStructure.IsStructureNeedFulfilled(INeed);
                    missing &= structureFulfilled;
                    currentValue += structureFulfilled ? 1 : 0;
                }
                else {
                    float Fulfilled = INeed.GetFulfillment(homeStructure.PopulationLevel);
                    missing |= Fulfilled < _fulfillmentLimit;
                    currentValue += Fulfilled;
                }
            }
            return new Tuple<float, bool>(CalculateRealPercentage(currentValue, Needs.Count), missing);
        }

        public bool HasNeed(INeed need) {
            return Needs.Contains(need);
        }

        public bool IsUnlocked() {
            return Needs.Count > 0;
        }
    }
}