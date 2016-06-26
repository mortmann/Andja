using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class UnitUI : MonoBehaviour {
	public Canvas content;
	public GameObject itemPrefab;
	public Inventory inv;
	Dictionary<Item, GameObject> itemToGO;
	Unit unit;
	public void Show(Unit unit){
		Debug.Log ("show"); 
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
		foreach (Item i in inv.items.Values) {
			addItemGameObject(i);
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
			EventTrigger trigger = go.GetComponent<EventTrigger> ();
			EventTrigger.Entry entry = new EventTrigger.Entry( );
			entry.eventID = EventTriggerType.PointerClick;
			Item i = item.Clone ();
			entry.callback.AddListener( ( data ) => { OnItemClick( i ); } );
			trigger.triggers.Add( entry );

		} else {
			s.maxValue = inv.maxStackSize;
			s.value = 0;
			t.text = 0 + "t";
		}
		itemToGO.Add (item,go);
		go.transform.SetParent (content.transform);

	}
	void OnItemClick(Item clicked){
		Debug.Log (clicked.name); 
		unit.clickedItem (clicked);
	}
	//TODO: make this so it adds it infront
	public void OnInvChange(Inventory changedInv){
		foreach(Item item in changedInv.items.Values){
			if (item.ID != -1 && itemToGO.ContainsKey (item)) {
				itemToGO [item].GetComponentInChildren<Text> ().text = item.count + "t";
				itemToGO [item].GetComponentInChildren<Slider> ().value = item.count;
			} else {
				if(item.ID == -1){
					continue;
				}
				Item toRemove = null;
				foreach (Item it in itemToGO.Keys) {
					if(it.ID==-1){
						toRemove = it;
					}
				}
				if(toRemove == null){
					continue;
				}
				GameObject.Destroy (itemToGO[toRemove]);
				itemToGO.Remove (toRemove);
				addItemGameObject (item);
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
