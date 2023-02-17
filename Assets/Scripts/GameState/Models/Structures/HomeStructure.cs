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
    public class HomeStructure : TargetStructure, IHomeStructure {

        public enum CitizenMoods { Mad, Neutral, Happy }

        #region Serialize

        [JsonPropertyAttribute] public int People { get; protected set; }
        [JsonPropertyAttribute] protected float peopleDecreaseTimer;
        [JsonPropertyAttribute] protected float peopleIncreaseTimer;
        [JsonPropertyAttribute] public bool IsAbandoned { get; protected set; }

        #endregion Serialize

        #region RuntimeOrOther

        protected HomePrototypeData _homeData;

        public HomePrototypeData HomeData => _homeData ??= (HomePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);

        public CitizenMoods CurrentMood { get; protected set; }
        protected List<NeedStructure> NeedStructures;
        public int MaxLivingSpaces => HomeData.maxLivingSpaces;
        public HomeStructure NextLevel => HomeData.nextLevel;
        public HomeStructure PrevLevel => HomeData.prevLevel;

        public float IncreaseTime => CalculateRealValue(nameof(HomeData.increaseTime), HomeData.increaseTime);
        public float DecreaseTime => CalculateRealValue(nameof(HomeData.decreaseTime), HomeData.decreaseTime);

        public override bool CanBeUpgraded => MaxLivingSpaces == People // is full
                                && CurrentMood == CitizenMoods.Happy // still wants more People
                                && IsMaxLevel() == false // if there is smth to be upgraded to
                                && base.CanBeUpgraded // set through xml prototype file
                                && City.HasEnoughOfItems(NextLevel.BuildingItems) // city has enough items to build
                                && City.HasOwnerEnoughMoney(NextLevel.BuildCost) // player has enough money
                                && City.HasOwnerUnlockedAllNeeds(PopulationLevel);

        public List<INeedGroup> GetNeedGroups() {
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
            NeedStructures = new List<NeedStructure>();
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

        public float GetTaxPercentage() {
            return City.GetTaxPercentage(PopulationLevel);
        }

        private void OnNeedStructureChange(Tile tile, NeedStructure type, bool add) {
            if (add) {
                NeedStructures.Add(type);
            }
            else {
                NeedStructures.Remove(type);
            }
        }

        public override void OnUpdate(float deltaTime) {
            if (City.IsWilderness()) {
                //here the People are very unhappy and will leave veryfast
                CloseExtraUI();
                CurrentMood = CitizenMoods.Mad;
                return;
            }
            CalculateMood();
            UpdatePeopleChange(deltaTime);
        }

        public void CalculateMood() {
            float summedFulfillment = 0f;
            float summedImportance = 0;
            bool needNotFulfilled = false;
            foreach (INeedGroup ng in GetNeedGroups()) {
                if (ng.IsUnlocked() == false)
                    continue;
                Tuple<float, bool> fulfill = ng.GetFulfillmentForHome(this);
                summedFulfillment += fulfill.Item1;
                summedImportance += ng.ImportanceLevel;
                needNotFulfilled |= fulfill.Item2;
            }
            float percentage = summedFulfillment / summedImportance;
            //Tax can offset some unhappiness from missing stuff
            //this needs to be balanced tho
            //for now just 1:1 % from 1.5 to 0.5 happiness offset
            float tax = Mathf.Clamp(GetTaxPercentage(), 0.5f, 1.5f);
            percentage *= 2f - Mathf.Clamp(tax * tax, 0.5f, 2);

            if (percentage <= 0.5 || HasNegativeEffect) {
                CloseExtraUI();
                CurrentMood = CitizenMoods.Mad;
                return;
            }
            if (percentage > 0.9f && needNotFulfilled == false) {
                CurrentMood = CitizenMoods.Happy;
                return;
            }
            if (percentage > 0.5) {
                CloseExtraUI();
                CurrentMood = CitizenMoods.Neutral;
            }
        }

        protected void TryToIncreasePeople() {
            if (CurrentMood == CitizenMoods.Happy && People == MaxLivingSpaces) {
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

        public bool IsStructureNeedFulfilled(INeed need) {
            if (!need.HasToReachPerRoad)
                return need.IsSatisfiedThroughStructure(NeedStructures.Where((x) => x.City == City).ToList());
            if (GetRoutes().Count == 0)
                return false;
            need.IsSatisfiedThroughStructure(NeedStructures
                .Where((x) => x.City == City && x.GetRoutes().Overlaps(GetRoutes())).ToList());
            return need.IsSatisfiedThroughStructure(NeedStructures.Where((x) => x.City == City).ToList());
        }

        public void OnCityChange(Structure str, ICity old, ICity newOne) {
            if (old != null && old.IsWilderness() == false) {
                old.RemovePeople(PopulationLevel, People);
            }
            if (newOne.IsWilderness() == false) {
                newOne.AddPeople(PopulationLevel, People);
            }
        }

        protected void UpdatePeopleChange(float deltaTime) {
            switch (CurrentMood) {
                case CitizenMoods.Mad:
                    if (IsAbandoned == true)
                        return;
                    peopleIncreaseTimer = Mathf.Clamp(peopleIncreaseTimer - deltaTime, 0, IncreaseTime);
                    peopleDecreaseTimer = Mathf.Clamp(peopleDecreaseTimer + deltaTime, 0, DecreaseTime);
                    break;

                case CitizenMoods.Neutral:
                    if (IsAbandoned == true)
                        return;
                    peopleIncreaseTimer = Mathf.Clamp(peopleIncreaseTimer - deltaTime, 0, IncreaseTime);
                    peopleDecreaseTimer = Mathf.Clamp(peopleDecreaseTimer - deltaTime, 0, DecreaseTime);
                    break;

                case CitizenMoods.Happy:
                    peopleIncreaseTimer = Mathf.Clamp(peopleIncreaseTimer + deltaTime, 0, IncreaseTime);
                    peopleDecreaseTimer = Mathf.Clamp(peopleDecreaseTimer - deltaTime, 0, DecreaseTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (peopleIncreaseTimer >= IncreaseTime) {
                TryToIncreasePeople();
                peopleIncreaseTimer = 0f;
            }
            if (peopleDecreaseTimer >= DecreaseTime) {
                TryToDecreasePeople();
                peopleDecreaseTimer = 0f;
            }
        }

        public override void OnDestroy() {
            City.RemovePeople(PopulationLevel, People);
        }

        public bool UpgradeHouse() {
            if (CanBeUpgraded == false) {
                return false;
            }
            if (City.HasEnoughOfItems(NextLevel.BuildingItems) == false) {
                return false;
            }
            if (City.HasOwnerEnoughMoney(NextLevel.BuildCost) == false) {
                return false;
            }
            CloseExtraUI();
            ID = NextLevel.ID;
            City.RemovePeople(PopulationLevel, People);
            City.RemoveItems(NextLevel.BuildingItems);
            City.ReduceTreasureFromOwner(NextLevel.BuildCost);
            OnUpgrade();
            City.AddPeople(PopulationLevel, People);
            cbStructureChanged?.Invoke(this);
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
            prototypeData = null;
            City.AddPeople(PopulationLevel, People);
            cbStructureChanged?.Invoke(this);
        }

        public bool IsMaxLevel() {
            return PrototypController.Instance.GetMaxStructureLevelForStructureType(GetType()) == PopulationLevel;
        }

    }
}