using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class IslandInfoUI : MonoBehaviour {
    CameraController cc;
    CanvasGroup cg;
    public Transform CityBuildItems;
    public Transform Fertilites;
    public Transform Resources;
    public Transform CityInfo;
    public Text CityName;
    public ImageText ImageWithText;
    public Image SimpleImage;
    Dictionary<string, Text> itemToText = new Dictionary<string, Text>();
    Dictionary<int, GameObject> populationLevelToGO = new Dictionary<int, GameObject>();
    public int maxItemsPerRow = 5;    
    Island currentIsland = null;

    void Start() {
        cc = CameraController.Instance;
        cg = GetComponent<CanvasGroup>();
        CreateCityInfo();
    }

    void Update() {
        if (cc.nearestIsland == null) {
            cg.alpha = 0;
            return;
        }
        cg.alpha = 1;
        if(currentIsland != cc.nearestIsland)
            CreateIslandInfo();
        currentIsland = cc.nearestIsland;
        City c = cc.nearestIsland.Cities.Find(x => x.PlayerNumber == PlayerController.currentPlayerNumber);
        if (c == null) {
            CityBuildItems.gameObject.SetActive(false);
            CityInfo.gameObject.SetActive(false);
            return;
        }
        CityBuildItems.gameObject.SetActive(true);
        CityInfo.gameObject.SetActive(true);
        Item[] items = c.Inventory.GetBuildMaterial();
        for (int i = 0; i < items.Length; i++) {
            itemToText[items[i].ID].text = items[i].countString;
        }
        for (int i = 0; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
            PopulationLevel pl = c.GetPopulationLevel(i);
            if (pl.populationCount > 0) {
                itemToText[pl.Level + ""].text = "" + pl.populationCount;
                populationLevelToGO[pl.Level].SetActive(true);
            }
            else {
                populationLevelToGO[pl.Level].SetActive(false);
            }
        }
    }
    private void CreateCityInfo() {
        foreach (Transform t in CityBuildItems)
            Destroy(t.gameObject);
        foreach (Transform t in CityInfo)
            Destroy(t.gameObject);
        Item[] items = PrototypController.BuildItems;
        int lastRowNumber = items.Length % maxItemsPerRow;
        for (int i = 0; i < items.Length; i++) {
            if (items[i] == null) {
                continue;
            }
            ImageText imageText = Instantiate(ImageWithText);
            imageText.Set(UISpriteController.GetItemImageForID(items[i].ID), items[i].Data, 0 + "t");
            imageText.transform.SetParent(CityBuildItems, false);
            imageText.text.enabled = true;
            itemToText.Add(items[i].ID, imageText.text);
        }
        foreach(PopulationLevelPrototypData pl in PrototypController.Instance.PopulationLevelDatas.Values) {
            ImageText imageText = Instantiate(ImageWithText);
            imageText.GetComponent<LayoutElement>().minWidth = 110;
            imageText.Set(UISpriteController.GetIcon(pl.iconSpriteName), pl, 0+"");
            itemToText.Add(pl.LEVEL+"", imageText.text);
            populationLevelToGO.Add(pl.LEVEL, imageText.gameObject);
            imageText.transform.SetParent(CityInfo, false);
        }
    }

    private void CreateIslandInfo() {
        foreach (Transform t in Fertilites)
            Destroy(t.gameObject);
        foreach (Transform t in Resources)
            Destroy(t.gameObject);
        foreach (Fertility item in cc.nearestIsland.Fertilities) {
            Image image = Instantiate(SimpleImage) as Image;
            image.name = item.ID;
            image.sprite = UISpriteController.GetIcon(item.ID);
            image.preserveAspect = true;
            image.GetComponent<ShowHoverOver>().SetVariable(item.Data, true);
            image.transform.SetParent(Fertilites, false);
        }
        foreach (string item in cc.nearestIsland.Resources.Keys) {
            ImageText imageText = Instantiate(ImageWithText);
            imageText.Set(UISpriteController.GetIcon(item), PrototypController.Instance.GetItemPrototypDataForID(item)
                ,cc.nearestIsland.Resources[item] + "");
            imageText.transform.SetParent(Resources, false);
        }
    }
}
