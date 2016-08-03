using UnityEngine;
using System.Collections;
using UnityEngine.UI;


public class ItemUI : MonoBehaviour {
	public Image image;//TODO load it fromanywhere?
	public Text text;
	public Slider slider;
	public void SetItem(Item i, int maxValue){
		slider.maxValue = maxValue;
		//set item pic andso
	}
	public void ChangeItemCount(Item i){
		ChangeItemCount (i.count);
	}
	public void ChangeItemCount(int amount){
		text.text =amount+"t";
		slider.value =amount;
	}
	public void ChangeItemCount(float amount){
		text.text =amount+"t";
		slider.value =amount;
	}
	public void ChangeMaxValue(int maxValue){
		slider.maxValue = maxValue;
	}
}
