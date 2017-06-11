using UnityEngine;
using System.Collections;
using System;
using Newtonsoft.Json;


public enum ItemType {Build,Intermediate,Luxury}

[JsonObject(MemberSerialization.OptIn)]
public class Item {
	[JsonPropertyAttribute] public int ID;
	[JsonPropertyAttribute] public string name;
	[JsonPropertyAttribute] public int count;
	[JsonPropertyAttribute] public ItemType Type;
    public Item(int id, string name,ItemType type, int count = 0) {
        this.ID = id;
        this.name = name;
        this.count = count;
		this.Type = type;
    }
	public Item(Item other){
		this.ID = other.ID;
		this.name = other.name;
		this.Type = other.Type;
	}
	public Item(){
	}

	virtual public Item Clone (){
		return new Item(this);
	}
	virtual public Item CloneWithCount (){
		Item i = new Item(this);
		i.count = this.count;
		return i;
	}

	public override string ToString (){
		return string.Format ("[Item] " + ID +":"+name+":"+count);
	}


}
