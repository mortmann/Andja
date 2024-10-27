using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    public class PopulationLevelPrototypData : LanguageVariables {
        public List<INeedGroup> needGroupList; //not used as "copy" of the USED in "ticks" just as a reference which Needs gets unlocked with this level!
        public int LEVEL; // cant be negative!
        public string iconSpriteName;
        public int taxPerPerson = 1;
        [Ignore] public HomeStructure HomeStructure { get; set; }
        public List<INeedGroup> GetCopyGroupNeedList() {
            List<INeedGroup> newList = new List<INeedGroup>();
            if (needGroupList == null)
                return newList;
            for (int i = 0; i < needGroupList.Count; i++) {
                newList.Add(needGroupList[i].CloneEmptyList());
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
    public class PopulationLevel : IPopulationLevel {

        [JsonPropertyAttribute] public int Level { get; set; }
        [JsonPropertyAttribute] private List<INeedGroup> _needGroupList;
        [JsonPropertyAttribute] public PopulationLevel previousLevel;
        [JsonPropertyAttribute] public float taxPercentage = 1f;
        private ICity _city;
        public int PopulationCount { get; set; } = 0;
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
            LoadPreviouseNeedGroups();
            this.previousLevel = previous;
            this._city = city;
            UpdateNeeds();
            city.GetOwner().RegisterNeedUnlock(OnUnlockedNeed);
        }

        private void LoadPreviouseNeedGroups() {
            if (previousLevel == null) return;
            AllNeedGroupList.AddRange(previousLevel.GetAllPreviousNeedGroups());
        }

        public PopulationLevel(PopulationLevel pl) {
            this.Level = pl.Level;
        }

        public void FulfillNeedsAndCalcHappiness() {
            foreach (INeedGroup group in AllNeedGroupList) {
                group.CalculateFulfillment(_city, this);
            }
        }

        public void SetTaxPercentage(float percentage) {
            taxPercentage = Mathf.Clamp(percentage, 0, 100); //not real restrictions but just a complete fuckup prevention
        }

        public int GetTaxIncome() {
            return Mathf.FloorToInt(taxPercentage * TaxPerPerson * PopulationCount);
        }

        public void RegisterNeedUnlock(Action<Need> onNeedUnlock) {
            _cbNeedUnlockAdded += onNeedUnlock;
        }

        public void UnregisterNeedUnlock(Action<Need> onNeedUnlock) {
            _cbNeedUnlockAdded -= onNeedUnlock;
        }

        public void AddPeople(int count) {
            if (count < 0) {
                return;
            }
            //IF there is better way to stop People after upgrading -- change this 
            PopulationCount += count;
            _city.GetOwner().UpdateMaxPopulationCount(Level, PopulationCount);
        }

        public void RemovePeople(int count) {
            if (count < 0) {
                return;
            }
            PopulationCount -= count;
        }
        public List<INeedGroup> GetAllPreviousNeedGroups() {
            List<INeedGroup> temp = new List<INeedGroup>();
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
            LoadPreviouseNeedGroups();
            UpdateNeeds();
        }

        private void UpdateNeeds() {
            _needGroupList ??= new List<INeedGroup>();
            for (int i = 0; i < _needGroupList.Count; i++) {
                if (_needGroupList[i].ID != null && Data.needGroupList.Find(x => x.ID == _needGroupList[i].ID) != null) {
                    continue;
                }
                AllNeedGroupList.Remove(_needGroupList[i]);
                _needGroupList.Remove(_needGroupList[i]);
            }
            if (Data.needGroupList == null)
                return;
            IPlayer player = _city.GetOwner();
            player.RegisterNeedUnlock(OnUnlockedNeed);
            foreach (INeedGroup ng in Data.needGroupList) {
                INeedGroup inList = _needGroupList.Find(x => x.ID == ng.ID);
                if (inList == null) {
                    inList = ng.CloneEmptyList();
                    _needGroupList.Add(inList);
                    AllNeedGroupList.Add(inList);
                }
                inList.UpdateNeeds(player);
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
            INeedGroup ng = _needGroupList.Find(x => x.ID == need.Group.ID);
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