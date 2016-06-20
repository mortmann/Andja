﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Inventory : IXmlSerializable{
	public int maxStackSize { get; protected set;}
    public Dictionary<int, Item> items {
		get;
		protected set;
	}
	public int numberOfSpaces {
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
			//for cities
			items = GameObject.FindObjectOfType<BuildController> ().getCopieOfAllItems ();
		} else {
			//for units
			if (this.needsPlaceholders) {
				for (int i = 0; i < +numberOfSpaces; i++) {
					items.Add (i, new Item (-1, "empty"));
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

		if(numberOfSpaces == -1){
			return cityAddItem (item);
		} else {
			return unitAddItem (item);
		}

    }

	private int cityAddItem(Item toAdd){
		//in a city there is always a stack to be added to
		Item inInv = items [toAdd.ID];
		if(inInv.count == maxStackSize) {
			return 0;
		}
		int amount = 0;
		//if there´s not enough space
		if(toAdd.count + inInv.count > maxStackSize) {
			//add as much as possible if 
			amount = maxStackSize - inInv.count;
			lowerItemAmount (toAdd, amount);
			inInv.count = maxStackSize;
			if(cbInventoryChanged != null)
				cbInventoryChanged (this);
			return amount;
		}

		//now there´s enough space
		amount = toAdd.count;
		lowerItemAmount (toAdd, amount);
		inInv.count += amount;
		if(cbInventoryChanged != null)
			cbInventoryChanged (this);
		return amount;
	}
	private int unitAddItem(Item toAdd){
		int amount = 0;
		foreach (Item inInv in items.Values) {
			if(inInv.ID == toAdd.ID){
				if(inInv.count == maxStackSize){
					continue;
				}
				int temp = Mathf.Clamp (toAdd.count, 0, maxStackSize - inInv.count);
				amount += temp;
				inInv.count += temp;
				lowerItemAmount (toAdd, temp);
				if(toAdd.count==0){
					break;
				}
			}
		}
		if(toAdd.count>0){
			if(InventorySpaces ()>0){
				int id = RemovePlaceholder ();
				Item temp = toAdd.Clone ();
				temp.count = Mathf.Clamp (toAdd.count,0,maxStackSize);
				lowerItemAmount (toAdd, temp.count);
				amount += temp.count;
				items [id] = temp;
			}
		}
		return amount;
	}

	public int getPlaceInItem(Item item){
		if(numberOfSpaces == -1){
			return item.ID;
		} else {
			foreach (int inInv in items.Keys) {
				if(items[inInv].ID == item.ID){
					return inInv;
				}
			}
			return -1;
		}
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
		if(cbInventoryChanged != null)
			cbInventoryChanged (this);
		return temp;
	}
	private Item getItemInInventory(Item item){
		return items [getPlaceInItem (item)];
	}

	public void setItemCountNull (Item item){
		getItemInInventory (item).count = 0;
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
	public int RemovePlaceholder(){
		int id = int.MinValue;
		foreach (int item in items.Keys) {
			if (items [item].ID == -1) {
				id = item;
				break;
			}
		}
		if(items.ContainsKey (id))
			items.Remove (id);
		return id;
	}
	public bool hasAnythingOf(Item item){
		if(getItemInInventory (item).count > 0){
			return true;
		}
		return false;
	}


	public int GetAmountForItem(Item item){
		return getItemInInventory (item).count;
	}
    /// <summary>
    /// returns amount added
    /// </summary>
    /// <param name="inInv"></param>
    /// <param name="toAdd"></param>
    /// <returns></returns>
//    private int tryToAddCount(Item inInv,Item toAdd) {
//        //if there´s no space for it
//        if(inInv.count == maxStackSize) {
//            return 0;
//        }
//        //if there´s not enough space
//        if(toAdd.count + inInv.count > maxStackSize) {
//            //add as much as possible if 
//            toAdd.count -= maxStackSize - inInv.count;
//            inInv.count = maxStackSize;
//			if(cbInventoryChanged != null)
//				cbInventoryChanged (this);
//            return 0;
//        }
//
//        //now there´s enough space
//        inInv.count += toAdd.count;
//		if(cbInventoryChanged != null)
//			cbInventoryChanged (this);
//        return 1;
//    }
	/// <summary>
	/// moves item amount to the given inventory
	/// </summary>
	/// <returns><c>true</c>, if item was moved, <c>false</c> otherwise.</returns>
	/// <param name="inv">Inv.</param>
	/// <param name="it">It.</param>
	public bool moveItem(Inventory moveToInv, Item it){
		if (items.ContainsKey (it.ID)) {
			Item i = items [it.ID];
			moveToInv.addItem (i);
			if (it.count > 0) {
				cbInventoryChanged (this);
				return true;	
			}
			if (it.count==0) {
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
			Item i = getItemInInventory (it);
			if (i.count < it.count) {
				return false;
			}
			if(cbInventoryChanged != null)
				cbInventoryChanged (this);
			lowerItemAmount (i, it.count);
			return true;
		}
		return false;
	}	

	private void lowerItemAmount(Item i,int amount){
		i.count -= amount;
		if(numberOfSpaces!=-1){
			if (i.count == 0) {
				int id = getPlaceInItem (i);
				items.Remove (id);
				if(needsPlaceholders)
					items.Add (id, new Item (-1, "empty"));
			}
		}
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
	//////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 						SAVING & LOADING
	/// 
	//////////////////////////////////////////////////////////////////////////////////////
	public XmlSchema GetSchema() {
		return null;
	}

	public void WriteXml(XmlWriter writer) {
		writer.WriteAttributeString( "MaxStackSize",maxStackSize.ToString ());
		writer.WriteAttributeString( "NumberOfSpaces",numberOfSpaces.ToString ());
		writer.WriteStartElement("Items");
		foreach (int c in items.Keys) {
			writer.WriteStartElement("Item");
			writer.WriteAttributeString( "Number", c.ToString () );
			items [c].WriteXml (writer);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}
	public void ReadXml(XmlReader reader) {
		maxStackSize = int.Parse( reader.GetAttribute("MaxStackSize") );
		numberOfSpaces = int.Parse( reader.GetAttribute("NumberOfSpaces") );
		BuildController bs = BuildController.Instance;
		if(reader.ReadToDescendant("Item") ) {
			do {
				int number = int.Parse( reader.GetAttribute("Number") );
				int id = int.Parse( reader.GetAttribute("ID") );
				Item i = bs.allItems[id].Clone();
				i.ReadXml (reader);
				items[number] = i;
			} while( reader.ReadToNextSibling("Item") );
		}
	}
}
