using Andja.Controller;
using Andja.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class IslandInfoUI : MonoBehaviour {
        public static IslandInfoUI Instance;
        private CameraController cc;
        private CanvasGroup cg;
        public Transform CityBuildItems;
        public Transform Fertilites;
        public Transform Resources;
        public Transform CityInfo;
        public Text CityName;
        public ImageText ImageWithText;
        public Image SimpleImage;
        private Dictionary<string, ImageText> itemToText = new Dictionary<string, ImageText>();
        private Dictionary<int, GameObject> populationLevelToGO = new Dictionary<int, GameObject>();
        public int maxItemsPerRow = 5;
        private IIsland currentIsland = null;
        private City CurrentCity;

        private void Start() {
            Instance = this;
            cc = CameraController.Instance;
            cg = GetComponent<CanvasGroup>();
            CreateCityInfo();
            BuildController.Instance.RegisterBuildStateChange(OnBuildStateChange);
        }

        private void OnBuildStateChange(BuildStateModes state) {
            if (CurrentCity == null)
                return;
            switch (state) {
                case BuildStateModes.None:
                    foreach (Item item in PrototypController.Instance.BuildItems) {
                        itemToText[item.ID].RemoveAddon();
                    }
                    break;
                case BuildStateModes.Build:
                    break;
                case BuildStateModes.Destroy:
                    break;
            }
        }
        public void ResetAddons() {
            foreach (Item item in PrototypController.Instance.BuildItems) {
                itemToText[item.ID].RemoveAddon();
            }
        }
        private void Update() {
            if (cc.nearestIsland == null) {
                cg.alpha = 0;
                return;
            }
            cg.alpha = 1;
            if (currentIsland != cc.nearestIsland)
                CreateIslandInfo();
            currentIsland = cc.nearestIsland;
            CurrentCity = cc.nearestIsland.Cities.Find(x => x.PlayerNumber == PlayerController.currentPlayerNumber);
            if (CurrentCity == null) {
                CityBuildItems.gameObject.SetActive(false);
                CityInfo.gameObject.SetActive(false);
                return;
            }
            CityBuildItems.gameObject.SetActive(true);
            CityInfo.gameObject.SetActive(true);
            Item[] items = CurrentCity.Inventory.GetBuildMaterial();
            for (int i = 0; i < items.Length; i++) {
                itemToText[items[i].ID].SetText(items[i].CountString);
            }
            if(MouseController.Instance.NeededItemsToBuild != null) {
                foreach (Item item in MouseController.Instance.NeededItemsToBuild) {
                    TextColor t = CurrentCity.HasEnoughOfItem(item)? TextColor.Positive : TextColor.Negative;
                    itemToText[item.ID].ShowAddon(item.CountString, t);
                }
            } 
            for (int i = 0; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
                PopulationLevel pl = CurrentCity.GetPopulationLevel(i);
                if (pl.populationCount > 0) {
                    itemToText[pl.Level + ""].SetText("" + pl.populationCount);
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
            Item[] items = PrototypController.Instance.BuildItems;
            int lastRowNumber = items.Length % maxItemsPerRow;
            for (int i = 0; i < items.Length; i++) {
                if (items[i] == null) {
                    continue;
                }
                ImageText imageText = Instantiate(ImageWithText);
                imageText.Set(UISpriteController.GetItemImageForID(items[i].ID), items[i].Data, 0 + "t");
                imageText.transform.SetParent(CityBuildItems, false);
                imageText.text.enabled = true;
                itemToText.Add(items[i].ID, imageText);
            }
            foreach (PopulationLevelPrototypData pl in PrototypController.Instance.PopulationLevelDatas.Values) {
                ImageText imageText = Instantiate(ImageWithText);
                imageText.GetComponent<LayoutElement>().minWidth = 110;
                imageText.Set(UISpriteController.GetIcon(pl.iconSpriteName), pl, 0 + "");
                itemToText.Add(pl.LEVEL + "", imageText);
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
                image.GetComponent<ShowToolTip>().SetVariable(item.Data, true);
                image.transform.SetParent(Fertilites, false);
            }
            foreach (string item in cc.nearestIsland.Resources.Keys) {
                ImageText imageText = Instantiate(ImageWithText);
                imageText.Set(UISpriteController.GetIcon(item), PrototypController.Instance.GetItemPrototypDataForID(item)
                    , cc.nearestIsland.Resources[item] + "");
                imageText.transform.SetParent(Resources, false);
            }
        }
        private void OnDestroy() {
            Instance = null;
        }
    }
}