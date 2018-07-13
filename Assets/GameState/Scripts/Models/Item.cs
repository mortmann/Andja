using UnityEngine;
using System.Collections;
using System;
using Newtonsoft.Json;


public enum ItemType {Build,Intermediate,Luxury}

public class ItemPrototypeData : LanguageVariables {
	public ItemType Type;
}


[JsonObject(MemberSerialization.OptIn)]
public class Item {
	[JsonPropertyAttribute] public int ID;
	[JsonPropertyAttribute] public int count;

	protected ItemPrototypeData _prototypData;
	public ItemPrototypeData data {
		get { 
			if(_prototypData==null){
				_prototypData = PrototypController.Instance.GetItemPrototypDataForID (ID);
			}
			return _prototypData;
		}
	}
	public ItemType Type {
		get {
			return data.Type;
		}
	}
	public string name {
		get {
			return data.Name;
		}
	}
	public Item(int id, int count = 0) {
        this.ID = id;
        this.count = count;
    }
	public Item(int id, ItemPrototypeData ipd){
		this.ID = id;
		this._prototypData = ipd;
		this.count = 0;
	}
	public Item(Item other){
		this.ID = other.ID;
	}
	public Item(){
	}

	virtual public Item Clone (){
		return new Item(this);
	}
	virtual public Item CloneWithCount (){
        Item i = new Item(this) {
            count = this.count
        };
        return i;
	}

    internal string ToSmallString() {
        return string.Format(name + ":" + count +"t");
    }

    public override string ToString (){
		return string.Format ("[Item] " + ID +":"+name+":"+count);
	}


}
