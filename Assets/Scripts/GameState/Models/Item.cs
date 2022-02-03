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
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Item {
        [JsonPropertyAttribute] public string ID;
        [JsonPropertyAttribute] public int count;

        protected ItemPrototypeData _prototypData;
        internal string countString => count + "t";

        public ItemPrototypeData Data {
            get {
                if (_prototypData == null) {
                    _prototypData = PrototypController.Instance.GetItemPrototypDataForID(ID);
                }
                return _prototypData;
            }
        }

        public ItemType Type {
            get {
                return Data.type;
            }
        }

        public string Name {
            get {
                return Data.Name;
            }
        }

        public Item(string id, int count = 0) {
            this.ID = id;
            this.count = count;
        }

        public Item(string id, ItemPrototypeData ipd) {
            this.ID = id;
            this._prototypData = ipd;
            this.count = 0;
        }

        public Item(Item other) {
            this.ID = other.ID;
        }

        public Item() {
        }

        virtual public Item Clone() {
            return new Item(this);
        }

        virtual public Item CloneWithCount() {
            Item i = new Item(this) {
                count = this.count
            };
            return i;
        }

        internal string ToSmallString() {
            return string.Format(Name + ":" + count + "t");
        }

        public override string ToString() {
            return string.Format("[Item] " + ID + ":" + Name + ":" + count);
        }

        internal bool Exists() {
            return Data.type != ItemType.Missing;
        }
    }
}