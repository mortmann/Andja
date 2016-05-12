using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class ProduktionUI : MonoBehaviour {

	public Canvas inputContent;
	public Canvas outputContent;
	public GameObject progressContent;
	public GameObject itemPrefab;
	Dictionary<Item, GameObject> itemToGO;
	ProductionBuilding str;
	Slider progress;
	public void Show(ProductionBuilding str){
		if (this.str == str) {
			return;
		}
		this.str = str;
		progress = progressContent.GetComponentInChildren<Slider> ();
		progress.maxValue = str.produceTime;
		progress.value = 0;
		itemToGO = new Dictionary<Item, GameObject> ();
		if (str.intake != null) {
			for (int i = 0; i < str.intake.Length; i++) {
				GameObject go = GameObject.Instantiate (itemPrefab);
				Slider s = go.GetComponentInChildren<Slider> ();
				s.maxValue = str.maxIntake [i];
				s.value = str.intake [i].count;
				Text t = go.GetComponentInChildren<Text> ();
				t.text = str.intake [i].count + "t";
				go.transform.SetParent (inputContent.transform);
				itemToGO.Add (str.intake [i], go);
			}
		}
		if (str.output != null) {
			for (int i = 0; i < str.output.Length; i++) {
				GameObject go = GameObject.Instantiate (itemPrefab);
				Slider s = go.GetComponentInChildren<Slider> ();
				s.maxValue = str.maxOutputStorage;
				s.value = str.output [i].count;
				Text t = go.GetComponentInChildren<Text> ();
				t.text = str.output [i].count + "t";
				go.transform.SetParent (outputContent.transform);
				itemToGO.Add (str.output [i], go);
			}
		}
	}

	// Update is called once per frame
	void Update () {
		if(str != null){
			foreach (Item item in itemToGO.Keys) {
				GameObject go = itemToGO [item];
				Slider s = go.GetComponentInChildren<Slider> ();
				s.value = item.count;
				Text t = go.GetComponentInChildren<Text> ();
				t.text = item.count + "t";
			}
			progress.value = str.produceTime - str.produceCountdown;
		}
	}
}
