using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class ProduktionUI : MonoBehaviour {

	public Canvas inputContent;
	public Canvas outputContent;
	public GameObject progressContent;
	public GameObject itemPrefab;
	Dictionary<Item, GameObject> itemToGO;

	OutputStructure currentStructure;

	Slider progress;
	Text efficiency;
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
			foreach(GameObject go in itemToGO.Values){
				Destroy (go);
			}
		}
		itemToGO = new Dictionary<Item, GameObject> ();
		if (ustr is ProductionBuilding) {
			ProductionBuilding pstr = (ProductionBuilding)ustr;
			if(pstr.MyIntake!=null){
				for (int i = 0; i < pstr.MyIntake.Length; i++) {
					GameObject go = GameObject.Instantiate (itemPrefab);
					go.GetComponent<ItemUI> ().SetItem (pstr.MyIntake [i], pstr.maxIntake [i]);
					go.transform.SetParent (inputContent.transform);
					itemToGO.Add (pstr.MyIntake [i], go);
				}
			}
		}
		if (ustr.output != null) {
			for (int i = 0; i < ustr.output.Length; i++) {
				GameObject go = GameObject.Instantiate (itemPrefab);
				go.GetComponent<ItemUI> ().SetItem (ustr.output [i], ustr.maxOutputStorage );
				go.transform.SetParent (outputContent.transform);
				itemToGO.Add (ustr.output [i], go);
			}
		}
	}
//	public void ShowProduce(OutputStructure ustrs){
//		if(ustrs == null || ustrs == userStr ){
//			return;
//		}
//		this.userStr = ustrs;
//		efficiency = progressContent.GetComponentInChildren<Text> ();
//		progress = progressContent.GetComponentInChildren<Slider> ();
//		progress.maxValue = userStr.produceTime;
//		progress.value = 0;
//		if(itemToGO != null){
//			foreach (GameObject item in itemToGO.Values) {
//				GameObject.Destroy (item);
//			}
//		}
//		itemToGO = new Dictionary<Item, GameObject> ();
//		if (userStr.output != null) {
//			for (int i = 0; i < userStr.output.Length; i++) {
//				GameObject go = GameObject.Instantiate (itemPrefab);
//				Slider s = go.GetComponentInChildren<Slider> ();
//				s.maxValue = userStr.maxOutputStorage;
//				s.value = userStr.output [i].count;
//				Text t = go.GetComponentInChildren<Text> ();
//				t.text = userStr.output [i].count + "t";
//				go.transform.SetParent (outputContent.transform);
//				itemToGO.Add (userStr.output [i], go);
//			}
//		}
//	}

	// Update is called once per frame
	void Update () {
		if(currentStructure == null){
			Debug.LogError ("Why is it open, when it has no structure?");
			return;
		}
		foreach (Item item in itemToGO.Keys) {
			GameObject go = itemToGO [item];
			Slider s = go.GetComponentInChildren<Slider> ();
			s.value = item.count;
			Text t = go.GetComponentInChildren<Text> ();
			t.text = item.count + "t";
		}
		progress.value = currentStructure.produceCountdown;
		efficiency.text = currentStructure.Efficiency + "%";
	
	}
}
