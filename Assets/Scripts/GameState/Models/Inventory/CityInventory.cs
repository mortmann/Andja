using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class CityInventory : Inventory {
        [JsonPropertyAttribute(PropertyName = "Items")]
        public override Item[] SerializableItems {
            get => Items?.Where(x => x.Value.count > 0)
                .Select(x=>x.Value).ToArray();
            set {
                Items = value.ToDictionary(x => x.ID, y => y);
            } 
        }
        public Dictionary<string, Item> Items {
            get;
            protected set;
        }
        public override IEnumerable<Item> BaseItems  => Items.Values; 
        

        /// <summary>
        /// Workaround because if we load with this constructor we get empty item counts
        /// because somehow it overrides it after deserializing 
        /// </summary>
        /// <param name="fakeNumber"></param>
        public CityInventory(int fakeNumber) {
            Items ??= PrototypController.Instance.GetCopieOfAllItems();
            MaxStackSize = 50;
        }
        public CityInventory() {

        }

        public override int AddItem(Item toAdd) {
            Item inInv = Items[toAdd.ID];
            return MoveAmountFromItemToInv(toAdd, inInv);
        }

        public override int GetAmountFor(Item item) {
            return GetAmountFor(item.ID);
        }

        public override int GetAmountFor(string itemID) {
            return Items[itemID].count;
        }

        public override int GetRemainingSpaceForItem(Item item) {
            return MaxStackSize - GetAmountFor(item);
        }
    
        public override Item[] GetAllItemsAndRemoveThem() {
            //get all items in a list
            List<Item> temp = new List<Item>(Items.Values.Where(x=>x.count > 0));
            Items = PrototypController.Instance.GetCopieOfAllItems();
            cbInventoryChanged?.Invoke(this);
            return temp.ToArray();
        }

        protected override void LowerItemAmount(Item i, int amount) {
            Item invItem = Items[i.ID];
            invItem.count = Mathf.Max(invItem.count - amount, 0);
            cbInventoryChanged?.Invoke(this);
        }
        public override void Load() {
            base.Load();
            CheckForMissingItems();
        }
        internal void CheckForMissingItems() {
            var copyItems = PrototypController.Instance.GetCopieOfAllItems();
            foreach (var item in copyItems.Where(item => Items.ContainsKey(item.Key) == false)) {
                Items.Add(item.Key, item.Value);
            }
        }

        protected override void RemoveNotExistingItem(Item item) {
            Items.Remove(item.ID);
        }

        internal Item GetItemClone(Item item) {
            return Items[item.ID].CloneWithCount();
        }

        public override Item GetAllAndRemoveItem(Item item) {
            Item clone = Items[item.ID].CloneWithCount();
            Items[item.ID].count = 0;
            return clone;
        }

    }
}