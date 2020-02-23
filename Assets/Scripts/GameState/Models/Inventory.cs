using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Inventory {
    [JsonPropertyAttribute] public int MaxStackSize { get; protected set; }

    [JsonPropertyAttribute]
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
        if (String.IsNullOrEmpty(toAdd.ID)) {
            Debug.LogError("ITEM ID is smaller or equal to -1");
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
                toAdd.count -= amount;
                if (toAdd.count == 0) {
                    break;
                }
            }
        }
        if (toAdd.count > 0) {
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
    internal float GetFilledPercantage() {
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
        if (Items.ContainsKey(""+ i) == false) {
            return null;
        }
        return Items["" +i];
    }

    public Item GetAllOfItem(Item item) {
        return GetItemWithMaxAmount(item, int.MaxValue);
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
    /// IF NumberOfSpaces == -1 then this will return null because there are no multiple items of this
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    protected Item[] GetItemsInInventory(Item item) {
        if (NumberOfSpaces == -1)
            return null;
        List<Item> inInv = new List<Item>();
        foreach (Item i in Items.Values) {
            if (i.ID == item.ID)
                inInv.Add(i);
        }
        return inInv.ToArray();
    }

    protected Item GetFirstItemInInventory(Item item) {
        string pos = GetPlaceInItems(item);
        if (String.IsNullOrEmpty(pos)) {
            return null;
        }
        return Items[pos];
    }
    /// <summary>
    /// Gets CLONED item WITH count but DOESNT REMOVE it FROM inventory.
    /// </summary>
    /// <returns>The item in inventory clone.</returns>
    /// <param name="item">Item.</param>
    public Item GetItemInInventoryClone(Item item) {
        return Items[GetPlaceInItems(item)].CloneWithCount();
    }

    public virtual void SetItemCountNull(Item item) {
        Item i = GetFirstItemInInventory(item);
        if (i == null) {
            return;
        }
        i.count = 0;
        cbInventoryChanged?.Invoke(this);
    }

    public bool HasAnythingOf(Item item) {
        if (GetFirstItemInInventory(item).count > 0) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Warning find the first Item and returns the amount!
    /// WARNING!!!!!
    /// </summary>
    /// <returns>The amount for item.</returns>
    /// <param name="item">Item.</param>
    public int GetAmountForItem(Item item) {
        Item it = GetFirstItemInInventory(item);
        if (it == null) {
            return 0;
        }
        return GetFirstItemInInventory(item).count;
    }

    public virtual int GetTotalAmountFor(Item item) {
        //now check for everyspace if its the item
        int count = 0;
        foreach (Item inInv in Items.Values) {
            if (inInv.ID == item.ID) {
                count += inInv.count;
            }
        }
        return count;
    }


    /// <summary>
    /// moves item amount to the given inventory
    /// </summary>
    /// <returns><c>true</c>, if item was moved, <c>false</c> otherwise.</returns>
    /// <param name="inv">Inv.</param>
    /// <param name="it">It.</param>
    public int MoveItem(Inventory moveToInv, Item it, int amountMove) {
        if (Items.ContainsKey(GetPlaceInItems(it))) {
            Item i = Items[GetPlaceInItems(it)];
            Debug.Log(i.ToString());
            if (i.count <= 0) {
                return 0;
            }
            it = it.CloneWithCount();
            it.count = Mathf.Clamp(it.count, 0, amountMove);
            int amount = moveToInv.AddItem(it);
            Debug.Log("Itemamount " + amount);
            LowerItemAmount(i, amount);
            return amount;
        }
        return 0;
    }
    /// <summary>
    /// should only be called for Inventories associated with units
    /// cause city technically are always full / empty that means it
    /// always has a spot to unload the item
    /// </summary>
    /// <returns></returns>
    internal bool IsFullWithItems() {
        if (this is CityInventory)
            return false;
        return NumberOfSpaces <= Items.Count;
    }
    /// <summary>
    /// should only be called for Inventories associated with units
    /// cause city technically are always full / empty that means it
    /// always has a spot to unload the item
    /// </summary>
    /// <returns></returns>
    internal bool HasSpaceForItem(Item item, bool hasToFitAll = false) {
        //if its full and does not contain item type return false
        if (IsFullWithItems() && ContainsItemWithID(item.ID) == false) {
            return false;
        }
        if (hasToFitAll && IsFullWithItems()) {
            if (RemainingSpaceForItem(item) < item.count) {
                return false;
            }
        }
        return true;
    }

    protected virtual int RemainingSpaceForItem(Item item) {
        int remaining = 0;
        foreach (Item i in Items.Values) {
            if (i.ID == item.ID) {
                remaining += MaxStackSize - i.count;
            }
        }
        return remaining;
    }

    /// <summary>
    /// removes an item amount from this inventory but not moving it to any other inventory
    /// destroys item amount forever! FOREVER!
    /// </summary>
    /// <returns><c>true</c>, if item amount was removed, <c>false</c> otherwise.</returns>
    /// <param name="it">It.</param>
    public bool RemoveItemAmount(Item it) {
        if (it == null) {
            return true;
        }
        Item i = GetFirstItemInInventory(it);
        if (i == null) {
            return false;
        }
        if (i.count < it.count) {
            return false;
        }
        LowerItemAmount(i, it.count);
        return true;
    }
    /// <summary>
    /// CHECK FIRST IF AVAIBLE THEN REMOVE!
    /// Removes the items amounts. It will return false
    /// if there couldnt be removed a single item in the list
    /// but still removes the rest. So be careful!
    /// </summary>
    /// <returns><c>true</c>, if items amount was removed, <c>false</c> otherwise.</returns>
    /// <param name="coll">Coll.</param>
    public bool RemoveItemsAmount(ICollection<Item> coll) {
        bool successful = false;
        foreach (Item i in coll) {
            successful = RemoveItemAmount(i);
        }
        return successful;
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

    protected virtual void LowerItemAmount(Item i, int amount) {
        if (Items.ContainsKey(GetPlaceInItems(i))) {
            Items[GetPlaceInItems(i)].count -= amount;
        }
        else {
            Debug.Log("not in");
            i.count -= amount;
        }
        if (i.count == 0) {
            for (int d = 0; d < Items.Count; d++) {
                if (Items[""+d].count == 0) {
                    Items.Remove(""+d);
                }
            }
        }
        cbInventoryChanged?.Invoke(this);
    }

    protected int MoveAmountFromItemToInv(Item toBeMoved, Item toReceive) {
        //whats the amount to be moved
        int amount = toBeMoved.count;
        //clamp it to the maximum it can be
        amount = Mathf.Clamp(amount, 0, MaxStackSize - toReceive.count);
        IncreaseItemAmount(toReceive, amount);
        return amount;
    }
    protected void IncreaseItemAmount(Item item, int amount) {
        if (amount < 0) {
            Debug.LogError("Increase Amount is " + amount + "! ");
        }
        item.count += amount;
        cbInventoryChanged?.Invoke(this);
    }


    public void AddIventory(Inventory inv) {
        foreach (Item item in inv.Items.Values) {
            AddItem(item);
        }
    }
    public virtual Item[] GetBuildMaterial() {
        List<Item> itemlist = new List<Item>();
        foreach (Item i in PrototypController.BuildItems) {
            if (ContainsItemWithID(i.ID)) {
                itemlist.Add(GetItemWithID(i.ID));
            }
        }
        return itemlist.ToArray();
    }
    protected virtual Item GetItemWithID(string id) {
        Item i = null;
        foreach (Item item in Items.Values) {
            if (item.ID == id) {
                if (i == null) {
                    i = item;
                }
                else {
                    i.count += item.count;
                }
            }
        }
        return i;
    }
    public virtual Item GetItemWithIDClone(string id) {
        Item i = null;
        foreach (Item item in Items.Values) {
            if (item.ID == id) {
                if (i == null) {
                    i = item;
                }
                else {
                    i.count += item.count;
                }
            }
        }
        if (i == null) {
            return null;
        }
        return i.CloneWithCount();
    }
    public int GetSpaceFor(Item i) {
        int amount = GetAmountForItem(i);
        return MaxStackSize - amount;
    }
    public void AddItems(IEnumerable<Item> items) {
        foreach (Item item in items) {
            AddItem(item);
        }
    }
    public virtual bool ContainsItemWithID(string id) {
        foreach (Item item in Items.Values) {
            if (item.ID == id) {
                return true;
            }
        }
        return false;
    }
    public bool ContainsItemsWithRequiredAmount(Item[] items) {
        if (items == null) {
            return true;
        }
        foreach (Item i in items) {
            if (i.count > GetTotalAmountFor(i)) {
                return false;
            }
        }
        return true;
    }
    public void AddToStackSize(int value) {
        if (value <= 0) {
            Debug.LogError("Increase Stacksize is " + value + "! ");
        }
        this.MaxStackSize += value;
    }
    internal bool HasEnoughOfItems(IEnumerable<Item> item) {
        foreach (Item i in item) {
            if (HasEnoughOfItem(i) == false)
                return false;
        }
        return true;
    }
    internal bool HasEnoughOfItem(Item item) {
        Item inInventory = GetFirstItemInInventory(item);
        if (inInventory == null)
            return false;
        return inInventory.count >= item.count;
    }

    public void SubFromStackSize(int value) {
        if (value <= 0) {
            Debug.LogError("Decrease Stacksize is " + value + "! ");
        }
        this.MaxStackSize -= value;
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
    public void RegisterOnChangedCallback(Action<Inventory> cb) {
        cbInventoryChanged += cb;
    }

    public void UnregisterOnChangedCallback(Action<Inventory> cb) {
        cbInventoryChanged -= cb;
    }

}
