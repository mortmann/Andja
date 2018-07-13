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
		this.name = need.Name;
		string name = need.Name + " | ";
		if (need.IsItemNeed()) {
			name += need.Item.name;
		} else {
			if(need.Structure==null){
				nameText.text = "Missing Structure";
				//					Debug.LogWarning(ns[i].ID + " " + curr.name +" is missing its structure! Either non declared or structure not existing!");
				return;
			}
			name += need.Structure.SmallName;
		}
		nameText.text = need.Name;
	}
	void Update(){
		if(PlayerController.Instance.CurrPlayer.HasUnlockedNeed(need)==false){
			percentageText.text = "LOCKED!";
			return;
		}
		if (need.IsItemNeed()) {
			percentageText.text = need.percantageAvailability * 100 + "%";
			slider.value = need.percantageAvailability * 100;
		} else {
			if(home.IsStructureNeedFullfilled(need)){
				percentageText.text = "In Range";
				slider.value = 100;
			}else {
				percentageText.text = "Not in Range";
				slider.value = 0;
			}
		}
	}

}
