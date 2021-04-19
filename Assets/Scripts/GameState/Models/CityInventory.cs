using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class CityInventory : Inventory {

        public CityInventory() {
            NumberOfSpaces = -1;
            Items = PrototypController.Instance.GetCopieOfAllItems();
            MaxStackSize = 50;
        }

        public override int AddItem(Item toAdd) {
            if (String.IsNullOrEmpty(toAdd.ID)) {
                Debug.LogError("ITEM ID is empty or null");
                return 0;
            }
            Item inInv = GetItem(toAdd.ID);
            //if its already full no need to put it in there
            if (inInv.count == MaxStackSize) {
                Debug.Log("inventory-item is full " + inInv.count);
                return 0;
            }
            return MoveAmountFromItemToInv(toAdd, inInv);
        }

        protected override string GetPlaceInItems(Item item) {
            return item.ID;
        }

        public override int GetTotalAmountFor(Item item) {
            return GetAmountForItem(item);
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
            return MaxStackSize - GetAmountForItem(item);
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
    }
}