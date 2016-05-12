using UnityEngine;
using System.Collections;
using System;

public class Item {
    public int ID;
    public string name;
    public int count;

    public Item(int id, string name, int count = 0) {
        this.ID = id;
        this.name = name;
        this.count = count;
    }
	public Item(Item other){
		this.ID = other.ID;
		this.name = other.name;
	}


	virtual public Item Clone (){
		return new Item(this);
	}
	virtual public Item CloneWithCount (){
		Item i = new Item(this);
		i.count = this.count;
		return i;
	}

}
