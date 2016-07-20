using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class UnitUI : MonoBehaviour {
	public Canvas content;
	public GameObject itemPrefab;
	public Inventory inv;
	Dictionary<int, GameObject> itemToGO;
	Unit unit;
	public void Show(Unit unit){
		if (unit == this.unit) {
			return;
		}
		this.unit = unit;
		inv = unit.inventory;
		inv.RegisterOnChangedCallback (OnInvChange);
		itemToGO = new Dictionary<int, GameObject> ();
		if(inv == null){
			return;
		}
		for (int i=0; i<inv.numberOfSpaces; i++) {
			addItemGameObject(i);
		}
	}

	private void addItemGameObject(int i){
		GameObject go = GameObject.Instantiate (itemPrefab);
		Slider s = go.GetComponentInChildren<Slider> ();
		Text t = go.GetComponentInChildren<Text> ();
		go.transform.SetParent (content.transform);
		if(inv.items.ContainsKey(i) == false){
			go.name = "item " + i;
			s.maxValue = inv.maxStackSize;
			s.value = 0;
			t.text = 0 + "t";
			itemToGO.Add (i,go);
			return;
		}
		Item item = inv.items [i];
		go.name = "item " + i;
		if (item.ID != -1) {
			s.maxValue = inv.maxStackSize;
			s.value = item.count;
			t.text = item.count + "t";
			EventTrigger trigger = go.GetComponent<EventTrigger> ();
			EventTrigger.Entry entry = new EventTrigger.Entry( );
			entry.eventID = EventTriggerType.PointerClick;
			entry.callback.AddListener( ( data ) => { OnItemClick( i ); } );
			trigger.triggers.Add( entry );
		} 
		itemToGO.Add (i,go);

	}
	void OnItemClick(int clicked){
		Debug.Log ("clicked " + clicked); 
		unit.clickedItem (inv.items[clicked]);
	}
	public void OnInvChange(Inventory changedInv){
		foreach(int i in itemToGO.Keys){
			GameObject.Destroy (itemToGO[i].gameObject);
		}
		itemToGO = new Dictionary<int, GameObject> ();
		for (int i=0;i<inv.numberOfSpaces;i++) {
			addItemGameObject(i);
		}
		inv = changedInv;
//		foreach(Item item in changedInv.items.Values){
//			if (item.ID != -1 && itemToGO.ContainsKey (item)) {
//				itemToGO [item].GetComponentInChildren<Text> ().text = item.count + "t";
//				itemToGO [item].GetComponentInChildren<Slider> ().value = item.count;
//			} else {
//				if(item.ID == -1){
//					continue;
//				}
//				Item toRemove = null;
//				foreach (Item it in itemToGO.Keys) {
//					if(it.ID==-1){
//						toRemove = it;
//					}
//				}
//				if(toRemove == null){
//					continue;
//				}
//				GameObject.Destroy (itemToGO[toRemove]);
//				itemToGO.Remove (toRemove);
//				addItemGameObject (item);
//			}
//		}
	}
	// Update is called once per frame
//	void Update () {
//		if(inv != null){
//			foreach (int item in itemToGO.Keys) {
//				GameObject go = itemToGO [item];
//				Slider s = go.GetComponentInChildren<Slider> ();
//				s.value = item.count;
//				Text t = go.GetComponentInChildren<Text> ();
//				t.text = item.count + "t";
//			}
//		}
//	}
}
