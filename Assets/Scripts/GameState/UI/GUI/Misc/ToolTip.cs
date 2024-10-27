using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    //TODO: multi level hover script
    public class ToolTip : MonoBehaviour {
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
        private ImageText BuildCostText;
        public Text Description;
        public RectTransform fitForm;
        private Vector2 Position = Vector2.negativeInfinity;
        private bool staticPosition = false;
        private bool truePosition = true;
        private bool instantShow = false;
        private ImageText UpkeepCostText;
        private ImageText LockedText;

        public void Start() {
            rect = GetComponent<RectTransform>();
            if (Loading.IsLoading)
                return;
            stringToImageText = new Dictionary<string, ImageText>();
            
            BuildCostText = Instantiate(ImageTextPrefab);
            BuildCostText.Set(UISpriteController.GetIcon(CommonIcon.Money), StaticLanguageVariables.BuildCost, 0 + "");
            BuildCostText.transform.SetParent(MoneyThings.transform, false);
            BuildCostText.SetBrightColorText();
            UpkeepCostText = Instantiate(ImageTextPrefab);
            UpkeepCostText.Set(UISpriteController.GetIcon(CommonIcon.Upkeep), StaticLanguageVariables.Upkeep, 0 + "");
            UpkeepCostText.SetBrightColorText();
            UpkeepCostText.transform.SetParent(MoneyThings.transform, false);

            LockedText = Instantiate(ImageTextPrefab);
            LockedText.Set(UISpriteController.GetIcon(CommonIcon.People), StaticLanguageVariables.Locked, 0 + "");
            LockedText.SetColorText(Color.red);
            LockedText.transform.SetParent(Locked.transform, false);
        }

        public void Show(string header) {
            Show(header, null);
        }

        public void Show(string header, params string[] descriptions) {
            Header.text = header;
            if (descriptions != null) {
                Description.gameObject.SetActive(true);
                string description = descriptions.Where(s => s != null)
                    .Aggregate("", (current, s) => current + ("\n" + s));
                Description.text = description.Trim();
            }
            else {
                Description.gameObject.SetActive(false);
            }
            show = true;
            Items.SetActive(false);
            MoneyThings.SetActive(false);
            Locked.SetActive(false);
            staticPosition = false;
            truePosition = true;
        }

        public void Show(string header, Vector3 position, bool truePosition, bool instantShow, params string[] descriptions) {
            Show(header, descriptions);
            Position = position;
            staticPosition = true;
            this.truePosition = truePosition;
            this.instantShow = instantShow;
            if (instantShow)
                fitForm.gameObject.SetActive(true);
            fitForm.ForceUpdateRectTransforms();
        }

        public void Unshow() {
            fitForm.gameObject.SetActive(false);
            show = false;
            isDebug = false;
            staticPosition = false;
            this.instantShow = false;
            this.truePosition = true;
        }

        private void Update() {
            switch (show) {
                case false when hovertime == HoverDuration:
                    return;
                case true when EventSystem.current.IsPointerOverGameObject() == false && isDebug == false:
                    Unshow();
                    return;
                case true:
                    hovertime -= Time.deltaTime;
                    break;
                default:
                    hovertime += Time.deltaTime;
                    break;
            }

            hovertime = Mathf.Clamp(hovertime, 0, HoverDuration);
            if (hovertime > 0 && instantShow == false) {
                return;
            }
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
                position.x = Screen.width - (sizeDeltaModified.x);
            }
            if (sizeDeltaModified.y + position.y > Screen.height) {
                position.y = Screen.height - (sizeDeltaModified.y);
            }
            if (position.x < 0) {
                position.x = 0;
            }
            if (position.y < 0) {
                position.y = 0;
            }
            fitForm.transform.position = position + offset;
            lifetime -= Time.deltaTime;
            fitForm.gameObject.SetActive(true);
        }

        internal void Show(Structure structure, bool unlocked) {
            Show(structure.Name, structure.Description);
            if (unlocked) {
                this.Locked.SetActive(false);
            }
            else {
                LockedText.SetText(structure.PopulationCount + "");
                this.Locked.SetActive(true);
            }
            if (structure.BuildingItems != null) {
                SetItemTexts(structure.BuildingItems);
            }
            else {
                Items.SetActive(false);
            }
            SetMoneyTexts(structure.UpkeepCost, structure.BuildCost);
        }

        private void SetItemTexts(Item[] items) {
            Items.SetActive(true);
            foreach(ImageText text in stringToImageText.Values) {
                text.gameObject.SetActive(false);
            }
            foreach (Item item in items) {
                if (stringToImageText.ContainsKey(item.ID)) {
                    stringToImageText[item.ID].SetText(item.CountString, () => {
                        return CameraController.Instance.nearestIsland.GetCurrentPlayerCity().HasEnoughOfItem(item) == false;
                    });
                    stringToImageText[item.ID].gameObject.SetActive(true);
                }
                else {
                    ImageText imageText = Instantiate(ImageTextPrefab);
                    imageText.Set(UISpriteController.GetItemImageForID(item.ID), item.Data, item.CountString, () => {
                        return CameraController.Instance.nearestIsland.GetCurrentPlayerCity().HasEnoughOfItem(item) == false;
                    });
                    imageText.transform.SetParent(Items.transform, false);
                    imageText.SetBrightColorText();
                    stringToImageText.Add(item.ID, imageText);
                }
            }
        }

        internal void Show(Unit unit, bool unlocked) {
            Show(unit.Name, unit.Description);
            if (unlocked) {
                this.Locked.SetActive(false);
            }
            else {
                LockedText.SetText(unit.PopulationCount + "");
                this.Locked.SetActive(true);
            }
            if (unit.BuildingItems != null) {
                SetItemTexts(unit.BuildingItems);
            }
            else {
                Items.SetActive(false);
            }
            SetMoneyTexts(unit.UpkeepCost, unit.BuildCost);
        }

        private void SetMoneyTexts(int UpkeepCost, int BuildCost) {
            UpkeepCostText.SetText(UpkeepCost + "");
            BuildCostText.SetText(BuildCost + "", () => { 
                return PlayerController.CurrentPlayer.HasEnoughMoney(BuildCost) == false; 
            });
            MoneyThings.SetActive(true);
        }

        internal void DebugTileInfo(Tile tile) {
            isDebug = true;
            hovertime = 0;
            //show tile info and when structure not null that as well
            Show(tile.ToBaseString(), tile.Structure?.ToString(), 
                "MovementCost " + (tile.Island?.Grid.GetNode(tile).MovementCost ?? 1), 
                AIController.Instance?.GetTileValue(tile));
        }
    }
}