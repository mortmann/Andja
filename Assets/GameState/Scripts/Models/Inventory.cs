using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Inventory {
	[JsonPropertyAttribute] public int MaxStackSize { get; protected set;}

	[JsonPropertyAttribute] public Dictionary<int, Item> Items {
		get;
		protected set;
	}
	[JsonPropertyAttribute] public int NumberOfSpaces {
		get;
		protected set;
	}
	[JsonPropertyAttribute] public string name;

	protected Action<Inventory> cbInventoryChanged;
	public bool HasLimitedSpace {
		get { return NumberOfSpaces != -1;}
	}

    /// <summary>
    /// leave blanc for unlimited spaces! To limited it give a int > 0 
    /// </summary>
    /// <param name="numberOfSpaces"></param>
	public Inventory(int numberOfSpaces = -1,int maxStackSize = 50, string name = "noname") {
        this.MaxStackSize = maxStackSize;
		this.name = name + "-INV";
        Items = new Dictionary<int, Item>();
		if (numberOfSpaces == -1) {
			//for cities
			Items = BuildController.Instance.GetCopieOfAllItems ();
		} 
        this.NumberOfSpaces = numberOfSpaces;
    }
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public Inventory(){}
    /// <summary>
	/// returns amount  
	/// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
	public virtual int AddItem(Item toAdd) {
		if (toAdd.ID <= -1) {
			Debug.LogError ("ITEM ID is smaller or equal to -1");
			return 0;
		}
		int amount = 0;
		foreach (Item inInv in Items.Values) {
			if(inInv.ID == toAdd.ID){
				if(inInv.count == MaxStackSize){
					continue;
				}
				// move
				amount = MoveAmountFromItemToInv (toAdd,inInv); 
				toAdd.count -= amount;
				if(toAdd.count==0){
					break;
				}
			}
		}
		if(toAdd.count>0){
			Item temp = toAdd.Clone ();
			amount+=MoveAmountFromItemToInv (toAdd,temp);
			for (int i = 0; i < NumberOfSpaces; i++) {
				if(Items.ContainsKey (i)==false){
					Items.Add (i,temp); 
					break;
				}
			}
            cbInventoryChanged?.Invoke(this);
        }
		return amount;
    }


	public virtual int GetPlaceInItems(Item item){
		for (int i = 0; i < NumberOfSpaces;i++) {
			if(Items.ContainsKey (i)&&Items[i].ID == item.ID){
				return i;
			}
		}
		return -1;
	}
	public Item GetItemWithNRinItems(int i) {
		if(Items.ContainsKey(i)==false){
			return null;
		}
		return Items [i];
	}

	public Item GetAllOfItem(Item item){
		return GetItemWithMaxAmount (item,int.MaxValue);
	}
	public Item GetItemWithMaxAmount(Item item, int maxAmount){
		Item output = GetItemInInventory (item);
		if(output==null){
			return null;
		}
		Item temp = output.CloneWithCount();
		temp.count = Mathf.Clamp (temp.count, 0, maxAmount );
		LowerItemAmount (output, temp.count);
		maxAmount -= temp.count;
		if(maxAmount>0 && NumberOfSpaces !=-1){
			temp.count += GetItemWithMaxAmount (item, maxAmount).count;
		}
		return temp;
	}
	protected Item GetItemInInventory(Item item){
		int pos = GetPlaceInItems (item);
		if(pos<0){
			return null;
		}
		return Items [pos];
	}
	/// <summary>
	/// Gets CLONED item WITH count but DOESNT REMOVE it FROM inventory.
	/// </summary>
	/// <returns>The item in inventory clone.</returns>
	/// <param name="item">Item.</param>
	public Item GetItemInInventoryClone(Item item){
		return Items [GetPlaceInItems (item)].CloneWithCount ();
	}

	public virtual void SetItemCountNull (Item item){
		Item i = GetItemInInventory (item);
		if(i==null){
			return;
		}
		i.count = 0;
        cbInventoryChanged?.Invoke(this);
    }

	public bool HasAnythingOf(Item item){
		if(GetItemInInventory (item).count > 0){
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
	public int GetAmountForItem(Item item){
		Item it = GetItemInInventory (item);
		if(it==null){
			return 0;
		}
		return GetItemInInventory (item).count;
	}

	public virtual int GetTotalAmountFor(Item item){
		//now check for everyspace if its the item
		int count=0;
		foreach (Item inInv in Items.Values) {
			if(inInv.ID == item.ID){
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
	public int MoveItem(Inventory moveToInv, Item it,int amountMove){
		if (Items.ContainsKey (GetPlaceInItems (it))) {
			Item i = Items [GetPlaceInItems (it)];
			Debug.Log (i.ToString ()); 
			if(i.count<=0){
				return 0;
			}
			it = it.CloneWithCount ();
			it.count = Mathf.Clamp (it.count, 0, amountMove);
			int amount = moveToInv.AddItem (it);
			Debug.Log ("Itemamount " + amount); 
			LowerItemAmount (i, amount);
			return amount;	
		}
		return 0;
	}
	/// <summary>
	/// removes an item amount from this inventory but not moving it to any other inventory
	/// destroys item amount forever! FOREVER!
	/// </summary>
	/// <returns><c>true</c>, if item amount was removed, <c>false</c> otherwise.</returns>
	/// <param name="it">It.</param>
	public bool RemoveItemAmount(Item it){
		if(it==null){
			return true;
		}
		Item i = GetItemInInventory (it);
		if(i == null){
			return false;
		}
		if (i.count < it.count) {
			return false;
		}
		LowerItemAmount (i, it.count);
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
	public bool RemoveItemsAmount(ICollection<Item> coll){
		bool successful = false;
		foreach(Item i in coll){
			successful = RemoveItemAmount (i);
		}
		return successful;
	}
	/// <summary>
	/// WARNING THIS WILL EMPTY THE COMPLETE
	/// INVENTORY NOTHING WILL BE LEFT
	/// </summary>
	public virtual Item[] GetAllItemsAndRemoveThem(){
		//get all items in a list
		List<Item> temp = new List<Item> (Items.Values);
		//reset the inventory here
		Items = new Dictionary<int, Item>();
        cbInventoryChanged?.Invoke(this);
        return temp.ToArray ();
	}

	protected virtual void LowerItemAmount(Item i,int amount){
		if (Items.ContainsKey (GetPlaceInItems (i))) {
			Items [GetPlaceInItems (i)].count -= amount;
		} else {
			Debug.Log ("not in");
			i.count -= amount;
		}
		if (i.count == 0) {
			for (int d = 0; d < Items.Count; d++) {
				if(Items[d].count==0){
					Items.Remove (d);
				}
			}
		}
        cbInventoryChanged?.Invoke(this);
    }

	protected int MoveAmountFromItemToInv(Item toBeMoved,Item toReceive){
		//whats the amount to be moved
		int amount = toBeMoved.count;
		//clamp it to the maximum it can be
		amount = Mathf.Clamp (amount, 0, MaxStackSize - toReceive.count);
		IncreaseItemAmount (toReceive, amount);
		return amount;
	}
	protected void IncreaseItemAmount(Item item,int amount){
		if(amount<0){
			Debug.LogError ("Increase Amount is " + amount + "! "); 
		}
		item.count += amount;
        cbInventoryChanged?.Invoke(this);
    }


	public void AddIventory(Inventory inv){
		foreach (Item item in inv.Items.Values) {
			AddItem (item);
		}
	}
	public virtual Item[] GetBuildMaterial(){
		//there are 10 ids reserved for buildingmaterial
		//currently there are 7 builditems
		List<Item> itemlist = new List<Item> ();
		foreach (Item i in PrototypController.buildItems) {
			if(ContainsItemWithID(i.ID)){
				itemlist.Add(GetItemWithID (i.ID));
			}
		}
		return itemlist.ToArray ();
	}
	protected virtual Item GetItemWithID(int id){
		Item i = null;
		foreach (Item item in Items.Values) {
			if(item.ID==id){
				if(i==null){
					i = item;
				} else {
					i.count += item.count;
				}
			}
		}
		return i;
	}
	public virtual Item GetItemWithIDClone(int id){
		Item i = null;
		foreach (Item item in Items.Values) {
			if(item.ID==id){
				if(i==null){
					i = item;
				} else {
					i.count += item.count;
				}
			}
		}
		if(i == null){
			return null;
		}
		return i.CloneWithCount ();
	}
	public int GetSpaceFor(Item i){
		int amount= GetAmountForItem (i);
		return MaxStackSize - amount;
	}
	public void AddItems(IEnumerable<Item> items){
		foreach (Item item in items) {
			AddItem (item);
		}
	}
	public virtual bool ContainsItemWithID(int id){
		foreach (Item item in Items.Values) {
			if(item.ID==id){
				return true;
			}
		}
		return false;
	}
	public bool ContainsItemsWithRequiredAmount(Item[] items){
		if(items==null){
			return true;
		}
		foreach(Item i in items){
			if(i.count>GetTotalAmountFor (i)){
				return false;
			}
		}
		return true;
	}
    public void AddToStackSize(int value) {
		if(value<=0){
			Debug.LogError ("Increase Stacksize is " + value + "! "); 
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
        Item inInventory = GetItemInInventory(item);
        if (inInventory == null)
            return false;
        return inInventory.count >= item.count;
    }

    public void SubFromStackSize(int value) {
		if(value<=0){
			Debug.LogError ("Decrease Stacksize is " + value + "! "); 
		}
        this.MaxStackSize -= value;
    }

	public void RegisterOnChangedCallback(Action<Inventory> cb) {
		cbInventoryChanged += cb;
	}

	public void UnregisterOnChangedCallback(Action<Inventory> cb) {
		cbInventoryChanged -= cb;
	}

}
