using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public class PopulationLevelPrototypData : LanguageVariables {
        public List<NeedGroup> needGroupList; //not used as "copy" of the USED in "ticks" just as a reference which Needs gets unlocked with this level!
        public int LEVEL; // cant be negative!
        public string iconSpriteName;
        public int taxPerPerson = 1;

        public HomeStructure HomeStructure { get; internal set; }

        internal List<NeedGroup> GetCopyGroupNeedList() {
            List<NeedGroup> newList = new List<NeedGroup>();
            if (needGroupList == null)
                return newList;
            for (int i = 0; i < needGroupList.Count; i++) {
                newList.Add(needGroupList[i].Clone());
            }
            return newList;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PopulationLevel {

        #region Serialize

        public int populationCount = 0;
        [JsonPropertyAttribute] public bool criticalMissingNeed = false;
        [JsonPropertyAttribute] public int Level;
        [JsonPropertyAttribute] public List<NeedGroup> NeedGroupList;
        [JsonPropertyAttribute] public PopulationLevel previousLevel;
        [JsonPropertyAttribute] public float taxPercantage = 1f;
        private City city;
        public string IconSpriteName => Data.iconSpriteName;

        #endregion Serialize

        #region Runtime

        protected PopulationLevelPrototypData _Data;

        public PopulationLevelPrototypData Data {
            get {
                if (_Data == null) {
                    _Data = (PopulationLevelPrototypData)PrototypController.Instance.GetPopulationLevelPrototypDataForLevel(Level);
                }
                return _Data;
            }
        }

        private Action<Need> cbNeedUnlockAdded;
        public int TaxPerPerson => Data.taxPerPerson;
        public float Happiness { get; internal set; }

        #endregion Runtime

        public PopulationLevel() {
        }

        public PopulationLevel(int level, City city, PopulationLevel previous) {
            this.Level = level;
            NeedGroupList = Data.GetCopyGroupNeedList();
            this.previousLevel = previous;
            this.city = city;
            city.GetOwner().RegisterNeedUnlock(OnUnlockedNeed);
        }

        public PopulationLevel(PopulationLevel pl) {
            this.Level = pl.Level;
        }

        internal void FullfillNeedsAndCalcHappiness(City city) {
            float fullfilled = 0;
            bool missingNeed = false;
            float summedImportance = 0;
            foreach (NeedGroup group in NeedGroupList) {
                group.CalculateFullfillment(city, this);
                fullfilled += group.LastFullfillmentPercentage;
                summedImportance += group.ImportanceLevel;
                if (group.HasMissingNeed)
                    missingNeed = true;
            }
            criticalMissingNeed = missingNeed;
            fullfilled /= summedImportance;
            //Debug.Log("City " + city.Name + " - " + Level + " Fullfilled: " + fullfilled);
            //TODO: make it trend towards the happiness? so it doesnt swing like crazy
        }

        internal void SetTaxPercantage(float percantage) {
            taxPercantage = Mathf.Clamp(percantage, 0, 100); //not real restrictions but just a complete fuckup prevention
        }

        public int GetTaxIncome(City city) {
            return Mathf.FloorToInt(taxPercantage * TaxPerPerson * populationCount);
        }

        internal void RegisterNeedUnlock(Action<Need> onNeedUnlock) {
            cbNeedUnlockAdded += onNeedUnlock;
        }

        internal void UnregisterNeedUnlock(Action<Need> onNeedUnlock) {
            cbNeedUnlockAdded -= onNeedUnlock;
        }

        internal void AddPeople(int count) {
            city.GetOwner().UpdateMaxPopulationCount(Level, populationCount);
            populationCount += count;
        }

        internal void RemovePeople(int count) {
            populationCount -= count;
        }

        public List<NeedGroup> GetAllPreviousNeedGroups() {
            List<NeedGroup> temp = new List<NeedGroup>();
            if (NeedGroupList != null) {
                temp.AddRange(NeedGroupList);
            }
            if (previousLevel != null)
                temp.AddRange(previousLevel.GetAllPreviousNeedGroups());
            return temp;
        }

        internal PopulationLevel Clone() {
            return new PopulationLevel(this);
        }

        internal void Load(City city) {
            this.city = city;
            if (previousLevel == null || previousLevel.Exists() == false)
                previousLevel = city.GetPreviousPopulationLevel(Level);
            UpdateNeeds();
        }

        private void UpdateNeeds() {
            if (NeedGroupList == null)
                NeedGroupList = new List<NeedGroup>();
            for (int i = 0; i < NeedGroupList.Count; i++) {
                if (NeedGroupList[i].ID == null || Data.needGroupList.Find(x => x.ID == NeedGroupList[i].ID) == null) {
                    NeedGroupList.Remove(NeedGroupList[i]);
                }
            }
            if (Data.needGroupList == null)
                return;
            Player player = PlayerController.GetPlayer(city.PlayerNumber);
            player.RegisterNeedUnlock(OnUnlockedNeed);
            foreach (NeedGroup ng in Data.needGroupList) {
                NeedGroup inList = NeedGroupList.Find(x => x.ID == ng.ID);
                if (inList == null) {
                    inList = new NeedGroup(ng.ID);
                    NeedGroupList.Add(inList);
                }
                inList.UpdateNeeds(city.GetOwner());
            }
            foreach (string needID in player.UnlockedItemNeeds[Level]) {
                OnUnlockedNeed(new Need(needID));
            }
            foreach (string needID in player.UnlockedStructureNeeds[Level]) {
                OnUnlockedNeed(new Need(needID));
            }
        }

        internal bool Exists() {
            return Data != null;
        }

        private void OnUnlockedNeed(Need need) {
            if (need.StartLevel != Level)
                return;
            NeedGroup ng = NeedGroupList.Find(x => x.ID == need.Group.ID);
            if (ng == null) {
                Debug.LogError("UnlockedNeed " + need.ID + " doesnt have the right group inside this level " + Level);
                return;
            }
            if (ng.HasNeed(need))
                return;
            Need clone = need.Clone();
            cbNeedUnlockAdded?.Invoke(clone);
            ng.AddNeed(clone);
        }
    }
}