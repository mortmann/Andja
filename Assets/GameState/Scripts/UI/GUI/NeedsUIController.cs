using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class NeedsUIController : MonoBehaviour {

	public GameObject needPrefab;
	public GameObject buttonPopulationsLevelContent;
	public GameObject contentCanvas;
	public GameObject citizenCanvas;
	public GameObject upgradeButton;
	public Dictionary<Need,GameObject> needToGO;
	public List<Need>[] needs;
	int actualLevel;
	HomeBuilding home ;
	public void Show (HomeBuilding home) {
		if(this.home == home){
			return;
		}
		this.home = home;

		var children = new List<GameObject>();
		foreach (Transform child in contentCanvas.transform) {
			children.Add (child.gameObject);
		}
		children.ForEach(child => Destroy(child));

		needToGO = new Dictionary<Need, GameObject> ();
		List<Need> ns = new List<Need> ();
		ns.AddRange (home.City.structureNeeds);
		ns.AddRange (home.City.itemNeeds);

		citizenCanvas.GetComponentInChildren<Text> ().text=home.people+"/"+home.maxLivingSpaces;
		needs = new List<Need>[5];
		for (int i = 0; i < needs.Length; i++) {
			needs [i] = new List<Need> ();
		}
		for (int i = 0; i < ns.Count; i++) {
			GameObject b = Instantiate(needPrefab);
			//Setting the name
			//--The first text IS the name
			//--there must be a better way?
			b.name = ns [i].name;
			string name = ns [i].name + " | ";
			if (ns [i].item != null) {
				name += ns [i].item.name;
			} else {
				if(ns [i].structure==null){
					Destroy (b);
//					Debug.LogWarning(ns[i].ID + " " + ns [i].name +" is missing its structure! Either non declared or structure not existing!");
					continue;
				}
				name += ns [i].structure.spriteName;
			}
			b.GetComponentInChildren<Text> ().text = name;
			//Slider settings
			Slider s = b.GetComponentInChildren<Slider> ();
			Text t = s.transform.parent.GetComponentInChildren<Text> ();

			if (ns [i].item != null) {
				t.text = home.City.getPercentage (ns [i]) * 100 + "%";
				s.value = home.City.getPercentage (ns [i])* 100;
			} else {
				if(home.isInRangeOf (ns[i].structure)) {
					t.text = "In Range";
					s.value = 100;
				} else {
					t.text = "Not in Range";
					s.value = 0;
				}
			}
			needs [ns [i].startLevel].Add (ns [i]);
			b.transform.SetParent(contentCanvas.transform);
			needToGO.Add (ns[i],b);
		}
		ChangeNeedLevel (0);

		for (int i = 0; i < buttonPopulationsLevelContent.transform.childCount; i++) {
			GameObject g = buttonPopulationsLevelContent.transform.GetChild (i).gameObject;
			if (i > home.buildingLevel) {
				g.GetComponent<Button>().interactable = false; 
			} else {
				g.GetComponent<Button>().interactable = true;
			}
		}
		PlayerController.Instance.currPlayer.RegisterMaxPopulationCountChange (OnPopulationCountChange);
	}

	public void OnPopulationCountChange(int level,int count){
		if(home.buildingLevel<level){
			return;
		}
		if(actualLevel<level){
			return;
		}
		foreach(Need n in needs[level]){
			if(n.popCount<=count){
				needToGO [n].SetActive (true);
			}
		}
	}

	public void ChangeNeedLevel(int level){
		for (int i = 0; i < 5; i++) {
			if (i == level) {
				continue;
			}
			for (int s = 0; s < needs[i].Count; s++) {
				needToGO [needs [i][s]].SetActive (false);
			}
		}
		for (int i = 0; i < needs[level].Count; i++) {
			needToGO [needs [level][i]].SetActive (true);
		}
		actualLevel = level;
	}
	public void UpgradeHome(){
		home.UpgradeHouse ();
	}
	// Update is called once per frame
	void Update () {
		if(home==null){
			return;
		}
		if(home.BuildingCanBeUpgraded()){
			upgradeButton.SetActive (true);
		} else {
			upgradeButton.SetActive (false);
		}
		citizenCanvas.GetComponentInChildren<Text> ().text=home.people+"/"+home.maxLivingSpaces;

		foreach (Need item in needs [actualLevel]) {
			Slider s= needToGO [item].GetComponentInChildren<Slider> ();
			Text t  = s.transform.parent.GetComponentInChildren<Text> ();
			if (item.IsItemNeed()) {
				t.text = item.percantageAvailability * 100 + "%";
				s.value = item.percantageAvailability * 100;
			} else {
				if(home.isStructureNeedFullfilled(item)){
					t.text = "In Range";
					s.value = 100;
				}else {
					t.text = "Not in Range";
					s.value = 0;
				}
			}
		}
	}
}
