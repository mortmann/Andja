using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Andja.Utility;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class UnitInventory : Inventory {

        public Item[] Items;

        [JsonPropertyAttribute]
        public byte NumberOfSpaces {
            get;
            protected set;
        }

        private float _amountInInventory;

        public override IEnumerable<Item> BaseItems => Items.Where(x=> x != null);

        /// <summary>
        /// leave blanc for unlimited spaces! To limited it give a int > 0
        /// </summary>
        /// <param name="numberOfSpaces"></param>
        /// <param name="maxStackSize"></param>
        public UnitInventory(byte numberOfSpaces = 0, int maxStackSize = 50) {
            this.MaxStackSize = maxStackSize;
            Items = new Item[numberOfSpaces];
            this.NumberOfSpaces = numberOfSpaces;
            RegisterOnChangedCallback(OnChanged);
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public UnitInventory() {
            RegisterOnChangedCallback(OnChanged);
        }

        public override Item GetAllAndRemoveItem(Item item) {
            return GetItemWithMaxAmount(item, int.MaxValue);
        }

        /// <summary>
        /// returns amount
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override int AddItem(Item toAdd) {
            if (toAdd == null) {
                return 0;
            }
            if (string.IsNullOrEmpty(toAdd.ID)) {
                Debug.LogError("ITEM ID is not set!");
                return 0;
            }
            int amount = 0;
            foreach (Item inInv in BaseItems) {
                if (inInv.ID == toAdd.ID) {
                    if (inInv.count == MaxStackSize) {
                        continue;
                    }
                    // move
                    amount = MoveAmountFromItemToInv(toAdd, inInv);
                    if (toAdd.count == 0) {
                        break;
                    }
                }
            }
            while (toAdd.count > 0 && AreSlotsFilledWithItems() == false) {
                Item temp = toAdd.Clone();
                amount += MoveAmountFromItemToInv(toAdd, temp);
                Items[FirstFreeSpace()] = temp;
                cbInventoryChanged?.Invoke(this);
            }
            return amount;
        }

        private int FirstFreeSpace() {
            return Array.IndexOf(Items, null);
        }

        public override Item[] GetAllItemsAndRemoveThem() {
            List<Item> temp = new List<Item>(BaseItems);
            for (int i = 0; i < Items.Length; i++) {
                Items[i] = null;
            }
            cbInventoryChanged?.Invoke(this);
            return temp.ToArray();
        }

        public override int GetAmountFor(string itemID) {
            return BaseItems.Where(x => x.ID == itemID).Sum(x => x.count);
        }

        protected int GetFirstPlaceInItems(Item item) {
            for (int i = 0; i < NumberOfSpaces; i++) {
                if (Items.Length <= i && Items[i].ID == item.ID) {
                    return i;
                }
            }
            return -1;
        }
        
        public float GetFilledPercentage() {
            return _amountInInventory / (float)(NumberOfSpaces * MaxStackSize);
        }

        public override int GetRemainingSpaceForItem(Item item) {
            return (MaxStackSize + MaxStackSize * FreeSpacesLeft()) - GetAmountFor(item);
        }
        public Item GetItemInSpace(int i) {
            return Items[i];
        }

        public bool HasItemInSpace(int i) {
            return Items[i] != null;
        }
        
        public override void OnChanged(Inventory me) {
            _amountInInventory = 0;
            foreach (Item i in BaseItems) {
                _amountInInventory += i.count;
            }
        }
        public Item[] GetItemsInInventory(Item item) {
            return BaseItems.Where(x => x.ID == item.ID).ToArray();
        }
        
        protected override void LowerItemAmount(Item lower, int amount) {
            //Item[] array = Items.Where(i => i.ID == lower.ID).ToArray();
            for (int i = 0; i < Items.Length; i++) {
                Item inInv = Items[i];
                if (inInv == null || inInv.ID != lower.ID)
                    continue;
                int loweredAmount = amount.ClampZero(inInv.count);
                inInv.count -= loweredAmount;
                amount -= loweredAmount;
                if (amount == 0) {
                    break;
                }
                if (inInv.count == 0) {
                    Items[i] = null;
                }
            }
            cbInventoryChanged?.Invoke(this);
        }
        protected Item GetFirstItemInInventory(Item item) {
            return GetFirstItemInInventory(item.ID);
        }
        protected Item GetFirstItemInInventory(string itemID) {
            int pos = GetFirstPlaceInItems(new Item(itemID));
            return pos < 0 ? null : Items[pos];
        }

        public bool AreSlotsFilledWithItems() {

            return NumberOfSpaces <= BaseItems.Count();
        }

        /// <summary>
        /// Only works with non city inventories.
        /// </summary>
        /// <param name="space"></param>
        public virtual int AddItemInSpace(int space, Item item) {
            int amount = 0;
            Item inSpace = GetItemInSpace(space);
            if (inSpace == null) {
                amount = item.count.ClampZero(MaxStackSize);
                item.count = amount;
                Items[space] = item;
                cbInventoryChanged?.Invoke(this);
            }
            else if (inSpace.ID == item.ID) {
                amount = (amount - inSpace.count).ClampZero(MaxStackSize);
                inSpace.count += amount;
            }
            return amount;
        }
        /// <summary>
        /// Only works with non city inventories.
        /// </summary>
        /// <param name="space"></param>
        public virtual void RemoveItemInSpace(int space) {
            Items[space] = null;
            cbInventoryChanged?.Invoke(this);
        }

        public int FreeSpacesLeft() {
            return NumberOfSpaces - Items.Count(x => x != null);
        }

        protected override void RemoveNotExistingItem(Item item) {
            for (int i = 0; i < Items.Length; i++) {
                if (Items[i].ID == item.ID) {
                    Items[i] = null;
                }
            }
        }
    }
}