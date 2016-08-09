using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class UnitUI : MonoBehaviour {
	public Canvas content;
	public GameObject itemPrefab;
	public GameObject settleButton;
	public Text healthText;
	public Inventory inv;
	Dictionary<int, ItemUI> itemToGO;
	Unit unit;
	public void Show(Unit unit){
		if (unit == this.unit) {
			return;
		}
		this.unit = unit;
		inv = unit.inventory;
		if(unit is Ship){
			settleButton.SetActive (true);
		} else {
			settleButton.SetActive (false);
		}
		if(inv==null){
			return;
		}

		inv.RegisterOnChangedCallback (OnInvChange);
		itemToGO = new Dictionary<int, ItemUI> ();
		if(inv == null){
			return;
		}
		for (int i=0; i<inv.numberOfSpaces; i++) {
			addItemGameObject(i);
		}
	}

	private void addItemGameObject(int i){
		GameObject go = GameObject.Instantiate (itemPrefab);
		go.transform.SetParent (content.transform);
		ItemUI iui = go.GetComponent<ItemUI> ();
		if(inv.items.ContainsKey(i) == false){
			go.name = "item " + i;
			iui.SetItem (null, inv.maxStackSize);
			itemToGO.Add (i,iui);
			return;
		}
		Item item = inv.items [i];
		go.name = "item " + i;
		if (item.ID != -1) {
			iui.SetItem (item, inv.maxStackSize);
			iui.AddListener (( data ) => { OnItemClick( i ); });
//			EventTrigger trigger = go.GetComponent<EventTrigger> ();
//			EventTrigger.Entry entry = new EventTrigger.Entry( );
//			entry.eventID = EventTriggerType.PointerClick;
//			entry.callback.AddListener(  );
//			trigger.triggers.Add( entry );
		} 
		itemToGO.Add (i,iui);

	}
	void OnItemClick(int clicked){
		Debug.Log ("clicked " + clicked); 
		unit.clickedItem (inv.items[clicked]);
	}
	public void OnInvChange(Inventory changedInv){
		foreach(int i in itemToGO.Keys){
			GameObject.Destroy (itemToGO[i].gameObject);
		}
		itemToGO = new Dictionary<int, ItemUI> ();
		for (int i=0;i<inv.numberOfSpaces;i++) {
			addItemGameObject(i);
		}
		inv = changedInv;

	}
	public void Update(){
		healthText.text = Mathf.CeilToInt (unit.currHealth) + "/" + unit.maxHP+"HP";
	}
}
