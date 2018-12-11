using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

public class FertilityPrototypeData : LanguageVariables {
	public Climate[] climates;
}

[JsonObject(MemberSerialization.OptIn)]
public class Fertility : IComparable<Fertility>, IEqualityComparer<Fertility> {
	public int ID;

	protected FertilityPrototypeData _prototypData;
	public FertilityPrototypeData Data {
		get {
            if (_prototypData==null) {
				_prototypData = PrototypController.Instance.GetFertilityPrototypDataForID (ID);
			}
			return _prototypData;
		}
	}

	public string Name {
		get {return Data.Name;}
	}
	public Climate[] Climates{
		get {return Data.climates;}
	}
	public Fertility(){
		
	}

	public Fertility(int ID, FertilityPrototypeData fpd) {
		this.ID = ID;
		this._prototypData = fpd;
	}

	#region IComparable implementation
	public int CompareTo (Fertility other) {
		return ID.CompareTo (other.ID);
	}
	#endregion

	#region IEqualityComparer implementation

	public bool Equals (Fertility x, Fertility y) {
		return x.ID == y.ID;
	}
	public int GetHashCode (Fertility obj) {
		return GetHashCode();
	}
	#endregion
	public override bool Equals (object obj){
		Fertility f = obj as Fertility;
		if (f == null)
			return false;
		return f.ID == ID;
	}
	public override int GetHashCode (){
		return base.GetHashCode ();
	}
}
