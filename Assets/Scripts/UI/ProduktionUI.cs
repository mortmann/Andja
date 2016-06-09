using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class ProduktionUI : MonoBehaviour {

	public Canvas inputContent;
	public Canvas outputContent;
	public GameObject progressContent;
	public GameObject itemPrefab;
	Dictionary<Item, GameObject> itemToGO;
	ProductionBuilding pbstr;
	UserStructure userStr;
	Slider progress;
	Text efficiency;
	public void Show(UserStructure ustr){
		if (this.pbstr == ustr || ustr is ProductionBuilding == false) {
			return;
		}
		this.pbstr = (ProductionBuilding)ustr;
		efficiency = progressContent.GetComponentInChildren<Text> ();
		progress = progressContent.GetComponentInChildren<Slider> ();
		progress.maxValue = pbstr.produceTime;
		progress.value = 0;
		itemToGO = new Dictionary<Item, GameObject> ();
		if (pbstr.intake != null) {
			for (int i = 0; i < pbstr.intake.Length; i++) {
				GameObject go = GameObject.Instantiate (itemPrefab);
				Slider s = go.GetComponentInChildren<Slider> ();
				s.maxValue = pbstr.maxIntake [i];
				s.value = pbstr.intake [i].count;
				Text t = go.GetComponentInChildren<Text> ();
				t.text = pbstr.intake [i].count + "t";
				go.transform.SetParent (inputContent.transform);
				itemToGO.Add (pbstr.intake [i], go);
			}
		}
		if (pbstr.output != null) {
			for (int i = 0; i < pbstr.output.Length; i++) {
				GameObject go = GameObject.Instantiate (itemPrefab);
				Slider s = go.GetComponentInChildren<Slider> ();
				s.maxValue = pbstr.maxOutputStorage;
				s.value = pbstr.output [i].count;
				Text t = go.GetComponentInChildren<Text> ();
				t.text = pbstr.output [i].count + "t";
				go.transform.SetParent (outputContent.transform);
				itemToGO.Add (pbstr.output [i], go);
			}
		}
	}
	public void ShowProduce(UserStructure ustrs){
		if(ustrs == null || ustrs == userStr ){
			return;
		}
		Debug.Log ("SHOW"); 
		this.userStr = ustrs;
		efficiency = progressContent.GetComponentInChildren<Text> ();
		progress = progressContent.GetComponentInChildren<Slider> ();
		progress.maxValue = userStr.produceTime;
		progress.value = 0;
		if(itemToGO != null){
			foreach (GameObject item in itemToGO.Values) {
				GameObject.Destroy (item);
			}
		}
		itemToGO = new Dictionary<Item, GameObject> ();
		if (userStr.output != null) {
			for (int i = 0; i < userStr.output.Length; i++) {
				GameObject go = GameObject.Instantiate (itemPrefab);
				Slider s = go.GetComponentInChildren<Slider> ();
				s.maxValue = userStr.maxOutputStorage;
				s.value = userStr.output [i].count;
				Text t = go.GetComponentInChildren<Text> ();
				t.text = userStr.output [i].count + "t";
				go.transform.SetParent (outputContent.transform);
				itemToGO.Add (userStr.output [i], go);
			}
		}
	}

	// Update is called once per frame
	void Update () {
		if(pbstr != null || userStr != null){
			foreach (Item item in itemToGO.Keys) {
				GameObject go = itemToGO [item];
				Slider s = go.GetComponentInChildren<Slider> ();
				s.value = item.count;
				Text t = go.GetComponentInChildren<Text> ();
				t.text = item.count + "t";
			}
			if(pbstr != null){
				progress.value = pbstr.produceTime - pbstr.produceCountdown;
				efficiency.text = pbstr.Efficiency + "%";
			} 
			if(userStr != null){
				progress.value = userStr.produceTime - userStr.produceCountdown;
				efficiency.text = userStr.Efficiency + "%";
			}
		}
	}
}
