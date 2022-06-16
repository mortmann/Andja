using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Andja.Utility;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class Inventory {
        [JsonPropertyAttribute] public int MaxStackSize { get; protected set; }

        [JsonPropertyAttribute(PropertyName = "Items")]
        public Dictionary<string, Item> SerializableItems {
            get {
                return Items?.Where(x => x.Value.count > 0).ToDictionary(entry => entry.Key,
                                                                        entry => entry.Value);
            }
            set {
                Items = value;
            }
        }
        public Dictionary<string, Item> Items {
            get;
            protected set;
        }

        [JsonPropertyAttribute]
        public int NumberOfSpaces {
            get;
            protected set;
        }

        protected Action<Inventory> cbInventoryChanged;
        protected Action<Inventory, Item, bool> cbInventoryItemChange;
        private float amountInInventory;

        public bool HasLimitedSpace {
            get { return NumberOfSpaces != -1; }
        }

        /// <summary>
        /// leave blanc for unlimited spaces! To limited it give a int > 0
        /// </summary>
        /// <param name="numberOfSpaces"></param>
        public Inventory(int numberOfSpaces = -1, int maxStackSize = 50) {
            this.MaxStackSize = maxStackSize;
            Items = new Dictionary<string, Item>();
            this.NumberOfSpaces = numberOfSpaces;
            RegisterOnChangedCallback(OnChanged);
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public Inventory() {
            RegisterOnChangedCallback(OnChanged);
        }

        /// <summary>
        /// returns amount
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual int AddItem(Item toAdd) {
            if(toAdd == null) {
                return 0;
            }
            if (String.IsNullOrEmpty(toAdd.ID)) {
                Debug.LogError("ITEM ID is not set!");
                return 0;
            }
            int amount = 0;
            foreach (Item inInv in Items.Values) {
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
            while (toAdd.count > 0 && IsSpacesFilled() == false) {
                Item temp = toAdd.Clone();
                amount += MoveAmountFromItemToInv(toAdd, temp);
                for (int i = 0; i < NumberOfSpaces; i++) {
                    if (Items.ContainsKey("" + i) == false) {
                        Items.Add("" + i, temp);
                        break;
                    }
                }
                cbInventoryChanged?.Invoke(this);
            }
            return amount;
        }

        /// <summary>
        /// Only works with Limited Inventory Spaces!
        /// -- if needed change amountInInventory to be caluclated each time smth is added/removed (increased/decreased)
        /// </summary>
        /// <returns></returns>
        public float GetFilledPercantage() {
            return amountInInventory / (float)(NumberOfSpaces * MaxStackSize);
        }

        protected virtual string GetPlaceInItems(Item item) {
            for (int i = 0; i < NumberOfSpaces; i++) {
                if (Items.ContainsKey("" + i) && Items["" + i].ID == item.ID) {
                    return "" + i;
                }
            }
            return "";
        }

        /// <summary>
        /// Dont use this with CITY Inventory
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public bool HasItemInSpace(int i) {
            return Items.ContainsKey("" + i);
        }

        /// <summary>
        /// Dont use this with CITY Inventory
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Item GetItemInSpace(int i) {
            if (HasItemInSpace(i) == false) {
                return null;
            }
            return Items["" + i];
        }

        public Item GetAllOfItem(Item item) {
            return GetItemWithMaxAmount(item, int.MaxValue);
        }

        public Item GetAllOfItem(string itemID) {
            return GetAllOfItem(new Item(itemID));
        }

        public Item GetItemWithMaxItemCount(Item item) {
            return GetItemWithMaxAmount(item, item.count);
        }

        public Item GetItemWithMaxAmount(Item item, int maxAmount) {
            Item[] inInv = GetItemsInInventory(item);
            if (inInv == null) {
                return null;
            }
            Item output = item.Clone();
            foreach (Item i in inInv) {
                if (maxAmount <= 0)
                    break;
                int getFromThisItem = 0;
                //get as much as needed from this item in the inventory
                getFromThisItem = Mathf.Clamp(i.count, 0, maxAmount);
                LowerItemAmount(i, getFromThisItem);
                maxAmount -= getFromThisItem;
                output.count += getFromThisItem;
            }
            return output;
        }

        /// <summary>
        /// IF NumberOfSpaces == -1 then this will return the only item there is because there are no multiple items of this
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual Item[] GetItemsInInventory(Item item) {
            if (NumberOfSpaces == -1)
                return null;
            return Items.Where(x => x.Value.ID == item.ID).Select(x=>x.Value).ToArray();
        }

        protected Item GetFirstItemInInventory(Item item) {
            return GetFirstItemInInventory(item.ID);
        }
        protected Item GetFirstItemInInventory(string itemID) {
            string pos = GetPlaceInItems(new Item(itemID));
            if (string.IsNullOrEmpty(pos)) {
                return null;
            }
            return Items[pos];
        }

        public bool HasAnythingOf(Item item) {
            if (GetFirstItemInInventory(item)?.count > 0) {
                return true;
            }
            return false;
        }

        public virtual int GetAmountFor(Item item) {
            return GetAmountFor(item.ID);
        }

        public virtual int GetAmountFor(string itemID) {
            return Items.Where(x=>x.Value.ID == itemID).Sum(x=>x.Value.count);
        }

        public bool HasAnything() {
            return Items.Any((x)=>x.Value.count>0);
        }

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
        /// should only be called for Inventories associated with units
        /// cause city technically are always full / empty that means it
        /// always has a spot to unload the item
        /// </summary>
        /// <returns></returns>
        public virtual bool IsSpacesFilled() {
            
            return NumberOfSpaces <= Items.Count;
        }

        public virtual int GetRemainingSpaceForItem(Item item) {
            return (MaxStackSize+ MaxStackSize * FreeSpacesLeft()) - GetAmountFor(item);
        }
        public virtual bool HasRemainingSpaceForItem(Item item) {
            return GetRemainingSpaceForItem(item) > 0;
        }
        /// <summary>
        /// Only works with non city inventories.
        /// </summary>
        /// <param name="space"></param>
        public virtual int AddItemInSpace(int space, Item item) {
            int amount = 0;
            Item inSpace = GetItemInSpace(space);
            if(inSpace == null) {
                amount = item.count.ClampZero(MaxStackSize);
                item.count = amount;
                Items.Add("" + space, item);
                cbInventoryChanged?.Invoke(this);
            } 
            else if(inSpace.ID == item.ID) {
                amount = (amount-inSpace.count).ClampZero(MaxStackSize);
                inSpace.count += amount;
            } 
            return amount;
        }
        /// <summary>
        /// Only works with non city inventories.
        /// </summary>
        /// <param name="space"></param>
        public virtual void RemoveItemInSpace(int space) {
            Items.Remove("" + space);
            cbInventoryChanged?.Invoke(this);
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
            if (GetAmountFor(remove) < remove.count) {
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
            if(HasEnoughOfItems(toRemove) == false) {
                return false;
            }
            foreach (Item i in toRemove) {
                LowerItemAmount(i, i.count);
            }
            return true;
        }

        /// <summary>
        /// WARNING THIS WILL EMPTY THE COMPLETE
        /// INVENTORY NOTHING WILL BE LEFT
        /// </summary>
        public virtual Item[] GetAllItemsAndRemoveThem() {
            //get all items in a list
            List<Item> temp = new List<Item>(Items.Values);
            //reset the inventory here
            Items = new Dictionary<string, Item>();
            cbInventoryChanged?.Invoke(this);
            return temp.ToArray();
        }

        protected virtual void LowerItemAmount(Item lower, int amount) {
            foreach (var idItemPair in Items.Where(i => i.Value.ID == lower.ID).ToArray()) {
                Item inInv = idItemPair.Value;
                int loweredAmount = amount.ClampZero(inInv.count);
                inInv.count -= loweredAmount;
                amount -= loweredAmount;
                if(amount == 0) {
                    break;
                }
                if (idItemPair.Value.count == 0) {
                    Items.Remove(idItemPair.Key);
                }
            }
            cbInventoryChanged?.Invoke(this);
        }

        protected int MoveAmountFromItemToInv(Item toBeMoved, Item toReceive) {
            int amount = toBeMoved.count.ClampZero(MaxStackSize - toReceive.count);
            IncreaseItemAmount(toReceive, amount);
            toBeMoved.count -= amount;
            return amount;
        }

        protected void IncreaseItemAmount(Item item, int amount) {
            if (amount < 0) {
                Debug.LogError("Increase Amount is " + amount + "! ");
            }
            item.count += amount;
            cbInventoryChanged?.Invoke(this);
        }
        
        public void AddInventory(Inventory inv) {
            foreach (Item item in inv.GetAllItemsAndRemoveThem()) {
                AddItem(item);
            }
        }

        public virtual Item[] GetBuildMaterial() {
            List<Item> itemlist = new List<Item>();
            foreach (Item i in PrototypController.Instance.BuildItems) {
                if (HasAnythingOf(i)) {
                    itemlist.Add(GetAllOfItem(i));
                }
            }
            return itemlist.ToArray();
        }
        
        public void AddItems(IEnumerable<Item> items) {
            foreach (Item item in items) {
                AddItem(item);
            }
        }

        public int FreeSpacesLeft() {
            if(HasLimitedSpace == false) {
                return 0;
            } else {
                return NumberOfSpaces - Items.Count;
            }
        }

        public bool HasEnoughOfItems(IEnumerable<Item> item) {
            return item.All(x=>HasEnoughOfItem(x));
        }

        public bool HasEnoughOfItem(Item item) {
            return GetAmountFor(item) >= item.count;
        }

        public bool HasEnoughOfItems(IEnumerable<Item> items, int times = 1) {
            if (items == null || times <= 0)
                return true;
            items.ToList().ForEach(x => { x.count *= times; });
            return HasEnoughOfItems(items);
        }

        //TODO: make this not relay on load function?
        public void OnChanged(Inventory me) {
            if (HasLimitedSpace == false)
                return;
            amountInInventory = 0;
            foreach (Item i in Items.Values) {
                amountInInventory += i.count;
            }
        }

        public virtual void Load() {
            foreach (var item in Items.ToArray()) {
                if(item.Value.Exists() == false) {
                    Items.Remove(item.Key);
                } 
            }
        }

        public void RegisterOnChangedCallback(Action<Inventory> cb) {
            cbInventoryChanged += cb;
        }

        public void UnregisterOnChangedCallback(Action<Inventory> cb) {
            cbInventoryChanged -= cb;
        }

        public void RegisterOnChangedCallback(Action<Inventory, Item, bool> cb) {
            cbInventoryItemChange += cb;
        }

        public void UnregisterOnChangedCallback(Action<Inventory, Item, bool> cb) {
            cbInventoryItemChange -= cb;
        }
    }
}