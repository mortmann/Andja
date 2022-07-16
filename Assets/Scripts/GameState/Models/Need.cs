using Andja.Controller;
using Andja.Model.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    public class NeedPrototypeData : LanguageVariables {
        public Item item;
        public NeedStructure[] structures;
        public NeedGroupPrototypeData group; // only for the typ of the group needed!
        public float[] UsageAmounts;
        public int startLevel;
        public int startPopulationCount;
        public bool hasToReachPerRoad;
        [Ignore] public Dictionary<Produce, int[]> produceForPeople;
    }

    /// <summary>
    /// each need needs to be in a group with similar needs
    /// those group have a priority level and each member has a "priority in its group"
    /// how important it is for its owngroup fulfillment
    /// </summary>

    [JsonObject(MemberSerialization.OptIn)]
    public class Need {
        protected NeedPrototypeData _prototypData;

        public NeedPrototypeData Data => _prototypData ??= PrototypController.Instance.GetNeedPrototypDataForID(ID);

        public NeedGroupPrototypeData Group => Data.group;

        public string Name => Data.Name;

        public Item Item => Data.item;

        public NeedStructure[] Structures => Data.structures;

        public float[] Uses => Data.UsageAmounts;

        public int StartLevel => Data.startLevel;

        public bool HasToReachPerRoad => Data.hasToReachPerRoad;

        public int StartPopulationCount => Data.startPopulationCount;

        [JsonPropertyAttribute]
        public string ID;

        [JsonPropertyAttribute]
        public float[] lastNeededNotConsumed;

        [JsonPropertyAttribute]
        public float[] PercentageAvailability;

        [JsonPropertyAttribute]
        public float notUsedOfTon = 0;

        public Need(string id, NeedPrototypeData npd) : this() {
            this.ID = id;
            this._prototypData = npd;
        }

        [JsonConstructor]
        public Need() {
            if (lastNeededNotConsumed == null) {
                lastNeededNotConsumed = new float[PrototypController.Instance.NumberOfPopulationLevels];
                PercentageAvailability = new float[PrototypController.Instance.NumberOfPopulationLevels];
            }
            else {
                if (lastNeededNotConsumed.Length < PrototypController.Instance.NumberOfPopulationLevels) {
                    Array.Resize(ref lastNeededNotConsumed, PrototypController.Instance.NumberOfPopulationLevels);
                    Array.Resize(ref PercentageAvailability, PrototypController.Instance.NumberOfPopulationLevels);
                }
            }
        }

        public Need Clone() {
            return new Need(this);
        }

        protected Need(Need other) : this() {
            this.ID = other.ID;
        }

        public Need(string id) {
            this.ID = id;
        }

        public void CalculateFulfillment(ICity city, PopulationLevel level) {
            TryToConsumeThisIn(city, level.populationCount, level.Level);
        }

        public void TryToConsumeThisIn(ICity city, int people, int level) {
            if (people == 0) {
                return;
            }
            if (Item == null) {
                //this does not require any item -> it needs a structure
                PercentageAvailability[level] = 0;
                return;
            }
            float neededConsumAmount = 0;
            // how much do we need to consum?
            neededConsumAmount += Uses[level] * ((float)people);
            if (neededConsumAmount <= 0) {
                //we dont need anything to consum so no need to go anyfurther
                PercentageAvailability[level] = 0;
                return;
            }
            //IF it has still enough no need to calculate more
            if (notUsedOfTon >= neededConsumAmount) {
                notUsedOfTon -= neededConsumAmount;
                PercentageAvailability[level] = 1;
                return;
            }
            //so just remove from needed what we have
            neededConsumAmount -= notUsedOfTon;
            notUsedOfTon = 0;
            //now we have a amount that still needs to be Fulfilled by the cities inventory

            float availableAmount = city.GetAmountForThis(Item);
            //either we need to get 1 ton or as much as we need
            neededConsumAmount = Mathf.CeilToInt(neededConsumAmount);
            //now how much do we have in the city
            //if we have none?
            if (availableAmount == 0) {
                //we can just set the lastneeded to current needed
                lastNeededNotConsumed[level] = neededConsumAmount;
                PercentageAvailability[level] = 0;
                return;
            }

            // how much to we consum of the avaible?
            float usedAmount = Mathf.Clamp(availableAmount, 0, neededConsumAmount);
            //now remove that amount of items
            if (usedAmount > neededConsumAmount)
                notUsedOfTon = usedAmount - neededConsumAmount;

            city.RemoveItem(Item, Mathf.CeilToInt(usedAmount));
            //minimum is 1 because if 0 -> ERROR due dividing through 0
            //calculate the Percentage of availability
            PercentageAvailability[level] = (usedAmount / neededConsumAmount);
        }

        internal bool IsSatisfiedThroughStructure(List<NeedStructure> strs) {
            return Array.Exists(Structures, x => strs.Exists(y => y.ID == x.ID));
        }

        internal void SetStructureFulfilled(bool Fulfilled) {
            if (IsItemNeed())
                return;
            if (Fulfilled) {
                Array.ForEach(PercentageAvailability, x => x = 1);
            }
            else {
                Array.ForEach(PercentageAvailability, x => x = 0);
            }
        }

        internal float GetFulfillment(int populationLevel) {
            return PercentageAvailability[populationLevel];
        }

        internal float GetCombinedFulfillment() {
            return PercentageAvailability.Sum() / PercentageAvailability.Length;
        }

        public bool IsItemNeed() {
            return Item != null;
        }

        public bool IsStructureNeed() {
            return Structures != null;
        }

        public bool Exists() {
            return Data != null;
        }

        public override bool Equals(object obj) {
            if (!(obj is Need item)) {
                return false;
            }
            return ID.Equals(item.ID);
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }
    }
}