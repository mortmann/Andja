using UnityEngine;
using System.Collections;

public class Need {
	
	public int id;
	public string name;
	public Item item;
	public Structure structure;
	public float[] uses;
	public int startLevel;

	public Need(int id, string name,int level, Item item,Structure structure, float[] uses){
		this.id = id;
		this.name = name;
		this.startLevel = level;
		this.item = item;
		this.uses = uses;
		this.structure = structure;
	}
	public Need(){
		
	}
	public float TryToConsumThisIn(City city,int level,int[] peoples){
		if(item == null){
			//this does not require any item -> it needs a structure
			return 0;
		}
		float neededCounsumAmount = 0;
		for (int i = level; i < peoples.Length; i++) {
			neededCounsumAmount += uses [level] * ((float)peoples[i]);
		}
		neededCounsumAmount = Mathf.RoundToInt (neededCounsumAmount);
		float availableAmount = city.TryToRemoveAmount (item,neededCounsumAmount);
		if(availableAmount < 0){
			Debug.LogError ("TryToConsumThis - AMOUNT gotten is negativ");
			return 0;
		}
		if(availableAmount == 0){
			return 0;
		}
		int usedAmount = (int) Mathf.Clamp (availableAmount, 1, neededCounsumAmount);
		city.removeRessource (item,usedAmount);
		//minimum is 1 because if 0 -> ERROR due dividing through 0
		return usedAmount / neededCounsumAmount;
	}
}
