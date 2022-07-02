using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [JsonPropertyAttribute] public int people;
        [JsonPropertyAttribute] public float decTimer;
        [JsonPropertyAttribute] public float incTimer;
        [JsonPropertyAttribute] public bool isAbandoned;

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

        public CitizienMoods currentMood { get; protected set; }
        private List<NeedStructure> needStructures;
        public int MaxLivingSpaces => HomeData.maxLivingSpaces;
        public HomeStructure NextLevel => HomeData.nextLevel;
        public HomeStructure PrevLevel => HomeData.prevLevel;

        public float IncreaseTime => CalculateRealValue(nameof(HomeData.increaseTime), HomeData.increaseTime);
        public float DecreaseTime => CalculateRealValue(nameof(HomeData.decreaseTime), HomeData.decreaseTime);

        public override bool CanBeUpgraded => MaxLivingSpaces == people // is full
                                && currentMood == CitizienMoods.Happy // still wants more people
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
            people = 1;
        }

        protected HomeStructure(HomeStructure b) {
            BaseCopyData(b);
            people = 1;
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public HomeStructure() { }

        public override Structure Clone() {
            return new HomeStructure(this);
        }

        public override void OnBuild() {
            needStructures = new List<NeedStructure>();
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
                needStructures.Add(type);
            }
            else {
                needStructures.Remove(type);
            }
        }

        public override void OnUpdate(float deltaTime) {
            if (City == null || City.IsWilderness()) {
                //here the people are very unhappy and will leave veryfast
                CloseExtraUI();
                currentMood = CitizienMoods.Mad;
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
                currentMood = CitizienMoods.Happy;
            }
            else
            if (percentage > 0.5) {
                CloseExtraUI();
                currentMood = CitizienMoods.Neutral;
            }
            else {
                CloseExtraUI();
                currentMood = CitizienMoods.Mad;
            }
            if (HasNegativEffect)
                currentMood = CitizienMoods.Mad;
            UpdatePeopleChange(deltaTime);
        }

        private void TryToIncreasePeople() {
            if (currentMood == CitizienMoods.Happy && people == MaxLivingSpaces) {
                if (CanBeUpgraded) {
                    OpenExtraUI();
                    TryToUpgrade();
                }
            }
            if (people >= MaxLivingSpaces) {
                return;
            }
            if (isAbandoned == true) {
                isAbandoned = false;
            }
            people++;
            City.AddPeople(PopulationLevel, 1);
        }

        private void TryToDecreasePeople() {
            if (people <= 0) {
                isAbandoned = true;
                return;
            }
            people--;
            City.RemovePeople(PopulationLevel, 1);
            if (PrevLevel != null && people < PrevLevel.MaxLivingSpaces)
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

        public override void CloseExtraUI() {
            base.CloseExtraUI();
        }

        public bool IsStructureNeedFullfilled(Need need) {
            if (need.HasToReachPerRoad) {
                if (GetRoutes().Count == 0)
                    return false;
                need.IsSatisifiedThroughStructure(needStructures.Where((x) => x.City == City && x.GetRoutes().Overlaps(GetRoutes())).ToList());
            }
            return need.IsSatisifiedThroughStructure(needStructures.Where((x) => x.City == City).ToList());
        }

        protected void OnCityChange(Structure str, City old, City newOne) {
            if (old != null && old.IsWilderness() == false) {
                old.RemovePeople(PopulationLevel, people);
            }
            if (newOne.IsWilderness() == false) {
                newOne.AddPeople(PopulationLevel, people);
            }
        }

        protected void UpdatePeopleChange(float deltaTime) {
            switch (currentMood) {
                case CitizienMoods.Mad:
                    if (isAbandoned == true)
                        return;
                    incTimer = Mathf.Clamp(incTimer - deltaTime, 0, IncreaseTime);
                    decTimer = Mathf.Clamp(decTimer + deltaTime, 0, DecreaseTime);
                    break;

                case CitizienMoods.Neutral:
                    if (isAbandoned == true)
                        return;
                    incTimer = Mathf.Clamp(incTimer - deltaTime, 0, IncreaseTime);
                    decTimer = Mathf.Clamp(decTimer - deltaTime, 0, DecreaseTime);
                    break;

                case CitizienMoods.Happy:
                    incTimer = Mathf.Clamp(incTimer + deltaTime, 0, IncreaseTime);
                    decTimer = Mathf.Clamp(decTimer - deltaTime, 0, DecreaseTime);
                    break;
            }
            if (incTimer >= IncreaseTime) {
                TryToIncreasePeople();
                incTimer = 0f;
            }
            if (decTimer >= DecreaseTime) {
                TryToDecreasePeople();
                decTimer = 0f;
            }
        }

        public override void OnDestroy() {
            City.RemovePeople(PopulationLevel, people);
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
            City.RemovePeople(PopulationLevel, people);
            City.RemoveResources(NextLevel.BuildingItems);
            City.GetOwner().ReduceTreasure(NextLevel.BuildCost);
            OnUpgrade();
            City.AddPeople(PopulationLevel, people);
            cbStructureChanged(this);
            return true;
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            _homeData = null;
        }
        public void DowngradeHouse() {
            ID = PrevLevel.ID;
            City.RemovePeople(PopulationLevel, people);
            _homeData = null;
            _prototypData = null;
            City.AddPeople(PopulationLevel, people);
            cbStructureChanged(this);
        }

        public bool IsMaxLevel() {
            return PrototypController.Instance.GetMaxStructureLevelForStructureType(GetType()) == PopulationLevel;
        }

        protected override void AddSpecialEffect(Effect effect) {
            base.AddSpecialEffect(effect);
        }

        protected override void RemoveSpecialEffect(Effect effect) {
            base.RemoveSpecialEffect(effect);
        }
    }
}