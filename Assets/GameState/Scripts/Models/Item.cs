using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Item : IXmlSerializable{
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

	//////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 						SAVING & LOADING
	/// 
	//////////////////////////////////////////////////////////////////////////////////////
	public XmlSchema GetSchema() {
		return null;
	}
	public void WriteXml(XmlWriter writer) {
		writer.WriteAttributeString( "ID", ID.ToString () );
		writer.WriteAttributeString( "Count", count.ToString () );
	}
	public void ReadXml(XmlReader reader) {
		count = int.Parse( reader.GetAttribute("Count") );
	}
}
