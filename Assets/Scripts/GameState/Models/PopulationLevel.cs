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
        [Ignore] public HomeStructure HomeStructure { get; set; }
        public List<NeedGroup> GetCopyGroupNeedList() {
            List<NeedGroup> newList = new List<NeedGroup>();
            if (needGroupList == null)
                return newList;
            for (int i = 0; i < needGroupList.Count; i++) {
                newList.Add(needGroupList[i].Clone());
            }
            return newList;
        }
    }
    /// <summary>
    /// Contains how many People of this level in the city exist.
    /// Claculates Need fullfilment (happiness excluding the structure needs for corresponding level 
    /// and the Taxpercentage for income.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class PopulationLevel {

        [JsonPropertyAttribute] public int Level;
        [JsonPropertyAttribute] private List<NeedGroup> _needGroupList;
        [JsonPropertyAttribute] public PopulationLevel previousLevel;
        [JsonPropertyAttribute] public float taxPercentage = 1f;
        private ICity _city;
        public int populationCount = 0;
        public string IconSpriteName => Data.iconSpriteName;

        private PopulationLevelPrototypData _data;
        public List<INeedGroup> AllNeedGroupList;

        public PopulationLevelPrototypData Data => _data ??= PrototypController.Instance.GetPopulationLevelPrototypDataForLevel(Level);

        private Action<Need> _cbNeedUnlockAdded;
        public int TaxPerPerson => Data.taxPerPerson;
        public float Happiness { get; protected set; }

        public PopulationLevel() {
        }

        public PopulationLevel(int level, ICity city, PopulationLevel previous) {
            this.Level = level;
            _needGroupList = Data.GetCopyGroupNeedList();
            AllNeedGroupList = new List<INeedGroup>(_needGroupList);
            AllNeedGroupList.AddRange(GetAllPreviousNeedGroups());
            this.previousLevel = previous;
            this._city = city;
            city.GetOwner().RegisterNeedUnlock(OnUnlockedNeed);
        }

        public PopulationLevel(PopulationLevel pl) {
            this.Level = pl.Level;
        }

        public void FulfillNeedsAndCalcHappiness() {
            foreach (INeedGroup group in AllNeedGroupList) {
                group.CalculateFulfillment(_city, this);
            }
            //Debug.Log("City " + city.Name + " - " + Level + " Fulfilled: " + Fulfilled);
            //TODO: make it trend towards the happiness? so it doesnt swing like crazy
        }

        public void SetTaxPercentage(float percentage) {
            taxPercentage = Mathf.Clamp(percentage, 0, 100); //not real restrictions but just a complete fuckup prevention
        }

        public int GetTaxIncome() {
            return Mathf.FloorToInt(taxPercentage * TaxPerPerson * populationCount);
        }

        public void RegisterNeedUnlock(Action<Need> onNeedUnlock) {
            _cbNeedUnlockAdded += onNeedUnlock;
        }

        public void UnregisterNeedUnlock(Action<Need> onNeedUnlock) {
            _cbNeedUnlockAdded -= onNeedUnlock;
        }

        public void AddPeople(int count) {
            //IF there is better way to stop People after upgrading -- change this 
            populationCount += count;
            _city.GetOwner().UpdateMaxPopulationCount(Level, populationCount);
        }

        public void RemovePeople(int count) {
            populationCount -= count;
        }

        public List<NeedGroup> GetAllPreviousNeedGroups() {
            List<NeedGroup> temp = new List<NeedGroup>();
            if (_needGroupList != null) {
                temp.AddRange(_needGroupList);
            }
            if (previousLevel != null)
                temp.AddRange(previousLevel.GetAllPreviousNeedGroups());
            return temp;
        }

        public PopulationLevel Clone() {
            return new PopulationLevel(this);
        }

        public void Load(ICity city) {
            this._city = city;
            if (previousLevel == null || previousLevel.Exists() == false)
                previousLevel = city.GetPreviousPopulationLevel(Level);
            AllNeedGroupList = new List<INeedGroup>(_needGroupList);
            AllNeedGroupList.AddRange(GetAllPreviousNeedGroups());
            UpdateNeeds();
        }

        private void UpdateNeeds() {
            _needGroupList ??= new List<NeedGroup>();
            for (int i = 0; i < _needGroupList.Count; i++) {
                if (_needGroupList[i].ID == null || Data.needGroupList.Find(x => x.ID == _needGroupList[i].ID) == null) {
                    _needGroupList.Remove(_needGroupList[i]);
                }
            }
            if (Data.needGroupList == null)
                return;
            Player player = PlayerController.Instance.GetPlayer(_city.PlayerNumber);
            player.RegisterNeedUnlock(OnUnlockedNeed);
            foreach (NeedGroup ng in Data.needGroupList) {
                NeedGroup inList = _needGroupList.Find(x => x.ID == ng.ID);
                if (inList == null) {
                    inList = new NeedGroup(ng.ID);
                    _needGroupList.Add(inList);
                }
                inList.UpdateNeeds(_city.GetOwner());
            }
            foreach (string needID in player.UnlockedItemNeeds[Level]) {
                OnUnlockedNeed(new Need(needID));
            }
            foreach (string needID in player.UnlockedStructureNeeds[Level]) {
                OnUnlockedNeed(new Need(needID));
            }
        }

        public bool Exists() {
            return Data != null;
        }

        private void OnUnlockedNeed(Need need) {
            if (need.StartLevel != Level)
                return;
            NeedGroup ng = _needGroupList.Find(x => x.ID == need.Group.ID);
            if (ng == null) {
                Debug.LogError("UnlockedNeed " + need.ID + " doesnt have the right group inside this level " + Level);
                return;
            }
            if (ng.HasNeed(need))
                return;
            Need clone = need.Clone();
            _cbNeedUnlockAdded?.Invoke(clone);
            ng.AddNeed(clone);
        }

    }
}