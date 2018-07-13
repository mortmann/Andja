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
	public NeedPrototypeData Data {
		get { if(_prototypData==null){
				_prototypData = PrototypController.Instance.GetNeedPrototypDataForID (ID);
			}
			return _prototypData;
		}
	}
	public string Name {
		get { return Data.Name;}	
	}
	public Item Item{
		get { return Data.item;}	
	}
	public NeedsBuilding Structure{
		get { return Data.structure;}	
	}
	public float[] Uses{
		get { return Data.uses;}	
	}
	public int StartLevel{
		get { return Data.startLevel;}	
	}

	public int PopCount{
		get { return Data.popCount;}	
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
		if(Item == null){
			//this does not require any item -> it needs a structure
			percantageAvailability = 0;
			return;
		}

		float neededConsumAmount = 0;
		// how much do we need to consum?
		for (int i = StartLevel; i < peoples.Length; i++) {
			neededConsumAmount += Uses [i] * ((float)peoples[i]);
		}
		if(neededConsumAmount==0){
			//we dont need anything to consum so no need to go anyfurther
			percantageAvailability = 0;
			return;
		}

		//IF it has still enough no need to calculate more
		if(notUsedOfTon>=neededConsumAmount){
			notUsedOfTon -= neededConsumAmount;
			percantageAvailability = 1;
			return;
		}
		//so just remove from needed what we have
		neededConsumAmount -= notUsedOfTon;
		//now we have a amount that still needs to be fullfilled by the cities inventory

		float availableAmount = city.GetAmountForThis (Item,neededConsumAmount);
		notUsedOfTon = Mathf.CeilToInt (neededConsumAmount) - neededConsumAmount;
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
		city.RemoveRessource (Item,usedAmount);

		//minimum is 1 because if 0 -> ERROR due dividing through 0
		//calculate the percantage of availability
		percantageAvailability = Mathf.RoundToInt (100*(usedAmount / neededConsumAmount))/100;
	}

	public bool IsItemNeed(){
		return Item != null;
	}
	public bool IsStructureNeed(){
		return Structure != null;
	}
}
