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
	BuildController buildController;
	GameObject oldButton;
	int selectedCivLevel = 0;
	Player player;
	public bool enableAllBuildings = false;
    // Use this for initialization
    void Start () {
		nameToGOMap = new Dictionary<string, GameObject> ();
		nameToIDMap = new Dictionary<string, int> ();
		buildController = BuildController.Instance;
		buttons= new List<string>[4];

		player = PlayerController.Instance.CurrPlayer;

		for (int i = 0; i < 4; i++) {
			buttons [i] = new List<string> ();
		}
		foreach (Structure s in buildController.StructurePrototypes.Values) {
			if(s.CanBeBuild==false){
				continue; 
			}
			GameObject b = Instantiate(buttonPrefab);
			b.name = s.SpriteName;
			b.GetComponentInChildren<Text>().text = s.SpriteName;
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
		OnMaxPopLevelChange (player.MaxPopulationLevel);

		player.RegisterMaxPopulationCountChange (OnMaxPopLevelChange);
		buildController.RegisterBuildStateChange (OnBuildModeChange);
    }
	void OnEnable(){
		
	}
	public void OnBuildModeChange (BuildStateModes mode){
		if(mode!=BuildStateModes.Build){
			if(oldButton!=null)
				oldButton.GetComponent<Image> ().color = Color.white;
		}
	}
	public void OnMaxPopLevelChange(int level){
		for (int i = 0; i < buttonPopulationsLevelContent.transform.childCount; i++) {
			GameObject g = buttonPopulationsLevelContent.transform.GetChild (i).gameObject;
			if (i > level && enableAllBuildings == false) {
				g.GetComponent<Button>().interactable = false; 
			} else {
				g.GetComponent<Button>().interactable = true; 
			}
		}
	}
	public void OnMaxPopLevelChange(int level,int count){
		OnMaxPopLevelChange (level);
		if(level!=selectedCivLevel){
			return;
		}
		foreach (string name in buttons[level]) {
			if (count >= buildController.StructurePrototypes [nameToIDMap [name]].PopulationCount) {
				nameToGOMap [name].SetActive (true);
			}
		}
	}
	public void Update(){
		if (Input.GetMouseButtonDown (1)&&oldButton!=null) {
			oldButton.GetComponent<Image> ().color = Color.white;
			oldButton = null;
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
		buildController.OnClick (nameToIDMap[name]);
	}

	public void OnCivilisationLevelClick(int i){
		foreach (string item in buttons[selectedCivLevel]) {
			nameToGOMap[item].SetActive (false);
		}
		foreach (string name in buttons[i]) {
			if (player.MaxPopulationCount >= buildController.StructurePrototypes [nameToIDMap [name]].PopulationCount) {
				nameToGOMap [name].SetActive (true);
			}
		}
		selectedCivLevel = i;
	}
	public void OnDisable() {
		if(oldButton != null)
		oldButton.GetComponent<Image> ().color = Color.white;
	}
}
