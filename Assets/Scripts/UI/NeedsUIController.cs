using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class NeedsUIController : MonoBehaviour {

	public GameObject needPrefab;
	public GameObject buttonPopulationsLevelContent;
	public GameObject contentCanvas;
	public Dictionary<Need,GameObject> needToGO;
	public List<Need>[] needs;
	int actualLevel;
	// Use this for initialization
	void Start () {
		BuildController bs = GameObject.FindObjectOfType<BuildController> ();
		needToGO = new Dictionary<Need, GameObject> ();
		List<Need> ns = bs.allNeeds;
		needs = new List<Need>[5];
		for (int i = 0; i < needs.Length; i++) {
			needs [i] = new List<Need> ();
		}
		for (int i = 0; i < ns.Count; i++) {
			GameObject b = Instantiate(needPrefab);
			b.name = ns [i].name;
			string name = b.name + " - ";
			if (ns [i].item != null) {
				name += ns [i].item.name;
			} else {
				name += ns [i].structure.name;
			}
			b.GetComponentInChildren<Text> ().text = name;
			b.transform.SetParent(contentCanvas.transform);
			needToGO [ns [i]] = b;
			needs [ns [i].startLevel].Add (ns [i]);
		}
		ChangeNeedLevel (1);
		actualLevel = 1;
		for (int i = 0; i < buttonPopulationsLevelContent.transform.childCount; i++) {
			GameObject g = buttonPopulationsLevelContent.transform.GetChild (i).gameObject;
			if (i > actualLevel) {
				g.GetComponent<Button>().interactable = false; 
			} else {
				g.GetComponent<Button>().interactable = true;
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
	// Update is called once per frame
	void Update () {
		
	}
}
