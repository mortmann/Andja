using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class NeedUI : MonoBehaviour {
	public Slider slider;
	public Text nameText;
	public Text percentageText;
	public Image image;

	protected Need need;
	protected HomeBuilding home;

	public void setNeed(Need need, HomeBuilding home){
		this.need = need;
		this.home = home;
		this.name = need.name;
		string name = need.name + " | ";
		if (need.IsItemNeed()) {
			name += need.item.name;
		} else {
			if(need.structure==null){
				nameText.text = "Missing Structure";
				//					Debug.LogWarning(ns[i].ID + " " + curr.name +" is missing its structure! Either non declared or structure not existing!");
				return;
			}
			name += need.structure.SmallName;
		}
		nameText.text = need.name;
	}
	void Update(){
		if(PlayerController.Instance.currPlayer.hasUnlockedNeed(need)==false){
			percentageText.text = "LOCKED!";
			return;
		}
		if (need.IsItemNeed()) {
			percentageText.text = need.percantageAvailability * 100 + "%";
			slider.value = need.percantageAvailability * 100;
		} else {
			if(home.isStructureNeedFullfilled(need)){
				percentageText.text = "In Range";
				slider.value = 100;
			}else {
				percentageText.text = "Not in Range";
				slider.value = 0;
			}
		}
	}

}
