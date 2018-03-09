using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class ProduktionUI : MonoBehaviour {

	public Transform inputContent;
	public Transform outputContent;
	public GameObject progressContent;
	public GameObject itemPrefab;
	public GameObject itemORSeperatorPrefab;

	Dictionary<Item, ItemUI> itemToGO;

	OutputStructure currentStructure;

	Slider progress;
	Text efficiency;
	Item currORItem;

	public void Show(OutputStructure ustr){
		if (this.currentStructure == ustr) {
			return;
		}
		this.currentStructure = ustr;
		efficiency = progressContent.GetComponentInChildren<Text> ();
		progress = progressContent.GetComponentInChildren<Slider> ();
		progress.maxValue = currentStructure.produceTime;
		progress.value = 0;
		if(itemToGO!=null){
			foreach(ItemUI go in itemToGO.Values){
				Destroy (go);
			}
		}
		itemToGO = new Dictionary<Item, ItemUI> ();
		if (ustr.output != null) {
			for (int i = 0; i < ustr.output.Length; i++) {
				ItemUI go = GameObject.Instantiate(itemPrefab).GetComponent<ItemUI>();
				go.SetItem (ustr.output [i], ustr.maxOutputStorage );
				go.transform.SetParent (outputContent);
				itemToGO.Add (ustr.output [i], go);
			}
		}

		if (ustr is ProductionBuilding) {
			ProductionBuilding pstr = (ProductionBuilding)ustr;
			if (pstr.MyIntake == null) {
				return;
			}
			if(pstr.myInputTyp == InputTyp.AND){
				for (int i = 0; i < pstr.MyIntake.Length; i++) {
					ItemUI go = GameObject.Instantiate (itemPrefab).GetComponent<ItemUI>();
					go.SetItem (pstr.MyIntake [i], pstr.GetMaxIntakeForIntakeIndex(i) );
					go.transform.SetParent (inputContent);
					itemToGO.Add (pstr.MyIntake [i], go);
				}
			} 
			else if(pstr.myInputTyp == InputTyp.OR){
				for (int i = 0; i < pstr.ProductionData.intake.Length; i++) {
					ItemUI go = GameObject.Instantiate (itemPrefab).GetComponent<ItemUI>();
					if(i > 0){
						GameObject or = GameObject.Instantiate (itemORSeperatorPrefab);
						or.transform.SetParent (inputContent);
					}
					if(i == pstr.orItemIndex){
						go.SetItem (pstr.MyIntake [0],pstr.GetMaxIntakeForIntakeIndex(pstr.orItemIndex));
						currORItem = pstr.MyIntake [0];
						itemToGO.Add (pstr.MyIntake [0], go);
					} else {
						go.SetItem (pstr.ProductionData.intake [i],pstr.GetMaxIntakeForIntakeIndex(i));
						int temp = i;
						go.AddClickListener (( data ) => { OnItemClick( pstr.ProductionData.intake [temp] ); } );
						go.setInactive (true);
						itemToGO.Add (pstr.ProductionData.intake [i], go);
					}
					go.transform.SetParent (inputContent);
				}
			} 
		}
		
	}

	public void OnItemClick(Item item){
		//first get remove the current orItem and add the version from intake
		itemToGO [currORItem].setInactive (true);
		ItemUI go = itemToGO [currORItem];
		ProductionBuilding pstr = (ProductionBuilding)currentStructure;
		for (int i = 0; i < pstr.ProductionData.intake.Length; i++) {
			if(pstr.ProductionData.intake[i].ID == currORItem.ID){
				switchItemKey (currORItem, pstr.ProductionData.intake [i]);
				go.AddClickListener (( data ) => { OnItemClick( pstr.ProductionData.intake [i] ); } );
				break;
			}
		}
		itemToGO [item].setInactive (false);
		//now change the input to the selected
		//also do change the associated item
		if (currentStructure is ProductionBuilding) {
			((ProductionBuilding)currentStructure).ChangeInput (item);
		}
		go = itemToGO [item];
		go.ClearAllTriggers ();
		switchItemKey (item, ((ProductionBuilding)currentStructure).MyIntake [0]);
		currORItem = ((ProductionBuilding)currentStructure).MyIntake [0];
	}
	private void switchItemKey(Item oldKey, Item newKey){
		ItemUI go = itemToGO [oldKey];
		itemToGO.Remove (oldKey);
		itemToGO [newKey] = go;
	}
	// Update is called once per frame
	void Update () {
		if(currentStructure == null){
			Debug.LogError ("Why is it open, when it has no structure?");
			return;
		}
		foreach (Item item in itemToGO.Keys) {
			itemToGO [item].ChangeItemCount(item);
		}
		progress.value = currentStructure.produceCountdown;
		efficiency.text = currentStructure.Efficiency + "%";
	
	}
}
