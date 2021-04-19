using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class BuildMenuUIController : MonoBehaviour {

    public GameObject buttonBuildStructuresContent;
    public GameObject buttonPopulationsLevelContent;

    public Button buildButtonPrefab;
    public GameObject populationButtonPrefab;
    public Foldable GroupPrefab;

    public Dictionary<string, Button> nameToGOMap;
    public Dictionary<int, ButtonSetter> popLevelToGO;
    List<Button>[] buttonsPerLevel; 
    Button oldButton;
    static int selectedPopulationLevel = 0;
    Player Player => PlayerController.CurrentPlayer;
    internal static BuildMenuUIController Instance;
    private Dictionary<string, Foldable> groupGameObjects;

    // Use this for initialization
    private void Awake() {
        if(Instance != null) {
            Debug.LogError("There should never be two BuildMenuUIController.");
        }
        Instance = this;
        Setup();
        PlayerController.Instance.cbPlayerChange += PlayerSetup;
        PlayerSetup(null, Player);
        BuildController.Instance.RegisterBuildStateChange(OnBuildModeChange);
    }
    private void Setup() {
        foreach (Transform child in buttonPopulationsLevelContent.transform) {
            Destroy(child.gameObject);
        }
        popLevelToGO = new Dictionary<int, ButtonSetter>();

        foreach (PopulationLevelPrototypData pl in PrototypController.Instance.PopulationLevelDatas.Values) {
            GameObject go = Instantiate(populationButtonPrefab);
            go.transform.SetParent(buttonPopulationsLevelContent.transform, false);
            ButtonSetter bs = go.GetComponent<ButtonSetter>();
            bs.Set(pl.Name, () => { OnPopulationLevelButtonClick(pl.LEVEL); }, UISpriteController.GetIcon(pl.iconSpriteName), pl.Name);
            popLevelToGO.Add(pl.LEVEL, bs);
            bs.Interactable(Player.MaxPopulationLevel > pl.LEVEL);
        }
        foreach (Transform child in buttonBuildStructuresContent.transform) {
            Destroy(child.gameObject);
        }
        buttonsPerLevel = new List<Button>[PrototypController.Instance.NumberOfPopulationLevels];
        for (int i = 0; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
            buttonsPerLevel[i] = new List<Button>();
        }

        nameToGOMap = new Dictionary<string, Button>();
        groupGameObjects = new Dictionary<string, Foldable>();

        foreach (Structure s in BuildController.Instance.StructurePrototypes.Values) {
            if (s.CanBeBuild == false) {
                continue;
            }
            Button b = Instantiate(buildButtonPrefab);
            b.name = s.ID;
            string type = s.GetType().Name;
            if (groupGameObjects.ContainsKey(type) == false) {
                groupGameObjects[type] = Instantiate(GroupPrefab);
                groupGameObjects[type].transform.SetParent(buttonBuildStructuresContent.transform, false);
                groupGameObjects[type].Set(type);
            }
            groupGameObjects[type].Add(b.gameObject);
            b.GetComponent<Button>().onClick.AddListener(() => { OnClick(b.name); });
            b.GetComponent<Image>().color = Color.white;
            b.GetComponent<StructureBuildUI>().Show(s);
            nameToGOMap[b.name] = b;
            buttonsPerLevel[s.PopulationLevel].Add(b);
            if (s.PopulationLevel != selectedPopulationLevel) {
                b.gameObject.SetActive(false);
            }
            b.interactable = BuildController.Instance.allStructuresEnabled || Player.HasStructureUnlocked(s.ID);
        }

        //check em if they are active
        foreach (Foldable f in groupGameObjects.Values)
            f.Check();
        Canvas.ForceUpdateCanvases();
    }

    private void PlayerSetup(Player old, Player current) {
        old?.UnregisterStructuresUnlock(OnStructuresUnlock);
        OnMaxPopLevelChange(Player.MaxPopulationLevel);
        Player.RegisterStructuresUnlock(OnStructuresUnlock);
        foreach(string id in nameToGOMap.Keys) {
            nameToGOMap[id].interactable = BuildController.Instance.allStructuresEnabled || Player.HasStructureUnlocked(id);
        }
    }

    public void OnBuildModeChange(BuildStateModes mode) {
        if (mode != BuildStateModes.Build) {
            if (oldButton != null)
                oldButton.SetNormalColor(Color.white);
        }
    }
    public void OnMaxPopLevelChange(int setlevel) {
        foreach (int level in popLevelToGO.Keys) {
            ButtonSetter g = popLevelToGO[level];
            if (level > setlevel && BuildController.Instance.allStructuresEnabled == false) {
                g.Interactable(false);
            }
            else {
                g.Interactable(true);
            }
        }
    }
    public void OnStructuresUnlock(IEnumerable<Structure> structures) {
        OnMaxPopLevelChange(Player.MaxPopulationLevel);
        foreach (Structure structure in structures) {
            nameToGOMap[structure.ID].interactable = true;
            nameToGOMap[structure.ID].SetNormalColor(new Color32(0, 220, 0, 255));
        }
    }
    public void Update() {
        if (Input.GetMouseButtonDown(1) && oldButton != null) {
            oldButton.SetNormalColor(Color.white);
            oldButton = null;
        }
    }
    public void OnClick(string name) {
        if (nameToGOMap.ContainsKey(name) == false) {
            Debug.LogError("nameToButtonMap doesnt contain the pressed button");
            return;
        }
        if (oldButton != null) {
            oldButton.SetNormalColor(Color.white);
        }
        oldButton = nameToGOMap[name];
        nameToGOMap[name].SetNormalColor(Color.red);
        BuildController.Instance.StartStructureBuild(name);
    }

    public void OnPopulationLevelButtonClick(int i) {
        foreach (Button item in buttonsPerLevel[selectedPopulationLevel]) {
            ChangeButton(item, false);
        }
        foreach (Button name in buttonsPerLevel[i]) {
            ChangeButton(name,true);
        }
        selectedPopulationLevel = i;
    }
    void ChangeButton(Button item, bool change) {
        item.gameObject.SetActive(change);
        item.GetComponentInParent<Foldable>().Check();
        item.interactable = BuildController.Instance.allStructuresEnabled || Player.HasStructureUnlocked(item.name);
    }
    public void OnDisable() {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false)
            return;
#endif
        if (oldButton != null)
            oldButton.SetNormalColor(Color.white);
        BuildController.Instance.UnregisterBuildStateChange(OnBuildModeChange);
    }
}
