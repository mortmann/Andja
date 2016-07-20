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
			items = GameObject.FindObjectOfType<BuildController> ().getCopieOfAllItems ();
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

	public int getPlaceInItem(Item item){
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
		return items [getPlaceInItem (item)];
	}

	public void setItemCountNull (Item item){
		getItemInInventory (item).count = 0;
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
	/// moves item amount to the given inventory
	/// </summary>
	/// <returns><c>true</c>, if item was moved, <c>false</c> otherwise.</returns>
	/// <param name="inv">Inv.</param>
	/// <param name="it">It.</param>
	public bool moveItem(Inventory moveToInv, Item it,int amountMove){
		if (items.ContainsKey (getPlaceInItem (it))) {
			Item i = items [getPlaceInItem (it)];
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

	private void lowerItemAmount(Item i,int amount){
		Debug.Log (name + " lower " + i.name + " -=" +amount); 
		if (items.ContainsKey (getPlaceInItem (i))) {
//			Debug.Log ("contains "+ items [getPlaceInItem (i)].count); 
			items [getPlaceInItem (i)].count -= amount;
			Debug.Log ("contains only " + items [getPlaceInItem (i)].count); 
		} else {
			Debug.Log ("not in");
			i.count -= amount;
		}
		if(numberOfSpaces!=-1){
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
//		Debug.Log ("move " + toBeMoved.count + " already in " + toReceive.count);
		//whats the amount to be moved
		int amount = toBeMoved.count;
		//clamp it to the maximum it can be
		amount = Mathf.Clamp (amount, 0, maxStackSize - toReceive.count);
//		toBeMoved.count-=amount;
		increaseItemAmount (toReceive, amount);

		return amount;
	}
	private void increaseItemAmount(Item item,int amount){
		if(amount<=0){
			Debug.LogError ("Increase Amount is " + amount + "! "); 
		}
		Debug.Log (name + " increase " + amount); 
		item.count += amount;
		if(cbInventoryChanged!=null){
			cbInventoryChanged (this);
		}
	}


	public void addIventory(Inventory inv){
		foreach (Item item in inv.items.Values) {
//			Debug.Log ("add Item " + item.name);
			addItem (item);
		}
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

	public Item getItem(Item item){
		return items [getPlaceInItem (item)];
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
