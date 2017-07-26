using UnityEngine;
using System.Collections;

public class FertilityPrototypeData : LanguageVariables {
	public string name;
	public Climate[] climates;

}


public class Fertility {
	public int ID;

	protected FertilityPrototypeData _prototypData;
	public FertilityPrototypeData data {
		get { if(_prototypData==null){
				_prototypData = PrototypController.Instance.GetFertilityPrototypDataForID (ID);
			}
			return _prototypData;
		}
	}

	public string name {
		get {return data.name;}
	}
	public Climate[] climates{
		get {return data.climates;}
	}

	public Fertility(){
		
	}
	public Fertility(int ID, FertilityPrototypeData fpd){
		this.ID = ID;
		this._prototypData = fpd;
	}
	
}
