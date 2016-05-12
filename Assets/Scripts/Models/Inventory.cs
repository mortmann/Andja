using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory {
	public int maxStackSize { get; protected set;}
    public Dictionary<int, Item> items {
		get;
		protected set;
	}
	public int numberOfSpaces{
		get;
		protected set;
	}

	Action<Inventory> cbInventoryChanged;
	bool needsPlaceholders;
    /// <summary>
    /// leave blanc for unlimited spaces! To limited it give a int > 0 
    /// </summary>
    /// <param name="numberOfSpaces"></param>
	public Inventory(int numberOfSpaces = -1, bool needsPlaceholders = true) {
        maxStackSize = 50;
        items = new Dictionary<int, Item>();
		this.needsPlaceholders = needsPlaceholders;
		if (numberOfSpaces == -1) {
			items = GameObject.FindObjectOfType<BuildController> ().getCopieOfAllItems ();
		} else {
			if (needsPlaceholders) {
				for (int i = 0; i < +numberOfSpaces; i++) {
					items.Add (i-1000, new Item (-1, "empty"));
				}
			}
		}
        this.numberOfSpaces = numberOfSpaces;
    }
    /// <summary>
	/// returns 1 if the toAdd will be empty
	/// returns 0 when it could add some of it
	/// returns -1 when theres no place   
	/// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public int addItem(Item item) {
		if (item.ID == -1) {
			return -1;
		}
        if (items.ContainsKey(item.ID)) {
            return tryToAddCount(items[item.ID],item);
        } else {
            //-1 stands for unlimited spaces for Islands 
			if(numberOfSpaces != -1) {
				if (needsPlaceholders) {
					if (InventorySpaces () == 0) {
						return -1;
					}
					if (InventorySpaces () < 0) {
						Debug.LogError ("There are more Items in inventory than there are places.");
						return -1;
					}
				} else {
					if (items.Count == numberOfSpaces) {
						return -1;
					}
					if (items.Count > numberOfSpaces) {
						Debug.LogError ("There are more Items in inventory than there are places.");
						return -1;
					}
				}
            } 
			RemovePlaceholder ();
			items.Add(item.ID, item);

			if(cbInventoryChanged != null)
				cbInventoryChanged (this);
            return 1;
        }
    }
	public int InventorySpaces(){
		int count = 0;
		foreach (int item in items.Keys) {
			if (item == -1) {
				count++;
			}
		}
		return count;
	}
	public void RemovePlaceholder(){
		int id = int.MinValue;
		foreach (int item in items.Keys) {
			if (items [item].ID == -1) {
				id = item;
				break;
			}
		}
		if(items.ContainsKey (id))
			items.Remove (id);
	}
    /// <summary>
    /// returns 1 if the toAdd will be empty
	/// returns 0 when it could add some of it
	/// returns -1 when theres no place
    /// </summary>
    /// <param name="inInv"></param>
    /// <param name="toAdd"></param>
    /// <returns></returns>
    private int tryToAddCount(Item inInv,Item toAdd) {
        //if there´s no space for it
        if(inInv.count == maxStackSize) {
            return -1;
        }
        //if there´s not enough space
        if(toAdd.count + inInv.count > maxStackSize) {
            //add as much as possible if 
            toAdd.count -= maxStackSize - inInv.count;
            inInv.count = maxStackSize;
			if(cbInventoryChanged != null)
				cbInventoryChanged (this);
            return 0;
        }

        //now there´s enough space
        inInv.count += toAdd.count;
		if(cbInventoryChanged != null)
			cbInventoryChanged (this);
        return 1;
    }
	/// <summary>
	/// moves item amount to the given inventory
	/// </summary>
	/// <returns><c>true</c>, if item was moved, <c>false</c> otherwise.</returns>
	/// <param name="inv">Inv.</param>
	/// <param name="it">It.</param>
	public bool moveItem(Inventory inv, Item it){
		if (items.ContainsKey (it.ID)) {
			Item i = items [it.ID];
			if (inv.addItem (i) == 0) {
				cbInventoryChanged (this);
				return true;	
			}
			if (inv.addItem (i) == 1) {
				if(this.numberOfSpaces != -1){
					items.Remove (it.ID);
				}
				if(cbInventoryChanged != null)
					cbInventoryChanged (this);
				return true;	
			}
		}
		return false;
	}
	/// <summary>
	/// removes an item amount from this inventory but not moving it to any other inventory
	/// destroys item amount forever! FOREVER!
	/// </summary>
	/// <returns><c>true</c>, if item amount was removed, <c>false</c> otherwise.</returns>
	/// <param name="it">It.</param>
	public bool removeItemAmount(Item it){
		if (items.ContainsKey (it.ID)) {
			Item i = items [it.ID];
			if (i.count < it.count) {
				return false;
			}
			if(cbInventoryChanged != null)
				cbInventoryChanged (this);
			i.count -= it.count;
			return true;
		}
		return false;
	}	

	public void addIventory(Inventory inv){
		foreach (Item item in inv.items.Values) {
//			Debug.Log ("add Item " + item.name);
			addItem (item);
		}
	}

    public void addToStackSize(int value) {
        this.maxStackSize += value;
    }
    public void subFromStackSize(int value) {
        this.maxStackSize -= value;
    }

	public void RegisterOnChangedCallback(Action<Inventory> cb) {
		cbInventoryChanged += cb;
	}

	public void UnregisterOnChangedCallback(Action<Inventory> cb) {
		cbInventoryChanged -= cb;
	}
}
