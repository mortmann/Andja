using UnityEngine;
using System.Collections;
using Newtonsoft.Json;

public class NeedPrototypeData : LanguageVariables {
	public Item item;
	public NeedsBuilding structure;
	public float[] uses;
	public int startLevel;
	public int popCount;
}

[JsonObject(MemberSerialization.OptIn)]
public class Need {
	protected NeedPrototypeData _prototypData;
	public NeedPrototypeData data {
		get { if(_prototypData==null){
				_prototypData = PrototypController.Instance.GetNeedPrototypDataForID (ID);
			}
			return _prototypData;
		}
	}
	public string name {
		get { return data.Name;}	
	}
	public Item item{
		get { return data.item;}	
	}
	public NeedsBuilding structure{
		get { return data.structure;}	
	}
	public float[] uses{
		get { return data.uses;}	
	}
	public int startLevel{
		get { return data.startLevel;}	
	}

	public int popCount{
		get { return data.popCount;}	
	}

	[JsonPropertyAttribute]
	public int ID;
	[JsonPropertyAttribute]
	public float lastNeededNotConsumed;
	[JsonPropertyAttribute]
	public float percantageAvailability;
	[JsonPropertyAttribute]
	public float notUsedOfTon;

	public Need(int id, NeedPrototypeData npd){
		this.ID = id;
		this._prototypData = npd;
	}
	public Need(){
		
	}
	public Need Clone() {
		return new Need (this);
	}
	protected Need(Need other){
		this.ID = other.ID;
	}

	public void TryToConsumThisIn(City city,int[] peoples){
		if(item == null){
			//this does not require any item -> it needs a structure
			percantageAvailability = 0;
			return;
		}
		float neededConsumAmount = 0;
		// how much do we need to consum?
		for (int i = startLevel; i < peoples.Length; i++) {
			neededConsumAmount += uses [i] * ((float)peoples[i]);
		}

		if(neededConsumAmount==0){
			//we dont need anything to consum so no need to go anyfurther
			percantageAvailability = 0;
			return;
		}
		float availableAmount = city.GetAmountForThis (item,neededConsumAmount);

		notUsedOfTon = Mathf.Max(0,availableAmount - neededConsumAmount);

		//either we need to get 1 ton or as much as we need
		neededConsumAmount = Mathf.CeilToInt (neededConsumAmount);
		//now how much do we have in the city
		//if we have none?
		if(availableAmount == 0){
			//we can just set the lastneeded to current needed
			lastNeededNotConsumed = neededConsumAmount;
			percantageAvailability = 0;
			return;
		}

		// how much to we consum of the avaible?
		int usedAmount = (int) Mathf.Clamp (availableAmount, 0, neededConsumAmount);
		//now remove that amount of items
		city.removeRessource (item,usedAmount);
		//minimum is 1 because if 0 -> ERROR due dividing through 0
		//calculate the percantage of availability
		percantageAvailability = Mathf.RoundToInt (100*(usedAmount / neededConsumAmount))/100;
	}

	public bool IsItemNeed(){
		return item != null;
	}
	public bool IsStructureNeed(){
		return structure != null;
	}
}
