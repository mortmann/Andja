using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class CityInventory : Inventory {
        /// <summary>
        /// Workaround because if we load with this constructor we get empty item counts
        /// because somehow it overrides it after deserializing 
        /// </summary>
        /// <param name="fakeNumber"></param>
        public CityInventory(int fakeNumber) {
            NumberOfSpaces = -1;
            if (Items == null)
                Items = PrototypController.Instance.GetCopieOfAllItems();
            MaxStackSize = 50;
        }
        public CityInventory() {

        }

        public override int AddItem(Item toAdd) {
            if (String.IsNullOrEmpty(toAdd.ID)) {
                Debug.LogError("ITEM ID is empty or null");
                return 0;
            }
            Item inInv = GetItem(toAdd.ID);
            //if its already full no need to put it in there
            if (inInv.count == MaxStackSize) {
                return 0;
            }
            return MoveAmountFromItemToInv(toAdd, inInv);
        }

        protected override string GetPlaceInItems(Item item) {
            return item.ID;
        }

        public override int GetAmountFor(Item item) {
            return GetAmountFor(item.ID);
        }

        public override int GetAmountFor(string itemID) {
            return Items[itemID].count;
        }

        protected override Item GetItem(string id) {
            return Items[id];
        }

        public override Item GetItemClone(string id) {
            return Items[id].CloneWithCount();
        }

        public override bool ContainsItemWithID(string id) {
            return true;
        }

        protected override int RemainingSpaceForItem(Item item) {
            return MaxStackSize - GetAmountFor(item);
        }
        protected override Item[] GetItemsInInventory(Item item) {
            return new Item[] { GetItem(item.ID) };
        }
        public override void SetItemCountNull(Item item) {
            GetFirstItemInInventory(item).count = 0;
            cbInventoryChanged?.Invoke(this);
        }

        public override Item[] GetAllItemsAndRemoveThem() {
            //get all items in a list
            List<Item> temp = new List<Item>(Items.Values);
            Items = BuildController.Instance.GetCopieOfAllItems();
            cbInventoryChanged?.Invoke(this);
            return temp.ToArray();
        }

        protected override void LowerItemAmount(Item i, int amount) {
            Item invItem = Items[GetPlaceInItems(i)];
            invItem.count = Mathf.Max(invItem.count - amount, 0);
            cbInventoryChanged?.Invoke(this);
        }

        internal override void RemoveItemInSpace(int space) {
            Debug.LogWarning("This function does not work with city inventories.");
        }
        public override void Load() {
            base.Load();
            CheckForMissingItems();
        }
        internal void CheckForMissingItems() {
            var copyItems = PrototypController.Instance.GetCopieOfAllItems();
            foreach (var item in copyItems) {
                if(Items.ContainsKey(item.Key) == false) {
                    Items.Add(item.Key, item.Value);
                }
            }
        }
    }
}