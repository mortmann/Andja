﻿using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    //TODO: multi level hover script
    public class HoverOverScript : MonoBehaviour {
        private float lifetime = 1;
        public float hovertime = HoverDuration;
        public const float HoverDuration = 1.5f;
        private bool isDebug = false;
        public RectTransform rect;
        private bool show;
        public Text Header;
        public GameObject Items;
        public GameObject Locked;
        public GameObject MoneyThings;
        public ImageText ImageTextPrefab;
        private Dictionary<string, ImageText> stringToImageText;
        public Text Description;
        public RectTransform fitForm;
        private Vector2 Position = Vector2.negativeInfinity;
        private bool staticPosition = false;
        private bool truePosition = true;
        private bool instantShow = false;

        private void Start() {
            rect = GetComponent<RectTransform>();
            stringToImageText = new Dictionary<string, ImageText>();
            
            ImageText cost = Instantiate(ImageTextPrefab);
            cost.Set(UISpriteController.GetIcon(CommonIcon.Money), StaticLanguageVariables.BuildCost, 0 + "");
            cost.transform.SetParent(MoneyThings.transform, false);
            cost.SetBrightColorText();
            stringToImageText.Add(StaticLanguageVariables.BuildCost.ToString(), cost);
            ImageText upkeep = Instantiate(ImageTextPrefab);
            upkeep.Set(UISpriteController.GetIcon(CommonIcon.Upkeep), StaticLanguageVariables.Upkeep, 0 + "");
            upkeep.SetBrightColorText();
            upkeep.transform.SetParent(MoneyThings.transform, false);
            stringToImageText.Add(StaticLanguageVariables.Upkeep.ToString(), upkeep);

            ImageText locked = Instantiate(ImageTextPrefab);
            locked.Set(UISpriteController.GetIcon(CommonIcon.People), StaticLanguageVariables.Locked, 0 + "");
            locked.SetColorText(Color.red);
            locked.transform.SetParent(Locked.transform, false);
            stringToImageText.Add(StaticLanguageVariables.Locked.ToString(), locked);
        }

        public void Show(string header) {
            Show(header, null);
        }

        public void Show(string header, params string[] descriptions) {
            transform.GetChild(0).gameObject.SetActive(true);
            Header.text = header;
            if (descriptions != null) {
                Description.gameObject.SetActive(true);
                string description = "";
                foreach (string s in descriptions) {
                    if (s == null)
                        continue;
                    description += "\n" + s;
                }
                Description.text = description.Trim();
            }
            else {
                Description.gameObject.SetActive(false);
            }
            transform.GetChild(0).gameObject.SetActive(isDebug);
            show = true;
            Items.SetActive(false);
            MoneyThings.SetActive(false);
            Locked.SetActive(false);
        }

        public void Show(string header, Vector3 position, bool truePosition, bool instantShow, params string[] descriptions) {
            Show(header, descriptions);
            Position = position;
            staticPosition = true;
            this.truePosition = truePosition;
            this.instantShow = instantShow;
            if (instantShow)
                transform.GetChild(0).gameObject.SetActive(true);
            fitForm.ForceUpdateRectTransforms();
        }

        public void Unshow() {
            transform.GetChild(0).gameObject.SetActive(false);
            show = false;
            isDebug = false;
            staticPosition = false;
            this.instantShow = false;
            this.truePosition = true;
        }

        private void Update() {
            if (show == false && hovertime == HoverDuration)
                return;
            if (show) {
                hovertime -= Time.deltaTime;
            }
            else {
                hovertime += Time.deltaTime;
            }
            if (EventSystem.current.IsPointerOverGameObject() == false && isDebug == false) {
                Unshow();
                hovertime = HoverDuration;
            }
            hovertime = Mathf.Clamp(hovertime, 0, HoverDuration);
            if (hovertime > 0 && instantShow == false) {
                return;
            }
            transform.GetChild(0).gameObject.SetActive(true);
            Vector3 offset = Vector3.zero;
            if (truePosition == false) {
                offset = -fitForm.sizeDelta / 2;
                offset *= CanvasScale.Vector; //Fix for the scaling
            }
            Vector3 position = Input.mousePosition;
            if (staticPosition)
                position = Position;
            Vector2 sizeDeltaModified = fitForm.sizeDelta * CanvasScale.Vector;//Fix for the scaling
            if (sizeDeltaModified.x + position.x > Screen.width) {
                offset.x = Screen.width - (sizeDeltaModified.x + position.x);
            }
            if (sizeDeltaModified.y + position.y > Screen.height) {
                offset.y = Screen.height - (sizeDeltaModified.y + position.y);
            }
            if (position.x < 0) {
                position.x = 0;
            }
            if (position.y < 0) {
                position.y = 0;
            }
            fitForm.transform.position = position + offset;
            lifetime -= Time.deltaTime;
        }

        internal void Show(Structure structure, bool Locked) {
            Show(structure.Name, structure.Description);
            if (Locked) {
                stringToImageText[StaticLanguageVariables.Locked.ToString()].SetText(structure.PopulationCount + "");
                this.Locked.SetActive(true);
            }
            if (structure.BuildingItems != null) {
                Items.SetActive(true);
                foreach (Item item in structure.BuildingItems) {
                    if (stringToImageText.ContainsKey(item.ID)) {
                        stringToImageText[item.ID].SetText(item.countString);
                    }
                    else {
                        ImageText imageText = Instantiate(ImageTextPrefab);
                        imageText.Set(UISpriteController.GetItemImageForID(item.ID), item.Data, item.countString);
                        imageText.transform.SetParent(Items.transform, false);
                        imageText.SetBrightColorText();
                        stringToImageText.Add(item.ID, imageText);
                    }
                }
            }
            else {
                Items.SetActive(false);
            }
            stringToImageText[StaticLanguageVariables.Upkeep.ToString()].SetText(structure.UpkeepCost + "");
            stringToImageText[StaticLanguageVariables.BuildCost.ToString()].SetText(structure.BuildCost + "");

            MoneyThings.SetActive(true);
        }

        internal void DebugTileInfo(Tile tile) {
            isDebug = true;
            hovertime = 0;
            //show tile info and when structure not null that as well
            Show(tile.ToBaseString(), tile.Structure?.ToString(), AIController.Instance?.GetTileValue(tile));
        }
    }
}