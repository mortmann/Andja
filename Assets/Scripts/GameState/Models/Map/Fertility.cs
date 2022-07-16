using Andja.Controller;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public class FertilityPrototypeData : LanguageVariables, IWeighted {
        public string ID;
        public Climate[] climates;
        public float percentageOfIslands;
        [Ignore] public HashSet<string> ItemsDependentOnThis = new HashSet<string>();
        [Ignore] public int UnlockLevel;
        [Ignore] public int UnlockPopulationCount;

        public float GetStartWeight() {
            return percentageOfIslands;
        }

        public float GetCurrentWeight(int maximumSelect) {
            return Mathf.Clamp(percentageOfIslands - _generated / (float)maximumSelect, 0.01f, 1);
        }

        [Ignore] private int _generated;
        public float Select(int maximumSelect) {
            float old = GetCurrentWeight(maximumSelect);
            _generated++;
            return old - GetCurrentWeight(maximumSelect);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Fertility : IComparable<Fertility>, IEqualityComparer<Fertility> {
        [JsonPropertyAttribute] public string ID;

        private FertilityPrototypeData _prototypeData;

        public FertilityPrototypeData Data => _prototypeData ??= PrototypController.Instance.GetFertilityPrototypDataForID(ID);

        public string Name => Data.Name;

        public Climate[] Climates => Data.climates;

        public Fertility() {
        }

        public Fertility(string ID, FertilityPrototypeData fpd) {
            this.ID = ID;
            this._prototypeData = fpd;
        }

        #region IComparable implementation

        public int CompareTo(Fertility other) {
            return ID.CompareTo(other.ID);
        }

        #endregion IComparable implementation

        #region IEqualityComparer implementation

        public bool Equals(Fertility x, Fertility y) {
            return x.ID == y.ID;
        }

        public int GetHashCode(Fertility obj) {
            return GetHashCode();
        }

        #endregion IEqualityComparer implementation

        public override bool Equals(object obj) {
            if (!(obj is Fertility f))
                return false;
            return f.ID == ID;
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }

        internal bool IsUnlocked(Player player) {
            return player.MaxPopulationLevel > Data.UnlockLevel && player.MaxPopulationCount > Data.UnlockPopulationCount;
        }

        public override string ToString() {
            return ID;
        }
    }

    public class FertilityCost {
        public List<Fertility> fertilities;

        public FertilityCost(params Fertility[] required) {
            fertilities = new List<Fertility>(required);
        }

        public bool Fulfills(Fertility fert) {
            return fertilities.Contains(fert);
        }
    }
}