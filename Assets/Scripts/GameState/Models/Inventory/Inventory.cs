using Andja.Controller;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Inventory {

        protected Action<Inventory> cbInventoryChanged;
        protected Action<Inventory, Item, bool> cbInventoryItemChange;
        [JsonPropertyAttribute] public int MaxStackSize { get; protected set; }

        public abstract IEnumerable<Item> baseItems {
            get;
        }

        public void AddInventory(Inventory inv) {
            foreach (Item item in inv.GetAllItemsAndRemoveThem()) {
                AddItem(item);
            }
        }

        /// <summary>
        /// returns amount
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public abstract int AddItem(Item toAdd);

        public void AddItems(IEnumerable<Item> items) {
            foreach (Item item in items) {
                AddItem(item);
            }
        }
        public Item GetItemWithMaxItemCount(Item item) {
            return GetItemWithMaxAmount(item, item.count);
        }
        public Item GetItemWithMaxAmount(Item item, int maxAmount) {
            Item output = item.Clone();
            output.count = maxAmount.ClampZero(GetAmountFor(item));
            LowerItemAmount(item, output.count);
            return output;
        }
        /// <summary>
        /// WARNING THIS WILL EMPTY THE COMPLETE
        /// INVENTORY NOTHING WILL BE LEFT
        /// </summary>
        public abstract Item[] GetAllItemsAndRemoveThem();

        public abstract Item GetAllAndRemoveItem(Item item);
        public bool HasAnythingOf(Item item) {
            return GetAllAndRemoveItem(item).count > 0;
        }
        public Item GetAllOfItem(string itemID) {
            return GetAllAndRemoveItem(new Item(itemID));
        }

        public virtual int GetAmountFor(Item item) {
            return GetAmountFor(item.ID);
        }

        public abstract int GetAmountFor(string itemID);

        public Item[] GetBuildMaterial() {
            List<Item> itemlist = new List<Item>();
            foreach (Item i in PrototypController.Instance.BuildItems) {
                if (HasAnythingOf(i)) {
                    itemlist.Add(GetAllAndRemoveItem(i));
                }
            }
            return itemlist.ToArray();
        }

        public abstract int GetRemainingSpaceForItem(Item item);

        public bool HasAnything() {
            return baseItems.Any((x) => x.count > 0);
        }

        public bool HasEnoughOfItem(Item item) {
            return GetAmountFor(item) >= item.count;
        }

        public bool HasEnoughOfItems(IEnumerable<Item> item) {
            return item.All(x => HasEnoughOfItem(x));
        }

        public bool HasEnoughOfItems(IEnumerable<Item> items, int times = 1) {
            if (items == null || times <= 0)
                return true;
            items.ToList().ForEach(x => { x.count *= times; });
            return HasEnoughOfItems(items);
        }

        public virtual bool HasRemainingSpaceForItem(Item item) {
            return GetRemainingSpaceForItem(item) > 0;
        }

        public virtual void Load() {
            foreach (var item in baseItems.ToArray()) {
                if (item.Exists() == false) {
                    RemoveNotExistingItem(item);
                }
            }
        }
        public virtual void OnChanged(Inventory me) {
            
        }
        protected abstract void RemoveNotExistingItem(Item item);

        /// <summary>
        /// moves item amount to the given inventory
        /// </summary>
        /// <returns><c>true</c>, if item was moved, <c>false</c> otherwise.</returns>
        /// <param name="inv">Inv.</param>
        /// <param name="it">It.</param>
        public int MoveItem(Inventory moveToInv, Item it, int amountToMove) {
            amountToMove = amountToMove.ClampZero(GetAmountFor(it));
            int movedAmount = moveToInv.AddItem(new Item(it.ID, amountToMove));
            LowerItemAmount(it, movedAmount);
            return movedAmount;
        }

        /// <summary>
        /// removes an item amount from this inventory but not moving it to any other inventory
        /// destroys item amount forever! FOREVER!
        /// </summary>
        /// <returns><c>true</c>, if item amount was removed, <c>false</c> otherwise.</returns>
        /// <param name="remove">It.</param>
        public bool RemoveItemAmount(Item remove) {
            if (remove == null) {
                return true;
            }
            if (GetAmountFor(remove) < remove.count || remove.count <= 0) {
                return false;
            }
            LowerItemAmount(remove, remove.count);
            return true;
        }

        /// <summary>
        /// Only removes when all items are present and enough.
        /// </summary>
        /// <returns><c>true</c>, if items amount was removed, <c>false</c> otherwise.</returns>
        /// <param name="toRemove">Items with count for remove amount</param>
        public bool RemoveItemsAmount(IEnumerable<Item> toRemove) {
            if (HasEnoughOfItems(toRemove) == false) {
                return false;
            }
            foreach (Item i in toRemove) {
                LowerItemAmount(i, i.count);
            }
            return true;
        }

        public void UnregisterOnChangedCallback(Action<Inventory> cb) {
            cbInventoryChanged -= cb;
        }

        public void UnregisterOnChangedCallback(Action<Inventory, Item, bool> cb) {
            cbInventoryItemChange -= cb;
        }

        public void RegisterOnChangedCallback(Action<Inventory> cb) {
            cbInventoryChanged += cb;
        }

        public void RegisterOnChangedCallback(Action<Inventory, Item, bool> cb) {
            cbInventoryItemChange += cb;
        }

        protected void IncreaseItemAmount(Item item, int amount) {
            if (amount < 0) {
                Debug.LogError("Increase Amount is " + amount + "! ");
            }
            item.count += amount;
            cbInventoryChanged?.Invoke(this);
        }

        protected abstract void LowerItemAmount(Item lower, int amount);

        protected int MoveAmountFromItemToInv(Item toBeMoved, Item toReceive) {
            int amount = toBeMoved.count.ClampZero(MaxStackSize - toReceive.count.ClampZero());
            IncreaseItemAmount(toReceive, amount);
            toBeMoved.count -= amount;
            return amount;
        }
    }
}