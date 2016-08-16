using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildMenuUIController : MonoBehaviour {
	public GameObject buttonBuildingContent;
	public GameObject buttonPopulationsLevelContent;

	public GameObject buttonPrefab;
	public Dictionary<string,GameObject> nameToGOMap; 
	public Dictionary<string,int> nameToIDMap;
	public List<string>[] buttons;
	BuildController bc;
	GameObject oldButton;
	int oldSelectedCivLevel = 0;
	Player player;
    // Use this for initialization
    void Start () {
		nameToGOMap = new Dictionary<string, GameObject> ();
		nameToIDMap = new Dictionary<string, int> ();
		bc = GameObject.FindObjectOfType<BuildController> ();
		buttons= new List<string>[4];

		player = GameObject.FindObjectOfType<PlayerController> ().currPlayer;

		for (int i = 0; i < 4; i++) {
			buttons [i] = new List<string> ();
		}
		foreach (Structure s in bc.structurePrototypes.Values) {
			GameObject b = Instantiate(buttonPrefab);
			b.name = s.name;
			b.GetComponentInChildren<Text>().text = s.name;
			b.transform.SetParent(buttonBuildingContent.transform);


			b.GetComponent<Button> ().onClick.AddListener(() => {OnClick(b.name);});
			b.GetComponent<Image> ().color = Color.white;
			b.GetComponent<BuildingBuildUI> ().Show(s);
			nameToGOMap [b.name] = b.gameObject;
			nameToIDMap[b.name] = s.ID;
			buttons [s.PopulationLevel].Add (b.name);
			if(s.PopulationLevel != 0){
				b.SetActive(false);
			}
		}
		for (int i = 0; i < buttonPopulationsLevelContent.transform.childCount; i++) {
			GameObject g = buttonPopulationsLevelContent.transform.GetChild (i).gameObject;
			if (i > player.maxPopulationLevel) {
				g.GetComponent<Button>().interactable = false; 
			} else {
				g.GetComponent<Button>().interactable = true;
			}
		}
    }
	public void Update(){
		for (int i = 0; i < buttonPopulationsLevelContent.transform.childCount; i++) {
			GameObject g = buttonPopulationsLevelContent.transform.GetChild (i).gameObject;
			if (i > player.maxPopulationLevel) {
				g.GetComponent<Button>().interactable = false; 
			} else {
				g.GetComponent<Button>().interactable = true; 
			}
		}
		foreach (string name in buttons[oldSelectedCivLevel]) {
			if (player.maxPopulationCount >= bc.structurePrototypes [nameToIDMap [name]].PopulationCount) {
				nameToGOMap [name].SetActive (true);
			}
		}
	}
	public void OnClick(string name){
		if(nameToGOMap.ContainsKey (name) == false){
			Debug.LogError ("nameToButtonMap doesnt contain the pressed button");
			return;
		}
		if(oldButton!=null){
			oldButton.GetComponent<Image> ().color = Color.white;
		}
		oldButton = nameToGOMap [name];
		nameToGOMap [name].GetComponent<Image> ().color = Color.red;
		bc.OnClick (nameToIDMap[name]);
	}

	public void OnCivilisationLevelClick(int i){
		foreach (string item in buttons[oldSelectedCivLevel]) {
			nameToGOMap[item].SetActive (false);
		}
		foreach (string name in buttons[i]) {
			if (player.maxPopulationCount >= bc.structurePrototypes [nameToIDMap [name]].PopulationCount) {
				nameToGOMap [name].SetActive (true);
			}
		}
		oldSelectedCivLevel = i;
	}
	public void OnDisable() {
		if(oldButton != null)
		oldButton.GetComponent<Image> ().color = Color.white;
	}
}
