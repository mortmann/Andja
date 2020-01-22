using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildMenuUIController : MonoBehaviour {

    public GameObject buttonBuildStructuresContent;
    public GameObject buttonPopulationsLevelContent;

    public GameObject buildButtonPrefab;
    public GameObject populationButtonPrefab;
    public Foldable GroupPrefab;

    public Dictionary<string, GameObject> nameToGOMap;
    public Dictionary<string, string> nameToIDMap;

    public Dictionary<int, ButtonSetter> popLevelToGO;

    public List<string>[] buttons;
    BuildController buildController;
    GameObject oldButton;
    static int selectedPopulationLevel = 0;
    Player player;
    public bool enableAllStructures = false;
    internal static BuildMenuUIController Instance;
    private Dictionary<string, Foldable> groupGameObjects;

    // Use this for initialization
    void Awake() {
        if (Instance != null) {
            Debug.LogError("There should never be two BuildMenuUIController.");
        }
        Instance = this;

        nameToGOMap = new Dictionary<string, GameObject>();
        nameToIDMap = new Dictionary<string, string>();
        buildController = BuildController.Instance;
        buttons = new List<string>[PrototypController.Instance.NumberOfPopulationLevels];

        foreach(Transform child in buttonPopulationsLevelContent.transform) {
            Destroy(child.gameObject);
        }
        popLevelToGO = new Dictionary<int, ButtonSetter>();
        foreach (PopulationLevelPrototypData pl in PrototypController.Instance.PopulationLevelDatas.Values) {
            GameObject go = Instantiate(populationButtonPrefab);
            go.transform.SetParent(buttonPopulationsLevelContent.transform);
            ButtonSetter bs = go.GetComponent<ButtonSetter>();
            bs.Set(pl.Name, () => { OnPopulationLevelButtonClick(pl.LEVEL); }, IconSpriteController.GetIcon(pl.iconSpriteName),pl.Name);
            popLevelToGO.Add(pl.LEVEL, bs);
        }

        player = PlayerController.Instance.CurrPlayer;

        for (int i = 0; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
            buttons[i] = new List<string>();
        }

        groupGameObjects = new Dictionary<string, Foldable>();

        foreach (Structure s in buildController.StructurePrototypes.Values) {
            if (s.CanBeBuild == false) {
                continue;
            }
            GameObject b = Instantiate(buildButtonPrefab);
            b.name = s.ID;
            string type = s.GetType().Name;
            if (groupGameObjects.ContainsKey(type)==false) {
                groupGameObjects[type] = Instantiate(GroupPrefab);
                groupGameObjects[type].transform.SetParent(buttonBuildStructuresContent.transform);
                groupGameObjects[type].Set(type);
            }
            groupGameObjects[type].Add(b);

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
        //check em if they are active
        foreach (Foldable f in groupGameObjects.Values)
            f.Check();
        //Update Canvas required because groups are not displayed correctly
        Canvas.ForceUpdateCanvases();

        OnMaxPopLevelChange(player.MaxPopulationLevel);
        OnMaxPopLevelCountChange(player.MaxPopulationLevel, player.MaxPopulationCount);
        player.RegisterMaxPopulationCountChange(OnMaxPopLevelCountChange);
        buildController.RegisterBuildStateChange(OnBuildModeChange);

    }
    public void OnBuildModeChange(BuildStateModes mode) {
        if (mode != BuildStateModes.Build) {
            if (oldButton != null)
                oldButton.GetComponent<Image>().color = Color.white;
        }
    }
    public void OnMaxPopLevelChange(int setlevel) {
        foreach (int level in popLevelToGO.Keys) {
            ButtonSetter g = popLevelToGO[level];
            if (level > setlevel && enableAllStructures == false) {
                g.Interactable(false);
            }
            else {
                g.Interactable(true);
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
                ChangeButton(name, true);
            }
            else {
                ChangeButton(name, false);
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
            ChangeButton(item, false);
        }
        foreach (string name in buttons[i]) {
            if (player.MaxPopulationCount >= buildController.StructurePrototypes[nameToIDMap[name]].PopulationCount) {
                ChangeButton(name,true);
            }
        }
        selectedPopulationLevel = i;
    }
    void ChangeButton(string item, bool change) {
        nameToGOMap[item].SetActive(change);
        nameToGOMap[item].GetComponentInParent<Foldable>().Check();
    }
    public void OnDisable() {
        if (oldButton != null)
            oldButton.GetComponent<Image>().color = Color.white;
    }
}
