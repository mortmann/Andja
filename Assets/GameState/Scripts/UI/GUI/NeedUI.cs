﻿using System.Collections;
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

	public void SetNeed(Need need, HomeBuilding home){
		this.need = need;
		this.home = home;
		this.name = need.Name;
		string name = need.Name + " | ";
		if (need.IsItemNeed()) {
			name += need.Item.name;
		} else {
			if(need.Structures==null){
				nameText.text = "Missing Structure";
				//					Debug.LogWarning(ns[i].ID + " " + curr.name +" is missing its structure! Either non declared or structure not existing!");
				return;
			}
            //TODO: rework needed
			name += need.Structures[0].SmallName;
		}
		nameText.text = need.Name;
	}
	void Update(){
		if(PlayerController.Instance.CurrPlayer.HasUnlockedNeed(need)==false){
			percentageText.text = "LOCKED!";
			return;
		}
		if (need.IsItemNeed()) {
            float percantage = need.GetFullfiment(home.PopulationLevel) * 100;
            percentageText.text = percantage + "%";
			slider.value = percantage;
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
