using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class NeedsUIController : MonoBehaviour {

	public GameObject needPrefab;
	public GameObject buttonPopulationsLevelContent;
	public GameObject contentCanvas;
	public GameObject citizenCanvas;
	public GameObject upgradeButton;
    public GameObject needGroupCanvas;

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
        List<NeedGroup> ns = new List<NeedGroup>();
        ns.AddRange(home.NeedGroups);
        Debug.LogError("Not working atm!");
		Player p = PlayerController.Instance.CurrPlayer;

		citizenCanvas.GetComponentInChildren<Text> ().text=home.people+"/"+home.MaxLivingSpaces;
		needs = new List<Need>[PrototypController.NumberOfPopulationLevels];
		
		for (int i = 0; i < ns.Count; i++) {
            GameObject go = Instantiate(needGroupCanvas); //TODO: make it look good
            go.GetComponent<NeedGroupUI>().Show(ns[i]);
            go.transform.SetParent(contentCanvas.transform);
            foreach (Need need in ns[i].Needs) {
                GameObject b = Instantiate(needPrefab);
                b.transform.SetParent(go.transform);
                NeedUI ui = b.GetComponent<NeedUI>();
                ui.SetNeed(need, home);
                needToUI[need] = ui;
                needs[need.StartLevel].Add(need);
            }
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
		PlayerController.Instance.CurrPlayer.RegisterNeedUnlock (OnNeedUnlock);
	}

	public void OnNeedUnlock(Need need){
		//highlight it or so
	}

	public void ChangeNeedLevel(int level){
		for (int i = 0; i < PrototypController.NumberOfPopulationLevels; i++) {
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
