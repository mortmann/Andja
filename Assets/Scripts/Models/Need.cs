using UnityEngine;
using System.Collections;

public class Need {
	int id;
	string name;
	Item item;
	Structure structure;
	float[] uses;
	int level;
	bool isInrange;
	public Need(int id, string name,int level, Item item,Structure structure, float[] uses){
		this.id = id;
		this.name = name;
		this.level = level;
		this.item = item;
		this.uses = uses;
		this.structure = structure;
	}
	public float TryToConsumThis(City city,int level,int people){
		if(this.level > level){
			//whoever called this doesnt have the permissions to consum
			return 0;
		}
		if(item == null){
			//this does not require any item -> it needs a structure
			return 0;
		}
		float neededCounsumAmount = uses [level] * ((float)people);
		float gottenAmount = city.TryToRemoveAmount (item,neededCounsumAmount);

		return gottenAmount / neededCounsumAmount;
	}
}
