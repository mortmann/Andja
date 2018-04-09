using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class CityInventory : Inventory {

	public CityInventory(string name = "CITY"){
		numberOfSpaces = -1;
		this.name = name;
		items = BuildController.Instance.getCopieOfAllItems ();
		maxStackSize = 50;
	}


	public override int addItem(Item toAdd) {
		if (toAdd.ID <= -1) {
			Debug.LogError ("ITEM ID is smaller or equal to -1");
			return 0;
		}
		Item inInv = GetItemWithID(toAdd.ID);
		//if its already full no need to put it in there
		if(inInv.count == maxStackSize) {
			Debug.Log ("inventory-item is full " + inInv.count); 
			return 0;
		}
		return moveAmountFromItemToInv (toAdd, inInv);
	}
	public override int getPlaceInItems(Item item){
		return item.ID;
	}
	public override int GetTotalAmountFor(Item item){
		return GetAmountForItem (item);
	}
	protected override Item GetItemWithID(int id){
		return items [id];
	}
	public override Item GetItemWithIDClone(int id){
		return items [id].CloneWithCount ();
	}
	public override bool ContainsItemWithID(int id){
		return true;
	}
	public override void setItemCountNull (Item item){
		getItemInInventory (item).count = 0;
        cbInventoryChanged?.Invoke(this);
    }
	public override Item[] GetAllItemsAndRemoveThem(){
		//get all items in a list
		List<Item> temp = new List<Item> (items.Values);
		items = BuildController.Instance.getCopieOfAllItems ();
        cbInventoryChanged?.Invoke(this);
        return temp.ToArray ();
	}

	protected override void lowerItemAmount(Item i,int amount){
		Item invItem = items [getPlaceInItems (i)];
		invItem.count -= Mathf.Max(invItem.count-amount,0);
        cbInventoryChanged?.Invoke(this);
    }

}
