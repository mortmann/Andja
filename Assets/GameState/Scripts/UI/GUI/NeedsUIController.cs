using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class NeedsUIController : MonoBehaviour {

	public GameObject needPrefab;
	public GameObject buttonPopulationsLevelContent;
	public GameObject contentCanvas;
	public GameObject citizenCanvas;
	public GameObject upgradeButton;
	public Dictionary<Need,NeedUI> needToUI;
	public List<Need>[] needs;
	HomeBuilding home;

	public void Show (HomeBuilding home) {
		if(this.home == home){
			return;
		}
		foreach (Transform child in contentCanvas.transform) {
			Destroy (child.gameObject);
		}

		this.home = home;

		needToUI = new Dictionary<Need, NeedUI> ();
		List<Need> ns = new List<Need> ();
		ns.AddRange (home.City.itemNeeds);
		Player p = PlayerController.Instance.currPlayer;

		citizenCanvas.GetComponentInChildren<Text> ().text=home.people+"/"+home.maxLivingSpaces;
		needs = new List<Need>[City.citizienLevels];
		for (int i = 0; i < City.citizienLevels; i++) {
			needs [i] = new List<Need> ();
			ns.AddRange (p.lockedNeeds [i]);
			ns.AddRange (p.unlockedStructureNeeds[i]);
		}

		for (int i = 0; i < ns.Count; i++) {
			GameObject b = Instantiate (needPrefab);
			b.transform.SetParent (contentCanvas.transform);
			NeedUI ui = b.GetComponent<NeedUI>();
			ui.setNeed (ns [i], home);
			needToUI [ns [i]] = ui;
			needs [ns [i].startLevel].Add (ns [i]);
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
		PlayerController.Instance.currPlayer.RegisterNeedUnlock (OnNeedUnlock);
	}

	public void OnNeedUnlock(Need need){
		//highlight it or so
	}

	public void ChangeNeedLevel(int level){
		for (int i = 0; i < City.citizienLevels; i++) {
			if (i == level) {
				continue;
			}
			for (int s = 0; s < needs[i].Count; s++) {
				needToUI [needs [i][s]].gameObject.SetActive (false);
			}
		}
		for (int i = 0; i < needs[level].Count; i++) {
			needToUI [needs [level][i]].gameObject.SetActive (true);
		}
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
	}
}
