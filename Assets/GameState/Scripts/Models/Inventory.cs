using System;
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
	public bool HasUnlimitedSpace {
		get { return numberOfSpaces != -1;}
	}
	public string name;

	Action<Inventory> cbInventoryChanged;
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
	public Item getItemInInventory(Item item){
		return items [getPlaceInItems (item)];
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
	public bool moveItem(Inventory moveToInv, Item it,int amountMove){
		if (items.ContainsKey (getPlaceInItems (it))) {
			Item i = items [getPlaceInItems (it)];
			Debug.Log (i.ToString ()); 
			if(i.count<=0){
				return false;
			}
			it = it.CloneWithCount ();
			it.count = Mathf.Clamp (it.count, 0, amountMove);
			int amount = moveToInv.addItem (it);
			Debug.Log ("Itemamount " + amount); 
			lowerItemAmount (i, amount);
			return true;	
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
		for (int i = 0; i < 10; i++) {
			if(ContainsItemWithID(i)){
				itemlist.Add(GetItemWithID (i));
			}
		}
		return itemlist.ToArray ();
	}
	private Item GetItemWithID(int id){
		if(numberOfSpaces==-1){
			if(items.ContainsKey (id))
				return items[id];
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
