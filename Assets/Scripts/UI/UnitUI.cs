using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class UnitUI : MonoBehaviour {
	public Canvas content;
	public GameObject itemPrefab;
	public Inventory inv;
	Dictionary<Item, GameObject> itemToGO;
	Unit unit;
	public void Show(Unit unit){
		if (unit == this.unit) {
			return;
		}
		this.unit = unit;
		inv = unit.inventory;
		inv.RegisterOnChangedCallback (OnInvChange);
		itemToGO = new Dictionary<Item, GameObject> ();
		if(inv == null){
			return;
		}
		foreach (Item item in inv.items.Values) {
			addItemGameObject(item);
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
			s.maxValue = inv.maxStackSize;
			s.value = item.count;
			t.text = item.count + "t";
			itemToGO.Add (item,go);
		} else {
			s.maxValue = inv.maxStackSize;
			s.value = 0;
			t.text = 0 + "t";
		}
		go.transform.SetParent (content.transform);

	}

	public void OnInvChange(Inventory changedInv){
		foreach(Item i in changedInv.items.Values){
			if(inv.items.ContainsValue (i)){
				if(inv.items[i.ID].count != i.count){
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
		if(inv != null){
			foreach (Item item in itemToGO.Keys) {
				GameObject go = itemToGO [item];
				Slider s = go.GetComponentInChildren<Slider> ();
				s.value = item.count;
				Text t = go.GetComponentInChildren<Text> ();
				t.text = item.count + "t";
			}
		}
	}
}
