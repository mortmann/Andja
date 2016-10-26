using UnityEngine;
using System.Collections;

public class Need {
	
	public int ID;
	public string name;
	public Item item;
	public NeedsBuilding structure;
	public float[] uses;
	public int startLevel;
	public int popCount;
	private float lastNeededNotConsumed;
	public Need(int id, string name,int level,int count, Item item,NeedsBuilding structure, float[] uses){
		this.ID = id;
		this.name = name;
		this.startLevel = level;
		this.item = item;
		this.uses = uses;
		this.structure = structure;
		this.popCount = count;
	}
	public Need(){
		
	}
	public float TryToConsumThisIn(City city,int[] peoples){
		if(item == null){
			//this does not require any item -> it needs a structure
			return 0;
		}
		float neededConsumAmount = 0;
		for (int i = startLevel; i < peoples.Length; i++) {
			neededConsumAmount += uses [i] * ((float)peoples[i]);
		}
		if(neededConsumAmount==0){
			return 0;
		}
		neededConsumAmount = Mathf.Clamp (Mathf.RoundToInt (neededConsumAmount),1,Mathf.Infinity);

		float availableAmount = city.GetAmountForThis (item,neededConsumAmount);

		if(availableAmount == 0){
			return 0;
		}
		if(neededConsumAmount<1){
			if (lastNeededNotConsumed == 0) {
				lastNeededNotConsumed = neededConsumAmount;
			} else {
				neededConsumAmount += lastNeededNotConsumed;
				if (neededConsumAmount < 1) {
					lastNeededNotConsumed = neededConsumAmount;
				} else {
					lastNeededNotConsumed = 0;
				}
			}
		}
		int usedAmount = (int) Mathf.Clamp (availableAmount, 0, neededConsumAmount);
		city.removeRessource (item,usedAmount);
		//minimum is 1 because if 0 -> ERROR due dividing through 0
		return Mathf.RoundToInt (100*(usedAmount / neededConsumAmount))/100;
	}
}
