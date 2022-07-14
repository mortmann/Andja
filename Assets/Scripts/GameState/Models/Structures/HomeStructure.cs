using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Andja.Utility;
using UnityEngine;

namespace Andja.Model {

    public class HomePrototypeData : StructurePrototypeData {
        public int maxLivingSpaces;
        public float increaseTime;
        public float decreaseTime;
        [Ignore]
        public HomeStructure nextLevel;
        [Ignore]
        public HomeStructure prevLevel;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class HomeStructure : TargetStructure {

        public enum CitizienMoods { Mad, Neutral, Happy }

        #region Serialize

        [JsonPropertyAttribute] public int People;
        [JsonPropertyAttribute] private float _decTimer;
        [JsonPropertyAttribute] private float _incTimer;
        [JsonPropertyAttribute] public bool IsAbandoned { get; protected set; }

        #endregion Serialize

        #region RuntimeOrOther

        protected HomePrototypeData _homeData;

        public HomePrototypeData HomeData {
            get {
                if (_homeData == null) {
                    _homeData = (HomePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
                }
                return _homeData;
            }
        }

        public CitizienMoods CurrentMood { get; protected set; }
        private List<NeedStructure> _needStructures;
        public int MaxLivingSpaces => HomeData.maxLivingSpaces;
        public HomeStructure NextLevel => HomeData.nextLevel;
        public HomeStructure PrevLevel => HomeData.prevLevel;

        public float IncreaseTime => CalculateRealValue(nameof(HomeData.increaseTime), HomeData.increaseTime);
        public float DecreaseTime => CalculateRealValue(nameof(HomeData.decreaseTime), HomeData.decreaseTime);

        public override bool CanBeUpgraded => MaxLivingSpaces == People // is full
                                && CurrentMood == CitizienMoods.Happy // still wants more People
                                && IsMaxLevel() == false // if there is smth to be upgraded to
                                && base.CanBeUpgraded // set through xml prototype file
                                && City.HasEnoughOfItems(NextLevel.BuildingItems) // city has enough items to build
                                && City.GetOwner().HasEnoughMoney(NextLevel.BuildCost)
                                && City.GetOwner().HasUnlockedAllNeeds(PopulationLevel); // player has enough money

        internal List<NeedGroup> GetNeedGroups() {
            return City.GetPopulationNeedGroups(PopulationLevel);
        }

        #endregion RuntimeOrOther

        public HomeStructure(string pid, HomePrototypeData proto) {
            this.ID = pid;
            this._homeData = proto;
            People = 1;
        }

        protected HomeStructure(HomeStructure b) {
            BaseCopyData(b);
            People = 1;
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public HomeStructure() { }

        public override Structure Clone() {
            return new HomeStructure(this);
        }

        public override void OnBuild() {
            _needStructures = new List<NeedStructure>();
            if (City.IsWilderness() == false) {
                OnCityChange(this, null, City);
            }
            foreach (Tile t in Tiles) {
                ((LandTile)t).RegisterOnNeedStructureChange(OnNeedStructureChange);
                List<NeedStructure> needsStructures = t.GetListOfInRangeCityNeedStructures();
                if (needsStructures == null)
                    continue;
                foreach (NeedStructure ns in needsStructures) {
                    OnNeedStructureChange(t, ns, true);
                }
            }
            RegisterOnOwnerChange(OnCityChange);
        }

        internal float GetTaxPercantage() {
            return City.GetPopulationLevel(PopulationLevel).taxPercantage;
        }

        private void OnNeedStructureChange(Tile tile, NeedStructure type, bool add) {
            if (add) {
                _needStructures.Add(type);
            }
            else {
                _needStructures.Remove(type);
            }
        }

        public override void OnUpdate(float deltaTime) {
            if (City == null || City.IsWilderness()) {
                //here the People are very unhappy and will leave veryfast
                CloseExtraUI();
                CurrentMood = CitizienMoods.Mad;
                return;
            }

            float summedFullfillment = 0f;
            float summedImportance = 0;
            bool needNotFullfilled = false;
            foreach (NeedGroup ng in GetNeedGroups()) {
                if (ng.IsUnlocked() == false)
                    continue;
                Tuple<float, bool> fullfill = ng.GetFullfillmentForHome(this);
                summedFullfillment += fullfill.Item1;
                summedImportance += ng.ImportanceLevel;
                needNotFullfilled |= fullfill.Item2;
            }
            float percentage = summedFullfillment / summedImportance;

            //Tax can offset some unhappines from missing stuff
            //this needs to be balanced tho
            //for now just 1:1 % from 1.5 to 0.5 happiness offset
            float tax = Mathf.Clamp(City.GetPopulationLevel(PopulationLevel).taxPercantage, 0.5f, 1.5f);
            percentage += 2f - Mathf.Clamp(tax * tax, 0.5f, 2);
            percentage /= 2;
            if (percentage > 0.9f && needNotFullfilled == false) {
                CurrentMood = CitizienMoods.Happy;
            }
            else
            if (percentage > 0.5) {
                CloseExtraUI();
                CurrentMood = CitizienMoods.Neutral;
            }
            else {
                CloseExtraUI();
                CurrentMood = CitizienMoods.Mad;
            }
            if (HasNegativEffect)
                CurrentMood = CitizienMoods.Mad;
            UpdatePeopleChange(deltaTime);
        }

        protected void TryToIncreasePeople() {
            if (CurrentMood == CitizienMoods.Happy && People == MaxLivingSpaces) {
                if (CanBeUpgraded) {
                    OpenExtraUI();
                    TryToUpgrade();
                }
            }
            if (People >= MaxLivingSpaces) {
                return;
            }
            if (IsAbandoned) {
                IsAbandoned = false;
            }
            People++;
            City.AddPeople(PopulationLevel, 1);
        }

        protected void TryToDecreasePeople() {
            if (People <= 0) {
                return;
            }
            People--;
            IsAbandoned = People == 0;
            City.RemovePeople(PopulationLevel, 1);
            if (PrevLevel != null && People < PrevLevel.MaxLivingSpaces)
                DowngradeHouse();
        }

        private void TryToUpgrade() {
            if (City.AutoUpgradeHomes == false) {
                return;
            }
            UpgradeHouse();
        }

        public override void OpenExtraUI() {
            if (base.CanBeUpgraded)
                base.OpenExtraUI();
        }

        public bool IsStructureNeedFullfilled(Need need) {
            if (!need.HasToReachPerRoad)
                return need.IsSatisifiedThroughStructure(_needStructures.Where((x) => x.City == City).ToList());
            if (GetRoutes().Count == 0)
                return false;
            need.IsSatisifiedThroughStructure(_needStructures
                .Where((x) => x.City == City && x.GetRoutes().Overlaps(GetRoutes())).ToList());
            return need.IsSatisifiedThroughStructure(_needStructures.Where((x) => x.City == City).ToList());
        }

        protected void OnCityChange(Structure str, ICity old, ICity newOne) {
            if (old != null && old.IsWilderness() == false) {
                old.RemovePeople(PopulationLevel, People);
            }
            if (newOne.IsWilderness() == false) {
                newOne.AddPeople(PopulationLevel, People);
            }
        }

        protected void UpdatePeopleChange(float deltaTime) {
            switch (CurrentMood) {
                case CitizienMoods.Mad:
                    if (IsAbandoned == true)
                        return;
                    _incTimer = Mathf.Clamp(_incTimer - deltaTime, 0, IncreaseTime);
                    _decTimer = Mathf.Clamp(_decTimer + deltaTime, 0, DecreaseTime);
                    break;

                case CitizienMoods.Neutral:
                    if (IsAbandoned == true)
                        return;
                    _incTimer = Mathf.Clamp(_incTimer - deltaTime, 0, IncreaseTime);
                    _decTimer = Mathf.Clamp(_decTimer - deltaTime, 0, DecreaseTime);
                    break;

                case CitizienMoods.Happy:
                    _incTimer = Mathf.Clamp(_incTimer + deltaTime, 0, IncreaseTime);
                    _decTimer = Mathf.Clamp(_decTimer - deltaTime, 0, DecreaseTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (_incTimer >= IncreaseTime) {
                TryToIncreasePeople();
                _incTimer = 0f;
            }
            if (_decTimer >= DecreaseTime) {
                TryToDecreasePeople();
                _decTimer = 0f;
            }
        }

        public override void OnDestroy() {
            City.RemovePeople(PopulationLevel, People);
        }

        public bool UpgradeHouse() {
            if (base.CanBeUpgraded == false && IsMaxLevel()) {
                return false;
            }
            if (City.HasEnoughOfItems(NextLevel.BuildingItems) == false) {
                return false;
            }
            if (City.GetOwner().HasEnoughMoney(NextLevel.BuildCost) == false) {
                return false;
            }
            CloseExtraUI();
            ID = NextLevel.ID;
            if(City.GetPopulationCount(PopulationLevel) == 0) {
                //not a nice solution for the problem of the level not being calculated value!
                //TODO: find a nicer way todo it
                City.GetPopulationLevel(PopulationLevel).FullfillNeedsAndCalcHappiness(City);
            }
            City.RemovePeople(PopulationLevel, People);
            City.RemoveItems(NextLevel.BuildingItems);
            City.GetOwner().ReduceTreasure(NextLevel.BuildCost);
            OnUpgrade();
            City.AddPeople(PopulationLevel, People);
            cbStructureChanged(this);
            return true;
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            _homeData = null;
        }
        public void DowngradeHouse() {
            ID = PrevLevel.ID;
            City.RemovePeople(PopulationLevel, People);
            _homeData = null;
            _prototypData = null;
            City.AddPeople(PopulationLevel, People);
            cbStructureChanged(this);
        }

        public bool IsMaxLevel() {
            return PrototypController.Instance.GetMaxStructureLevelForStructureType(GetType()) == PopulationLevel;
        }

    }
}