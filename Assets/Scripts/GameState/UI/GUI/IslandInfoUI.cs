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
    public Text CityName;
    public Text PopulationName;
    public ImageText ImageWithText;
    public Image SimpleImage;
    Dictionary<string, Text> itemToText = new Dictionary<string, Text>();
    public int maxItemsPerRow = 5;
    // Use this for initialization
    void Start() {
        cc = CameraController.Instance;
        cg = GetComponent<CanvasGroup>();
        CreateCityInfo();
    }


    Island currentIsland = null;
    // Update is called once per frame
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
            return;
        }
        CityBuildItems.gameObject.SetActive(true);
        Item[] items = c.Inventory.GetBuildMaterial();
        for (int i = 0; i < items.Length; i++) {
            itemToText[items[i].ID].text = items[i].countString;
        }
    }
    private void CreateCityInfo() {
        foreach (Transform t in CityBuildItems)
            Destroy(t.gameObject);
        Item[] items = PrototypController.BuildItems;
        int lastRowNumber = items.Length % maxItemsPerRow;
        for (int i = 0; i < items.Length; i++) {
            if (items[i] == null) {
                continue;
            }
            ImageText imageText = Instantiate(ImageWithText);
            imageText.Set(UIController.GetItemImageForID(items[i].ID), 0 + "t", items[i].Name);
            imageText.transform.SetParent(CityBuildItems, false);
            imageText.text.enabled = true;
            itemToText.Add(items[i].ID, imageText.text);
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
            image.sprite = IconSpriteController.GetIcon(item.ID);
            image.preserveAspect = true;
            image.GetComponent<ShowHoverOver>().Text = item.Name;
            image.transform.SetParent(Fertilites, false);
        }
        foreach (string item in cc.nearestIsland.Resources.Keys) {
            ImageText imageText = Instantiate(ImageWithText);
            imageText.Set(IconSpriteController.GetIcon(item), cc.nearestIsland.Resources[item] + "",
                PrototypController.Instance.GetItemPrototypDataForID(item).Name);
            imageText.transform.SetParent(Resources, false);
        }
    }
}
