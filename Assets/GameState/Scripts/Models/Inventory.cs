using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Inventory {
	[JsonPropertyAttribute] public int maxStackSize { get; protected set;}

	[JsonPropertyAttribute] public Dictionary<int, Item> items {
		get;
		protected set;
	}
	[JsonPropertyAttribute] public int numberOfSpaces {
		get;
		protected set;
	}
	[JsonPropertyAttribute] public string name;

	Action<Inventory> cbInventoryChanged;
	public bool HasUnlimitedSpace {
		get { return numberOfSpaces != -1;}
	}

    /// <summary>
    /// leave blanc for unlimited spaces! To limited it give a int > 0 
    /// </summary>
    /// <param name="numberOfSpaces"></param>
	public Inventory(int numberOfSpaces = -1,string name = "noname") {
        maxStackSize = 50;
		this.name = name + "-INV";
        items = new Dictionary<int, Item>();
		if (numberOfSpaces == -1) {
			//for cities
			items = BuildController.Instance.getCopieOfAllItems ();
		} 
        this.numberOfSpaces = numberOfSpaces;
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
    public int addItem(Item item) {
		if (item.ID <= -1) {
			Debug.LogError ("ITEM ID is smaller or equal to -1");
			return 0;
		}
		if(numberOfSpaces == -1){
			return cityAddItem (item);
		} else {
			return unitAddItem (item);
		}

    }

	private int cityAddItem(Item toAdd){
		//in a city there is always a stack to be added to
		Item inInv = items [toAdd.ID];
		//if its already full no need to put it in there
		if(inInv.count == maxStackSize) {
			Debug.Log ("inventory-item is full"); 
			return 0;
		}
		return moveAmountFromItemToInv (toAdd, inInv);
	}
	private int unitAddItem(Item toAdd){
		int amount = 0;
		foreach (Item inInv in items.Values) {
			if(inInv.ID == toAdd.ID){
				if(inInv.count == maxStackSize){
					continue;
				}
				// move
				amount = moveAmountFromItemToInv (toAdd,inInv); 
				toAdd.count -= amount;
				if(toAdd.count==0){
					break;
				}
			}
		}
		if(toAdd.count>0){
			Item temp = toAdd.Clone ();
			amount+=moveAmountFromItemToInv (toAdd,temp);
			for (int i = 0; i < numberOfSpaces; i++) {
				if(items.ContainsKey (i)==false){
					items.Add (i,temp); 
					break;
				}
			}
			if(cbInventoryChanged != null)
				cbInventoryChanged (this);
		}
		return amount;
	}

	public int getPlaceInItems(Item item){
		if(numberOfSpaces == -1){
			if(items.ContainsKey (item.ID)==false){
				Debug.LogError("CITY DOESNT HAVE THE ITEM ?!? " + item.ToString ()); 
				return 1;
			}
			return item.ID;
		} else {
			for (int i = 0; i < numberOfSpaces;i++) {
				if(items.ContainsKey (i)&&items[i] == item){
					return i;
				}
			}
			return -1;
		}
	}
	public Item GetItemWithNRinItems(int i) {
		if(items.ContainsKey(i)==false){
			return null;
		}
		return items [i];
	}

	public Item getAllOfItem(Item item){
		return getItemWithMaxAmount (item,int.MaxValue);
	}
	public Item getItemWithMaxAmount(Item item, int maxAmount){
		Item output = getItemInInventory (item);
		Item temp = output.CloneWithCount();
		temp.count = Mathf.Clamp (temp.count, 0, maxAmount );
		lowerItemAmount (output, temp.count);
		maxAmount -= temp.count;
		if(maxAmount>0 && numberOfSpaces !=-1){
			temp.count += getItemWithMaxAmount (item, maxAmount).count;
		}
		return temp;
	}
	private Item getItemInInventory(Item item){
		return items [getPlaceInItems (item)];
	}
	/// <summary>
	/// Gets CLONED item WITH count but DOESNT REMOVE it FROM inventory.
	/// </summary>
	/// <returns>The item in inventory clone.</returns>
	/// <param name="item">Item.</param>
	public Item getItemInInventoryClone(Item item){
		return items [getPlaceInItems (item)].CloneWithCount ();
	}

	public void setItemCountNull (Item item){
		if(HasUnlimitedSpace == false){
			items.Remove ( getPlaceInItems (item) );
		} else {
			getItemInInventory (item).count = 0;
		}
		if(cbInventoryChanged!=null){
			cbInventoryChanged (this); 
		}
	}

	public bool hasAnythingOf(Item item){
		if(getItemInInventory (item).count > 0){
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
		return getItemInInventory (item).count;
	}

	public int GetTotalAmountFor(Item item){
		if(numberOfSpaces==-1){
			//theres only one stack
			return GetAmountForItem (item);
		} 
		//now check for everyspace if its the item
		int count=0;
		foreach (Item inInv in items.Values) {
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
	public int moveItem(Inventory moveToInv, Item it,int amountMove){
		if (items.ContainsKey (getPlaceInItems (it))) {
			Item i = items [getPlaceInItems (it)];
			Debug.Log (i.ToString ()); 
			if(i.count<=0){
				return 0;
			}
			it = it.CloneWithCount ();
			it.count = Mathf.Clamp (it.count, 0, amountMove);
			int amount = moveToInv.addItem (it);
			Debug.Log ("Itemamount " + amount); 
			lowerItemAmount (i, amount);
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
	public bool removeItemAmount(Item it){
		if (items.ContainsKey (it.ID)) {
			Item i = getItemInInventory (it);
			if (i.count < it.count) {
				return false;
			}
			lowerItemAmount (i, it.count);
			return true;
		}
		return false;
	}	
	/// <summary>
	/// WARNING THIS WILL EMPTY THE COMPLETE
	/// INVENTORY NOTHING WILL BE LEFT
	/// </summary>
	public Item[] GetAllItemsAndRemoveThem(){
		//get all items in a list
		List<Item> temp = new List<Item> (items.Values);
		//reset the inventory here
		if (HasUnlimitedSpace == false) {
			items = new Dictionary<int, Item>();
		} else {
			items = BuildController.Instance.getCopieOfAllItems ();
		}
		if(cbInventoryChanged!=null){
			cbInventoryChanged (this);
		}
		return temp.ToArray ();
	}

	private void lowerItemAmount(Item i,int amount){
		if (items.ContainsKey (getPlaceInItems (i))) {
			items [getPlaceInItems (i)].count -= amount;
		} else {
			Debug.Log ("not in");
			i.count -= amount;
		}
		if(HasUnlimitedSpace == false){
			if (i.count == 0) {
				for (int d = 0; d < items.Count; d++) {
					if(items[d].count==0){
						items.Remove (d);
					}
				}
			}
		}
		if(cbInventoryChanged!=null){
			cbInventoryChanged (this);
		}
	}

	private int moveAmountFromItemToInv(Item toBeMoved,Item toReceive){
		//whats the amount to be moved
		int amount = toBeMoved.count;
		//clamp it to the maximum it can be
		amount = Mathf.Clamp (amount, 0, maxStackSize - toReceive.count);
		increaseItemAmount (toReceive, amount);
		return amount;
	}
	private void increaseItemAmount(Item item,int amount){
		if(amount<=0){
			Debug.LogError ("Increase Amount is " + amount + "! "); 
		}
		item.count += amount;
		if(cbInventoryChanged!=null){
			cbInventoryChanged (this);
		}
	}


	public void addIventory(Inventory inv){
		foreach (Item item in inv.items.Values) {
			addItem (item);
		}
	}
	public Item[] getBuildMaterial(){
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
	private Item GetItemWithID(int id){
		if(numberOfSpaces==-1){
			if (items.ContainsKey (id)) {
				Debug.Log (items[id].ID + " " + id); 
				return items [id];
			}
			return null;
		} else {
			Item i = null;
			foreach (Item item in items.Values) {
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
	}
	public Item GetItemWithIDClone(int id){
		if(numberOfSpaces==-1){
			if (items.ContainsKey (id)) {
				Debug.Log ("GetItemWithID "+ items[id].ToString ()); 
				return items [id].CloneWithCount ();
			}
			return null;
		} else {
			Item i = null;
			foreach (Item item in items.Values) {
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
	}

	public void AddItems(IEnumerable<Item> items){
		foreach (Item item in items) {
			addItem (item);
		}
	}
	public bool ContainsItemWithID(int id){
		if(numberOfSpaces==-1){
			return true;
		} else {
			foreach (Item item in items.Values) {
				if(item.ID==id){
					return true;
				}
			}
		}
		return false;
	}
	public bool ContainsItemsWithRequiredAmount(Item[] items){
		if(items==null){
			return true;
		}
		foreach(Item i in items){
			if(i.count<GetTotalAmountFor (i)){
				return false;
			}
		}
		return true;
	}
    public void addToStackSize(int value) {
		if(value<=0){
			Debug.LogError ("Increase Stacksize is " + value + "! "); 
		}
        this.maxStackSize += value;
    }
    public void subFromStackSize(int value) {
		if(value<=0){
			Debug.LogError ("Decrease Stacksize is " + value + "! "); 
		}
        this.maxStackSize -= value;
    }

	public void RegisterOnChangedCallback(Action<Inventory> cb) {
		cbInventoryChanged += cb;
	}

	public void UnregisterOnChangedCallback(Action<Inventory> cb) {
		cbInventoryChanged -= cb;
	}

}
