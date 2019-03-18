using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildMenuUIController : MonoBehaviour {
    public GameObject buttonBuildStructuresContent;
    public GameObject buttonPopulationsLevelContent;

    public GameObject buildButtonPrefab;
    public GameObject populationButtonPrefab;

    public Dictionary<string, GameObject> nameToGOMap;
    public Dictionary<string, int> nameToIDMap;

    public Dictionary<int, GameObject> popLevelToGO;

    public List<string>[] buttons;
    BuildController buildController;
    GameObject oldButton;
    static int selectedPopulationLevel = 0;
    Player player;
    public bool enableAllStructures = false;
    // Use this for initialization
    void Start() {
        nameToGOMap = new Dictionary<string, GameObject>();
        nameToIDMap = new Dictionary<string, int>();
        buildController = BuildController.Instance;
        buttons = new List<string>[4];

        foreach(Transform child in buttonPopulationsLevelContent.transform) {
            Destroy(child.gameObject);
        }
        popLevelToGO = new Dictionary<int, GameObject>();
        foreach (PopulationLevelPrototypData pl in PrototypController.Instance.PopulationLevelDatas.Values) {
            GameObject go = Instantiate(populationButtonPrefab);
            go.transform.SetParent(buttonPopulationsLevelContent.transform);
            go.GetComponent<ButtonSetter>().Set(pl.Name, () => { OnPopulationLevelButtonClick(pl.LEVEL); });
            popLevelToGO.Add(pl.LEVEL, go);
        }

        player = PlayerController.Instance.CurrPlayer;

        for (int i = 0; i < 4; i++) {
            buttons[i] = new List<string>();
        }
        foreach (Structure s in buildController.StructurePrototypes.Values) {
            if (s.CanBeBuild == false) {
                continue;
            }
            GameObject b = Instantiate(buildButtonPrefab);
            b.name = s.SpriteName;
            b.GetComponentInChildren<Text>().text = s.SpriteName;
            b.transform.SetParent(buttonBuildStructuresContent.transform);

            b.GetComponent<Button>().onClick.AddListener(() => { OnClick(b.name); });
            b.GetComponent<Image>().color = Color.white;
            b.GetComponent<StructureBuildUI>().Show(s);
            nameToGOMap[b.name] = b.gameObject;
            nameToIDMap[b.name] = s.ID;
            buttons[s.PopulationLevel].Add(b.name);
            if (s.PopulationLevel != selectedPopulationLevel) {
                b.SetActive(false);
            }
        }
        OnMaxPopLevelChange(player.MaxPopulationLevel);
        OnMaxPopLevelCountChange(player.MaxPopulationLevel, player.MaxPopulationCount);
        player.RegisterMaxPopulationCountChange(OnMaxPopLevelCountChange);
        buildController.RegisterBuildStateChange(OnBuildModeChange);
    }
    void OnEnable() {

    }
    public void OnBuildModeChange(BuildStateModes mode) {
        if (mode != BuildStateModes.Build) {
            if (oldButton != null)
                oldButton.GetComponent<Image>().color = Color.white;
        }
    }
    public void OnMaxPopLevelChange(int setlevel) {
        foreach (int level in popLevelToGO.Keys) {
            GameObject g = popLevelToGO[level];
            if (level > setlevel && enableAllStructures == false) {
                g.GetComponent<Button>().interactable = false;
            }
            else {
                g.GetComponent<Button>().interactable = true;
            }
        }
    }
    public void OnMaxPopLevelCountChange(int level, int count) {
        OnMaxPopLevelChange(level);
        if (level != selectedPopulationLevel) {
            return;
        }
        foreach (string name in buttons[level]) {
            if (count >= buildController.StructurePrototypes[nameToIDMap[name]].PopulationCount) {
                nameToGOMap[name].SetActive(true);
            } else {
                nameToGOMap[name].SetActive(false);
            }
        }
    }
    public void Update() {
        if (Input.GetMouseButtonDown(1) && oldButton != null) {
            oldButton.GetComponent<Image>().color = Color.white;
            oldButton = null;
        }
    }
    public void OnClick(string name) {
        if (nameToGOMap.ContainsKey(name) == false) {
            Debug.LogError("nameToButtonMap doesnt contain the pressed button");
            return;
        }
        if (oldButton != null) {
            oldButton.GetComponent<Image>().color = Color.white;
        }
        oldButton = nameToGOMap[name];
        nameToGOMap[name].GetComponent<Image>().color = Color.red;
        buildController.OnClick(nameToIDMap[name]);
    }

    public void OnPopulationLevelButtonClick(int i) {
        foreach (string item in buttons[selectedPopulationLevel]) {
            nameToGOMap[item].SetActive(false);
        }
        foreach (string name in buttons[i]) {
            if (player.MaxPopulationCount >= buildController.StructurePrototypes[nameToIDMap[name]].PopulationCount) {
                nameToGOMap[name].SetActive(true);
            }
        }
        selectedPopulationLevel = i;
    }
    public void OnDisable() {
        if (oldButton != null)
            oldButton.GetComponent<Image>().color = Color.white;
    }
}
