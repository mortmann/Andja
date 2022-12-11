using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Andja.Model {

    public enum ItemType { Missing, Build,/* Resource ,*/ Intermediate, Luxury, Military }

    public class ItemPrototypeData : LanguageVariables {
        public ItemType type;
        [Ignore] public int UnlockLevel;
        [Ignore] public int UnlockPopulationCount;
        [Ignore] public List<Need> SatisfiesNeeds;
        [Ignore] public float[] TotalUsagePerLevel; // is only for luxury goods & ai
        [Ignore] public float AIValue =>
            //TODO: calculate the *worth* of an item based on the cost/rarity of it
            UnlockLevel / (float)PrototypController.Instance.NumberOfPopulationLevels;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Item {
        protected ItemPrototypeData prototypeData;

        [JsonPropertyAttribute] public string ID;
        [JsonPropertyAttribute] public int count;

        internal string CountString => count + "t";

        public ItemPrototypeData Data => prototypeData ??= PrototypController.Instance.GetItemPrototypDataForID(ID);

        public ItemType Type => Data.type;

        public string Name => Data.Name;

        public Item(string id, int count = 0) {
            this.ID = id;
            this.count = count;
        }

        public Item(string id, ItemPrototypeData ipd) : this(id) {
            this.prototypeData = ipd;
            this.count = 0;
        }

        public Item(Item other) : this(other.ID) {
        }
        public Item(Item other, int count = 0) : this(other.ID, count) {
        }
        public Item() {
        }

        public virtual Item Clone() {
            return new Item(this);
        }
        public virtual Item Clone(int amount) {
            return new Item(this, amount);
        }
        public virtual Item CloneWithCount(int times = 1) {
            Item i = new Item(this) {
                count = this.count * times
            };
            return i;
        }

        internal string ToSmallString() {
            return Data == null ? ID : string.Format(Name + ":" + count + "t");
        }

        public override string ToString() {
            return Data == null ? ID : string.Format("[Item] " + ID + ":" + Name + ":" + count);
        }

        public bool Exists() {
            return Data.type != ItemType.Missing;
        }

        public static bool AreSame(Item one, Item two) {
            return one.ID == two.ID && one.count == two.count;
        }

    }
}