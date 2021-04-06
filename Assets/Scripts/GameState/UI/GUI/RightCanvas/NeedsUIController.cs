using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class NeedsUIController : MonoBehaviour {
    public GenericStructureUI structureUI;
    public GameObject needPrefab;
    public GameObject buttonPopulationsLevelContent;
    public GameObject contentCanvas;
    public Image citizenCanvas;
    public GameObject upgradeButton;
    public GameObject needGroupCanvas;
    public Text peopleCount;
    public GameObject populationButtonPrefab;
    public Slider taxSlider;
    HomeStructure home;
    public GameObject needGroupPrefab;

    Dictionary<NeedGroup, NeedGroupUI> needGroupToUI;
    public static int CurrentSelectedLevel = 0;
    private Dictionary<int, ButtonSetter> popLevelToGO;
    Player Player => PlayerController.CurrentPlayer;
    public void Setup() {
        foreach (Transform child in buttonPopulationsLevelContent.transform) {
            Destroy(child.gameObject);
        }
        popLevelToGO = new Dictionary<int, ButtonSetter>();
        taxSlider.onValueChanged.AddListener(TaxSliderChange);
        foreach (PopulationLevelPrototypData pl in PrototypController.Instance.PopulationLevelDatas.Values) {
            GameObject go = Instantiate(populationButtonPrefab);
            go.transform.SetParent(buttonPopulationsLevelContent.transform, false);
            ButtonSetter bs = go.GetComponent<ButtonSetter>();
            bs.Set(pl.Name, () => { ChangeNeedLevel(pl.LEVEL); }, UISpriteController.GetIcon(pl.iconSpriteName), pl.Name);
            popLevelToGO.Add(pl.LEVEL, bs);
            bs.Interactable(Player.MaxPopulationLevel >= pl.LEVEL);
        }
        foreach (Transform child in needGroupCanvas.transform) {
            Destroy(child.gameObject);
        }
        taxSlider.maxValue = 150;
        taxSlider.minValue = 50;
        taxSlider.wholeNumbers = true;
        needGroupToUI = new Dictionary<NeedGroup, NeedGroupUI>();
        foreach (NeedGroup needGroup in PrototypController.Instance.NeedGroups.Values) {
            GameObject go = Instantiate(needGroupPrefab); //TODO: make it look good
            NeedGroupUI ngui = go.GetComponent<NeedGroupUI>();
            needGroupToUI[needGroup] = ngui;
            ngui.SetGroup(needGroup);
            go.transform.SetParent(contentCanvas.transform, false);
        }
    }

    private void TaxSliderChange(float value) {
        home.City.SetTaxForPopulationLevel(home.StructureLevel, value/100f);
    }

    public void Show(HomeStructure home) {
        if (this.home == home) {
            return;
        }
        this.home = home;

        bool isPlayerHome = home.PlayerNumber == PlayerController.currentPlayerNumber;
        contentCanvas.SetActive(isPlayerHome);
        buttonPopulationsLevelContent.SetActive(isPlayerHome);
        upgradeButton.SetActive(isPlayerHome);
        needGroupCanvas.SetActive(isPlayerHome);
        peopleCount.gameObject.SetActive(isPlayerHome);

        if (needGroupToUI == null)
            Setup();
        foreach (NeedGroupUI ngui in needGroupToUI.Values) {
            ngui.Show(home);
        }
        float F = (float)Math.Round(home.GetTaxPercantage() * 100f, 2);
        taxSlider.value = F;
        structureUI.Show(home);
        ChangeNeedLevel(0);
        for (int i = 0; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
            popLevelToGO[i].Interactable(home.StructureLevel >= i);
        }

    }
    public void ChangeNeedLevel(int level) {
        CurrentSelectedLevel = level;
        foreach (NeedGroupUI groupUI in needGroupToUI.Values) {
            groupUI.UpdateLevel(level);
            groupUI.gameObject.SetActive(groupUI.IsEmpty == false);
        }
    }

    public void UpgradeHome() {
        home.UpgradeHouse();
        for (int i = 0; i < buttonPopulationsLevelContent.transform.childCount; i++) {
            GameObject g = buttonPopulationsLevelContent.transform.GetChild(i).gameObject;
            if (i > home.StructureLevel) {
                g.GetComponent<Button>().interactable = false;
            }
            else {
                g.GetComponent<Button>().interactable = true;
            }
        }
    }
    // Update is called once per frame
    void Update() {
        if (home == null || home.PlayerNumber != PlayerController.currentPlayerNumber) {
            return;
        }
        peopleCount.text = home.people + "/" + home.MaxLivingSpaces;
        if (home.CanUpgrade) {
            upgradeButton.SetActive(true);
        }
        else {
            upgradeButton.SetActive(false);
        }
        switch (home.currentMood) {
            case HomeStructure.CitizienMoods.Mad:
                citizenCanvas.color = Color.red;
                break;
            case HomeStructure.CitizienMoods.Neutral:
                citizenCanvas.color = Color.white;
                break;
            case HomeStructure.CitizienMoods.Happy:
                citizenCanvas.color = Color.green;
                break;
        }
    }

    public void OnDisable() {
        home = null;
    }
}
