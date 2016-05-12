using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class CityInventoryUI : MonoBehaviour {

	public GameObject contentCanvas;
	public GameObject itemPrefab;
	Inventory inventory;
	Dictionary<Item, GameObject> itemToGO;


	public void ShowInventory(City city){
		inventory = city.myInv;
		inventory.RegisterOnChangedCallback (OnInventoryChange);
		itemToGO = new Dictionary<Item, GameObject> ();
		foreach (Item item in inventory.items.Values) {
			GameObject go_i = GameObject.Instantiate (itemPrefab);
			itemToGO.Add (item,go_i);
			Slider s = go_i.GetComponentInChildren<Slider> ();
			s.maxValue = inventory.maxStackSize;
			s.value = item.count;
			AdjustSliderColor (s);
			// set image here
			Text tons = go_i.GetComponentInChildren<Text> ();
			tons.text = item.count +"t";
			go_i.transform.SetParent (contentCanvas.transform);
		}
	}

	void AdjustSliderColor(Slider s){
		if (s.value / s.maxValue < 0.2f) {
			s.GetComponentInChildren<Image> ().color = Color.red;
		} else {
			s.GetComponentInChildren<Image> ().color = Color.green;
		}
	}
	private void addItemGameObject(Item item){
		if(item == null){
			return;
		}
		GameObject go = GameObject.Instantiate (itemPrefab);
		Slider s = go.GetComponentInChildren<Slider> ();
		Text t = go.GetComponentInChildren<Text> ();
		if (item.ID != -1) {
			s.maxValue = inventory.maxStackSize;
			s.value = item.count;
			t.text = item.count + "t";
			itemToGO.Add (item,go);
		} else {
			s.maxValue = inventory.maxStackSize;
			s.value = 0;
			t.text = 0 + "t";
		}
		AdjustSliderColor (s);
		go.transform.SetParent (contentCanvas.transform);

	}
	public void OnInventoryChange(Inventory changedInv){
		foreach(Item i in changedInv.items.Values){
			if(inventory.items.ContainsValue (i)){
				if(inventory.items[i.ID].count != i.count){
					itemToGO[i].GetComponentInChildren<Text> ().text = i.count + "t";
					itemToGO [i].GetComponentInChildren<Slider> ().value = i.count;
				}
			} else {
				foreach(Item ig in itemToGO.Keys){
					if(ig.ID == -1){
						itemToGO.Remove (ig);
						addItemGameObject(i);
						break;
					}
				}
			}
		}
	}
	// Update is called once per frame
	void Update () {
		if(inventory != null){
			foreach (Item item in itemToGO.Keys) {
				GameObject go = itemToGO [item];
				Slider s = go.GetComponentInChildren<Slider> ();
				s.value = item.count;
				AdjustSliderColor (s);
				Text t = go.GetComponentInChildren<Text> ();
				t.text = item.count + "t";
			}
		}
	}
}
